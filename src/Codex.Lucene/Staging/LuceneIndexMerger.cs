using System.Collections.Immutable;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using BuildXL.Utilities.Collections;
using Codex.Logging;
using Codex.Lucene.Framework;
using Codex.Utilities;
using Lucene.Net.Index;
using Lucene.Net.Index.Sorter;
using Lucene.Net.Search;

namespace Codex.Lucene;

using D = SearchMappings.Definition;

public record LuceneIndexMerger(Logger Logger) :
    IndexMerger<IndexReader, IndexWriter, SegmentReader, LuceneIndexMerger>(Logger),
    IndexMergerOperations<IndexReader, IndexWriter, SegmentReader, LuceneIndexMerger>
{
    protected override IndexWriter AddIndexes(IndexWriter targetWriter, IndexData data)
    {
        var segmentMap = data.MergedSegments;
        var stableIdField = nameof(ISearchEntity.StableId);

        var segments = segmentMap.Segments.Keys.ToArray();

        var firstStableIds = segments.SelectArray(s =>
        {
            var dv = s.GetNumericDocValues(stableIdField);
            return dv.Get(0);
        });

        Array.Sort(keys: firstStableIds, segments);

        var sorter = new Sorter(new Sort(new SortField(stableIdField, SortFieldType.INT64)));

        var segmentsToSort = segments.Length == 1 ? segments : new AtomicReader[] { new AutoPrefixCompositeAtomicReader(SlowCompositeReaderWrapper.Wrap(new MultiReader(segments))) };
        var sortedView = Sorter.GetSortedMergeReader(segmentsToSort, sorter, out var docMap);

        var stableIdExtents = new List<(int StartStableId, Extent DocExtent)>();

        var sortedSegments = docMap == null ? segments : new[] { sortedView };

        if (Features.ComputeStableIdExtents)
        {
            //if (data.Name == SearchTypes.Definition.Name)
            //{
            //    foreach (var segment in segments)
            //    {
            //        var shortNameTerms = segment.GetTerms(D.ShortName.Name)?.GetEnumerator();
            //        if (shortNameTerms != null)
            //        {
            //            Logger.LogMessage($"#Segment {segment.Name} terms:\n{string.Join("\n", shortNameTerms.Enumerate().Select(v => v.GetString()).Take(100))}\n\n");
            //        }
            //    }
            //}

            //var shortNameTerms = sortedView.GetTerms(D.ShortName.Name)?.GetEnumerator();
            //if (shortNameTerms != null)
            //{
            //    shortNameTerms = SetTermsEnum.Create(shortNameTerms, includeDocs: false);
            //}

            var stableIdsDocValues = sortedView.GetNumericDocValues(stableIdField);

            (int Start, int Expected) stableId = (-1, -1);
            int docStart = 0;
            for (int i = 0; i <= sortedView.MaxDoc; i++)
            {
                int currentStableId = i == sortedView.MaxDoc ? -2 : (int)stableIdsDocValues.Get(i);
                if (currentStableId != stableId.Expected)
                {
                    if (docStart != i)
                    {
                        var docExtent = Extent.FromBounds(docStart, i);
                        stableIdExtents.Add((stableId.Start, docExtent));
                    }

                    stableId = (currentStableId, currentStableId);
                    docStart = i;
                }

                stableId.Expected++;
            }
        }

        // First pass to merge the segments
        targetWriter.AddIndexes(sortedSegments, segmentMap.Segments.Values.Where(s => s.Location == IndexLocation.Target).Select(s => s.Reader.SegmentInfo));

        //if (Features.IsTest)
        //{
        //    if (data.Name == SearchTypes.Definition.Name)
        //    {
        //        using var reader = targetWriter.GetReader(applyAllDeletes: false);
        //        var segment = reader.GetLeafSegments().MaxBy(s => s.SegmentInfo.Info.Ordinal);

        //        var shortNameTerms = segment.GetTerms(D.ShortName.Name)?.GetEnumerator();
        //        if (shortNameTerms != null)
        //        {
        //            Logger.LogMessage($"#Merge segment terms:\n{string.Join("\n", shortNameTerms.Enumerate().Select(v => v.GetString()).Take(100))}\n\n");
        //        }
        //    }
        //}

        return targetWriter;
    }

    public static (long Size, int FileCount) GetFileInfo(SegmentReader segment)
    {
        var files = segment.SegmentInfo.GetFiles();
        var size = files.Sum(fileName => segment.Directory.FileLength(fileName));

        return (Size: size, FileCount: files.Count);
    }

    public static IEnumerable<SegmentReader> GetLeafSegments(IndexReader reader)
    {
        return reader.GetLeafSegments();
    }

    public static long GetMergePriority(SegmentReader segment)
    {
        return segment.GetNumericDocValues(nameof(ISearchEntity.StableId))?.Get(segment.MaxDoc - 1) ?? 0;
    }

    public static string Name(SegmentReader segment)
    {
        return segment.Name;
    }
}