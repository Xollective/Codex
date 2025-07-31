using Tenray.ZoneTree.Collections;
using Tenray.ZoneTree.Exceptions;
using Tenray.ZoneTree.Segments;

namespace Tenray.ZoneTree.Core;

public sealed partial class ZoneTree<TKey, TValue> : IZoneTree<TKey, TValue>, IZoneTreeMaintenance<TKey, TValue>
{
    public bool ContainsKey(in TKey key)
    {
        TValue value;
        if (MutableSegment.ContainsKey(key))
        {
            if (MutableSegment.TryGet(key, out value))
                return !IsValueDeleted(value);
        }

        return TryGetFromReadOnlySegments(key, out _);
    }

    bool TryGetFromReadOnlySegments(in TKey key, out TValue value)
    {
        foreach (var segment in SegmentLayout.ReadOnlySegmentsTopFirst)
        {
            if (segment.TryGet(key, out value))
            {
                return !IsValueDeleted(value);
            }
        }

        while (true)
        {
            try
            {
                foreach (var segment in SegmentLayout.LazyValues.OrderedAllDiskSegments)
                {
                    if (segment.TryGet(key, out value))
                    {
                        return !IsValueDeleted(value);
                    }
                }

                value = default;
                return false;
            }
            catch (DiskSegmentIsDroppingException)
            {
                continue;
            }
        }
    }

    public bool TryGet(in TKey key, out TValue value)
    {
        if (MutableSegment.TryGet(key, out value))
        {
            return !IsValueDeleted(value);
        }
        return TryGetFromReadOnlySegments(in key, out value);
    }

    public bool TryGetAndUpdate(in TKey key, out TValue value, ValueUpdaterDelegate<TValue> valueUpdater)
    {
        EnsureMutable();

        if (MutableSegment.TryGet(key, out value))
        {
            if (IsValueDeleted(value))
                return false;
        }
        else if (!TryGetFromReadOnlySegments(in key, out value))
            return false;

        if (!valueUpdater(ref value))
        {
            // return true because
            // no update happened, but the value is found.
            return true;
        }
        Upsert(in key, in value);
        return true;
    }

    public bool TryAtomicGetAndUpdate(in TKey key, out TValue value, ValueUpdaterDelegate<TValue> valueUpdater)
    {
        EnsureMutable();

        lock (AtomicUpdateLock)
        {
            if (MutableSegment.TryGet(key, out value))
            {
                if (IsValueDeleted(value))
                    return false;
            }
            else if (!TryGetFromReadOnlySegments(in key, out value))
                return false;

            if (!valueUpdater(ref value))
            {
                // return true because
                // no update happened, but the value is found.
                return true;
            }
            
            Upsert(in key, in value);
            return true;
        }
    }

    public bool TryAtomicAdd(in TKey key, in TValue value)
    {
        EnsureMutable();

        lock (AtomicUpdateLock)
        {
            if (ContainsKey(key))
                return false;
            Upsert(key, value);
            return true;
        }
    }

    private void EnsureMutable()
    {
        if (IsReadOnly)
            throw new ZoneTreeIsReadOnlyException();
    }

    public bool TryAtomicUpdate(in TKey key, in TValue value)
    {
        EnsureMutable();

        lock (AtomicUpdateLock)
        {
            if (!ContainsKey(key))
                return false;
            Upsert(key, value);
            return true;
        }
    }

    public bool TryAtomicAddOrUpdate<TArg>(in TKey key, in TArg arg, ValueUpdaterDelegate<TValue, TArg> valueUpdater)
    {
        EnsureMutable();

        AddOrUpdateResult status;
        IMutableSegment<TKey, TValue> mutableSegment;
        while (true)
        {
            lock (AtomicUpdateLock)
            {
                mutableSegment = MutableSegment;
                
                if (mutableSegment.IsFrozen)
                {
                    status = AddOrUpdateResult.RETRY_SEGMENT_IS_FULL;
                }
                else if (mutableSegment.TryGet(in key, out var existing)
                    || TryGetFromReadOnlySegments(in key, out existing)
                    || true)
                {
                    if (!valueUpdater(ref existing, arg))
                        return false;
                    status = mutableSegment.Upsert(key, existing);
                }
                else
                {
                    throw new Exception("This should never be hit.");
                }
            }
            switch (status)
            {
                case AddOrUpdateResult.RETRY_SEGMENT_IS_FROZEN:
                    continue;
                case AddOrUpdateResult.RETRY_SEGMENT_IS_FULL:
                    MoveMutableSegmentForward(mutableSegment);
                    continue;
                default:
                    return status == AddOrUpdateResult.ADDED;
            }
        }
    }

    public void AtomicUpsert(in TKey key, in TValue value)
    {
        EnsureMutable();

        lock (AtomicUpdateLock)
        {
            Upsert(in key, in value);
        }
    }

    public void Upsert(in TKey key, in TValue value)
    {
        EnsureMutable();

        while (true)
        {
            var mutableSegment = MutableSegment;
            var status = mutableSegment.Upsert(key, value);
            switch (status)
            {
                case AddOrUpdateResult.RETRY_SEGMENT_IS_FROZEN:
                    continue;
                case AddOrUpdateResult.RETRY_SEGMENT_IS_FULL:
                    MoveMutableSegmentForward(mutableSegment);
                    continue;
                default:
                    return;
            }
        }
    }

    public bool TryDelete(in TKey key)
    {
        EnsureMutable();

        if (!ContainsKey(key))
            return false;
        ForceDelete(in key);
        return true;
    }

    public void ForceDelete(in TKey key)
    {
        EnsureMutable();

        while (true)
        {
            var mutableSegment = MutableSegment;
            var status = mutableSegment.Delete(key);
            switch (status)
            {
                case AddOrUpdateResult.RETRY_SEGMENT_IS_FROZEN:
                    continue;
                case AddOrUpdateResult.RETRY_SEGMENT_IS_FULL:
                    MoveMutableSegmentForward(mutableSegment);
                    continue;
                default: return;
            }

        }
    }
}
