using System.Buffers.Binary;
using System.Text;
using Xunit;

namespace LabGuard.Protocol.Tests;

public class FrameCodecTests
{
    // =========================================================================
    // TEST 1: Normal Frame Write & Read
    // =========================================================================
    [Fact]
    public async Task WriteAndRead_NormalFrame_ReturnsExactPayload()
    {
        // Arrange
        byte[] expectedPayload = Encoding.UTF8.GetBytes("{\"version\":1,\"messageType\":\"HELLO_AGENT\",\"deviceId\":\"TEST-PC01\"}");
        using var stream = new MemoryStream();

        // Act
        await FrameCodec.WriteFrameAsync(stream, expectedPayload);
        stream.Position = 0;
        byte[]? actualPayload = await FrameCodec.ReadFrameAsync(stream);

        // Assert
        Assert.NotNull(actualPayload);
        Assert.Equal(expectedPayload.Length, actualPayload.Length);
        Assert.Equal(expectedPayload, actualPayload);
    }

    // =========================================================================
    // TEST 2: Partial Header (Chunks: 1b, 1b, 2b)
    // =========================================================================
    [Fact]
    public async Task ReadFrameAsync_PartialHeader_AssemblesCorrectly()
    {
        // Arrange
        byte[] payload = Encoding.UTF8.GetBytes("{\"test\":\"partial_header_payload\"}");
        byte[] frameData = new byte[FrameCodec.HeaderSize + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(frameData.AsSpan(0, 4), (uint)payload.Length);
        payload.CopyTo(frameData.AsSpan(4));

        // Stream yields header in fragments: 1 byte, 1 byte, 2 bytes, then payload in one chunk
        using var chunkedStream = new ChunkedStream(frameData, 1, 1, 2, payload.Length);

        // Act
        byte[]? result = await FrameCodec.ReadFrameAsync(chunkedStream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(payload, result);
    }

    // =========================================================================
    // TEST 3: Partial Payload (CP2-TEST-001: 500 bytes in 100b, 200b, 200b chunks)
    // =========================================================================
    [Fact]
    public async Task ReadFrameAsync_PartialPayload_CP2_TEST_001_AssemblesCorrectly()
    {
        // Arrange: 500-byte test payload
        byte[] payload = new byte[500];
        for (int i = 0; i < payload.Length; i++)
        {
            payload[i] = (byte)(i % 256);
        }

        byte[] frameData = new byte[FrameCodec.HeaderSize + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(frameData.AsSpan(0, 4), (uint)payload.Length);
        payload.CopyTo(frameData.AsSpan(4));

        // Header (4b), then payload split across 100b, 200b, 200b
        using var chunkedStream = new ChunkedStream(frameData, 4, 100, 200, 200);

        // Act
        byte[]? result = await FrameCodec.ReadFrameAsync(chunkedStream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(500, result.Length);
        Assert.Equal(payload, result);
    }

    // =========================================================================
    // TEST 4: Multiple Coalesced Frames arriving together in single stream
    // =========================================================================
    [Fact]
    public async Task ReadFrameAsync_MultipleCoalescedFrames_SeparatesIndependently()
    {
        // Arrange: 3 distinct frames coalesced into one stream
        byte[] payload1 = Encoding.UTF8.GetBytes("{\"msg\":\"FIRST_FRAME_CONTENT\"}");
        byte[] payload2 = Encoding.UTF8.GetBytes("{\"msg\":\"SECOND_FRAME_LONGER_CONTENT_FOR_TESTING\"}");
        byte[] payload3 = Encoding.UTF8.GetBytes("{\"msg\":\"THIRD_FINAL_FRAME\"}");

        using var stream = new MemoryStream();
        await FrameCodec.WriteFrameAsync(stream, payload1);
        await FrameCodec.WriteFrameAsync(stream, payload2);
        await FrameCodec.WriteFrameAsync(stream, payload3);

        stream.Position = 0;

        // Act & Assert: Read 1
        byte[]? frame1 = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(frame1);
        Assert.Equal(payload1, frame1);

        // Read 2
        byte[]? frame2 = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(frame2);
        Assert.Equal(payload2, frame2);

        // Read 3
        byte[]? frame3 = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(frame3);
        Assert.Equal(payload3, frame3);

        // Clean EOF at boundary
        byte[]? eof = await FrameCodec.ReadFrameAsync(stream);
        Assert.Null(eof);
    }

    // =========================================================================
    // TEST 5: Oversize Frame (> 262,144 bytes) throws InvalidDataException
    // =========================================================================
    [Theory]
    [InlineData(262_145)] // 1 byte over max
    [InlineData(500_000)]
    [InlineData(10_000_000)]
    public async Task ReadFrameAsync_OversizeFrame_ThrowsInvalidDataException(int oversizedLength)
    {
        // Arrange
        byte[] header = new byte[FrameCodec.HeaderSize];
        BinaryPrimitives.WriteUInt32BigEndian(header, (uint)oversizedLength);

        using var stream = new MemoryStream(header);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => FrameCodec.ReadFrameAsync(stream));
        Assert.Contains("exceeds maximum allowed size", ex.Message);
    }

    // =========================================================================
    // TEST 6: Zero-Length Frame (N == 0) throws InvalidDataException
    // =========================================================================
    [Fact]
    public async Task ReadFrameAsync_ZeroLengthFrame_ThrowsInvalidDataException()
    {
        // Arrange: Header indicates 0 bytes
        byte[] header = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(header, 0);

        using var stream = new MemoryStream(header);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => FrameCodec.ReadFrameAsync(stream));
        Assert.Contains("payload length is 0", ex.Message);
    }

    // =========================================================================
    // TEST 7: EOF Mid-Frame throws EndOfStreamException
    // =========================================================================
    [Fact]
    public async Task ReadFrameAsync_EofMidHeader_ThrowsEndOfStreamException()
    {
        // Arrange: Only 2 bytes available when expecting 4-byte header
        byte[] truncatedHeader = [0x00, 0x01];
        using var stream = new MemoryStream(truncatedHeader);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<EndOfStreamException>(() => FrameCodec.ReadFrameAsync(stream));
        Assert.Contains("closed prematurely", ex.Message);
    }

    [Fact]
    public async Task ReadFrameAsync_EofMidPayload_ThrowsEndOfStreamException()
    {
        // Arrange: Header declares 100 bytes, but only 30 bytes are in stream
        byte[] frameData = new byte[FrameCodec.HeaderSize + 30];
        BinaryPrimitives.WriteUInt32BigEndian(frameData.AsSpan(0, 4), 100);
        for (int i = 4; i < frameData.Length; i++)
        {
            frameData[i] = (byte)i;
        }

        using var stream = new MemoryStream(frameData);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<EndOfStreamException>(() => FrameCodec.ReadFrameAsync(stream));
        Assert.Contains("closed prematurely", ex.Message);
    }

    // =========================================================================
    // Additional Edge Cases: Clean EOF, Write Validations
    // =========================================================================
    [Fact]
    public async Task ReadFrameAsync_EmptyStream_ReturnsNull()
    {
        using var emptyStream = new MemoryStream();
        byte[]? result = await FrameCodec.ReadFrameAsync(emptyStream);
        Assert.Null(result);
    }

    [Fact]
    public async Task WriteFrameAsync_EmptyPayload_ThrowsArgumentException()
    {
        using var stream = new MemoryStream();
        await Assert.ThrowsAsync<ArgumentException>(() => FrameCodec.WriteFrameAsync(stream, ReadOnlyMemory<byte>.Empty));
    }

    [Fact]
    public async Task WriteFrameAsync_OversizedPayload_ThrowsArgumentException()
    {
        using var stream = new MemoryStream();
        byte[] oversized = new byte[FrameCodec.MaxPayloadSize + 1];
        await Assert.ThrowsAsync<ArgumentException>(() => FrameCodec.WriteFrameAsync(stream, oversized));
    }
}
