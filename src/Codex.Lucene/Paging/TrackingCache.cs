using System.Collections.Concurrent;
using Codex.Utilities;

namespace Codex.Lucene.Search
{
    public record SegmentTracker(PageSegmentKey Key, PageFileSegment Segment) : SegmentTrackerBase()
    {
        public long? MinAccessedPosition;
        public long? MaxAccessedPosition;
        public int Version = -1;
        public int Uses;

        public int AccessLength
        {
            get
            {
                try
                {
                    return (int)(MaxAccessedPosition - MinAccessedPosition).Value;
                }
                catch
                {
                    return -1;
                }

            }
        }

        public override void BeforeCopy(long position)
        {
            lock (this)
            {
                MinAccessedPosition = Math.Min(position, MinAccessedPosition ?? long.MaxValue);
            }
        }

        public override void AfterCopy(long position)
        {
            lock (this)
            {
                MaxAccessedPosition = Math.Max(position, MaxAccessedPosition ?? 0);
            }
        }
    }

    public class TrackingCache : SegmentCacheMap
    {
        public int Version = 0;
        public int Misses = 0;
        public int Hits = 0;

        public ConcurrentDictionary<PageSegmentKey, SegmentTracker> TrackedSegments { get; } = new();
        public ConcurrentDictionary<PageSegmentKey, bool> MissedSegments { get; } = new();

        public override bool TryGetSegment(PageSegmentKey key, int desiredLength, out PageFileSegment segment)
        {
            if (base.TryGetSegment(key, desiredLength, out segment))
            {
                Interlocked.Increment(ref Hits);
                return true;
            }
            else
            {
                MissedSegments.TryAdd(key, true);
                Interlocked.Increment(ref Misses);
                return false;
            }
        }
        //public override bool TryGetSegment(PageSegmentKey key, int desiredLength, out PageFileSegment segment)
        //{
        //    segment = null;
        //    return false;
        //}

        public override void OnLoadedSegment(PageSegmentKey key, PageFileSegment segment)
        {
            if (segment.Tracker is not SegmentTracker trackedSegment)
            {
                segment.Tracker = trackedSegment = TrackedSegments.GetOrAdd(
                    key,
                    static (key, segment) => new SegmentTracker(key, segment),
                    segment);

                if (segment.Source != SegmentSource.Precache)
                {
                }
            }

            if (Interlocked.Exchange(ref trackedSegment.Version, Version) != Version)
            {
                Interlocked.Increment(ref trackedSegment.Uses);
            }
        }

        public void NextScenario()
        {
            Version++;
        }

        public void Reset()
        {
            TrackedSegments.Clear();
        }
    }
}
