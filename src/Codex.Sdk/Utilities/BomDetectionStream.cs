
using System.Text;
using Codex.ObjectModel;
using Codex.Utilities.Serialization;

namespace Codex.Utilities;

public class BomDetectionStream : Stream
{
    private Stream _inner;
    private int _capturedPrefixPosition;
    private byte[] _capturedPrefixBuffer = new byte[10];

    public int ReadBytes { get; private set; }

    public BomDetectionStream(Stream inner)
    {
        _inner = inner;
    }

    public int GetPreambleLength(Encoding encoding)
    {
        if (encoding.Preamble.Length > 0 
            && _capturedPrefixPosition >= encoding.Preamble.Length
            && _capturedPrefixBuffer.AsSpan(0, encoding.Preamble.Length).SequenceEqual(encoding.Preamble))
        {
            return encoding.Preamble.Length;
        }

        return 0;
    }

    public SourceEncodingInfo GetEncodingInfo(Encoding encoding)
    {
        return SourceEncodingInfo.FromEncoding(encoding, GetPreambleLength(encoding));
    }

    public override bool CanRead => _inner.CanRead;

    public override bool CanSeek => _inner.CanSeek;

    public override bool CanWrite => false;

    public override long Length => _inner.Length;

    public override long Position { get => _inner.Position; set => _inner.Position = value; }

    public override void Flush()
    {
        _inner.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Capture(_inner.Read(buffer, offset, count), buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        return Capture(_inner.Read(buffer), buffer);
    }

    private int Capture(int read, Span<byte> buffer)
    {
        ReadBytes += read;

        if (_capturedPrefixPosition < _capturedPrefixBuffer.Length && read > 0)
        {
            SpanWriter writer = _capturedPrefixBuffer.AsSpan();
            writer.Position = _capturedPrefixPosition;
            writer.Write(buffer.Slice(0, Math.Min(writer.RemainingLength, read)));
            _capturedPrefixPosition = writer.Position;
        }

        return read;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return Capture(await _inner.ReadAsync(buffer, offset, count, cancellationToken), buffer.AsSpan(offset, count));
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return Capture(await _inner.ReadAsync(buffer, cancellationToken), buffer.Span);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _inner.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }
}