using System.Buffers;
using System.Diagnostics;
using Codex.Logging;
using Codex.Lucene.Framework;
using Codex.Lucene.Search;
using Codex.Storage;
using Codex.Utilities;
using Lucene.Net.Index;

namespace Codex.Lucene;

using static CodexConstants;

public record StagedIngestionArguments(LuceneCodexStore StagingStore, LuceneCodexStore TargetStore)
{
}

public record StagedIngestCodexStore : ILuceneCodexStore
{
    private readonly LuceneCodexStore stagingStore;
    private readonly AsyncLazy<LuceneCodexStore> targetOverlayStore;
    private readonly AsyncLazy<LuceneWriteConfiguration> lazyTargetConfig;
    public LuceneWriteConfiguration Configuration { get; }
    public AsyncOut<PagingDirectoryInfo> PagingDirectoryInfo { get; } = new AsyncOut<PagingDirectoryInfo>();

    public readonly string OverlayDirectory;

    public StagedIngestCodexStore(LuceneWriteConfiguration configuration)
    {
        Configuration = configuration;
        OverlayDirectory = Configuration.StagingOverlayDirectory;
        var stagingConfig = Configuration with
        {
            // For staging index, the main directory points to the staging directory
            StagingDirectory = null,
            Directory = Configuration.StagingIndexDirectory,
            DirectoryFactory = CreateStagingDirectoryFactory(configuration)
        };

        stagingStore = new LuceneCodexStore(stagingConfig);
        lazyTargetConfig = new(CreateTargetConfigAsync);
        targetOverlayStore = new(InitializeTargetStoreAsync);
    }

    private Func<string, IndexDirectory> CreateStagingDirectoryFactory(LuceneWriteConfiguration configuration)
    {
        var stagingConfig = configuration with
        {
            Directory = Configuration.StagingIndexDirectory
        };

        return stagingConfig.OpenIndexFSDirectory;
    }

    public async Task InitializeAsync()
    {
        targetOverlayStore.Start();

        await stagingStore.InitializeAsync();
    }

    private async Task<LuceneWriteConfiguration> CreateTargetConfigAsync()
    {
        var targetConfig = Configuration with
        {
            // Disable block writing as that is handled by staging store
            // BlockWriterConfiguration = null
        };

        var directoryFactory = targetConfig.UseReadPaging
            ? await PagingHelpers.CreatePagingDirectoryAsync(targetConfig.Directory,
                pc => pc with
                {
                    // Use large 1MB page size
                    PageSize = 1 << 20
                })
            : targetConfig.OpenIndexFSDirectory;

        var overlayConfig = targetConfig with
        {
            Directory = OverlayDirectory
        };

        targetConfig = targetConfig with
        {
            // No need for filter tracking for target store since all
            // direct modifications happen in the  staging store
            IdTracker = new StagingStableIdStorage(Configuration.IdTracker),
            DisableStoredFilterUpdates = true,

            // When prefiltering, don't commit changes back to main index
            ApplyStagedFiles = !Configuration.IsPrefiltering,

            DirectoryFactory = relativeRoot =>
            {
                var backingPagingDirectory = directoryFactory(relativeRoot);
                var overlayDirectory = overlayConfig.OpenIndexFSDirectory(relativeRoot);

                return new TieredDirectory(
                    relativeRoot: relativeRoot,
                    overlayDirectory: overlayDirectory,
                    backingDirectory: backingPagingDirectory);
            }
        };

        return targetConfig;
    }

    private async Task<LuceneCodexStore> InitializeTargetStoreAsync()
    {
        if (Configuration.IsPrefiltering) return null;

        var targetConfig = await lazyTargetConfig.GetValueAsync();

        var targetStore = new LuceneCodexStore(targetConfig);

        await targetStore.InitializeAsync();

        return targetStore;
    }

    public Task<ICodexRepositoryStore> CreateRepositoryStore(RepositoryStoreInfo storeInfo)
    {
        return stagingStore.CreateRepositoryStore(storeInfo);
    }

    public async Task FinalizeAsync()
    {
        await stagingStore.FinalizeAsync();

        var targetOverlayStore = await this.targetOverlayStore.GetValueAsync();
        if (targetOverlayStore == null) return;

        var merger = new LuceneIndexMerger(targetOverlayStore.Configuration.Logger)
        {
            // Don't merge segments over 1gb
            MaxMergeableBucket = LuceneIndexMerger.GetBucket(1000)
        };

        var stagingReaders = stagingStore.Configuration.CreateReaders();

        await TaskUtilities.ForEachAsync(!Features.IsTest.Value, stagingReaders.Enumerate(allowInit: true), async (kvp, token) =>
        {
            (var searchType, var stagingReader) = kvp;

            var targetWriter = targetOverlayStore.Writers[searchType];

            using var targetReader = targetWriter.GetReaderNoFlush();

            merger.MergeIndices(searchType.Name, stagingReader, targetReader, targetWriter);

            stagingReader.Dispose();
        });

        // NOTE: WriteBlockListAsync will copy overlay files over index

        await targetOverlayStore.FinalizeAsync();
    }

    public Task<ICodexStoreWriter> CreateStoreWriterAsync(IRepositoryStoreInfo storeInfo)
    {
        return stagingStore.CreateStoreWriterAsync(storeInfo);
    }
}