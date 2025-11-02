using System.Diagnostics.ContractsLight;
using Codex.Lucene;
using Codex.Lucene.Search;
using Codex.Sdk;
using Codex.Storage;
using Codex.Storage.BlockLevel;
using Codex.Storage.Store;
using Codex.Utilities;

namespace Codex.Application.Verbs;

[Verb("ingest", HelpText = "Ingest analysis files into index.")]
public record IngestOperation : IndexReadOperationBase
{
    [Option("scan", HelpText = "Treats every directory under data directory as a separate store to ingest.")]
    public bool Scan { get; set; }

    [Option('i', "in", Required = true, HelpText = "The input directory or a zip file containing analysis data to load.")]
    public string InputPath { get; set; }

    [Option("dir-format", Default = false, HelpText = "Specifies whether to output to analysis directory format.")]
    public bool UseDirectoryFormat { get; set; } = false;

    [Option("clean", HelpText = "Reset output directory.")]
    public bool Clean { get; set; }

    [Option("clean-stage", Default = true, HelpText = "Reset staging output directory.")]
    public bool CleanStaging { get; set; } = true;

    [Option("test", HelpText = "Indicates that save should use test mode which disables optimization.")]
    public bool DisableOptimization { get; set; }

    [Option('n', "name", HelpText = "Override name of the repository")]
    public string RepoName { get; set; }

    [Option("alias", HelpText = "The alias used to identify the repo during search. NOTE: This supersedes repo name for this purpose, but does not mark files as belonging to a repo with this value")]
    public string? Alias { get; set; }

    [Option("include-type", HelpText = "Specifies inclusion list of search types")]
    public IList<string> IncludeTypes { get; set; }

    [Option("stored-filters", Default = true, HelpText = "Specifies whether stored filter tracking is enabled")]
    public bool UseStoredFilters { get; set; } = true;

    [Option("dump", HelpText = "Specifies directory to dump search entities.")]
    public string DumpDirectory { get; set; }

    [Option("upload-blob-sas", HelpText = "Specifies sas url for a blob store container for storing external files.")]
    public string BlobContainerSasUrl { get; set; }

    [Option("stage-out", HelpText = "Specifies the staging directory for index with additions")]
    public string StagingDirectory { get; set; }

    [Option("settings-root", HelpText = "Specifies the root directory for settings")]
    public string SettingsRoot { get; set; }

    [Option("use-git-storage", HelpText = "Specifies whether indexing data should be stored in a git repo at the output directory.")]
    public bool UseGitStorage { get; set; }

    [Option("temp", HelpText = "Specifies location to place temp files.")]
    public string TempDirectory { get; set; }

    [Option("id-storage-mode", HelpText = "Specifies whether to use RocksDb or ZoneTree for stable id storage.")]
    public IdStorageKind IdStorageMode { get; set; } = IdStorageKind.Default;

    internal AnalyzeOperation AnalyzeOperation { get; set; }

    private bool FinalizePerRepo { get; set; }

    public enum IdStorageKind
    {
        ZoneTree,
        Default = ZoneTree
    }

    public Func<string, bool> CanReadFilter { get; set; }

    public ILuceneCodexStore LuceneStore => (ILuceneCodexStore)OutputStore;

    protected override async ValueTask InitializeAsync()
    {
        //GitHelpers.Init();
        await base.InitializeAsync();

        TempDirectory ??= Path.Combine(OutputDirectory, CodexConstants.RelativeTempDirectory);
        TempDirectory = TempDirectory?.FluidSelect(t => Path.GetFullPath(t));


        CleanStaging |= Clean;

        OutputDirectory = Path.GetFullPath(OutputDirectory);

        if (Clean && Directory.Exists(OutputDirectory))
        {
            PathUtilities.ForceDeleteDirectory(OutputDirectory);
        }

        if (Clean && Directory.Exists(TempDirectory))
        {
            PathUtilities.ForceDeleteDirectory(OutputDirectory);
        }

        if (CleanStaging && !string.IsNullOrEmpty(StagingDirectory) && Directory.Exists(StagingDirectory))
        {
            PathUtilities.ForceDeleteDirectory(StagingDirectory);
        }

        if (DumpDirectory != null)
        {
            DumpDirectory = Path.GetFullPath(DumpDirectory);

            if (Clean && Directory.Exists(DumpDirectory))
            {
                PathUtilities.ForceDeleteDirectory(DumpDirectory);
            }
        }

        OutputStore = GetStore();
    }

    protected override async ValueTask<int> ExecuteAsync()
    {
        OutputStore = SdkFeatures.WrapIngestStore.Value?.Invoke(OutputStore) ?? OutputStore;

        if (AnalyzeOperation != null)
        {
            AnalyzeOperation.OutputStore = OutputStore;

            var exitCode = await AnalyzeOperation.RunAsync();
            if (exitCode != 0)
            {
                Logger.LogError($"Analyze operation failed with exit code: {exitCode}");
                return exitCode;
            }
        }
        else
        {
            await OutputStore.InitializeAsync();

            await LoadAsync(finalizePerRepo: FinalizePerRepo);

            await OutputStore.FinalizeAsync();
        }

        await CleanupAsync();

        if (!string.IsNullOrEmpty(BlobContainerSasUrl))
        {
            var storage = new BlobObjectStorage(Logger, BlobContainerSasUrl);
            storage.Initialize();

            await storage.UploadDirectory(OutputDirectory);
        }

        return 0;
    }

    private async Task LoadAsync(bool finalizePerRepo = true)
    {
        string loadPath(params string[] subPath) => Path.Combine([InputPath, .. subPath]);
        // Cases
        // 1. Single analysis store directory
        // 2. Single analysis store zip
        // 3. Single nested analysis store directory
        // 4. Directory containing multiple analysis directories or zips

        if (File.Exists(InputPath))
        {
            // 2. Single analysis store zip
            await LoadCoreAsync(InputPath);
        }
        else if (Directory.Exists(InputPath))
        {
            if (File.Exists(loadPath(DirectoryCodexStore.RepositoryInitializationFileName)))
            {
                // 1. Single analysis store directory
                await LoadCoreAsync(InputPath);
            }
            else if (File.Exists(loadPath("store", DirectoryCodexStore.RepositoryInitializationFileName)))
            {
                // 2. Single nested analysis store directory
                await LoadCoreAsync(loadPath("store"));
            }
            else
            {
                // 4. Directory containing multiple analysis store directories or zips
                var inputs = Directory.GetFileSystemEntries(InputPath);
                int i = 1;
                foreach (var input in inputs)
                {
                    Logger.LogMessage($"[{i} of {inputs.Length}] Loading {input}");
                    await LoadCoreAsync(input);

                    // Only clear indices on first use
                    Clean = false;
                    i++;
                }
            }
        }
    }

    protected async Task LoadCoreAsync(
        string loadDirectory)
    {
        var loadDirectoryStore = new DirectoryCodexStore(loadDirectory, Logger) { CanReadFilter = CanReadFilter };
        await loadDirectoryStore.ReadAsync(OutputStore, repositoryName: RepoName, finalize: FinalizePerRepo);
    }

    private ICodexStore GetStore()
    {
        if (!string.IsNullOrEmpty(OutputDirectory))
        {
            if (TryCreateLuceneStore(new(out var store)))
            {
                FinalizePerRepo = true;
                return store.Value;
            }
            else
            {
                return new DirectoryCodexStore(OutputDirectory) { Clean = Clean, DisableOptimization = DisableOptimization };
            }
        }
        else
        {
            return new NullCodexRepositoryStore();
        }
    }

    protected bool TryCreateLuceneStore(AsyncOut<ICodexStore> store)
    {
        if (UseDirectoryFormat) return false;

        string outputDirectory = OutputDirectory;
        var configuration = new LuceneWriteConfiguration(outputDirectory)
        {
            Logger = this.Logger,
            IncludedTypes = IncludeTypes?.Count == 0 ? null : IncludeTypes?.ToHashSet(),
            StoreIndexFilesInGit = UseGitStorage,
            StagingDirectory = StagingDirectory,
        };

        IObjectStorage getDiskObjectStorage(string relativePath = "")
        {
            var diskStorage = new DiskObjectStorage(Path.Combine(outputDirectory, relativePath));
            if (configuration.IsStaging)
            {
                diskStorage.IsReadOnly = true;
                return new StagedObjectStorage(
                    OverlayStorage: new DiskObjectStorage(Path.Combine(configuration.StagingOverlayDirectory, relativePath)),
                    BackingStorage: diskStorage);
            }

            return diskStorage;
            //if (string.IsNullOrEmpty(BlobContainerSasUrl)) return diskStorage;

            //return new CompositeObjectStorage(
            //    Local: diskStorage,
            //    Remote: new BlobObjectStorage(Logger, BlobContainerSasUrl, Path.Combine("index", relativePath)));
        }

        configuration.DiskStorageOverride = getDiskObjectStorage();
        configuration = configuration
        .ApplyIf(UseStoredFilters, c => c.IdTracker = CreateIdStorage(IdStorageMode, configuration))
        .ApplyIf(DumpDirectory != null, c => c.DebugStorage = new DiskObjectStorage(DumpDirectory))
        .ApplyIf(SettingsRoot != null, c => c.SettingsRoot = SettingsRoot)
        ;

        store.Value ??= string.IsNullOrEmpty(StagingDirectory)
            ? new LuceneCodexStore(configuration)
            : new StagedIngestCodexStore(configuration);

        return true;
    }

    private IStableIdStorage CreateIdStorage(IdStorageKind kind, LuceneWriteConfiguration configuration)
    {
        Contract.Check(!(configuration.IsPrefiltering && kind != IdStorageKind.ZoneTree))
            ?.Assert($"IsPrefiltering: {configuration.IsPrefiltering}, Kind: {kind}");

        var stagingDirectory = configuration.IsStaging
                        ? configuration.StagingDatabaseDirectory
                        : null;
        return new ZoneTreeStableIdStorage(configuration.DatabaseDirectory, stagingDirectory);
    }
}
