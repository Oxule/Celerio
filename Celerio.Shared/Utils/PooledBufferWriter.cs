using System;
using System.Buffers;

namespace Celerio.Utils
{
    /// <summary>
    /// Minimal high-performance buffer writer backed by ArrayPool.
    /// Similar role to ArrayBufferWriter<byte> but lightweight and portable.
    /// Not thread-safe.
    /// </summary>
    public sealed class PooledBufferWriter : IDisposable
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
}
