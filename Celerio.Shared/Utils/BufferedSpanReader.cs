using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Celerio.Utils
{
    /// <summary>
    /// Buffered reader optimized with Span.IndexOf and pooled line buffer.
    /// Provides ReadLineStringAsync (returns ASCII string without CRLF) and ReadExact variants.
    /// </summary>
    public sealed class BufferedSpanReader
    {
        private const int LineInitialBuffer = 256;
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
