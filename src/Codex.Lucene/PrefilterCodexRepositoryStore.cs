using Codex.Lucene;
using Codex.Lucene.Search;
using Codex.ObjectModel;
using Codex.Sdk;
using Codex.Storage;
using Codex.Storage.Store;

namespace Codex
{
    public record PrefilterCodexStore(ILuceneCodexStore PrefilterStore, DirectoryCodexStore TargetStore) : ICodexStore
    {
        public async Task InitializeAsync()
        {
            await PrefilterStore.InitializeAsync();

            await Requires.Expect<ICodexStore>(TargetStore).InitializeAsync();
        }

        public async Task FinalizeAsync()
        {
            await PrefilterStore.FinalizeAsync();

            await Requires.Expect<ICodexStore>(TargetStore).FinalizeAsync();
        }

        public async Task<ICodexRepositoryStore> CreateRepositoryStore(RepositoryStoreInfo storeInfo)
        {
            var prefilterRepoStore = (IndexingCodexRepositoryStoreBase)await PrefilterStore.CreateRepositoryStore(storeInfo);

            var targetRepoStore = (DirectoryCodexStore)await TargetStore.CreateRepositoryStore(storeInfo);

            return new PrefilterCodexRepositoryStore(this, prefilterRepoStore, targetRepoStore, storeInfo);
        }
    }

    public record PrefilterCodexRepositoryStore(
        PrefilterCodexStore PrefilterCodexStore,
        IndexingCodexRepositoryStoreBase PrefilterStore,
        DirectoryCodexStore TargetStore,
        RepositoryStoreInfo StoreInfo) : ICodexRepositoryStore
    {
        IPrefilterCodexStoreWriter PrefilterWriter { get; } = (IPrefilterCodexStoreWriter)PrefilterStore.StoreWriter;

        public virtual Task AddBoundFilesAsync(IReadOnlyList<BoundSourceFile> files)
        {
            foreach (var file in files)
            {
                TargetStore.PreprocessBoundSourceFile(file);
            }

            return AddPrefilteredAsync(files, static (store, files) => store.AddBoundFilesAsync(files));
        }

        public virtual Task AddLanguagesAsync(IReadOnlyList<LanguageInfo> languages)
        {
            return AddPrefilteredAsync(languages, static (store, languages) => store.AddLanguagesAsync(languages));
        }

        public virtual Task AddProjectsAsync(IReadOnlyList<AnalyzedProjectInfo> projects)
        {
            return AddPrefilteredAsync(projects, static (store, projects) => store.AddProjectsAsync(projects));
        }

        public virtual async Task FinalizeAsync()
        {
            await PrefilterStore.FinalizeAsync();

            if (PrefilterCodexStore.PrefilterStore.Configuration.PrefilterMode == PrefilterMode.Filter)
            {
                await PrefilterWriter.StoreFilterAsync(new DiskObjectStorage(TargetStore.DirectoryPath));
            }

            await Requires.Expect<ICodexRepositoryStore>(TargetStore).FinalizeAsync();
        }

        private async Task AddPrefilteredAsync<T>(IReadOnlyList<T> items, Func<ICodexRepositoryStore, IReadOnlyList<T>, Task> addAsync)
            where T : EntityBase
        {
            foreach (var item in items)
            {
                item.IsRequired ??= false;
            }

            await addAsync(PrefilterStore, items);

            if (PrefilterCodexStore.PrefilterStore.Configuration.PrefilterMode == PrefilterMode.Filter)
            {
                if (items.Any(i => i.IsRequired != true))
                {
                    items = items.Where(i => i.IsRequired == true).ToArray();
                }

                if (items.Count == 0) return;
            }

            await addAsync(TargetStore, items);
        }
    }
}
