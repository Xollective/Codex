using BuildXL.Utilities.Collections;
using Codex.Storage.ZoneTree;
using Codex.Utilities;
using Codex.Utilities.Serialization;
using Tenray.ZoneTree;
using Tenray.ZoneTree.AbstractFileStream;
using Tenray.ZoneTree.Comparers;
using Tenray.ZoneTree.Options;
using Tenray.ZoneTree.Serializers;
using static Codex.Storage.StableIdStorageHeader;

namespace Codex.Storage;

public record ZoneTreeStableIdStorage(
    string Directory,
    string StagingDirectory = null,
    Func<(string rootDirectory, string virtualRoot), RemapFileStreamProvider> GetBackingProvider = null) 
    : ZoneTreeStorageBase<ShortHash, DocumentRef>(Directory, StagingDirectory, GetBackingProvider), IStableIdStorage
{
    private StableIdStorageHeader _header;

    private LockSet _locks = LockSet.Create(Environment.ProcessorCount * 16);

    public SerializedInfo[] Columns { get; private set; }

    protected override ISerializer<ShortHash> KeySerializer { get; } = new StructBytesSerializer<ShortHash>();

    protected override IRefComparer<ShortHash> KeySorter { get; } = new StructBytesSerializer<ShortHash>();

    protected override ISerializer<DocumentRef> ValueSerializer { get; } = new StructBytesSerializer<DocumentRef>();

    public void Initialize(StableIdStorageHeader header)
    {
        _header = header;

        base.Initialize();

        Columns = new LazySearchTypesMap<SerializedInfo>(searchType =>
        {
            return _header.Infos.GetOrAdd(searchType.Name, new());
        }, initializeAll: true).Enumerate().Select(e => e.Value).ToArray();
    }

    public override ValueTask DisposeAsync()
    {
        return base.DisposeAsync();
    }

    IEnumerable<string> IStableIdStorage.GetPendingDeletions() => GetPendingDeletions();

    public bool TryGet(SearchType searchType, ShortHash entityUid, out DocumentRef docRef)
    {
        ShortHash key = GetKey(searchType, entityUid);
        if (Database.TryGet(key, out docRef))
        {
            return true;
        }

        return false;
    }

    private static ShortHash GetKey(SearchType searchType, ShortHash shortHash)
    {
        shortHash[0] = (byte)searchType.Id;
        return shortHash;
    }

    public bool TryReserve(SearchType searchType, ShortHash entityUid, out DocumentRef docRef)
    {
        var column = Columns[searchType.Id];

        if (TryGet(searchType, entityUid, out var dbDocRef))
        {
            docRef = dbDocRef;
            return false;
        }
        else
        {
            var key = GetKey(searchType, entityUid);
            var hashCode = (uint)key.GetHashCode();
            using (_locks.AcquireWriteLock(hashCode % (uint)_locks.Length))
            {
                if (!Database.TryGet(key, out docRef))
                {
                    docRef = new DocumentRef(column.ReserveStableId());
                    Database.Upsert(key, docRef);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }

    public void UnsafePut(SearchType searchType, ShortHash entityUid, DocumentRef docRef)
    {
        var key = GetKey(searchType, entityUid);
        Database.Upsert(key, docRef);
    }
}
