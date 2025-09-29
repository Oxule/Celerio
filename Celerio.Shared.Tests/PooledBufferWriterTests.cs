using System;
using System.Buffers;
using Celerio.Utils;
using Xunit;

namespace Celerio.Shared.Tests
{
    public class PooledBufferWriterTests : IDisposable
    {
        public void Dispose()
        {
            GC.Collect();
        }

        [Fact]
        public void Constructor_DefaultCapacity_CreatesWithDefaultCapacity()
        {
            using var writer = new PooledBufferWriter();
            Assert.Equal(0, writer.WrittenCount);
        }

        [Fact]
        public void Constructor_PositiveCapacity_CreatesWithSpecifiedCapacity()
        {
            using var writer = new PooledBufferWriter(1024);
            Assert.Equal(0, writer.WrittenCount);
        }

        [Fact]
        public void Constructor_ZeroOrNegativeCapacity_UsesDefaultCapacity()
        {
            using var writer1 = new PooledBufferWriter(0);
            using var writer2 = new PooledBufferWriter(-1);
            Assert.Equal(0, writer1.WrittenCount);
            Assert.Equal(0, writer2.WrittenCount);
        }

        [Fact]
        public void Write_ReadOnlySpan_EmptySpan_DoesNothing()
        {
            using var writer = new PooledBufferWriter(1024);
            writer.Write(ReadOnlySpan<byte>.Empty);
            Assert.Equal(0, writer.WrittenCount);
        }

        [Fact]
        public void Write_ReadOnlySpan_SingleByte_IncreasesWrittenCount()
        {
            using var writer = new PooledBufferWriter(1024);
            var data = new byte[] { 1 };
            writer.Write(data.AsSpan());
            Assert.Equal(1, writer.WrittenCount);
            var result = writer.ToArray();
            Assert.Single(result);
            Assert.Equal(1, result[0]);
        }

        [Fact]
        public void Write_ReadOnlySpan_MultipleBytes_IncreasesWrittenCount()
        {
            using var writer = new PooledBufferWriter(1024);
            var data = new byte[] { 1, 2, 3, 4, 5 };
            writer.Write(data.AsSpan());
            Assert.Equal(5, writer.WrittenCount);
            var result = writer.ToArray();
            Assert.Equal(data, result);
        }

        [Fact]
        public void Write_ReadOnlySpan_CausesReallocation_IncreasesBufferSize()
        {
            using var writer = new PooledBufferWriter(1);
            var data = new byte[256];
            for (int i = 0; i < 256; i++) data[i] = (byte)(i % 256);
            writer.Write(data.AsSpan());
            Assert.Equal(256, writer.WrittenCount);
            var result = writer.ToArray();
            Assert.Equal(data, result);
        }

        [Fact]
        public void Write_ArrayWithParams_NullArray_ThrowsArgumentNullException()
        {
            using var writer = new PooledBufferWriter(1024);
            Assert.Throws<ArgumentNullException>(() => writer.Write(null, 0, 0));
        }

        [Fact]
        public void Write_ArrayWithParams_InvalidOffset_ThrowsArgumentOutOfRangeException()
        {
            using var writer = new PooledBufferWriter(1024);
            var data = new byte[] { 1, 2, 3 };
            Assert.Throws<ArgumentOutOfRangeException>(() => writer.Write(data, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => writer.Write(data, 4, 1));
        }

        [Fact]
        public void Write_ArrayWithParams_InvalidCount_ThrowsArgumentOutOfRangeException()
        {
            using var writer = new PooledBufferWriter(1024);
            var data = new byte[] { 1, 2, 3 };
            Assert.Throws<ArgumentOutOfRangeException>(() => writer.Write(data, 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => writer.Write(data, 1, 3));
        }

        [Fact]
        public void Write_ArrayWithParams_EmptyCount_DoesNothing()
        {
            using var writer = new PooledBufferWriter(1024);
            var data = new byte[] { 1, 2, 3 };
            writer.Write(data, 0, 0);
            Assert.Equal(0, writer.WrittenCount);
        }

        [Fact]
        public void Write_ArrayWithParams_ValidData_IncreasesWrittenCount()
        {
            using var writer = new PooledBufferWriter(1024);
            var data = new byte[] { 1, 2, 3, 4, 5 };
            writer.Write(data, 1, 3);
            Assert.Equal(3, writer.WrittenCount);
            var result = writer.ToArray();
            Assert.Equal(3, result.Length);
            Assert.Equal(2, result[0]);
            Assert.Equal(3, result[1]);
            Assert.Equal(4, result[2]);
        }

        [Fact]
        public void Write_ArrayWithParams_CausesReallocation_IncreasesBufferSize()
        {
            using var writer = new PooledBufferWriter(1);
            var data = new byte[256];
            for (int i = 0; i < 256; i++) data[i] = (byte)(i % 256);
            writer.Write(data, 0, 256);
            Assert.Equal(256, writer.WrittenCount);
            var result = writer.ToArray();
            Assert.Equal(data, result);
        }

        [Fact]
        public void ToArray_EmptyWriter_ReturnsEmptyArray()
        {
            using var writer = new PooledBufferWriter(1024);
            var result = writer.ToArray();
            Assert.Empty(result);
        }

        [Fact]
        public void ToArray_WithData_ReturnsCopyOfData()
        {
            using var writer = new PooledBufferWriter(1024);
            var data = new byte[] { 1, 2, 3 };
            writer.Write(data.AsSpan());
            var result1 = writer.ToArray();
            Assert.Equal(data, result1);
            var result2 = writer.ToArray();
            Assert.Equal(data, result2);
        }

        [Fact]
        public void Dispose_MultipleCalls_DoesNotThrow()
        {
            var writer = new PooledBufferWriter(1024);
            writer.Dispose();
            writer.Dispose();
            Assert.Equal(0, writer.WrittenCount);
        }

        [Fact]
        public void MultipleWrites_AccumulateData()
        {
            using var writer = new PooledBufferWriter(1024);
            writer.Write(new byte[] { 1, 2 });
            writer.Write(new byte[] { 3, 4 });
            writer.Write(new byte[] { 5 }.AsSpan());
            Assert.Equal(5, writer.WrittenCount);
            var result = writer.ToArray();
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, result);
        }
    }
}
