using System.Buffers.Binary;
using System.Text.Json;
using LabGuard.Protocol.Serialization;

namespace LabGuard.Protocol;

/// <summary>
/// Implements 4-byte UInt32 Big-Endian Length-Prefix Framing for TCP streams (RFC 9293).
/// Wire frame format: [4-byte Big-Endian length N] + [N bytes UTF-8 JSON payload].
/// Valid payload size: 0 &lt; N &lt;= 262,144 bytes (256 KiB).
/// Max wire frame size: 262,148 bytes.
/// </summary>
public static class FrameCodec
{
    public const int HeaderSize = 4;
    public const int MaxPayloadSize = 262_144; // 256 KiB
    public const int MaxFrameSize = MaxPayloadSize; // Alias for WBS specification contract
    public const int MaxWireFrameSize = HeaderSize + MaxPayloadSize; // 262,148 bytes

    /// <summary>
    /// Reads exactly <paramref name="count"/> bytes from the stream into <paramref name="buffer"/> at <paramref name="offset"/>.
    /// Continues accumulating bytes across partial reads until all <paramref name="count"/> bytes are received.
    /// Throws <see cref="EndOfStreamException"/> if the stream closes prematurely before <paramref name="count"/> bytes are read.
    /// </summary>
    public static async Task ReadExactlyBytesAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (offset + count > buffer.Length)
        {
            throw new ArgumentException("Offset and count exceed buffer length.", nameof(count));
        }

        int totalRead = 0;
        while (totalRead < count)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset + totalRead, count - totalRead), ct).ConfigureAwait(false);
            if (read == 0)
            {
                throw new EndOfStreamException($"Stream closed prematurely after reading {totalRead} of {count} bytes.");
            }

            totalRead += read;
        }
    }

    /// <summary>
    /// Encodes a WireEnvelope into framed bytes: [4-byte Big-Endian length][UTF-8 JSON payload].
    /// Throws ArgumentException if serialized payload is empty or exceeds MaxPayloadSize (262,144 bytes).
    /// </summary>
    public static byte[] Encode<T>(WireEnvelope<T> envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        byte[] payloadBytes = ProtocolSerializer.Serialize(envelope);
        return Encode(payloadBytes);
    }

    /// <summary>
    /// Encodes a raw UTF-8 JSON payload into framed bytes: [4-byte Big-Endian length][payload].
    /// Throws ArgumentException if payload is empty or exceeds MaxPayloadSize (262,144 bytes).
    /// </summary>
    public static byte[] Encode(ReadOnlyMemory<byte> payload)
    {
        if (payload.Length == 0)
        {
            throw new ArgumentException("Payload length must be greater than 0. Zero-length frames are invalid.", nameof(payload));
        }

        if (payload.Length > MaxPayloadSize)
        {
            throw new ArgumentException($"Payload size {payload.Length} exceeds maximum allowed size of {MaxPayloadSize} bytes.", nameof(payload));
        }

        byte[] frameBuffer = new byte[HeaderSize + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(frameBuffer.AsSpan(0, HeaderSize), (uint)payload.Length);
        payload.Span.CopyTo(frameBuffer.AsSpan(HeaderSize));
        return frameBuffer;
    }

    /// <summary>
    /// Decodes a framed byte buffer [4-byte Big-Endian length][UTF-8 JSON payload] into payload bytes.
    /// Validates length prefix against actual payload length and MaxPayloadSize.
    /// </summary>
    public static byte[] Decode(ReadOnlySpan<byte> frameBytes, int maxPayloadSize = MaxPayloadSize)
    {
        if (frameBytes.Length < HeaderSize)
        {
            throw new InvalidDataException($"Frame too short: expected at least {HeaderSize} bytes header, got {frameBytes.Length}.");
        }

        uint payloadLength = BinaryPrimitives.ReadUInt32BigEndian(frameBytes[..HeaderSize]);
        if (payloadLength == 0)
        {
            throw new InvalidDataException("Invalid frame: payload length is 0. Zero-length frames are rejected by protocol specification.");
        }

        if (payloadLength > (uint)maxPayloadSize)
        {
            throw new InvalidDataException($"Invalid frame: payload length {payloadLength} bytes exceeds maximum allowed size of {maxPayloadSize} bytes.");
        }

        if (frameBytes.Length - HeaderSize < payloadLength)
        {
            throw new EndOfStreamException($"Incomplete frame: expected {payloadLength} payload bytes, got {frameBytes.Length - HeaderSize}.");
        }

        return frameBytes.Slice(HeaderSize, (int)payloadLength).ToArray();
    }

    /// <summary>
    /// Writes a length-prefixed frame to the stream.
    /// Encodes length as 4-byte UInt32 Big-Endian followed by payload bytes.
    /// </summary>
    public static async Task WriteFrameAsync(Stream stream, ReadOnlyMemory<byte> payload, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Copy header and payload into a single buffer for convenience before passing to stream.
        // NOTE: TCP is a byte stream; one WriteAsync call is NOT guaranteed to map to one TCP segment.
        // TCP may split or coalesce writes. The length-prefix framing + ReadFrameAsync logic reconstructs
        // application messages regardless of network packet boundaries. Concurrent writes must be guarded
        // by a Single Writer pattern to prevent interleaving application frame bytes.
        byte[] frameBuffer = Encode(payload);

        await stream.WriteAsync(frameBuffer, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads a length-prefixed frame from the stream.
    /// Returns the raw payload byte array, or null if the stream is closed gracefully at a frame boundary (0 bytes read).
    /// Throws InvalidDataException if length == 0 or length &gt; maxPayloadSize.
    /// Throws EndOfStreamException if stream closes mid-header or mid-payload.
    /// </summary>
    public static async Task<byte[]?> ReadFrameAsync(Stream stream, int maxPayloadSize = MaxPayloadSize, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (maxPayloadSize <= 0 || maxPayloadSize > MaxPayloadSize)
        {
            maxPayloadSize = MaxPayloadSize;
        }

        // 1. Read 4-byte header
        byte[] headerBuffer = new byte[HeaderSize];
        int initialRead = await stream.ReadAsync(headerBuffer.AsMemory(0, HeaderSize), ct).ConfigureAwait(false);
        if (initialRead == 0)
        {
            // Clean stream termination at frame boundary
            return null;
        }

        if (initialRead < HeaderSize)
        {
            // Stream yielded fewer than 4 bytes on initial read; accumulate remaining header bytes
            await ReadExactlyBytesAsync(stream, headerBuffer, initialRead, HeaderSize - initialRead, ct).ConfigureAwait(false);
        }

        // 2. Decode Big-Endian 4-byte length
        uint payloadLength = BinaryPrimitives.ReadUInt32BigEndian(headerBuffer);

        // 3. Strict length boundary validation (reject BEFORE allocating payload buffer)
        if (payloadLength == 0)
        {
            throw new InvalidDataException("Invalid frame: payload length is 0. Zero-length frames are rejected by protocol specification.");
        }

        if (payloadLength > (uint)maxPayloadSize)
        {
            throw new InvalidDataException($"Invalid frame: payload length {payloadLength} bytes exceeds maximum allowed size of {maxPayloadSize} bytes.");
        }

        // 4. Read exactly N payload bytes
        byte[] payloadBuffer = new byte[payloadLength];
        await ReadExactlyBytesAsync(stream, payloadBuffer, 0, (int)payloadLength, ct).ConfigureAwait(false);

        return payloadBuffer;
    }

    /// <summary>
    /// Reads and strictly decodes one complete WireEnvelope frame from the stream.
    /// Validates the frame against the frozen wire contract using EnvelopeValidator.ValidateAndParse.
    /// Returns the decoded WireEnvelope with JsonElement payload, or null if the stream is cleanly closed at a frame boundary.
    /// Throws InvalidDataException if the envelope violates the wire contract or contains malformed JSON.
    /// </summary>
    public static async Task<WireEnvelope<JsonElement>?> ReadEnvelopeAsync(Stream stream, int maxPayloadSize = MaxPayloadSize, CancellationToken ct = default)
    {
        byte[]? frameBytes = await ReadFrameAsync(stream, maxPayloadSize, ct).ConfigureAwait(false);
        if (frameBytes == null)
        {
            return null;
        }

        var (validation, envelope) = EnvelopeValidator.ValidateAndParse(frameBytes);
        if (!validation.IsValid || envelope == null)
        {
            throw new InvalidDataException($"[{validation.ErrorCode ?? ErrorCodes.InvalidPayload}] {validation.ErrorMessage ?? "Invalid envelope."}");
        }

        return envelope;
    }
}
