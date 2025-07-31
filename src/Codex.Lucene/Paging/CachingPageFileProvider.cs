using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Lucene.Net;

namespace Codex.Lucene.Search
{
    using static Placeholder;

    public record struct PageSegmentKey(string File, long AlignedPosition);

    public class ReadStats
    {
    }

    public class CachingPageFileProvider : PageFileProvider, IPageFileProvider
    {
        public static ThreadLocal<Thread> Threads { get; } = new ThreadLocal<Thread>(trackAllValues: true);
        public static ThreadLocal<ThreadDiag> ThreadDiags { get; } = new(trackAllValues: true);

        public long MaxPageRetrievalCount { get; set; } = long.MaxValue;

        public long FileCount = 0;
        public long ReadCount = 0;
        public long TotalRead = 0;
        public long TotalRequested = 0;

        public int PageSize { get; }
        public VolatileMap<PageSegmentKey, PageFileSegment> SegmentCache { get; }
        private ConcurrentDictionary<PageSegmentKey, PendingSegment> _pendings = new();

        public int ContiguousRangeDetectionThreshold { get; set; } = 3;
        public int MaxContiguousRangeSize { get; set; } = 1 << 20;

        public IPageFileAccessor Accessor { get; }

        public CachingPageFileProvider(IPageFileAccessor accessor, int cacheLimit = 0, int pageSize = 1 << 12)
        {
            Accessor = accessor;
            SegmentCache = new(cacheLimit);
            PageSize = pageSize;
        }

        public void Clear()
        {
            SegmentCache.Clear();
        }

        public void LoadPrecache(IEnumerable<CachedSegmentEntry> cachedEntries, byte[] precacheContent)
        {
            var preCache = (SegmentPreCache as SegmentCacheMap) ?? new SegmentCacheMap();
            long start = 0;
            foreach (var entry in cachedEntries)
            {
                var segment = precacheContent.AsMemory(
                        (int)start,
                        (int)entry.Length);

                for (long i = 0; i < segment.Length; i += PageSize)
                {
                    var position = entry.StartPosition + i;
                    var alignedPosition = AlignPosition(position);

                    preCache.Map[new PageSegmentKey(entry.Path, alignedPosition)] = new PageFileSegment(entry.StartPosition, segment);
                }

                start += entry.Length;
            }

            SegmentPreCache = preCache;
        }

        public IPageFile CreatePageFile(string path, PagingFileEntry entry)
        {
            Interlocked.Increment(ref FileCount);
            DebugLog($"Creating page file: Length={entry.Length} Path={path}");
            var state = Accessor.CreateState(path, entry);
            return new PageFile(this, state, path, entry);
        }

        private PageFileSegment GetSegment(PageFile file, long position, int? explicitLength)
        {
            long alignedPosition = AlignPosition(position);
            if (alignedPosition >= file.Entry.Length)
            {
                return PageFileSegment.Empty;
            }

            PageSegmentKey cacheKey = new(file.Path, alignedPosition);
            PageFileSegment segment;

            var segmentLength = explicitLength ?? PageSize;
            segmentLength = (int)Math.Min(segmentLength, file.Entry.Length - alignedPosition);

            if (SegmentCache.Limit > 0)
            {
                lock (SegmentCache)
                {
                    if (SegmentCache.TryGetValue(cacheKey, out segment))
                    {
                        if (segment.Contains(position))
                        {
                            DebugLog($"Read segment: Length={segment.Length} ({alignedPosition}: cached) {file.Path}");
                            return segment;
                        }
                    }
                }
            }

            if (SegmentPreCache.TryGetSegment(cacheKey, segmentLength, out segment))
            {
                if (segment.Contains(position))
                {
                    DebugLog($"Read segment: Length={segment.Length} ({alignedPosition}: precached) {file.Path}");
                    return segment;
                }
            }

            Placeholder.DebugLog3($"START: GetSegment: {file.Path} ({position})");
            if (!_pendings.TryGetValue(cacheKey, out var pendingSegment))
            {
                Threads.Value = Thread.CurrentThread;
                var diag = new ThreadDiag(Thread.CurrentThread.ManagedThreadId, file.Path, position);
                try
                {
                    ThreadDiags.Value = diag;
                    pendingSegment = file.State.GetSegment(alignedPosition, segmentLength);
                }
                catch (Exception ex)
                {
                    diag.ExceptionText = ex.ToString();
                }
                finally
                {
                    diag.IsActive = false;
                }

                //pendingSegment = file.State.GetSegmentAsync(alignedPosition, segmentLength);
                _pendings.TryAdd(cacheKey, pendingSegment);
            }

            Placeholder.DebugLog3($"END: GetSegment: {file.Path} ({position})");


            Placeholder.Todo("Ensure pending segments are cleared up");

            try
            {
                segment = pendingSegment.GetValue();
            }
            catch
            {
                _pendings.TryRemove(cacheKey, out _);
                throw;
            }

            Placeholder.DebugLog3($"Read segment: Length={segment.Length} ({alignedPosition}: read) {file.Path}");

            if (SegmentCache.Limit > 0 && segmentLength <= PageSize)
            {
                lock (SegmentCache)
                {
                    SegmentCache.Add(cacheKey, segment);
                }
            }

            SegmentPreCache.OnLoadedSegment(cacheKey, segment);

            Interlocked.Increment(ref file.ReadCount);
            var value = Interlocked.Increment(ref ReadCount);
            if (value > MaxPageRetrievalCount)
            {
                throw new InvalidOperationException("Retrieved too manys pages");
            }

            Interlocked.Add(ref file.TotalRead, segment.Length);
            Interlocked.Add(ref TotalRead, segment.Length);

            return segment;
        }

        private long AlignPosition(long position)
        {
            return (position / PageSize) * PageSize;
        }

        public record PageFile : IPageFile
        {
            public long ReadCount = 0;
            public long TotalRead = 0;
            public long TotalRequested = 0;

            public bool Disposed;
            public int Owners;

            public PageFile(CachingPageFileProvider provider, IPageFileState state, string path, PagingFileEntry entry)
            {
                State = state;
                Provider = provider;
                Path = path;
                Entry = entry;
            }

            public long Length => Entry.Length;

            public IPageFileState State { get; init; }
            public CachingPageFileProvider Provider { get; }
            public string Path { get; }
            public PagingFileEntry Entry { get; }

            public IPageFile CreateClone()
            {
                return this with
                {
                    State = State.CreateClone() ?? Provider.Accessor.CreateState(Path, Entry),
                    _contiguousSegmentCount = 0
                };
            }

            public override string ToString()
            {
                return $"Length={Entry.Length} Path={Path}";
            }

            public virtual void Dispose()
            {
                DebugLog($"Disposing Path={Path}");
                Disposed = true;
                (State as IDisposable)?.Dispose();
            }

            public int ReadRange(long position, byte[] buffer, int offset, int count)
            {
                Interlocked.Add(ref Provider.TotalRequested, count);
                Interlocked.Add(ref TotalRequested, count);

                long startCount = count;
                long startPosition = position;
                DebugLog($"Reading range pos={position} count={count} Path={Path}");

                while (count > 0)
                {
                    PageFileSegment segment = GetSegment(position);

                    if (segment.Length == 0)
                    {
                        DebugLog($"ERROR: Reading segment pos={position} count={count} Path={Path} start={segment.Start} length={segment.Length}");
                        break;
                    }

                    DebugLog($"Read segment pos={position} count={count} Path={Path} start={segment.Start} length={segment.Length}");

                    segment.CopyTo(ref position, buffer, ref offset, ref count);
                }

                DebugLog($"Finished reading range pos={position} count={count} Path={Path}");
                DebugLog2($"Read stats: File=[Count={ReadCount} Req={TotalRequested} Read={TotalRead}] All=[Count={Provider.ReadCount} Req={Provider.TotalRequested} Read={Provider.TotalRead}] ({startPosition}, {startCount}:{Path})");

                return (int)(position - startPosition);
            }

            private int _contiguousSegmentCount = 0;
            private PageFileSegment _lastSegment = PageFileSegment.Empty;

            private PageFileSegment GetSegment(long position)
            {
                int? explicitLength = null;
                if (_lastSegment.Contains(position))
                {
                    return _lastSegment;
                }
                else if (_lastSegment.End == position && position != 0)
                {
                    _contiguousSegmentCount++;
                    if (_contiguousSegmentCount >= Provider.ContiguousRangeDetectionThreshold)
                    {
                        explicitLength = Math.Min(_lastSegment.Length * 2, Provider.MaxContiguousRangeSize);
                    }
                }
                else
                {
                    _contiguousSegmentCount = 1;
                }

                var segment = Provider.GetSegment(this, position, explicitLength);
                _lastSegment = segment;
                return segment;
            }
        }
    }
}
