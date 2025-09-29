using System;
using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Celerio
{
    /// <summary>
    /// High-performance HTTP/1.1 request parser.
    /// </summary>
    public static class HttpRequestParser
    {
        private const int ReadBufferSize = 64 * 1024;
        private const int LineInitialBuffer = 256;
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

        /// <summary>
        /// Minimal high-performance buffer writer backed by ArrayPool.
        /// Similar role to ArrayBufferWriter<byte> but lightweight and portable.
        /// Not thread-safe.
        /// </summary>
        private sealed class PooledBufferWriter : IDisposable
        {
            private byte[] _buffer;
            private int _pos;

            public PooledBufferWriter(int initialCapacity = 16 * 1024)
            {
                if (initialCapacity <= 0) initialCapacity = 16 * 1024;
                _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
                _pos = 0;
            }

            public int WrittenCount => _pos;

            public void Write(ReadOnlySpan<byte> span)
            {
                EnsureCapacity(_pos + span.Length);
                span.CopyTo(new Span<byte>(_buffer, _pos, span.Length));
                _pos += span.Length;
            }

            public void Write(byte[] src, int offset, int count)
            {
                if (src == null) throw new ArgumentNullException(nameof(src));
                if ((uint)offset > (uint)src.Length || count < 0 || offset + count > src.Length)
                    throw new ArgumentOutOfRangeException();
                EnsureCapacity(_pos + count);
                Buffer.BlockCopy(src, offset, _buffer, _pos, count);
                _pos += count;
            }

            private void EnsureCapacity(int needed)
            {
                if (needed <= _buffer.Length) return;
                int newSize = _buffer.Length * 2;
                while (newSize < needed) newSize *= 2;
                var newBuf = ArrayPool<byte>.Shared.Rent(newSize);
                Buffer.BlockCopy(_buffer, 0, newBuf, 0, _pos);
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = newBuf;
            }

            /// <summary>
            /// Return a fresh array containing written data (allocates).
            /// </summary>
            public byte[] ToArray()
            {
                var result = new byte[_pos];
                Buffer.BlockCopy(_buffer, 0, result, 0, _pos);
                return result;
            }

            public void Dispose()
            {
                if (_buffer != null)
                {
                    ArrayPool<byte>.Shared.Return(_buffer);
                    _buffer = null!;
                    _pos = 0;
                }
            }
        }

        /// <summary>
        /// Buffered reader optimized with Span.IndexOf and pooled line buffer.
        /// Provides ReadLineStringAsync (returns ASCII string without CRLF) and ReadExact variants.
        /// </summary>
        private sealed class BufferedSpanReader
        {
            private readonly Stream _stream;
            private readonly byte[] _buffer;
            private int _pos;
            private int _len;

            public BufferedSpanReader(Stream stream, int bufferSize = 8192)
            {
                _stream = stream ?? throw new ArgumentNullException(nameof(stream));
                _buffer = new byte[Math.Max(512, bufferSize)];
                _pos = 0;
                _len = 0;
            }

            private async ValueTask<int> FillAsync(CancellationToken cancellation)
            {
                if (_pos < _len) return _len - _pos;
                _len = await _stream.ReadAsync(_buffer, 0, _buffer.Length, cancellation).ConfigureAwait(false);
                _pos = 0;
                return _len;
            }

            /// <summary>
            /// Read a line and return as ASCII string (without CRLF). Uses pooled buffer, minimal copies.
            /// </summary>
            public async Task<string> ReadLineStringAsync(CancellationToken cancellation)
            {
                byte[] lineBuf = ArrayPool<byte>.Shared.Rent(LineInitialBuffer);
                int write = 0;
                try
                {
                    while (true)
                    {
                        int available = await FillAsync(cancellation).ConfigureAwait(false);
                        if (available == 0)
                        {
                            if (write == 0) return null;
                            break;
                        }

                        var span = new ReadOnlySpan<byte>(_buffer, _pos, available);
                        int idx = span.IndexOf((byte)'\n');
                        if (idx == -1)
                        {
                            EnsureCapacity(ref lineBuf, write + available);
                            span.CopyTo(new Span<byte>(lineBuf, write, available));
                            write += available;
                            _pos = _len;
                            continue;
                        }

                        int segLen = idx;
                        int copyLen = segLen;
                        if (segLen > 0 && span[segLen - 1] == (byte)'\r') copyLen = segLen - 1;

                        EnsureCapacity(ref lineBuf, write + copyLen);
                        new ReadOnlySpan<byte>(_buffer, _pos, copyLen).CopyTo(new Span<byte>(lineBuf, write, copyLen));
                        write += copyLen;

                        _pos += idx + 1;
                        break;
                    }

                    return Encoding.ASCII.GetString(lineBuf, 0, write);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(lineBuf);
                }
            }

            /// <summary>
            /// Read exactly count bytes into dest[offset..offset+count) or throw IOException.
            /// </summary>
            public async Task ReadExactAsync(byte[] dest, int offset, int count, CancellationToken cancellation)
            {
                if (dest == null) throw new ArgumentNullException(nameof(dest));
                if (offset < 0 || count < 0 || offset + count > dest.Length) throw new ArgumentOutOfRangeException();

                while (count > 0)
                {
                    int available = _len - _pos;
                    if (available == 0)
                    {
                        _len = await _stream.ReadAsync(_buffer, 0, _buffer.Length, cancellation).ConfigureAwait(false);
                        _pos = 0;
                        if (_len == 0) throw new IOException("Unexpected EOF while reading exact bytes");
                        available = _len;
                    }

                    int take = Math.Min(available, count);
                    new ReadOnlySpan<byte>(_buffer, _pos, take).CopyTo(new Span<byte>(dest, offset, take));
                    _pos += take;
                    offset += take;
                    count -= take;
                }
            }

            /// <summary>
            /// Read exactly dest.Length bytes into provided Memory<byte> or throw IOException.
            /// Memory<byte> is safe across await boundaries.
            /// </summary>
            public async Task ReadExactAsync(Memory<byte> dest, CancellationToken cancellation)
            {
                int offset = 0;
                int count = dest.Length;
                while (count > 0)
                {
                    int available = _len - _pos;
                    if (available == 0)
                    {
                        _len = await _stream.ReadAsync(_buffer, 0, _buffer.Length, cancellation).ConfigureAwait(false);
                        _pos = 0;
                        if (_len == 0) throw new IOException("Unexpected EOF while reading exact bytes");
                        available = _len;
                    }

                    int take = Math.Min(available, count);
                    new ReadOnlySpan<byte>(_buffer, _pos, take).CopyTo(dest.Span.Slice(offset, take));
                    _pos += take;
                    offset += take;
                    count -= take;
                }
            }

            private static void EnsureCapacity(ref byte[] arr, int needed)
            {
                if (arr.Length >= needed) return;
                int newSize = arr.Length * 2;
                while (newSize < needed) newSize *= 2;
                byte[] newArr = ArrayPool<byte>.Shared.Rent(newSize);
                Buffer.BlockCopy(arr, 0, newArr, 0, arr.Length);
                ArrayPool<byte>.Shared.Return(arr);
                arr = newArr;
            }
        }
    }
}