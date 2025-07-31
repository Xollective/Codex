using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using Tenray.ZoneTree.Collections;
using Tenray.ZoneTree.PresetTypes;
using Tenray.ZoneTree.Segments;
using Tenray.ZoneTree.Segments.Disk;

namespace Tenray.ZoneTree.Core;

public record SegmentLayout<TKey, TValue>(IMutableSegment<TKey, TValue> MutableSegment) : SegmentLayoutBase<TKey, TValue>()
{
    public ImmutableArray<IReadOnlySegment<TKey, TValue>> ReadOnlySegmentsTopFirst { get; init; } = EmptyReadOnlySegments;

    public IDiskSegment<TKey, TValue> DiskSegment { get; init; } 
        = NullDiskSegment<TKey, TValue>.Instance;

    public ImmutableArray<IDiskSegment<TKey, TValue>> DiskSegmentsTopFirst { get; init; } = EmptyDiskSegments;

    public LazyComputedValues LazyValues => lazyValues ??= new LazyComputedValues(this);

    public ImmutableArray<IReadOnlySegment<TKey, TValue>> GetReadOnlySegmentsWithMergedSegmentsRemoved(ImmutableArray<IReadOnlySegment<TKey, TValue>> mergedSegments)
    {
        if (ReadOnlySegmentsTopFirst == mergedSegments)
        {
            return EmptyReadOnlySegments;
        }
        else
        {
            // Sanity check that the values
            Debug.Assert(ReadOnlySegmentsTopFirst.Length >= mergedSegments.Length);
            var removeStart = ReadOnlySegmentsTopFirst.Length - mergedSegments.Length;
            for (int i = 0, j = removeStart; i < mergedSegments.Length; i++, j++)
            {
                Debug.Assert(ReadOnlySegmentsTopFirst[j] == mergedSegments[i]);
            }

            return ReadOnlySegmentsTopFirst.RemoveRange(removeStart, mergedSegments.Length);
        }
    }

    
}

public record SegmentLayoutBase<TKey, TValue>
{
    protected static readonly ImmutableArray<IReadOnlySegment<TKey, TValue>> EmptyReadOnlySegments = ImmutableArray<IReadOnlySegment<TKey, TValue>>.Empty;
    protected static readonly ImmutableArray<IDiskSegment<TKey, TValue>> EmptyDiskSegments = ImmutableArray<IDiskSegment<TKey, TValue>>.Empty;

    /// <summary>
    /// Copy constructor defined to override default record behavior which woulc copy the fields.
    /// Instead we don't copy at all so that values can be recomputed lazily.
    /// </summary>
    protected SegmentLayoutBase(SegmentLayoutBase<TKey, TValue> original)
    {

    }

    protected LazyComputedValues lazyValues;

    public record LazyComputedValues(SegmentLayout<TKey, TValue> Owner)
    {
        private ImmutableArray<IDiskSegment<TKey, TValue>>? orderedAllDiskSegments;

        private ImmutableArray<T> Concat<T>(T item, IReadOnlyList<T> items)
        {
            var builder = ImmutableArray.CreateBuilder<T>(1 + items.Count);
            builder.Add(item);
            builder.AddRange(items);
            return builder.MoveToImmutable();
        }

        public ImmutableArray<IDiskSegment<TKey, TValue>> OrderedAllDiskSegments => orderedAllDiskSegments ??= Concat(Owner.DiskSegment, Owner.DiskSegmentsTopFirst.ToReverseList());
    }
}