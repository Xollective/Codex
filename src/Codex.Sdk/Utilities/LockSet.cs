using BuildXL.Utilities.Threading;

namespace Codex.Utilities;

/// <summary>
/// Helper class for managing locks and corresponding write counts for a ConcurrentBigSet
/// </summary>
public sealed class LockSet
{
    // A set of reader-writer locks, each guarding a section of the table
    private readonly (ReadWriteLock Lock, int WriteCount)[] m_locks;

    private LockSet(int concurrencyLevel)
    {
        m_locks = new (ReadWriteLock, int WriteCount)[concurrencyLevel];

        for (int i = 0; i < m_locks.Length; i++)
        {
            m_locks[i] = (ReadWriteLock.Create(), 0);
        }
    }

    public static LockSet Create(int concurrencyLevelHint)
    {
        return new LockSet(HashCodeHelper.GetGreaterOrEqualPrime(concurrencyLevelHint));
    }

    /// <summary>
    /// Gets the number of locks.
    /// </summary>
    public int Length => m_locks.Length;

    /// <summary>
    /// Acquires the write lock at the given index. Optionally allowing concurrent reads while holding the write lock. Also,
    /// increments the write count for the lock and returns the prior write count
    /// </summary>
    public WriteLock AcquireWriteLock(uint lockNo, bool allowReads = false)
    {
        ref var entry = ref m_locks[lockNo];
        Interlocked.Increment(ref entry.WriteCount);
        var writeLock = entry.Lock.AcquireWriteLock(allowReads);
        return writeLock;
    }

    /// <summary>
    /// Acquires a read lock at the given index and returns the write count for the lock.
    /// </summary>
    public ReadLock AcquireReadLock(uint lockNo)
    {
        ref var entry = ref m_locks[lockNo];
        return entry.Lock.AcquireReadLock();
    }
}