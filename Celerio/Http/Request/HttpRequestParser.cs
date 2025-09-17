using System.Buffers;
using System.Net.Sockets;
using System.Text;

namespace Celerio;

    public static class HttpRequestParser
    {
        // big read buffer — память не в приоритете, поэтому большой буфер для меньшего числа чтений
        private const int ReadBufferSize = 64 * 1024; // 64KB

        private static readonly byte[] HeaderDelimiter = new byte[] { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };

        public static async Task<Request> ParseAsync(NetworkStream ns, CancellationToken cancellation = default)
        {
            if (ns == null) throw new ArgumentNullException(nameof(ns));
            if (!ns.CanRead) throw new ArgumentException("NetworkStream is not readable", nameof(ns));

            // временный буфер в котором аккумулируем header+первые байты тела
            var ms = new MemoryStream();
            var buffer = ArrayPool<byte>.Shared.Rent(ReadBufferSize);
            int totalRead = 0;
            int headerEndIndex = -1;
            try
            {
                while (true)
                {
                    cancellation.ThrowIfCancellationRequested();
                    int read = await ns.ReadAsync(buffer, 0, ReadBufferSize, cancellation).ConfigureAwait(false);
                    if (read == 0)
                    {
                        // EOF before headers complete
                        throw new IOException("Connection closed while reading headers");
                    }

                    ms.Write(buffer, 0, read);
                    totalRead += read;

                    // search for \r\n\r\n in ms buffer efficiently
                    headerEndIndex = IndexOfSequence(ms.GetBuffer(), 0, totalRead, HeaderDelimiter);
                    if (headerEndIndex >= 0)
                    {
                        // headerEndIndex points to start of delimiter, header bytes are [0 .. headerEndIndex-1]
                        break;
                    }

                    // If headers exceed some very large guard (e.g., 1MB), stop
                    if (totalRead > 4 * 1024 * 1024) // 4MB headers guard
                        throw new FormatException("Headers too large");
                }

                // Split: header region and leftover (start of body)
                int headerRegionLength = headerEndIndex; // bytes before delimiter
                int delimiterLength = HeaderDelimiter.Length;
                int leftoverStart = headerEndIndex + delimiterLength;
                int leftoverCount = totalRead - leftoverStart;

                // copy header bytes to string
                var headerBytes = new byte[headerRegionLength];
                Array.Copy(ms.GetBuffer(), 0, headerBytes, 0, headerRegionLength);

                string headerText = Encoding.ASCII.GetString(headerBytes);

                // parse request line and header lines
                var lines = headerText.Split(new[] { "\r\n" }, StringSplitOptions.None);
                if (lines.Length == 0) throw new FormatException("Empty request");

                // request line: METHOD SP REQUEST-TARGET SP HTTP/VERSION
                var requestLineParts = lines[0].Split(' ');
                if (requestLineParts.Length < 3) throw new FormatException("Malformed request line");
                string method = requestLineParts[0];
                string requestTarget = requestLineParts[1];
                string httpVersion = requestLineParts[2];

                // parse headers
                var headers = new HeaderCollection();
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (string.IsNullOrEmpty(line)) continue;
                    int colon = line.IndexOf(':');
                    if (colon <= 0) continue; // ignore malformed header
                    string name = line.Substring(0, colon).Trim();
                    string value = line.Substring(colon + 1).Trim();
                    headers.Add(name, value);
                }

                // parse path + query and percent-decode
                string pathOnly;
                Dictionary<string, string> queryDict = new Dictionary<string, string>(StringComparer.Ordinal);
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

                // Determine body length and read body if needed
                byte[] body = Array.Empty<byte>();
                if (headers.Get("Content-Length") is string clVal && int.TryParse(clVal, out int contentLength) && contentLength > 0)
                {
                    body = new byte[contentLength];
                    // copy leftover first
                    if (leftoverCount > 0)
                    {
                        Array.Copy(ms.GetBuffer(), leftoverStart, body, 0, Math.Min(leftoverCount, contentLength));
                    }
                    int already = Math.Min(leftoverCount, contentLength);
                    int needed = contentLength - already;
                    int destOffset = already;
                    while (needed > 0)
                    {
                        int r = await ns.ReadAsync(buffer, 0, Math.Min(buffer.Length, needed), cancellation).ConfigureAwait(false);
                        if (r == 0) throw new IOException("Connection closed while reading body");
                        Array.Copy(buffer, 0, body, destOffset, r);
                        destOffset += r;
                        needed -= r;
                    }
                }
                else if (string.Equals(headers.Get("Transfer-Encoding"), "chunked", StringComparison.OrdinalIgnoreCase) ||
                         (headers.TryGetValues("Transfer-Encoding", out var vals) && vals.Count > 0 && vals[0].IndexOf("chunked", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    // parse chunked body
                    using var bodyMs = new MemoryStream();
                    // if leftover contains data, start with it
                    if (leftoverCount > 0)
                    {
                        bodyMs.Write(ms.GetBuffer(), leftoverStart, leftoverCount);
                    }
                    // read chunk by chunk (we need line-based reads)
                    while (true)
                    {
                        // read a line (chunk-size in hex)
                        string sizeLine = await ReadLineAsync(ns, buffer, bodyMs, cancellation).ConfigureAwait(false);
                        if (sizeLine == null) throw new IOException("Unexpected EOF reading chunk size");
                        int sem = sizeLine.IndexOf(';');
                        string sizeStr = sem >= 0 ? sizeLine.Substring(0, sem) : sizeLine;
                        if (!int.TryParse(sizeStr.Trim(), System.Globalization.NumberStyles.HexNumber, null, out int chunkSize))
                            throw new FormatException("Invalid chunk size");
                        if (chunkSize == 0)
                        {
                            // read and ignore trailing headers until blank line
                            while (true)
                            {
                                string trailer = await ReadLineAsync(ns, buffer, bodyMs, cancellation).ConfigureAwait(false);
                                if (string.IsNullOrEmpty(trailer)) break;
                            }
                            break;
                        }
                        // read chunkSize bytes into bodyMs
                        int remaining = chunkSize;
                        // If current bodyMs already contains extra data from leftover, we should not treat it as chunk content —
                        // to keep it simple: after finishing reading header we placed leftover into bodyMs; but chunked body starts AFTER headers delimiter,
                        // so leftover (if any) is actually beginning of chunked stream. We'll read from stream and bodyMs as source of initial bytes.
                        // For simplicity, build temporary buffer to satisfy chunk read.
                        var temp = ArrayPool<byte>.Shared.Rent(chunkSize);
                        int got = 0;
                        // First, if bodyMs currently has buffer tail that we haven't consumed, we need to read from stream instead.
                        // Simpler robust approach: read directly from stream into temp until filled.
                        while (got < chunkSize)
                        {
                            int r = await ns.ReadAsync(temp, got, chunkSize - got, cancellation).ConfigureAwait(false);
                            if (r == 0) throw new IOException("Unexpected EOF while reading chunk data");
                            got += r;
                        }
                        bodyMs.Write(temp, 0, chunkSize);
                        ArrayPool<byte>.Shared.Return(temp);
                        // consume the trailing CRLF after chunk
                        await ConsumeCRLFAsync(ns, buffer, cancellation).ConfigureAwait(false);
                    }
                    body = bodyMs.ToArray();
                }
                else
                {
                    // no body or unsupported transfer-encoding -> empty body
                    body = Array.Empty<byte>();
                }

                return new Request(method, pathOnly, queryDict, headers, body);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                ms.Dispose();
            }
        }

        // helper: find first occurrence of needle in haystack (simple mem search)
        private static int IndexOfSequence(byte[] haystack, int offset, int hayLen, byte[] needle)
        {
            if (needle.Length == 0) return offset;
            int limit = hayLen - needle.Length;
            for (int i = offset; i <= limit; i++)
            {
                bool ok = true;
                for (int j = 0; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j]) { ok = false; break; }
                }
                if (ok) return i;
            }
            return -1;
        }

        // parse query string into dictionary (percent-decode names and values)
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
                // last one wins
                dest[nameDecoded] = valDecoded;
            }
        }

        // Percent-decode bytes (treat incoming bytes as ASCII; percent sequences represent raw bytes that are UTF-8 encoded text)
        private static string PercentDecodeToString(byte[] raw)
        {
            if (raw == null || raw.Length == 0) return "";
            // quick path: if no '%' present, just interpret as URI-safe plain path (but we still need to decode '+' in query? we treat + literally for path)
            bool hasPct = false;
            foreach (var b in raw) if (b == (byte)'%') { hasPct = true; break; }
            if (!hasPct)
            {
                return Encoding.UTF8.GetString(raw); // safe to decode as UTF8; raw came from ASCII bytes of request-target (which bytes represent UTF-8 percent-encoded or ASCII)
            }

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

        // read one CRLF-terminated line from stream (returns line without CRLF)
        // This is a somewhat naive implementation: it reads bytes until '\n' found.
        // For chunk parsing it's fine.
        private static async Task<string> ReadLineAsync(NetworkStream ns, byte[] tempBuffer, MemoryStream initial, CancellationToken cancellation)
        {
            var sb = new StringBuilder();
            // if initial contains data we could consider reading from it first; to keep implementation robust, read from stream directly.
            int pos = 0;
            while (true)
            {
                int r = await ns.ReadAsync(tempBuffer, 0, tempBuffer.Length, cancellation).ConfigureAwait(false);
                if (r == 0) return null;
                for (int i = 0; i < r; i++)
                {
                    byte b = tempBuffer[i];
                    if (b == (byte)'\n')
                    {
                        // check previous char for CR
                        int len = sb.Length;
                        if (len > 0 && sb[len - 1] == '\r') sb.Length = len - 1;
                        // append current span up to i - consumed? We appended byte-by-byte already
                        var remaining = Encoding.ASCII.GetString(tempBuffer, 0, i);
                        // actually we appended to sb char-by-char, so return
                        return sb.ToString();
                    }
                    else
                    {
                        sb.Append((char)b);
                    }
                }
            }
        }

        private static async Task ConsumeCRLFAsync(NetworkStream ns, byte[] buffer, CancellationToken cancellation)
        {
            // read two bytes expecting \r\n
            int need = 2;
            int got = 0;
            while (got < need)
            {
                int r = await ns.ReadAsync(buffer, got, need - got, cancellation).ConfigureAwait(false);
                if (r == 0) throw new IOException("Unexpected EOF while reading CRLF");
                got += r;
            }
            if (buffer[0] != (byte)'\r' || buffer[1] != (byte)'\n')
                throw new FormatException("Invalid chunk delimiter");
        }
    }
