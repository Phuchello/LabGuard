namespace LabGuard.Protocol.Tests;

/// <summary>
/// A test stream that artificially restricts each Read/ReadAsync call to specified chunk sizes
/// to simulate network packet fragmentation, partial reads, and packet coalescing.
/// </summary>
public class ChunkedStream : Stream
{
    private readonly byte[] _data;
    private readonly int[] _chunkSizes;
    private int _dataPosition;
    private int _chunkIndex;

    public ChunkedStream(byte[] data, params int[] chunkSizes)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _chunkSizes = chunkSizes ?? [];
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        if (_dataPosition >= _data.Length)
        {
            return 0;
        }

        int maxToRead = buffer.Length;
        if (_chunkIndex < _chunkSizes.Length)
        {
            maxToRead = Math.Min(maxToRead, _chunkSizes[_chunkIndex++]);
        }

        int available = _data.Length - _dataPosition;
        int actual = Math.Min(maxToRead, available);

        _data.AsSpan(_dataPosition, actual).CopyTo(buffer);
        _dataPosition += actual;
        return actual;
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(Read(buffer.Span));
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return Task.FromResult(Read(buffer.AsSpan(offset, count)));
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => _data.Length;
    public override long Position
    {
        get => _dataPosition;
        set => throw new NotSupportedException();
    }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
