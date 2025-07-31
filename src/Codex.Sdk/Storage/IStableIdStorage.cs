using System.Collections.Concurrent;
using Codex.Utilities;

namespace Codex.Storage;

public interface IStableIdStorage : IReadOnlyStableIdStorage
{
    IEnumerable<string> GetPendingDeletions();

    bool TryReserve(SearchType searchType, ShortHash entityUid, out DocumentRef docRef);

    void UnsafePut(SearchType searchType, ShortHash entityUid, DocumentRef docRef);
}

public interface IReadOnlyStableIdStorage : IAsyncDisposable
{
    void Initialize(StableIdStorageHeader header);

    bool TryGet(SearchType searchType, ShortHash entityUid, out DocumentRef docRef);
}

public record struct DocumentRef(int DocId)
{
    public static implicit operator DocumentRef(int docId) => new(docId);
}

public record StagingStableIdStorage(IStableIdStorage Inner) : NullStableIdStorage, IStableIdStorage
{
    IEnumerable<string> IStableIdStorage.GetPendingDeletions() => Inner.GetPendingDeletions();
}

public record NullStableIdStorage : IStableIdStorage
{
    public IEnumerable<string> GetPendingDeletions() => Array.Empty<string>();

    public void Initialize(StableIdStorageHeader header)
    {
    }

    public bool TryGet(SearchType searchType, ShortHash entityUid, out DocumentRef docRef)
    {
        docRef = default;
        return false;
    }

    public bool TryReserve(SearchType searchType, ShortHash entityUid, out DocumentRef docRef)
    {
        docRef = default;
        return true;
    }

    public void UnsafePut(SearchType searchType, ShortHash entityUid, DocumentRef docRef)
    {
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}

public class StableIdStorageHeader
{
    public Dictionary<string, SerializedInfo> Infos { get; set; } = new();

    public class SerializedInfo
    {
        private int _nextStableId;
        public int NextStableDocId { get => _nextStableId; set => _nextStableId = value; }

        public ConcurrentQueue<int> AvailableStableIds { get; set; } = new ConcurrentQueue<int>();

        public int ReserveStableId()
        {
            if (!AvailableStableIds.TryDequeue(out int stableId))
            {
                stableId = Interlocked.Increment(ref _nextStableId);
            }

            return stableId;
        }

        public void ReturnStableId(int unusedStableId)
        {
            AvailableStableIds.Enqueue(unusedStableId);
        }
    }
}
