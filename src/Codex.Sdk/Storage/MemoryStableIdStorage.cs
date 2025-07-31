using BuildXL.Utilities.Collections;
using Codex.ObjectModel;
using Codex.Utilities;

namespace Codex.Storage;

public class MemoryStableIdStorage : IStableIdStorage
{
    public readonly ConcurrentBigMap<(SearchTypeId type, ShortHash hash), DocumentRef> Map = new();

    private LazySearchTypesMap<Counter> CounterMap = new(s => new());

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public IEnumerable<string> GetPendingDeletions()
    {
        return Array.Empty<string>();
    }

    public void Initialize(StableIdStorageHeader header)
    {
    }

    public bool TryGet(SearchType searchType, ShortHash entityUid, out DocumentRef docRef)
    {
        return Map.TryGetValue((searchType.TypeId, entityUid), out docRef);
    }

    public bool TryReserve(SearchType searchType, ShortHash entityUid, out DocumentRef docRef)
    {
        var counter = CounterMap[searchType];
        var result = Map.GetOrAdd((searchType.TypeId, entityUid), counter, static (k, d) => Interlocked.Increment(ref d.Value));
        docRef = result.Item.Value;
        return result.ItemWasAdded;
    }

    public void UnsafePut(SearchType searchType, ShortHash entityUid, DocumentRef docRef)
    {
        Map[(searchType.TypeId, entityUid)] = docRef;
    }

    private class Counter
    {
        public int Value;
    }
}
