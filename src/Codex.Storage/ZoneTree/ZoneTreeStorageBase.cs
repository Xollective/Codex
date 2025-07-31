using BuildXL.Utilities.Collections;
using Codex.Storage.ZoneTree;
using Codex.Utilities;
using Tenray.ZoneTree;
using Tenray.ZoneTree.AbstractFileStream;
using Tenray.ZoneTree.Comparers;
using Tenray.ZoneTree.Options;
using Tenray.ZoneTree.Serializers;

namespace Codex.Storage;

public abstract record ZoneTreeStorageBase<TKey, TValue>(
    string Directory,
    string StagingDirectory = null,
    Func<(string rootDirectory, string virtualRoot), RemapFileStreamProvider> GetBackingProvider = null)
{
    private const string RelativeRoot = "zt";
    private string _rootDirectory;

    public IZoneTree<TKey, TValue> Database { get; private set; }
    public IMaintainer Maintainer { get; private set; }

    public RemapFileStreamProvider BackingFsProvider { get; private set; }
    public IFileStreamProvider DbFsProvider { get; private set; }

    protected virtual IEqualityComparer<TKey> KeyComparer { get; } = EqualityComparer<TKey>.Default;
    protected abstract ISerializer<TKey> KeySerializer { get; }
    protected abstract IRefComparer<TKey> KeySorter { get; }
    protected abstract ISerializer<TValue> ValueSerializer { get; }

    public void Initialize()
    {
        _rootDirectory = Path.Combine(Directory, RelativeRoot);

        var dataDirectory = _rootDirectory;
        var virtualRoot = PathUtilities.NormalizePath("//virtualdb/dir/");

        var localFsProvider = new LocalFileStreamProvider();
        BackingFsProvider = GetBackingProvider?.Invoke((rootDirectory: _rootDirectory, virtualRoot: virtualRoot))
            ?? new RemapFileStreamProvider(
            localFsProvider,
            virtualRoot,
            _rootDirectory);

        DbFsProvider = BackingFsProvider;

        if (!string.IsNullOrEmpty(StagingDirectory))
        {
            var stagingRootDirectory = Path.Combine(StagingDirectory, RelativeRoot);
            var overlayFsProvider = new RemapFileStreamProvider(
                localFsProvider,
                virtualRoot,
                stagingRootDirectory);
            DbFsProvider = new TieredFileStreamProvider(
                OverlayDirectory: overlayFsProvider,
                BackingDirectory: BackingFsProvider);
        }

        Database = new ZoneTreeFactory<TKey, TValue>(DbFsProvider)
            .SetDataDirectory(virtualRoot)
            .ConfigureWriteAheadLogOptions(o => o.WriteAheadLogMode = WriteAheadLogMode.None)
            .SetKeySerializer(KeySerializer)
            .SetValueSerializer(ValueSerializer)
            .SetComparer(KeySorter)
            .Configure(o =>
            {
                o.MutableSegmentMaxItemCount = 100_000;
                o.DiskSegmentMaxItemCount = 1_000_000;
            })
            .OpenOrCreate();

        Maintainer = Database.CreateMaintainer();
    }

    public List<KeyValuePair<TKey, TValue>> Enumerate()
    {
        using var iterator = Database.CreateIterator();
        var values = iterator.AsEnumerable().ToList();
        return values;
    }

    public IEnumerable<string> GetPendingDeletions()
    {
        if (DbFsProvider is TieredFileStreamProvider tieredProvider)
        {
            return tieredProvider.GetDeletions()
                .Select(path =>
                {
                    BackingFsProvider.RemapPath(ref path, overrideTargetRoot: "");
                    return PathUtilities.UriCombine(RelativeRoot, path);
                }).ToArray();
                
        }

        return Array.Empty<string>();
    }

    public virtual ValueTask DisposeAsync()
    {
        if (Maintainer != null)
        {
            Maintainer.CompleteRunningTasks();
            Maintainer.Dispose();
        }

        if (Database != null)
        {
            var maintenance = Database.Maintenance;
            if (maintenance.MutableSegmentRecordCount > 0)
            {
                maintenance.MoveMutableSegmentForward();

                if (!OperatingSystem.IsBrowser())
                {
                    var merge = maintenance.StartMergeOperation();
                    merge?.Join();
                }
            }

            Database.Dispose();
        }

        return ValueTask.CompletedTask;
    }
}
