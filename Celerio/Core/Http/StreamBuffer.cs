using System.Net.Sockets;
using System.Text;

namespace Celerio;

public class StreamBuffer
{
    private readonly NetworkStream _stream;
    private byte[] _buffer;
    public int Offset { get; private set; }
    public int Length { get; private set; }

    public byte[] ReadBuffer => _buffer;

    public int Available => Length - Offset;

    public StreamBuffer(NetworkStream stream, byte[] initialBuffer, int initialLength)
    {
        _stream = stream;
        int bufSize = Math.Max(initialLength, 1024);
        _buffer = new byte[bufSize];
        if (initialLength > 0)
        {
            Array.Copy(initialBuffer, 0, _buffer, 0, initialLength);
        }

        Offset = 0;
        Length = initialLength;
    }

    public string? ReadLine()
    {
        while (true)
        {
            for (int i = Offset; i < Length - 1; i++)
            {
                if (_buffer[i] == (byte)'\r' && _buffer[i + 1] == (byte)'\n')
                {
                    int lineLength = i - Offset;
                    string line = Encoding.ASCII.GetString(_buffer, Offset, lineLength);
                    Offset = i + 2;
                    return line;
                }
            }

            if (!EnsureAvailable(1))
            {
                if (Available > 0)
                {
                    string line = Encoding.ASCII.GetString(_buffer, Offset, Available);
                    Offset = Length;
                    return line;
                }

                return null;
            }
        }
    }

    private bool EnsureAvailable(int count)
    {
        if (Available >= count)
            return true;
        if (Offset > 0)
        {
            Buffer.BlockCopy(_buffer, Offset, _buffer, 0, Available);
            Length = Available;
            Offset = 0;
        }

        if (Length + count > _buffer.Length)
        {
            Array.Resize(ref _buffer, (_buffer.Length + count) * 2);
        }

        int read = _stream.Read(_buffer, Length, _buffer.Length - Length);
        if (read <= 0)
            return false;
        Length += read;
        return Available >= count;
    }

    public bool ReadExact(byte[] dest, int destOffset, int count)
    {
        while (count > 0)
        {
            int available = Available;
            if (available > 0)
            {
                int toCopy = Math.Min(available, count);
                Array.Copy(_buffer, Offset, dest, destOffset, toCopy);
                Offset += toCopy;
                destOffset += toCopy;
                count -= toCopy;
            }
            else
            {
                int read = _stream.Read(dest, destOffset, count);
                if (read <= 0)
                    return false;
                destOffset += read;
                count -= read;
            }
        }

        return true;
    }

    public void Skip(int count)
    {
        Offset += count;
        if (Offset > Length)
            Offset = Length;
    }
}