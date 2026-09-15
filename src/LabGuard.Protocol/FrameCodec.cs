using System.Buffers.Binary;

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
    public const int MaxWireFrameSize = HeaderSize + MaxPayloadSize; // 262,148 bytes

    /// <summary>
    /// Writes a length-prefixed frame to the stream.
    /// Encodes length as 4-byte UInt32 Big-Endian followed by payload bytes.
    /// </summary>
    public static async Task WriteFrameAsync(Stream stream, ReadOnlyMemory<byte> payload, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (payload.Length == 0)
        {
            throw new ArgumentException("Payload length must be greater than 0. Zero-length frames are invalid.", nameof(payload));
        }

        if (payload.Length > MaxPayloadSize)
        {
            throw new ArgumentException($"Payload size {payload.Length} exceeds maximum allowed size of {MaxPayloadSize} bytes.", nameof(payload));
        }

        // Copy header and payload into a single buffer for convenience before passing to stream.
        // NOTE: TCP is a byte stream; one WriteAsync call is NOT guaranteed to map to one TCP segment.
        // TCP may split or coalesce writes. The length-prefix framing + ReadFrameAsync logic reconstructs
        // application messages regardless of network packet boundaries. Concurrent writes must be guarded
        // by a Single Writer pattern to prevent interleaving application frame bytes.
        byte[] frameBuffer = new byte[HeaderSize + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(frameBuffer.AsSpan(0, HeaderSize), (uint)payload.Length);
        payload.Span.CopyTo(frameBuffer.AsSpan(HeaderSize));

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
        int headerBytesRead = 0;

        while (headerBytesRead < HeaderSize)
        {
            int read = await stream.ReadAsync(headerBuffer.AsMemory(headerBytesRead, HeaderSize - headerBytesRead), ct).ConfigureAwait(false);
            if (read == 0)
            {
                if (headerBytesRead == 0)
                {
                    // Clean stream termination at frame boundary
                    return null;
                }

                // Stream closed abruptly mid-header
                throw new EndOfStreamException($"Stream closed prematurely after reading {headerBytesRead} of {HeaderSize} header bytes.");
            }

            headerBytesRead += read;
        }

        // 2. Decode Big-Endian 4-byte length
        uint payloadLength = BinaryPrimitives.ReadUInt32BigEndian(headerBuffer);

        // 3. Strict length boundary validation
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
        int payloadBytesRead = 0;

        while (payloadBytesRead < payloadLength)
        {
            int toRead = (int)payloadLength - payloadBytesRead;
            int read = await stream.ReadAsync(payloadBuffer.AsMemory(payloadBytesRead, toRead), ct).ConfigureAwait(false);
            if (read == 0)
            {
                // Stream closed abruptly mid-payload
                throw new EndOfStreamException($"Stream closed prematurely after reading {payloadBytesRead} of {payloadLength} payload bytes.");
            }

            payloadBytesRead += read;
        }

        return payloadBuffer;
    }
}
