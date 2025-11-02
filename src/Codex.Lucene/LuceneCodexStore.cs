using System.Collections.Concurrent;
using Codex.Logging;
using Codex.Lucene.Framework;
using Codex.Sdk;
using Codex.Sdk.Storage;
using Codex.Storage;
using Codex.Storage.BlockLevel;
using Codex.Utilities;
using Lucene.Net.Index;

namespace Codex.Lucene.Search
{
    public partial class LuceneCodexStore : ILuceneCodexStore
    {
        public LuceneWriteConfiguration Configuration { get; }
        public IStableIdStorage IdTracker => IdTrackerLazy.Value;
        public Logger Logger { get; }
        public LazySearchTypesMap<IndexWriter> Writers { get; }
        public IObjectStorage DiskStorage { get; }
        public StoredFilterUpdater StoredFilterUpdater { get; }

        public AsyncLazy<IStableIdStorage> IdTrackerLazy { get; private set; }

        public LuceneCodexStore(LuceneWriteConfiguration configuration)
        {
            Configuration = configuration;
            configuration.DebugStorage?.Initialize();

            Logger = configuration.Logger;

            Writers = Configuration.CreateWriters();

            DiskStorage = Configuration.DiskStorageOverride ?? new DiskObjectStorage(Configuration.Directory);

            if (!Configuration.DisableStoredFilterUpdates)
            {
                StoredFilterUpdater = new StoredFilterUpdater(DiskStorage, Logger, Configuration.SettingsRoot);
            }
        }

        public async Task<ICodexRepositoryStore> CreateRepositoryStore(RepositoryStoreInfo storeInfo)
        {
            var storeWriter = await CreateStoreWriterAsync(storeInfo);

            var store = CreateStore(storeInfo, storeWriter);

            await store.InitializeAsync();

            return store;
        }

        private IndexingCodexRepositoryStoreBase CreateStore(RepositoryStoreInfo storeInfo, ICodexStoreWriter storeWriter)
        {
            return new IndexingCodexRepositoryStore(storeWriter, Logger, storeInfo);
        }

        public virtual async Task<ICodexStoreWriter> CreateStoreWriterAsync(IRepositoryStoreInfo storeInfo)
        {
            ICodexStoreWriter writer = Configuration.DisableIndex
                ? new NullCodexStoreWriter()
                : new LuceneCodexStoreWriter(this, storeInfo);

            var wrapper = new StoredFilterBuilder(
                writer,
                IdTracker,
                Logger,
                Configuration,
                storeInfo,
                StoredFilterUpdater);

            return wrapper;
        }

        public virtual async Task InitializeAsync()
        {
            DiskStorage.Initialize();

            if (!Configuration.DisableStoredFilterUpdates)
            {

                await StoredFilterUpdater.InitializeAsync();

                IdTrackerLazy = new AsyncLazy<IStableIdStorage>(() => Task.Run(async () =>
                {
                    Logger?.LogMessage($"Initializing stable id tracker.");

                    Configuration.IdTracker.Initialize(StoredFilterUpdater.Header);

                    Logger?.LogMessage($"Initialized stable id tracker.");

                    return Configuration.IdTracker;
                }));

                IdTrackerLazy.Start();
            }

            await Parallel.ForEachAsync(Writers.EnumerateLazy(), (kvp, token) =>
            {
                var (searchType, lazyWriter) = kvp;
                Logger?.LogMessage($"Initializing {searchType.Name} index.");

                var writer = lazyWriter.Value;

                Logger?.LogMessage($"Initialized {searchType.Name} index.");

                return ValueTask.CompletedTask;
            });
        }

        public virtual async Task FinalizeAsync()
        {
            if (!Configuration.DisableStoredFilterUpdates)
            {
                Logger?.LogMessage($"Finalizing stable id tracker.");
                await IdTracker.DisposeAsync();

                await StoredFilterUpdater.FinalizeAsync();
            }

            List<PagingFileInfo> files = new List<PagingFileInfo>();
            List<string> allDeletedFiles = new List<string>();
            var relativeRoots = Configuration.GetRelativeRoots();

            await Parallel.ForEachAsync(Writers.Enumerate(), async (entry, token) =>
            {
                (var searchType, var writer) = entry;

                if (Configuration.DisposeWriters)
                {
                    Logger?.LogMessage($"Flushing {searchType.Name} index.");
                    writer.Flush(true, true);
                    Logger?.LogMessage($"Disposing {searchType.Name} index.");
                    writer.Dispose();
                }

                if (Features.EnableSummaryIndex)
                {
                    Logger?.LogMessage($"Updating {searchType.Name} summary index.");
                    SummaryIndexReader.Update(Configuration, searchType);
                }

                var relativeRoot = relativeRoots[searchType];
                if (Configuration.StagingDirectory != null && Configuration.ApplyStagedFiles)
                {
                    var tieredDirectory = (TieredDirectory)writer.Directory;

                    lock (allDeletedFiles)
                    {
                        allDeletedFiles.AddRange(tieredDirectory.GetDeletions()
                            .Select(f => PathUtilities.UriCombine(relativeRoot, f, normalize: true)));
                    }
                }

                var localFiles = writer.Directory.ListAll()
                    .Where(f => writer.Directory.FileExists(f))
                    .Select(f => new PagingFileInfo(PathUtilities.UriCombine(relativeRoot, f, normalize: true), writer.Directory.FileLength(f)));

                lock (files)
                {
                    files.AddRange(localFiles);
                }

                writer.Directory.Dispose();

                Logger?.LogMessage($"Finalized {searchType.Name} index.");
            });

            var deletedDbFiles = Configuration.IdTracker.GetPendingDeletions()
                .Select(d => PathUtilities.UriCombine(IndexDirectoryLayout.DatabaseRelativeDirectory, d, normalize: true))
                .ToArray();

            if (deletedDbFiles.Length > 0)
            {
                allDeletedFiles.AddRange(deletedDbFiles);
            }

            var updatedFileMap = new ConcurrentDictionary<string, long?>(StringComparer.OrdinalIgnoreCase);

            if (Configuration.StagingDirectory != null && Configuration.ApplyStagedFiles)
            {
                await SdkPathUtilities.CopyFilesRecursiveAsync(
                    sourceDirectory: Configuration.StagingOverlayDirectory.Directory,
                    targetDirectory: Configuration.Directory,
                    deletedRelativeTargetFiles: allDeletedFiles,

                    // Use large buffer size
                    bufferSize: 1 << 20,
                    fileEvent =>
                    {
                        updatedFileMap[fileEvent.RelativePath] = fileEvent.Size;
                    },
                    logCopy: Logger?.FluidSelect(l => Out.Action<string>(m => l.LogMessage(m))));
            }

            Logger?.LogMessage($"Creating paging directory info. ({files.Count} files)");

            files.Sort((p1, p2) => p1.RelativePath.CompareTo(p2.RelativePath));
            PagingHelpers.StoreInfo(Configuration.Directory, PagingDirectoryInfo.CreateFromFiles(files) with
            {
            });

            Logger?.LogMessage($"Created paging directory info.");
        }
    }
}
