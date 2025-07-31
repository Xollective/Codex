using System.Text;

namespace Codex.Utilities;

public class CompositeStream : BinaryReadStream<StreamPart>
{
    private readonly IReadOnlyList<StreamPart> _parts;

    public CompositeStream(IReadOnlyList<StreamPart> parts)
        : base(parts.SelectList(GetBinaryReadPart))
    {
        _parts = parts;
    }

    private static SpanReaderPart GetBinaryReadPart(StreamPart part)
    {
        return new SpanReaderPart(part, part.Extent.Length, ReadStream);
    }

    private static int ReadStream(in StreamPart data, Span<byte> target, long offset, bool initialize)
    {
        var stream = data.Stream;
        if (initialize)
        {
            stream.Position = data.Extent.Start + offset;
        }

        return stream.Read(target);
    }
}

public record struct StreamPart(Stream Stream, LongExtent Extent)
{
    public LazyEx<byte[]> Content { get; } = Lazy.Create(() =>
    {
        var content = new byte[Math.Min(100, Extent.Length)];
        var pos = Stream.Position;
        Stream.Read(content);
        Stream.Position = pos;
        return content;
    });

    public LazyEx<string> TextContent { get; } = Lazy.Create(() =>
    {
        var content = new byte[Math.Min(100, Extent.Length)];
        var pos = Stream.Position;
        Stream.Read(content);
        Stream.Position = pos;
        return Encoding.UTF8.GetString(content);
    });
}
