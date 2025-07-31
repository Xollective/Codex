using System.Collections.Immutable;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using Codex.Logging;
using Codex.Utilities;
using Lucene.Net.Index;

namespace Codex.Lucene;

using static CodexConstants;

public interface IndexMergerOperations<IndexReader, IndexWriter, SegmentReader, TOps>
    where TOps : IndexMergerOperations<IndexReader, IndexWriter, SegmentReader, TOps>
{
    static abstract string Name(SegmentReader segment);

    //static abstract IndexWriter AddIndexes(
    //    IndexWriter targetWriter, 
    //    IndexMerger<IndexReader, IndexWriter, SegmentReader, TOps>.IndexSegmentDataMap segmentMap);

    static abstract IEnumerable<SegmentReader> GetLeafSegments(IndexReader reader);

    static abstract (long Size, int FileCount) GetFileInfo(SegmentReader segment);

    static abstract long GetMergePriority(SegmentReader segment);
}

public abstract record IndexMerger<IndexReader, IndexWriter, SegmentReader, TOps>(Logger Logger = null)
    where TOps : IndexMergerOperations<IndexReader, IndexWriter, SegmentReader, TOps>
{
    public Logger Logger { get; set; } = Logger ?? SdkFeatures.GetGlobalLogger() ?? Logger.Null;

    public int LoadFactor { get; set; } = LuceneFeatures.IndexMergeLoadFactor;

    public int MaxMergeableBucket { get; set; } = int.MaxValue;

    protected abstract IndexWriter AddIndexes(
        IndexWriter targetWriter,
        IndexData mergeData);

    public IndexData MergeIndices(string name, IndexReader sourceReader, IndexReader targetReader, IndexWriter targetWriter)
    {
        var sourceDataMap = IndexSegmentDataMap.Create(sourceReader, IndexLocation.Source);
        var targetDataMap = IndexSegmentDataMap.Create(targetReader, IndexLocation.Target);
        var allDataMap = sourceDataMap.Combine(targetDataMap);

        var targetSegmentsToMerge = new List<IndexSegmentData>();
        bool mergeOverflow = false;

        var targetBucket = targetDataMap.SegmentsByBucket[sourceDataMap.Bucket];
        if (sourceDataMap.Bucket <= MaxMergeableBucket && targetBucket.Count() >= LoadFactor)
        {
            var mergeSize = sourceDataMap.ExactSizeMb;
            mergeOverflow = true;
            foreach (var targetSegment in targetDataMap.Segments.Values.OrderByDescending(s => s.MergePriority))
            {
                if (targetSegment.Bucket > sourceDataMap.Bucket)
                {
                    // Skip segments which are larger than current segment
                    continue;
                }

                mergeSize += targetSegment.ExactSizeMb;
                targetSegmentsToMerge.Add(targetSegment);
                if (GetBucket(mergeSize) > sourceDataMap.Bucket || targetSegment.Bucket == sourceDataMap.Bucket)
                {
                    // Once merged result would end up in different bucket or consume segment in current bucket,
                    // then stop
                    break;
                }
            }    
        }

        var allMergedSegments = sourceDataMap.Segments.Values.Concat(targetSegmentsToMerge).ToArray();
        var data = new IndexData(name, sourceDataMap, targetDataMap, IndexSegmentDataMap.Create(allMergedSegments));

        if (allMergedSegments.Length > 0)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Logger.LogMessage($"Merge {name}: Starting. {data}");
            if (mergeOverflow)
            {
                Logger.LogMessage($"Merge {name}: Found overflow in target bucket {sourceDataMap.Bucket}.");
            }

            var writer = AddIndexes(targetWriter, data);
            Logger.LogMessage($"Merge {name}: Completed ({sw.Elapsed}). {data}");
            Logger.LogMessage($"Merge {name}: Final Segments ({sw.Elapsed}). {data}");
        }
        else
        {
            Logger.LogMessage($"Merge {name}: Skipped. {data}");
        }

        return data;
    }

    private static double Truncate(double value) => ((int)(value * 1000)) / 1000.0;

    public record IndexData(string Name,
        IndexSegmentDataMap SourceSegments,
        IndexSegmentDataMap TargetSegments,
        IndexSegmentDataMap MergedSegments)
    {
        public double SourceSizeMb => SourceSegments.Segments.Sum(e => e.Value.Size) / (double)BytesInMb;
        public double TargetSizeMb => TargetSegments.Segments.Sum(e => e.Value.Size) / (double)BytesInMb;
        public double MergedSizeMb => AllMergedSegments.Sum(e => GetData(e).Size) / (double)BytesInMb;

        public SegmentReader[] AllMergedSegments { get; } = MergedSegments.Segments.Keys.ToArray();


        public IndexSegmentData GetData(SegmentReader segment)
        {
            return TargetSegments.Segments.GetValueOrDefault(segment)
                ?? SourceSegments.Segments[segment];
        }

        public string GetName(IndexSegmentData d)
        {
            var pre = TargetSegments.Segments.ContainsKey(d.Reader) ? "t" : "s";
            return $"{pre}:{d.Name}(B#{d.Bucket}:{Truncate(d.SizeMb)}mb)";
        }

        public override string ToString()
        {
            return String.Join(", ",
                $"SourceCount={Truncate(SourceSegments.Segments.Count)}",
                $"TargetCount={Truncate(TargetSegments.Segments.Count)}",
                $"SourceSizeMb=(B:{SourceSegments.Bucket}){Truncate(SourceSegments.ExactSizeMb)}",
                $"TargetSizeMb={Truncate(TargetSegments.ExactSizeMb)}",
                $"AllMergedSegments=({AllMergedSegments.Length})[{string.Join(", ", MergedSegments.Segments.Values.Select(GetName))}]",
                $"AllMergedSegmentsSizeMb={Truncate(MergedSizeMb)}",
                Environment.NewLine +
                $"SourceBuckets={SourceSegments.GetBucketsString()}",
                $"TargetBuckets={TargetSegments.GetBucketsString()}");
        }

    }

    public record IndexSegmentDataMap(ImmutableDictionary<SegmentReader, IndexSegmentData> Segments)
    {
        public long SizeMb => Segments.Values.Sum(e => e.SizeMb);

        public double ExactSizeMb => Segments.Sum(e => e.Value.Size) / (double) BytesInMb;

        public int MaxBucket { get; } = Segments.Values.Max(e => (int?)e.Bucket) ?? 0;
        public int MinBucket { get; } = Segments.Values.Min(e => (int?)e.Bucket) ?? 0;

        public int Bucket => GetBucket(SizeMb);

        public ILookup<int, IndexSegmentData> SegmentsByBucket { get; } = Segments.Values.ToLookup(s => s.Bucket);

        public override string ToString()
        {
            return $"[{string.Join(", ", Segments.Select(e => TOps.Name(e.Key)))}]";
        }

        public string GetBucketsString()
        {
            return $"[{string.Join(", ", SegmentsByBucket.OrderBy(e => e.Key).Select(e => $"{e.Key}: {e.Count()} ({e.Sum(d => d.SizeMb)}mb)"))}]";
        }

        public IndexSegmentDataMap Combine(IndexSegmentDataMap other)
        {
            return new(Segments.SetItems(other.Segments));
        }

        public static IndexSegmentDataMap Create(IndexReader reader, IndexLocation location)
        {
            return Create(TOps.GetLeafSegments(reader), location);
        }

        public static IndexSegmentDataMap Create(IEnumerable<SegmentReader> segments, IndexLocation location)
        {
            return Create(segments.Select(r => IndexSegmentData.GetData(r, location)));
        }

        public static IndexSegmentDataMap Create(IEnumerable<IndexSegmentData> segments)
        {
            return new(segments.ToImmutableDictionary(r => r.Reader));
        }
    }

    public enum IndexLocation
    {
        Source,
        Target
    }

    public record IndexSegmentData(SegmentReader Reader, long Size, int FileCount, IndexLocation Location)
    {
        public long SizeMb => Size / BytesInMb;

        public double ExactSizeMb => Size / BytesInMb;

        public int Bucket => GetBucket(SizeMb);

        public string Name => TOps.Name(Reader);

        public long MergePriority { get; } = TOps.GetMergePriority(Reader);

        public static IndexSegmentData GetData(SegmentReader reader, IndexLocation location)
        {
            var info = TOps.GetFileInfo(reader);
            return new IndexSegmentData(reader, info.Size, info.FileCount, location);
        }
    }

    public static int GetBucket(double sizeMb)
    {
        sizeMb = Math.Max(sizeMb, 16);
        var t = sizeMb / 16;
        var tl = Math.Ceiling(Math.Log(t, 4));
        return (int)tl;
    }
}