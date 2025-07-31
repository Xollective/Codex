using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Codex.Utilities
{
    public record struct ReadOnlySegment<T>(ReadOnlyMemory<T> Source, Extent Range)
    {
        public ReadOnlySegment(ReadOnlyMemory<T> source, int start, int length)
            : this(source, new Extent(start, length))
        {
        }

        public ReadOnlySegment<T> WithEnd(int end)
        {
            return new ReadOnlySegment<T>(Source, Extent.FromBounds(Range.Start, end));
        }

        public ReadOnlyMemory<T> AsMemory() => Source.Slice(Range.Start, Range.Length);

        public ReadOnlySpan<T> Span => AsMemory().Span;

        public static implicit operator ReadOnlyMemory<T>(ReadOnlySegment<T> segment)
        {
            return segment.AsMemory();
        }
    }
}
