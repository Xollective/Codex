namespace Codex.Utilities;

public static class BinaryReadStream
{
    public static BinaryReadStream<TData> Create<TData>(params BinaryReadStream<TData>.SpanReaderPart[] parts)
    {
        return new(parts);
    }

    public static BinaryReadStream<TData>.SpanReaderPart Part<TData>(in TData data, long length, BinaryReadStream<TData>.BinaryRead read)
    {
        return new(data, length, read);
    }
}

public class BinaryReadStream<TData> : ReadStream
{
    public delegate int BinaryRead(in TData data, Span<byte> target, long offset, bool initialize);

    public record struct SpanReaderPart(TData Data, long Length, BinaryRead Read);

    private long _position;
    private int _activeIndex = 0;
    private long _activeOffset;
    private bool _initialize = true;
    private IReadOnlyList<SpanReaderPart> _parts;

    public int PartIndex => _activeIndex;
    public long PartOffset => _activeOffset;

    public BinaryReadStream(IReadOnlyList<SpanReaderPart> parts)
    {
        _parts = parts;
        Length = parts.Sum(p => p.Length);
    }

    public override bool CanSeek => true;

    public override long Length { get; }

    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        offset = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            _ => Length + offset
        };

        var absOffset = offset;

        for (int i = 0; i < _parts.Count; i++)
        {
            var part = _parts[i];
            if (offset < part.Length)
            {
                SetActivePart(i, offset: offset);
                _position = absOffset;
                return absOffset;
            }
            else
            {
                offset -= part.Length;
            }
        }

        _position = Length;
        SetActivePart(_parts.Count, offset: 0);
        return Length;
    }

    private void SetActivePart(int partIndex, long offset = 0)
    {
        _activeIndex = partIndex;
        _activeOffset = offset;
        _initialize = true;
    }

    public override int ReadCore(Span<byte> buffer)
    {
        int read = 0;
        var remaining = buffer.Length;

        while (remaining > 0 && _activeIndex < _parts.Count)
        {
            var activePart = _parts[_activeIndex];
            var partRemaining = activePart.Length - _activeOffset;
            if (partRemaining <= 0)
            {
                SetActivePart(_activeIndex + 1);
                continue;
            }

            var adjustedCount = (int)Math.Min(remaining, partRemaining);

            var iterationRead = activePart.Read(
                activePart.Data,
                buffer.Slice(read, adjustedCount),
                offset: _activeOffset,
                initialize: _initialize | _activeOffset == 0);
            _initialize = false;

            Contract.Assert(iterationRead > 0);

            //iterationRead = Math.Max(0, iterationRead);
            //if (iterationRead == 0)
            //{
            //    // TODO: Doesn't seem like this is a valid case.
            //    SetActivePart(_activeIndex + 1);
            //    continue;
            //}

            remaining -= iterationRead;
            read += iterationRead;
            _activeOffset += iterationRead;
        }

        _position += read;
        return read;
    }
}
