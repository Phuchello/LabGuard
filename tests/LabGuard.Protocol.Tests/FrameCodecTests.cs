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
    // TEST 2: Partial Header (Chunks: 1b, 1b, 2b and 1b, 1b, 1b, 1b)
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

    [Fact]
    public async Task ReadFrameAsync_HeaderByteByByte_1ByteChunks_AssemblesCorrectly()
    {
        // Arrange: Header arrives 1 byte at a time (1, 1, 1, 1)
        byte[] payload = Encoding.UTF8.GetBytes("{\"test\":\"header_1byte_at_a_time\"}");
        byte[] frameData = new byte[FrameCodec.HeaderSize + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(frameData.AsSpan(0, 4), (uint)payload.Length);
        payload.CopyTo(frameData.AsSpan(4));

        using var chunkedStream = new ChunkedStream(frameData, 1, 1, 1, 1, payload.Length);

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

    [Fact]
    public async Task ReadFrameAsync_PayloadByteByByte_AssemblesCorrectly()
    {
        // Arrange: 100-byte test payload arriving 1 byte at a time
        byte[] payload = Encoding.UTF8.GetBytes("{\"msg\":\"1_byte_chunked_payload_for_testing_partial_reads_in_frame_codec\"}");
        byte[] frameData = new byte[FrameCodec.HeaderSize + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(frameData.AsSpan(0, 4), (uint)payload.Length);
        payload.CopyTo(frameData.AsSpan(4));

        // Header in 1 chunk (4b), then payload 1 byte at a time
        var chunks = new List<int> { 4 };
        for (int i = 0; i < payload.Length; i++)
        {
            chunks.Add(1);
        }

        using var chunkedStream = new ChunkedStream(frameData, chunks.ToArray());

        // Act
        byte[]? result = await FrameCodec.ReadFrameAsync(chunkedStream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(payload.Length, result.Length);
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

    [Fact]
    public void Encode_And_Decode_RoundTrip_ReturnsOriginalBytes()
    {
        byte[] payload = Encoding.UTF8.GetBytes("{\"test\":\"encode_decode\"}");
        byte[] framed = FrameCodec.Encode(payload);

        Assert.Equal(FrameCodec.HeaderSize + payload.Length, framed.Length);
        uint declaredLength = BinaryPrimitives.ReadUInt32BigEndian(framed.AsSpan(0, 4));
        Assert.Equal((uint)payload.Length, declaredLength);

        byte[] decoded = FrameCodec.Decode(framed);
        Assert.Equal(payload, decoded);
    }

    [Fact]
    public void Decode_FrameTooShort_ThrowsInvalidDataException()
    {
        byte[] tooShort = [0x00, 0x01];
        var ex = Assert.Throws<InvalidDataException>(() => FrameCodec.Decode(tooShort));
        Assert.Contains("Frame too short", ex.Message);
    }

    [Fact]
    public void Decode_ZeroLength_ThrowsInvalidDataException()
    {
        byte[] zeroLen = [0x00, 0x00, 0x00, 0x00];
        var ex = Assert.Throws<InvalidDataException>(() => FrameCodec.Decode(zeroLen));
        Assert.Contains("payload length is 0", ex.Message);
    }

    [Fact]
    public void Decode_OversizedLength_ThrowsInvalidDataException()
    {
        byte[] header = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(header, FrameCodec.MaxPayloadSize + 1);
        var ex = Assert.Throws<InvalidDataException>(() => FrameCodec.Decode(header));
        Assert.Contains("exceeds maximum allowed size", ex.Message);
    }

    [Fact]
    public void Decode_TruncatedPayload_ThrowsEndOfStreamException()
    {
        byte[] frame = new byte[FrameCodec.HeaderSize + 5];
        BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(0, 4), 10); // claims 10 bytes, only has 5
        var ex = Assert.Throws<EndOfStreamException>(() => FrameCodec.Decode(frame));
        Assert.Contains("Incomplete frame", ex.Message);
    }

    // =========================================================================
    // TEST 9: Exact Max Payload Boundary (262,144 bytes / 256 KiB)
    // =========================================================================
    [Fact]
    public async Task WriteAndRead_ExactMaxPayloadSize_262144Bytes_Succeeds()
    {
        // Arrange: Exactly 262,144 bytes payload
        byte[] payload = new byte[FrameCodec.MaxPayloadSize];
        Array.Fill(payload, (byte)'Z');

        using var stream = new MemoryStream();

        // Act
        await FrameCodec.WriteFrameAsync(stream, payload);
        stream.Position = 0;
        byte[]? result = await FrameCodec.ReadFrameAsync(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(FrameCodec.MaxPayloadSize, result.Length);
        Assert.Equal(payload, result);
    }

    [Fact]
    public async Task ReadFrameAsync_ExactMaxPayloadSize_WithMaxPayloadSizeArgument_Succeeds()
    {
        byte[] payload = new byte[FrameCodec.MaxPayloadSize];
        Array.Fill(payload, (byte)'B');

        byte[] framed = FrameCodec.Encode(payload);
        using var stream = new MemoryStream(framed);

        byte[]? result = await FrameCodec.ReadFrameAsync(stream, maxPayloadSize: FrameCodec.MaxPayloadSize);

        Assert.NotNull(result);
        Assert.Equal(FrameCodec.MaxPayloadSize, result.Length);
        Assert.Equal(payload, result);
    }

    // =========================================================================
    // TEST 10: ReadExactlyBytesAsync Dedicated Unit Tests
    // =========================================================================
    [Fact]
    public async Task ReadExactlyBytesAsync_FullBuffer_Succeeds()
    {
        byte[] source = [1, 2, 3, 4, 5, 6, 7, 8];
        using var stream = new MemoryStream(source);
        byte[] dest = new byte[8];

        await FrameCodec.ReadExactlyBytesAsync(stream, dest, 0, 8);

        Assert.Equal(source, dest);
    }

    [Fact]
    public async Task ReadExactlyBytesAsync_PartialChunks_AssemblesBuffer()
    {
        byte[] source = [10, 20, 30, 40, 50];
        using var chunkedStream = new ChunkedStream(source, 2, 1, 2);
        byte[] dest = new byte[5];

        await FrameCodec.ReadExactlyBytesAsync(chunkedStream, dest, 0, 5);

        Assert.Equal(source, dest);
    }

    [Fact]
    public async Task ReadExactlyBytesAsync_PrematureEof_ThrowsEndOfStreamException()
    {
        byte[] source = [10, 20]; // only 2 bytes available when expecting 5
        using var stream = new MemoryStream(source);
        byte[] dest = new byte[5];

        var ex = await Assert.ThrowsAsync<EndOfStreamException>(() => FrameCodec.ReadExactlyBytesAsync(stream, dest, 0, 5));
        Assert.Contains("closed prematurely", ex.Message);
    }

    [Fact]
    public async Task ReadExactlyBytesAsync_InvalidArguments_Throws()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        byte[] dest = new byte[5];

        await Assert.ThrowsAsync<ArgumentNullException>(() => FrameCodec.ReadExactlyBytesAsync(null!, dest, 0, 1));
        await Assert.ThrowsAsync<ArgumentNullException>(() => FrameCodec.ReadExactlyBytesAsync(stream, null!, 0, 1));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => FrameCodec.ReadExactlyBytesAsync(stream, dest, -1, 1));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => FrameCodec.ReadExactlyBytesAsync(stream, dest, 0, -1));
        await Assert.ThrowsAsync<ArgumentException>(() => FrameCodec.ReadExactlyBytesAsync(stream, dest, 3, 3)); // 3 + 3 = 6 > 5
    }
}
