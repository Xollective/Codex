using System.Diagnostics;
using Tenray.ZoneTree.Collections;
using Tenray.ZoneTree.Options;
using Tenray.ZoneTree.Segments;
using Tenray.ZoneTree.Segments.Disk;

namespace Tenray.ZoneTree.Core;

public sealed partial class ZoneTree<TKey, TValue> : IZoneTree<TKey, TValue>, IZoneTreeMaintenance<TKey, TValue>
{
    private void UpdateLayoutAtomic(Func<SegmentLayout<TKey, TValue>, SegmentLayout<TKey, TValue>> updateLayout)
    {
        lock(UpdateLayoutLock)
        {
            SegmentLayout = updateLayout(SegmentLayout);
        }
    }

    /// <summary>
    /// Moves mutable segment into readonly segment.
    /// This will clear the writable region of the LSM tree.
    /// This method is thread-safe and can be called from many threads.
    /// The movement only occurs if the current mutable segment
    /// is the mutable segment passed by argument.
    /// </summary>
    /// <param name="mutableSegment">The mutable segment to move forward.</param>
    void MoveMutableSegmentForward(IMutableSegment<TKey, TValue> mutableSegment)
    {
        lock (AtomicUpdateLock)
        {
            // move segment zero only if
            // the given mutable segment is the current mutable segment (not already moved)
            // and it is not frozen.
            if (mutableSegment.IsFrozen || mutableSegment != MutableSegment)
                return;

            //Don't move empty mutable segment.
            var c = mutableSegment.Length;
            if (c == 0)
                return;

            mutableSegment.Freeze();

            UpdateLayoutAtomic(layout => layout with
            {
                MutableSegment = new MutableSegment<TKey, TValue>(
                    Options, IncrementalIdProvider.NextId(),
                    mutableSegment.OpIndexProvider),
                ReadOnlySegmentsTopFirst = layout.ReadOnlySegmentsTopFirst.Insert(0, mutableSegment)
            });

            MetaWal.EnqueueReadOnlySegment(mutableSegment.SegmentId);
            MetaWal.NewMutableSegment(MutableSegment.SegmentId);
        }
        OnMutableSegmentMovedForward?.Invoke(this);
    }

    public void MoveMutableSegmentForward()
    {
        lock (AtomicUpdateLock)
        {
            MoveMutableSegmentForward(MutableSegment);
        }
    }

    public Thread StartMergeOperation()
    {
        if (IsMerging)
        {
            OnMergeOperationEnded?.Invoke(this, MergeResult.ANOTHER_MERGE_IS_RUNNING);
            return null;
        }
            
        OnMergeOperationStarted?.Invoke(this);
        var thread = new Thread(StartMergeOperationInternal);
        thread.Start();
        return thread;
    }

    void StartMergeOperationInternal()
    {
        if (IsMerging)
        {
            OnMergeOperationEnded?.Invoke(this, MergeResult.ANOTHER_MERGE_IS_RUNNING);
            return;
        }
        IsCancelMergeRequested = false;
        lock (LongMergerLock)
        {
            try
            {
                if (IsMerging)
                {
                    OnMergeOperationEnded?.Invoke(this, MergeResult.ANOTHER_MERGE_IS_RUNNING);
                    return;
                }
                IsMerging = true;
                var mergeResult = MergeReadOnlySegmentsInternal();
                IsMerging = false;
                OnMergeOperationEnded?.Invoke(this, mergeResult);

            }
            catch (Exception e)
            {
                Logger.LogError(e);
                OnMergeOperationEnded?.Invoke(this, MergeResult.FAILURE);
            }
            finally
            {
                IsMerging = false;
            }
        }
    }

    public void TryCancelMergeOperation()
    {
        IsCancelMergeRequested = true;
    }

    readonly int ReadOnlySegmentFullyFrozenSpinTimeout = 2000;

    private MergeResult MergeReadOnlySegmentsInternal()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        Logger.LogTrace("Merge starting.");

        var oldDiskSegment = DiskSegment;
        var segmentsToMerge = ReadOnlySegmentsTopFirst;

        if (segmentsToMerge.Length == 0)
            return MergeResult.NOTHING_TO_MERGE;

        if (segmentsToMerge.Any(x => !x.IsFullyFrozen))
        {
            SpinWait.SpinUntil(() => !segmentsToMerge.Any(x => !x.IsFullyFrozen), 
                ReadOnlySegmentFullyFrozenSpinTimeout);
            if (segmentsToMerge.Any(x => !x.IsFullyFrozen))
                return MergeResult.RETRY_READONLY_SEGMENTS_ARE_NOT_READY;
        }

        Logger.LogTrace("Merge started.");

        var hasBottomSegments = !DiskSegmentsTopFirst.IsEmpty;
        var readOnlySegmentsArray = segmentsToMerge.Select(x => x.GetSeekableIterator()).ToArray();

        var mergingSegments = new List<ISeekableIterator<TKey, TValue>>();
        mergingSegments.AddRange(readOnlySegmentsArray);
        if (oldDiskSegment is not NullDiskSegment<TKey, TValue>)
            mergingSegments.Add(oldDiskSegment.GetSeekableIterator());

        var result = MergeSegmentsCore(
            lastSegment: oldDiskSegment,
            mergingSegments: mergingSegments,
            isCanceled: () => IsCancelMergeRequested,
            writeDeletedValues: hasBottomSegments,
            out var newDiskSegment,
            out var mergeInfo);

        if (result != MergeResult.SUCCESS)
        {
            return result;
        }

        lock (ShortMergerLock)
        {
            if (newDiskSegment.Length > Options.DiskSegmentMaxItemCount)
            {
                UpdateLayoutAtomic(layout => layout with
                {
                    ReadOnlySegmentsTopFirst = layout.GetReadOnlySegmentsWithMergedSegmentsRemoved(segmentsToMerge),
                    DiskSegmentsTopFirst = SegmentLayout.DiskSegmentsTopFirst.Insert(0, newDiskSegment),
                    DiskSegment = NullDiskSegment<TKey, TValue>.Instance
                });

                MetaWal.EnqueueBottomSegment(newDiskSegment.SegmentId);
                MetaWal.NewDiskSegment(0);
            }
            else
            {
                UpdateLayoutAtomic(layout => layout with
                {
                    ReadOnlySegmentsTopFirst = layout.GetReadOnlySegmentsWithMergedSegmentsRemoved(segmentsToMerge),
                    DiskSegment = newDiskSegment
                });

                MetaWal.NewDiskSegment(newDiskSegment.SegmentId);
            }
            try
            {
                oldDiskSegment.Drop(mergeInfo.DiskSegmentCreator.AppendedPartSegmentIds);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                OnCanNotDropDiskSegment?.Invoke(oldDiskSegment, e);
            }

            foreach (var segment in segmentsToMerge.ToReverseList())
            {
                MetaWal.DequeueReadOnlySegment(segment.SegmentId);
                try
                {
                    segment.Drop();
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                    OnCanNotDropReadOnlySegment?.Invoke(segment, e);
                }
            }
        }

        TotalSkipCount += mergeInfo.SkipCount;
        TotalDropCount += mergeInfo.DropCount;
        Logger.LogTrace(
            new LogMergerSuccess(
                mergeInfo.DropCount,
                mergeInfo.SkipCount,
                stopwatch.ElapsedMilliseconds,
                TotalDropCount,
                TotalSkipCount));

        OnDiskSegmentActivated?.Invoke(this, newDiskSegment, false);
        return MergeResult.SUCCESS;
    }

    private record MergeInfo(IDiskSegmentCreator<TKey, TValue> DiskSegmentCreator, int SkipCount, int DropCount);

    private MergeResult MergeSegmentsCore(
        IDiskSegment<TKey, TValue> lastSegment,
        IReadOnlyList<ISeekableIterator<TKey, TValue>> mergingSegments,
        Func<bool> isCanceled,
        bool writeDeletedValues,
        out IDiskSegment<TKey, TValue> newMergedSegment,
        out MergeInfo mergeInfo)
    {
        newMergedSegment = default;
        mergeInfo = null;

        if (isCanceled())
        {
            return MergeResult.CANCELLED_BY_USER;
        }

        var enableMultiPartDiskSegment =
            Options.DiskSegmentOptions.DiskSegmentMode == DiskSegmentMode.MultiPartDiskSegment;

        var len = mergingSegments.Count;
        var bottomIndex = len - 1;

        using IDiskSegmentCreator<TKey, TValue> diskSegmentCreator =
            enableMultiPartDiskSegment ?
            new MultiPartDiskSegmentCreator<TKey, TValue>(Options, IncrementalIdProvider) :
            new DiskSegmentCreator<TKey, TValue>(Options, IncrementalIdProvider);

        var heap = new FixedSizeMinHeap<HeapEntry<TKey, TValue>>(len + 1, MinHeapEntryComparer);

        var fillHeap = () =>
        {
            for (int i = 0; i < len; i++)
            {
                var s = mergingSegments[i];
                if (!s.Next())
                    continue;
                var key = s.CurrentKey;
                var value = s.CurrentValue;
                var entry = new HeapEntry<TKey, TValue>(key, value, i);
                heap.Insert(entry);
            }
        };

        int minSegmentIndex = 0;

        var skipElement = () =>
        {
            var minSegment = mergingSegments[minSegmentIndex];
            if (minSegment.Next())
            {
                var key = minSegment.CurrentKey;
                var value = minSegment.CurrentValue;
                heap.ReplaceMin(new HeapEntry<TKey, TValue>(key, value, minSegmentIndex));
            }
            else
            {
                heap.RemoveMin();
            }
        };
        fillHeap();
        var comparer = Options.Comparer;
        var hasPrev = false;
        TKey prevKey = default;

        var firstKeysOfEveryPart = lastSegment.GetFirstKeysOfEveryPart();
        var lastKeysOfEveryPart = lastSegment.GetLastKeysOfEveryPart();
        var lastValuesOfEveryPart = lastSegment.GetLastValuesOfEveryPart();
        var diskSegmentMinimumRecordCount = Options.DiskSegmentOptions.MinimumRecordCount;

        var dropCount = 0;
        var skipCount = 0;
        while (heap.Count > 0)
        {
            if (isCanceled())
            {
                try
                {
                    diskSegmentCreator.DropDiskSegment();
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                    OnCanNotDropDiskSegmentCreator?.Invoke(diskSegmentCreator, e);
                }

                return MergeResult.CANCELLED_BY_USER;
            }

            var minEntry = heap.MinValue;
            minSegmentIndex = minEntry.SegmentIndex;

            // ignore deleted entries if writing bottommost disk segment.
            if (!writeDeletedValues && IsValueDeleted(minEntry.Value))
            {
                skipElement();
                prevKey = minEntry.Key;
                hasPrev = true;
                continue;
            }

            if (hasPrev && comparer.Compare(minEntry.Key, prevKey) == 0)
            {
                skipElement();
                continue;
            }

            prevKey = minEntry.Key;
            hasPrev = true;
            var isDiskSegmentKey = minSegmentIndex == bottomIndex;
            var iteratorPosition = IteratorPosition.None;
            var currentPartIndex = -1;
            if (isDiskSegmentKey)
            {
                var diskIterator = mergingSegments[minSegmentIndex];
                iteratorPosition =
                    diskIterator.IsBeginningOfAPart ?
                    IteratorPosition.BeginningOfAPart :
                    diskIterator.IsEndOfAPart ?
                    IteratorPosition.EndOfAPart :
                    IteratorPosition.MiddleOfAPart;
                currentPartIndex = diskIterator.GetPartIndex();
            }

            // skip a part without merge if possible
            if (enableMultiPartDiskSegment &&
                isDiskSegmentKey &&
                iteratorPosition == IteratorPosition.BeginningOfAPart)
            {
                var part = lastSegment
                    .GetPart(currentPartIndex);
                if (part.Length > diskSegmentMinimumRecordCount &&
                    diskSegmentCreator.CanSkipCurrentPart)
                {
                    var lastKey = lastKeysOfEveryPart[currentPartIndex];
                    var islastKeySmallerThanAllOtherKeys = true;
                    var heapKeys = heap.GetKeys();
                    var heapKeysLen = heapKeys.Length;
                    for (int i = 0; i < heapKeysLen; i++)
                    {
                        var s = heapKeys[i];
                        if (s.SegmentIndex == minSegmentIndex)
                            continue;
                        var key = s.Key;
                        if (comparer.Compare(lastKey, key) >= 0)
                        {
                            islastKeySmallerThanAllOtherKeys = false;
                            break;
                        }
                    }
                    if (islastKeySmallerThanAllOtherKeys)
                    {
                        diskSegmentCreator.Append(
                            part,
                            minEntry.Key,
                            lastKey,
                            minEntry.Value,
                            lastValuesOfEveryPart[currentPartIndex]);
                        mergingSegments[bottomIndex].Skip(part.Length - 2);
                        prevKey = lastKey;
                        skipElement();
                        ++skipCount;
                        continue;
                    }
                }
                ++dropCount;
                Logger.LogTrace(new LogMergerDrop(part.SegmentId, dropCount, skipCount));

            }

            diskSegmentCreator.Append(minEntry.Key, minEntry.Value, iteratorPosition);
            skipElement();
        }

        newMergedSegment = diskSegmentCreator.CreateReadOnlyDiskSegment();
        newMergedSegment.DropFailureReporter = (ds, e) => ReportDropFailure(ds, e);
        OnDiskSegmentCreated?.Invoke(this, newMergedSegment, false);

        mergeInfo = new MergeInfo(diskSegmentCreator, SkipCount: skipCount, DropCount: dropCount);
        return MergeResult.SUCCESS;
    }

    int TotalSkipCount;
    int TotalDropCount;
}
