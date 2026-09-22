using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LabGuard.Protocol;

namespace LabGuard.Scratch.TestFrameCodec;

/// <summary>
/// Standalone verification runner for LG-A-002: FrameCodec & message framing verification.
/// Tests all 9 authoritative scenarios and writes results to console / log.
/// </summary>
public static class Program
{
    public static async Task<int> Main()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("LABGUARD LG-A-002 — FrameCodec & Message Framing Standalone Verification");
        Console.WriteLine("================================================================================");

        int passed = 0;
        int failed = 0;

        async Task RunTest(string name, Func<Task> action)
        {
            try
            {
                await action();
                Console.WriteLine($"[PASS] {name}");
                passed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAIL] {name}: {ex.Message}");
                failed++;
            }
        }

        // Scenario 1: Header arriving 1 byte at a time
        await RunTest("Scenario 1: Header arriving 1 byte at a time", async () =>
        {
            byte[] payload = Encoding.UTF8.GetBytes("{\"test\":\"header_1b_at_a_time\"}");
            byte[] frame = new byte[4 + payload.Length];
            BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(0, 4), (uint)payload.Length);
            payload.CopyTo(frame.AsSpan(4));

            using var stream = new ChunkedStream(frame, 1, 1, 1, 1, payload.Length);
            byte[]? result = await FrameCodec.ReadFrameAsync(stream);
            if (result == null || !AreEqual(result, payload))
                throw new Exception("Payload mismatch");
        });

        // Scenario 2: Payload arriving in partial chunks
        await RunTest("Scenario 2: Payload arriving in partial chunks (100b, 200b, 200b)", async () =>
        {
            byte[] payload = new byte[500];
            for (int i = 0; i < payload.Length; i++) payload[i] = (byte)(i % 256);
            byte[] frame = new byte[4 + payload.Length];
            BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(0, 4), (uint)payload.Length);
            payload.CopyTo(frame.AsSpan(4));

            using var stream = new ChunkedStream(frame, 4, 100, 200, 200);
            byte[]? result = await FrameCodec.ReadFrameAsync(stream);
            if (result == null || !AreEqual(result, payload))
                throw new Exception("Payload mismatch");
        });

        // Scenario 3: 3 complete frames concatenated in one stream and read independently
        await RunTest("Scenario 3: 3 complete frames concatenated in one stream", async () =>
        {
            byte[] p1 = Encoding.UTF8.GetBytes("{\"f\":1}");
            byte[] p2 = Encoding.UTF8.GetBytes("{\"f\":2,\"extra\":\"data\"}");
            byte[] p3 = Encoding.UTF8.GetBytes("{\"f\":3}");

            using var stream = new MemoryStream();
            await FrameCodec.WriteFrameAsync(stream, p1);
            await FrameCodec.WriteFrameAsync(stream, p2);
            await FrameCodec.WriteFrameAsync(stream, p3);
            stream.Position = 0;

            byte[]? r1 = await FrameCodec.ReadFrameAsync(stream);
            byte[]? r2 = await FrameCodec.ReadFrameAsync(stream);
            byte[]? r3 = await FrameCodec.ReadFrameAsync(stream);
            byte[]? r4 = await FrameCodec.ReadFrameAsync(stream);

            if (r1 == null || !AreEqual(r1, p1)) throw new Exception("Frame 1 mismatch");
            if (r2 == null || !AreEqual(r2, p2)) throw new Exception("Frame 2 mismatch");
            if (r3 == null || !AreEqual(r3, p3)) throw new Exception("Frame 3 mismatch");
            if (r4 != null) throw new Exception("Expected null EOF at frame boundary");
        });

        // Scenario 4: N > 262144 rejected before payload allocation
        await RunTest("Scenario 4: N > 262144 rejected before payload allocation", async () =>
        {
            byte[] header = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(header, 262_145);
            using var stream = new MemoryStream(header);

            try
            {
                await FrameCodec.ReadFrameAsync(stream);
                throw new Exception("Expected InvalidDataException");
            }
            catch (InvalidDataException) { }
        });

        // Scenario 5: EOF in middle of header -> EndOfStreamException
        await RunTest("Scenario 5: EOF in middle of header throws EndOfStreamException", async () =>
        {
            byte[] truncatedHeader = [0x00, 0x01];
            using var stream = new MemoryStream(truncatedHeader);

            try
            {
                await FrameCodec.ReadFrameAsync(stream);
                throw new Exception("Expected EndOfStreamException");
            }
            catch (EndOfStreamException) { }
        });

        // Scenario 6: EOF in middle of payload -> EndOfStreamException
        await RunTest("Scenario 6: EOF in middle of payload throws EndOfStreamException", async () =>
        {
            byte[] frame = new byte[4 + 30];
            BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(0, 4), 100);
            using var stream = new MemoryStream(frame);

            try
            {
                await FrameCodec.ReadFrameAsync(stream);
                throw new Exception("Expected EndOfStreamException");
            }
            catch (EndOfStreamException) { }
        });

        // Scenario 7: Empty stream at frame boundary -> null
        await RunTest("Scenario 7: Empty stream at frame boundary returns null", async () =>
        {
            using var stream = new MemoryStream();
            byte[]? result = await FrameCodec.ReadFrameAsync(stream);
            if (result != null) throw new Exception("Expected null on empty stream");
        });

        // Scenario 8: Zero-length frame rejected (InvalidDataException)
        await RunTest("Scenario 8: Zero-length frame rejected with InvalidDataException", async () =>
        {
            byte[] header = [0x00, 0x00, 0x00, 0x00];
            using var stream = new MemoryStream(header);

            try
            {
                await FrameCodec.ReadFrameAsync(stream);
                throw new Exception("Expected InvalidDataException");
            }
            catch (InvalidDataException) { }
        });

        // Scenario 9: Exact 262,144-byte payload accepted
        await RunTest("Scenario 9: Exact 262,144-byte payload accepted", async () =>
        {
            byte[] payload = new byte[FrameCodec.MaxPayloadSize];
            Array.Fill(payload, (byte)'X');

            using var stream = new MemoryStream();
            await FrameCodec.WriteFrameAsync(stream, payload);
            stream.Position = 0;

            byte[]? result = await FrameCodec.ReadFrameAsync(stream);
            if (result == null || result.Length != FrameCodec.MaxPayloadSize || !AreEqual(result, payload))
                throw new Exception("262,144 bytes round-trip failed");
        });

        // Scenario 10: ReadExactlyBytesAsync accumulation & partial reads
        await RunTest("Scenario 10: ReadExactlyBytesAsync partial accumulation", async () =>
        {
            byte[] source = [1, 2, 3, 4, 5, 6, 7, 8];
            using var stream = new ChunkedStream(source, 2, 2, 4);
            byte[] dest = new byte[8];
            await FrameCodec.ReadExactlyBytesAsync(stream, dest, 0, 8);
            if (!AreEqual(source, dest)) throw new Exception("ReadExactlyBytesAsync mismatch");
        });

        Console.WriteLine("================================================================================");
        Console.WriteLine($"RESULT: {passed} passed, {failed} failed out of {passed + failed} scenarios.");
        Console.WriteLine("================================================================================");

        return failed == 0 ? 0 : 1;
    }

    private static bool AreEqual(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    private class ChunkedStream : Stream
    {
        private readonly byte[] _data;
        private readonly int[] _chunks;
        private int _pos;
        private int _chunkIdx;

        public ChunkedStream(byte[] data, params int[] chunks)
        {
            _data = data;
            _chunks = chunks;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_pos >= _data.Length) return 0;
            int maxRead = count;
            if (_chunkIdx < _chunks.Length)
            {
                maxRead = Math.Min(maxRead, _chunks[_chunkIdx++]);
            }
            int available = _data.Length - _pos;
            int actual = Math.Min(maxRead, available);
            Array.Copy(_data, _pos, buffer, offset, actual);
            _pos += actual;
            return actual;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, System.Threading.CancellationToken ct = default)
        {
            byte[] tmp = new byte[buffer.Length];
            int n = Read(tmp, 0, tmp.Length);
            tmp.AsSpan(0, n).CopyTo(buffer.Span);
            return ValueTask.FromResult(n);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _data.Length;
        public override long Position { get => _pos; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
