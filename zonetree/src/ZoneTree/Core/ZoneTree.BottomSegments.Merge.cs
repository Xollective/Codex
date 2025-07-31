using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using Tenray.ZoneTree.Collections;
using Tenray.ZoneTree.Exceptions;
using Tenray.ZoneTree.Options;
using Tenray.ZoneTree.Segments;
using Tenray.ZoneTree.Segments.Disk;

namespace Tenray.ZoneTree.Core;

public sealed partial class ZoneTree<TKey, TValue> : IZoneTree<TKey, TValue>, IZoneTreeMaintenance<TKey, TValue>
{
    public Thread StartBottomSegmentsMergeOperation(int from, int to)
    {
        if (from >= to)
        {
            throw new InvalidMergeRangeException(from, to);
        }
        if (IsBottomSegmentsMerging)
        {
            OnBottomSegmentsMergeOperationEnded?.Invoke(this, MergeResult.ANOTHER_MERGE_IS_RUNNING);
            return null;
        }

        OnBottomSegmentsMergeOperationStarted?.Invoke(this);
        var thread = new Thread(() => 
            StartBottomSegmentsMergeOperationInternal(from, to));
        thread.Start();
        return thread;
    }

    void StartBottomSegmentsMergeOperationInternal(int from, int to)
    {
        if (IsBottomSegmentsMerging)
        {
            OnBottomSegmentsMergeOperationEnded?.Invoke(this, MergeResult.ANOTHER_MERGE_IS_RUNNING);
            return;
        }
        IsCancelBottomSegmentsMergeRequested = false;
        lock (LongBottomSegmentsMergerLock)
        {
            try
            {
                if (IsBottomSegmentsMerging)
                {
                    OnBottomSegmentsMergeOperationEnded?.Invoke(this, MergeResult.ANOTHER_MERGE_IS_RUNNING);
                    return;
                }
                IsBottomSegmentsMerging = true;
                var mergeResult = MergeBottomSegmentsInternal(from, to);
                IsBottomSegmentsMerging = false;
                OnBottomSegmentsMergeOperationEnded?.Invoke(this, mergeResult);

            }
            catch (Exception e)
            {
                Logger.LogError(e);
                OnBottomSegmentsMergeOperationEnded?.Invoke(this, MergeResult.FAILURE);
            }
            finally
            {
                IsBottomSegmentsMerging = false;
            }
        }
    }

    public void TryCancelBottomSegmentsMergeOperation()
    {
        IsCancelBottomSegmentsMergeRequested = true;
    }

    MergeResult MergeBottomSegmentsInternal(int from, int to)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var bottomSegments = SegmentLayout.DiskSegmentsTopFirst;
        var bottomSegmentsToMerge = bottomSegments.AsSpan()[from..(to + 1)].ToArray();

        var writeDeletedValues = from > 0;

        var mergingSegments = bottomSegmentsToMerge
            .Select(x => x.GetSeekableIterator())
            .ToArray();
        to = from + mergingSegments.Length - 1;
        if (mergingSegments.Length == 0)
            return MergeResult.NOTHING_TO_MERGE;
        var bottomDiskSegment = bottomSegmentsToMerge[0];
        Logger.LogTrace($"Bottom Segments Merge started." +
            $" from: {from} - to: {to} out of: {bottomSegments.Length} ");

        if (mergingSegments.Length == 0)
            return MergeResult.NOTHING_TO_MERGE;

        var result = MergeSegmentsCore(
            lastSegment: bottomDiskSegment,
            mergingSegments: mergingSegments,
            isCanceled: () => IsCancelBottomSegmentsMergeRequested,
            writeDeletedValues: writeDeletedValues,
            out var newDiskSegment,
            out var mergeInfo);

        if (result != MergeResult.SUCCESS)
        {
            return result;
        }

        lock (ShortMergerLock)
        {
            var bottomSegmentsLength = bottomSegments.Length;
            var newBottomSegments = new List<IDiskSegment<TKey, TValue>>();
            for(var i = 0; i < bottomSegmentsLength; ++i)
            {
                var ri = bottomSegmentsLength - i - 1;
                if (i > from && i <= to)
                {
                    MetaWal.DeleteBottomSegment(bottomSegments[i].SegmentId);
                    continue;
                }
                if (i == from)
                {
                    MetaWal.InsertBottomSegment(newDiskSegment.SegmentId, i);
                    MetaWal.DeleteBottomSegment(bottomSegments[i].SegmentId);
                    newBottomSegments.Add(newDiskSegment);
                }
                else
                {
                    newBottomSegments.Add(bottomSegments[i]);
                }
            }

            UpdateLayoutAtomic(layout => layout with
            {
                DiskSegmentsTopFirst = newBottomSegments.ToImmutableArray()
            });

            for (var i = from; i <= to; ++i)
            {
                var ri = bottomSegmentsLength - i - 1;
                var diskSegmentToDrop = bottomSegments[i];
                try
                {
                    diskSegmentToDrop.Drop(mergeInfo.DiskSegmentCreator.AppendedPartSegmentIds);
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                    OnCanNotDropDiskSegment?.Invoke(diskSegmentToDrop, e);
                }
            }
        }

        TotalBottomSegmentsMergeSkipCount += mergeInfo.SkipCount;
        TotalBottomSegmentsMergeDropCount += mergeInfo.DropCount;
        Logger.LogTrace(
            new LogMergerSuccess(
                mergeInfo.DropCount,
                mergeInfo.SkipCount,
                stopwatch.ElapsedMilliseconds,
                TotalBottomSegmentsMergeDropCount,
                TotalBottomSegmentsMergeSkipCount));

        OnDiskSegmentActivated?.Invoke(this, newDiskSegment, true);
        return MergeResult.SUCCESS;
    }

    int TotalBottomSegmentsMergeSkipCount;
    int TotalBottomSegmentsMergeDropCount;
}
