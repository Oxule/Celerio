using System;
using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Celerio.Utils;

namespace Celerio
{
    /// <summary>
    /// High-performance HTTP/1.1 request parser.
    /// </summary>
    public static class HttpRequestParser
    {
        private const int ReadBufferSize = 64 * 1024;
        private const int ChunkTempBuffer = 16 * 1024; // read chunk-data in 16KB slices

        public static async Task<Request> ParseAsync(NetworkStream ns, CancellationToken cancellation = default)
        {
            if (ns == null) throw new ArgumentNullException(nameof(ns));
            if (!ns.CanRead) throw new ArgumentException("NetworkStream is not readable", nameof(ns));

            var reader = new BufferedSpanReader(ns, ReadBufferSize);

            string requestLine = await reader.ReadLineStringAsync(cancellation).ConfigureAwait(false)
                                 ?? throw new IOException("Connection closed while reading request line");
            var parts = SplitRequestLine(requestLine);
            if (parts.Length < 3) throw new FormatException("Malformed request line");
            string method = parts[0];
            string requestTarget = parts[1];
            string httpVersion = parts[2];

            // Headers
            var headers = new HeaderCollection();
            while (true)
            {
                string headerLine = await reader.ReadLineStringAsync(cancellation).ConfigureAwait(false)
                                    ?? throw new IOException("Connection closed while reading headers");
                if (headerLine.Length == 0) break; // end of headers

                int colon = headerLine.IndexOf(':');
                if (colon <= 0) continue;
                string name = headerLine.Substring(0, colon).Trim();
                string value = headerLine.Substring(colon + 1).Trim();
                headers.Add(name, value);
            }

            // Path and query
            string pathOnly;
            var queryDict = new Dictionary<string, string>(StringComparer.Ordinal);
            int qIdx = requestTarget.IndexOf('?');
            if (qIdx >= 0)
            {
                pathOnly = PercentDecodeToString(Encoding.ASCII.GetBytes(requestTarget.Substring(0, qIdx)));
                string query = requestTarget.Substring(qIdx + 1);
                ParseQueryString(query, queryDict);
            }
            else
            {
                pathOnly = PercentDecodeToString(Encoding.ASCII.GetBytes(requestTarget));
            }

            byte[] body = Array.Empty<byte>();

            // Content-Length (prefer exact-length path)
            if (headers.Get("Content-Length") is string clVal &&
                long.TryParse(clVal, NumberStyles.None, CultureInfo.InvariantCulture, out long contentLength) &&
                contentLength > 0)
            {
                if (contentLength > int.MaxValue) throw new FormatException("Content-Length too large");
                body = new byte[(int)contentLength];
                await reader.ReadExactAsync(body, 0, (int)contentLength, cancellation).ConfigureAwait(false);
            }
            else if (IsTransferEncodingChunked(headers))
            {
                var writer = new PooledBufferWriter(ChunkTempBuffer);
                byte[] temp = ArrayPool<byte>.Shared.Rent(ChunkTempBuffer);
                try
                {
                    while (true)
                    {
                        string sizeLine = await reader.ReadLineStringAsync(cancellation).ConfigureAwait(false)
                                          ?? throw new IOException("Unexpected EOF reading chunk size");
                        int sem = sizeLine.IndexOf(';');
                        string sizeStr = sem >= 0 ? sizeLine.Substring(0, sem) : sizeLine;
                        if (!long.TryParse(sizeStr.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                                out long chunkSize))
                            throw new FormatException("Invalid chunk size");
                        if (chunkSize < 0) throw new FormatException("Negative chunk size");

                        if (chunkSize == 0)
                        {
                            while (true)
                            {
                                string trailer = await reader.ReadLineStringAsync(cancellation).ConfigureAwait(false)
                                                 ?? throw new IOException("Unexpected EOF reading trailer");
                                if (trailer.Length == 0) break;
                            }

                            break;
                        }

                        long remaining = chunkSize;
                        while (remaining > 0)
                        {
                            int toRead = (int)Math.Min(temp.Length, remaining);
                            await reader.ReadExactAsync(temp, 0, toRead, cancellation).ConfigureAwait(false);
                            writer.Write(temp, 0, toRead);
                            remaining -= toRead;
                        }

                        await reader.ReadExactAsync(new Memory<byte>(temp, 0, 2), cancellation).ConfigureAwait(false);
                        if (temp[0] != (byte)'\r' || temp[1] != (byte)'\n')
                            throw new FormatException("Invalid chunk delimiter");
                    }

                    body = writer.ToArray();
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(temp);
                    writer.Dispose();
                }
            }
            else
            {
                body = Array.Empty<byte>();
            }

            return new Request(method, pathOnly, queryDict, headers, body);
        }

        private static bool IsTransferEncodingChunked(HeaderCollection headers)
        {
            if (string.Equals(headers.Get("Transfer-Encoding"), "chunked", StringComparison.OrdinalIgnoreCase))
                return true;
            if (headers.TryGetValues("Transfer-Encoding", out var vals) && vals.Count > 0 &&
                vals[0].IndexOf("chunked", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return false;
        }

        #region helpers (unchanged logic but compact)

        private static string[] SplitRequestLine(string requestLine)
        {
            return requestLine.Split(' ');
        }

        private static void ParseQueryString(string query, Dictionary<string, string> dest)
        {
            if (string.IsNullOrEmpty(query)) return;
            var parts = query.Split('&');
            foreach (var kv in parts)
            {
                if (kv.Length == 0) continue;
                int eq = kv.IndexOf('=');
                string name = eq >= 0 ? kv.Substring(0, eq) : kv;
                string value = eq >= 0 ? kv.Substring(eq + 1) : "";
                var nameDecoded = PercentDecodeToString(Encoding.ASCII.GetBytes(name));
                var valDecoded = PercentDecodeToString(Encoding.ASCII.GetBytes(value));
                dest[nameDecoded] = valDecoded;
            }
        }

        private static string PercentDecodeToString(byte[] raw)
        {
            if (raw == null || raw.Length == 0) return "";
            bool hasPct = false;
            foreach (var b in raw)
                if (b == (byte)'%')
                {
                    hasPct = true;
                    break;
                }

            if (!hasPct) return Encoding.UTF8.GetString(raw);

            var outBytes = new List<byte>(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                byte c = raw[i];
                if (c == (byte)'%')
                {
                    if (i + 2 >= raw.Length) throw new FormatException("Invalid percent-encoding");
                    int hi = FromHexChar((char)raw[i + 1]);
                    int lo = FromHexChar((char)raw[i + 2]);
                    if (hi < 0 || lo < 0) throw new FormatException("Invalid percent-encoding hex digits");
                    byte val = (byte)((hi << 4) | lo);
                    outBytes.Add(val);
                    i += 2;
                }
                else
                {
                    outBytes.Add(c);
                }
            }

            return Encoding.UTF8.GetString(outBytes.ToArray());
        }

        private static int FromHexChar(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            return -1;
        }

        #endregion
    }
}
