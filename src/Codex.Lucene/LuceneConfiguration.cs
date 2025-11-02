using System.IO;
using Codex.Logging;
using Codex.Lucene.Distributed;
using Codex.Lucene.Framework;
using Codex.ObjectModel.Implementation;
using Codex.Sdk.Storage;
using Codex.Search;
using Codex.Storage;
using Codex.Storage.BlockLevel;
using Codex.Utilities;
using Codex.Utilities.Zip;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using static Lucene.Net.Documents.Field;
using Directory = Lucene.Net.Store.Directory;

namespace Codex.Lucene.Search
{
    public enum PrefilterMode
    {
        Disabled,
        Check,
        Filter
    }

    public record LuceneWriteConfiguration : LuceneConfiguration
    {
        public LuceneWriteConfiguration(string directory)
            : base(directory)
        {
        }

        public string? Alias { get; set; }

        public IObjectStorage DebugStorage { get; set; }
        public IObjectStorage ObjectStorage { get; set; }
        public IObjectStorage DiskStorageOverride { get; set; }

        public string IngestionListingDirectory { get; set; }

        public bool StoreIndexFilesInGit { get; set; }

        public IndexDirectoryLayout? StagingDirectory { get; set; }

        public string SettingsRoot { get; set; } = "config";

        public bool IsStaging => StagingDirectory != null;

        public bool ApplyStagedFiles { get; set; }

        public bool DisableIndex { get; set; }

        /// <summary>
        /// Prefilter mode indicates that new entities should not be written,
        /// but existing entities are tracked.
        /// </summary>
        public PrefilterMode PrefilterMode { get; set; } = PrefilterMode.Disabled;

        public bool IsPrefiltering => PrefilterMode != PrefilterMode.Disabled;

        public bool UseReadPaging { get; set; } = true;

        public readonly string DatabaseRelativeDirectory = "db";

        public IndexDirectoryLayout? StagingOverlayDirectory => StagingDirectory?.OverlayDirectory;

        public string StagingIndexDirectory => StagingDirectory?.StagingIndexDirectory;
        public string StagingDatabaseDirectory => StagingOverlayDirectory?.DatabaseDirectory;

        public string DatabaseDirectory => Directory?.DatabaseDirectory;

        public bool EnsureUniquePaths { get; set; }

        public bool DisposeWriters { get; set; } = true;

        public IStableIdStorage IdTracker { get; set; } = new MemoryStableIdStorage();

        public IStableIdStorage TempIdTracker { get; set; } = null;

        public IDistributedIndexService DistributedService { get; set; } = null;
        public bool DisableStoredFilterUpdates { get; internal set; }
    }

    public record LuceneConfiguration : CodexBaseConfiguration
    {
        public IndexDirectoryLayout Directory { get; set; }

        public int StoredFilterCacheCount = 100;

        public RepoAccess DefaultAccessLevel { get; set; } = RepoAccess.Public;

        public string DefaultGroup { get; set; }

        public override bool UseBlockModel => ExternalRetrievalClient != null;

        public IExternalRetrievalClient ExternalRetrievalClient { get; set; }

        //private Directory Root { get; set; }

        private IPageFileAccessor pageFileAccessor;
        public IPageFileAccessor PageFileAccessor
        {
            get
            {
                if (pageFileAccessor == null)
                {
                    pageFileAccessor = new FileSystemPageFileAccessor(Directory);
                }

                return pageFileAccessor;
            }
            set
            {
                pageFileAccessor = value;
            }
        }

        public PagingDirectoryInfo PagingInfo { get; set; }

        public Func<string, Directory> DirectoryFactory { get; set; }

        public HashSet<string> IncludedTypes { get; set; }

        public Logger Logger = new ConsoleLogger();

        public DirectoryReader OpenReader(SearchType searchType, bool summary = false)
        {
            return DirectoryReader.Open(OpenIndexDirectory(searchType, summary));
        }

        public LazySearchTypesMap<string> GetRelativeRoots()
        {
            return new LazySearchTypesMap<string>(
                s => GetIndexRelativeRoot(s.IndexName));
        }

        public LazySearchTypesMap<DirectoryReader> CreateReaders(bool initializeAll = false)
        {
            return new LazySearchTypesMap<DirectoryReader>(
                s => OpenReader(s),
                initializeAll: initializeAll);
        }

        public LazySearchTypesMap<IndexWriter> CreateWriters(bool initializeAll = false)
        {
            return new LazySearchTypesMap<IndexWriter>(
                CreateWriter,
                initializeAll: initializeAll);
        }

        public IndexWriter CreateWriter(SearchType searchType)
        {
            var codec = new FieldMappingCodec(searchType);

            return new IndexWriter(
                        OpenIndexDirectory(searchType),
                        new IndexWriterConfig(
                            LuceneVersion.LUCENE_48,
                            PerFieldAnalyzer.Create(searchType, new StandardAnalyzer(LuceneVersion.LUCENE_48)))
                        {
                            Codec = codec,
                            UseCompoundFile = true,
                            RAMBufferSizeMB = 100,
                            MergePolicy =
                            {
                                MaxCFSSegmentSizeMB = 1
                            }
                        });
        }

        public IndexDirectory OpenIndexDirectory(SearchType searchType, bool summary = false)
        {
            var relativeRoot = searchType.IndexName;
            if (summary)
            {
                relativeRoot += ".sum";
            }

            relativeRoot = GetIndexRelativeRoot(relativeRoot);
            if (DirectoryFactory != null)
            {
                return DirectoryFactory(relativeRoot);
            }

            return OpenIndexFSDirectory(relativeRoot);
        }

        public FSDirectory OpenIndexFSDirectory(string relativeRoot)
        {
            return FSDirectory.Open(Path.Combine(Directory, relativeRoot));
        }

        public static string GetIndexRelativeRoot(string relativeRoot)
        {
            return PathUtilities.UriCombine(CodexConstants.IndicesDirectoryName, relativeRoot);
        }

        public LuceneConfiguration(string directory)
        {
            Directory = directory;
        }

        private LuceneConfiguration()
        {
        }

        public static LuceneConfiguration CreateFromRoot(Directory root)
        {
            return new LuceneConfiguration()
            {
                DirectoryFactory = relativePath => new ScopedDirectory(root, relativePath)
            };
        }

        public static LuceneConfiguration CreateFromFactory(Func<string, Directory> factory)
        {
            return new LuceneConfiguration() { DirectoryFactory = factory };
        }
    }
}
