using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Celerio.Utils;
using Xunit;

namespace Celerio.Shared.Tests
{
    public class BufferedSpanReaderTests
    {
        [Fact]
        public void Constructor_NullStream_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new BufferedSpanReader(null));
        }

        [Fact]
        public void Constructor_ValidStream_CreatesReader()
        {
            using var stream = new MemoryStream();
            var reader = new BufferedSpanReader(stream);
            Assert.NotNull(reader);
        }

        [Fact]
        public async Task ReadLineStringAsync_EmptyStream_ReturnsNull()
        {
            using var stream = new MemoryStream();
            var reader = new BufferedSpanReader(stream);
            var result = await reader.ReadLineStringAsync(default);
            Assert.Null(result);
        }

        [Fact]
        public async Task ReadLineStringAsync_SingleLineWithoutCRLF_ReturnsLine()
        {
            var data = Encoding.ASCII.GetBytes("Hello World\n");
            using var stream = new MemoryStream(data);
            var reader = new BufferedSpanReader(stream);
            var result = await reader.ReadLineStringAsync(default);
            Assert.Equal("Hello World", result);
        }

        [Fact]
        public async Task ReadLineStringAsync_SingleLineWithCRLF_ReturnsLineWithoutCR()
        {
            var data = Encoding.ASCII.GetBytes("Hello World\r\n");
            using var stream = new MemoryStream(data);
            var reader = new BufferedSpanReader(stream);
            var result = await reader.ReadLineStringAsync(default);
            Assert.Equal("Hello World", result);
        }

        [Fact]
        public async Task ReadLineStringAsync_SingleLineWithCROnly_ReturnsLineWithCR()
        {
            var data = Encoding.ASCII.GetBytes("Hello World\r");
            using var stream = new MemoryStream(data);
            var reader = new BufferedSpanReader(stream);
            var result = await reader.ReadLineStringAsync(default);
            Assert.Equal("Hello World\r", result); // Returns partial line if no \n
        }

        [Fact]
        public async Task ReadLineStringAsync_MultipleLines_ReadsSequentially()
        {
            var data = Encoding.ASCII.GetBytes("Line1\r\nLine2\r\n");
            using var stream = new MemoryStream(data);
            var reader = new BufferedSpanReader(stream);
            var line1 = await reader.ReadLineStringAsync(default);
            var line2 = await reader.ReadLineStringAsync(default);
            Assert.Equal("Line1", line1);
            Assert.Equal("Line2", line2);
        }

        [Fact]
        public async Task ReadLineStringAsync_LongLine_BuffersCorrectly()
        {
            var longLine = new string('A', 1000) + "\r\n";
            var data = Encoding.ASCII.GetBytes(longLine);
            using var stream = new MemoryStream(data);
            var reader = new BufferedSpanReader(stream);
            var result = await reader.ReadLineStringAsync(default);
            Assert.Equal(new string('A', 1000), result);
        }

        [Fact]
        public async Task ReadLineStringAsync_EmptyLine_ReturnsEmptyString()
        {
            var data = Encoding.ASCII.GetBytes("\r\n");
            using var stream = new MemoryStream(data);
            var reader = new BufferedSpanReader(stream);
            var result = await reader.ReadLineStringAsync(default);
            Assert.Equal("", result);
        }

        [Fact]
        public async Task ReadExactAsync_Array_NullDest_ThrowsArgumentNullException()
        {
            using var stream = new MemoryStream();
            var reader = new BufferedSpanReader(stream);
            await Assert.ThrowsAsync<ArgumentNullException>(() => reader.ReadExactAsync(null, 0, 0, default));
        }

        [Fact]
        public async Task ReadExactAsync_Array_InvalidOffset_ThrowsArgumentOutOfRangeException()
        {
            var data = new byte[10];
            using var stream = new MemoryStream(data);
            var reader = new BufferedSpanReader(stream);
            var dest = new byte[10];
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => reader.ReadExactAsync(dest, -1, 1, default));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => reader.ReadExactAsync(dest, 11, 1, default));
        }

        [Fact]
        public async Task ReadExactAsync_Array_InvalidCount_ThrowsArgumentOutOfRangeException()
        {
            var data = new byte[10];
            using var stream = new MemoryStream(data);
            var reader = new BufferedSpanReader(stream);
            var dest = new byte[10];
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => reader.ReadExactAsync(dest, 0, -1, default));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => reader.ReadExactAsync(dest, 5, 6, default));
        }

        [Fact]
        public async Task ReadExactAsync_Array_ZeroCount_DoesNothing()
        {
            var data = new byte[10];
            using var stream = new MemoryStream(data);
            var reader = new BufferedSpanReader(stream);
            var dest = new byte[10];
            await reader.ReadExactAsync(dest, 0, 0, default);
        }

        [Fact]
        public async Task ReadExactAsync_Array_ValidRead_ReadsData()
        {
            var data = new byte[] { 1, 2, 3, 4, 5 };
            using var stream = new MemoryStream(data);
            var reader = new BufferedSpanReader(stream);
            var dest = new byte[3];
            await reader.ReadExactAsync(dest, 0, 3, default);
            Assert.Equal(new byte[] { 1, 2, 3 }, dest);
        }

        [Fact]
        public async Task ReadExactAsync_Array_EOFBeforeCount_ThrowsIOException()
        {
            var data = new byte[] { 1, 2 };
            using var stream = new MemoryStream(data);
            var reader = new BufferedSpanReader(stream);
            var dest = new byte[5];
            await Assert.ThrowsAsync<IOException>(() => reader.ReadExactAsync(dest, 0, 5, default));
        }

        [Fact]
        public async Task ReadExactAsync_Array_PartialReadsInBuffer_ReadsAcrossBuffers()
        {
            var data = new byte[6000]; // Larger than buffer
            for (int i = 0; i < 6000; i++) data[i] = (byte)(i % 256);
            using var stream = new MemoryStream(data);
            var reader = new BufferedSpanReader(stream, 512); // Buffer at least 512
            var dest = new byte[3000];
            await reader.ReadExactAsync(dest, 0, 3000, default);
            for (int i = 0; i < 3000; i++)
                Assert.Equal((byte)(i % 256), dest[i]);
        }

        [Fact]
        public async Task ReadExactAsync_Memory_EmptyMemory_DoesNothing()
        {
            var data = new byte[10];
            using var stream = new MemoryStream(data);
            var reader = new BufferedSpanReader(stream);
            await reader.ReadExactAsync(Memory<byte>.Empty, default);
        }

        [Fact]
        public async Task ReadExactAsync_Memory_ValidRead_ReadsData()
        {
            var data = new byte[] { 1, 2, 3, 4, 5 };
            using var stream = new MemoryStream(data);
            var reader = new BufferedSpanReader(stream);
            var dest = new Memory<byte>(new byte[3]);
            await reader.ReadExactAsync(dest, default);
            Assert.Equal(new byte[] { 1, 2, 3 }, dest.ToArray());
        }

        [Fact]
        public async Task ReadExactAsync_Memory_EOFBeforeCount_ThrowsIOException()
        {
            var data = new byte[] { 1, 2 };
            using var stream = new MemoryStream(data);
            var reader = new BufferedSpanReader(stream);
            var dest = new Memory<byte>(new byte[5]);
            await Assert.ThrowsAsync<IOException>(() => reader.ReadExactAsync(dest, default));
        }

        [Fact]
        public async Task ReadExactAsync_Memory_LargeRead_ReadsAcrossBuffers()
        {
            var data = new byte[6000];
            for (int i = 0; i < 6000; i++) data[i] = (byte)(i % 256);
            using var stream = new MemoryStream(data);
            var reader = new BufferedSpanReader(stream, 512);
            var dest = new Memory<byte>(new byte[3000]);
            await reader.ReadExactAsync(dest, default);
            for (int i = 0; i < 3000; i++)
                Assert.Equal((byte)(i % 256), dest.Span[i]);
        }

        [Fact]
        public async Task Cancellation_SupportsCancellation_ReadLineStringAsync()
        {
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes("Line\r\n"));
            var reader = new BufferedSpanReader(stream);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(() => reader.ReadLineStringAsync(cts.Token));
        }

        [Fact]
        public async Task Cancellation_SupportsCancellation_ReadExactAsync_Array()
        {
            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var reader = new BufferedSpanReader(stream);
            var dest = new byte[3];
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(() => reader.ReadExactAsync(dest, 0, 3, cts.Token));
        }

        [Fact]
        public async Task Cancellation_SupportsCancellation_ReadExactAsync_Memory()
        {
            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var reader = new BufferedSpanReader(stream);
            var dest = new Memory<byte>(new byte[3]);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(() => reader.ReadExactAsync(dest, cts.Token));
        }
    }
}
