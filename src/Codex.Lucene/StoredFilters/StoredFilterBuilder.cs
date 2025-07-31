using System.Diagnostics.ContractsLight;
using Codex.Logging;
using Codex.Lucene.Search;
using Codex.ObjectModel;
using Codex.Storage;
using Codex.Utilities;
using static Lucene.Net.Documents.Field;

namespace Codex.Lucene;

public class StoredFilterConfiguration
{
    /// <summary>
    /// Prefilter mode indicates that new entities should not be written,
    /// but existing entities are tracked.
    /// </summary>
    public PrefilterMode PrefilterMode { get; set; } = PrefilterMode.Disabled;

    public bool IsPrefiltering => PrefilterMode != PrefilterMode.Disabled;

    public HashSet<string> IncludedTypes { get; set; }

    public IAsyncObjectStorage StoredFilterStorage { get; set; }

    public static implicit operator StoredFilterConfiguration(LuceneWriteConfiguration configuration)
    {
        return new StoredFilterConfiguration()
        {
            PrefilterMode = configuration.PrefilterMode,
            IncludedTypes = configuration.IncludedTypes
        };
    }
}

public record StoredFilterBuilder(
    ICodexStoreWriter StoreWriter, 
    IStableIdStorage IdTracker, 
    Logger Logger,
    StoredFilterConfiguration Configuration,
    IRepositoryStoreInfo StoreInfo,
    StoredFilterUpdater StoredFilterUpdater = null) : IPrefilterCodexStoreWriter, IEntityVisitor<IProjectSearchModel>
{
    private LazySearchTypesMap<EntityAssociation[]> EntityVerificationMap { get; set; }

    private ProjectReferenceMinCountSketch ProjectReferenceCountSketch { get; set; } = new();

    private AsyncLocal<EntityBase> currentRootEntity;

    public LuceneStoreFilterBuilder[] AllRepositoryEntitiesFilter { get; } = new[] { new LuceneStoreFilterBuilder() };

    private LuceneStoreFilterBuilder[] DeclaredDefinitionStoredFilter { get; } = new[] { new LuceneStoreFilterBuilder() };

    public AsyncLocalScope<EntityBase> EnterRootEntityScope(EntityBase rootEntity)
    {
        return currentRootEntity?.EnterScope(rootEntity) ?? default;
    }

    public async Task FinalizeAsync()
    {
        await StoreWriter.FinalizeAsync();

        if (Configuration.StoredFilterStorage != null)
        {
            await StoreFilterAsync(Configuration.StoredFilterStorage);
        }

        if (Configuration.IsPrefiltering || StoredFilterUpdater == null) return;

        await StoredFilterUpdater.UpdateRepoAsync(
            repoName: StoreInfo.Commit.Alias ?? StoreInfo.Repository.Name,
            GetFilter());
    }

    public Task InitializeAsync()
    {
        if (Configuration.PrefilterMode != PrefilterMode.Disabled)
        {
            EntityVerificationMap = new(s => new EntityAssociation[64]);
            currentRootEntity = new AsyncLocal<EntityBase>();
        }

        return StoreWriter.InitializeAsync();
    }

    public async ValueTask AddAsync<T>(SearchType<T> searchType, T entity, IndexAddOptions options)
        where T : class, ISearchEntity
    {
        if (Configuration.IncludedTypes?.Contains(searchType.Name) == false)
        {
            return;
        }

        if (this is IEntityVisitor<T> entityVisitor)
        {
            entityVisitor.OnAdding(searchType, entity);
        }

        Contract.Assert(entity.Uid != default);

        bool tryGetDocumentRef(out DocumentRef docRef)
        {
            if (options.HasStableId)
            {
                docRef = entity.StableId;
                return false;
            }
            if (Configuration.PrefilterMode == PrefilterMode.Disabled)
            {
                return !IdTracker.TryReserve(searchType, entity.Uid, out docRef);
            }
            else
            {
                return IdTracker.TryGet(searchType, entity.Uid, out docRef);
            }
        }

        bool foundExisting = tryGetDocumentRef(out var docRef);

        if (Configuration.IsPrefiltering)
        {
            if (foundExisting)
            {
                var mappings = EntityVerificationMap[searchType];

                updateSlot();
                void updateSlot()
                {
                    ref var slot = ref mappings[entity.Uid[0] % mappings.Length];

                    // Need to use greatest doc id to ensure db at least has all found mappings
                    // Otherwise, its possible that a mapping after the mapping stored for verification
                    // could differ.
                    while (slot == null || docRef.DocId > slot.DocId)
                    {
                        slot = new EntityAssociation(entity.Uid, docRef.DocId);
                    }
                }
            }
            else
            {
                SdkFeatures.OnRequiredEntityHandler.Value?.Invoke(IdTracker, entity);

                // Mark new entities as added so entity 
                currentRootEntity.Value?.MarkRequired();
                return;
            }
        }

        if (docRef.DocId > 0)
        {
            // Found the document or placeholder. Add the document to the filters.
            AddRef(searchType, docRef, options);
            entity.StableId = docRef.DocId;
        }

        if (foundExisting)
        {
            // Already added, so just return.
            return;
        }

        entity.IsAdded = true;

        SdkFeatures.OnRequiredEntityHandler.Value?.Invoke(IdTracker, entity);

        await StoreWriter.AddAsync<T>(searchType, entity, options);
    }

    public void AddRef(SearchType searchType, DocumentRef docRef, IndexAddOptions options)
    {
        var additionalFilters = GetFilters(options.AdditionalStoredFilters);
        AddToFilters(searchType, docRef, additionalFilters);
    }

    private StoredFile<PersistedStoredFilter> GetFilterFile(IAsyncObjectStorage storage)
    {
        return new StoredFile<PersistedStoredFilter>(storage, LuceneConstants.PrefilterRelativePath);
    }

    public async Task StoreFilterAsync(IAsyncObjectStorage storage)
    {
        var file = GetFilterFile(storage);
        var filter = GetFilter();

        filter.EntityVerificationMap = EntityVerificationMap.Enumerate(allowInit: false)
            .ToDictionary(kvp => kvp.Key.TypeId, kvp => kvp.Value);

        await file.WriteAsync(filter);
    }

    public async Task LoadFilterAsync(IAsyncObjectStorage storage)
    {
        var file = GetFilterFile(storage);
        var filter = await file.LoadAsync(new(out var exists));

        string failureMessage = null;
        if (exists.Value)
        {
            foreach (var (typeId, mappings) in filter.EntityVerificationMap)
            {
                var searchType = typeId.GetSearchType();

                foreach (var mapping in mappings)
                {
                    if (mapping == null) continue;

                    if (Out.Var(out var missing, !IdTracker.TryGet(searchType, mapping.EntityUid, out var docRef))
                        || docRef.DocId != mapping.DocId)
                    {
                        failureMessage = $"Prefilter verification failed: (Missing={missing}, EntityUid={mapping.EntityUid}, MappingDocId={mapping.DocId}, FoundDocId={docRef.DocId})";
                        Logger.LogError(failureMessage);
                    }
                }
            }

            if (failureMessage != null)
            {
                throw Contract.AssertFailure(failureMessage);
            }

            ProjectReferenceCountSketch = filter.ProjectReferenceCountSketch;
            AllRepositoryEntitiesFilter[0].LoadPersisted(filter.AllFilter);
            DeclaredDefinitionStoredFilter[0].LoadPersisted(filter.DeclaredDefinitionFilter);
        }
    }

    public PersistedStoredFilter GetFilter()
    {
        return new PersistedStoredFilter()
        {
            CommitInfo = StoreInfo.Commit,
            ProjectReferenceCountSketch = ProjectReferenceCountSketch,
            AllFilter = AllRepositoryEntitiesFilter[0].ToPersisted(),
            DeclaredDefinitionFilter = DeclaredDefinitionStoredFilter[0].ToPersisted()
        };
    }

    private LuceneStoreFilterBuilder[] GetFilters(FilterName name)
    {
        switch (name)
        {
            case FilterName.DeclaredDefinitions:
                return DeclaredDefinitionStoredFilter;
            case FilterName.None:
            default:
                Contract.Check(name == FilterName.None)?.Assert($"Unexpected enum value: {name}");
                return Array.Empty<LuceneStoreFilterBuilder>();
        }
    }

    private void AddToFilters(SearchType searchType, DocumentRef docRef, LuceneStoreFilterBuilder[] additionalStoredFilters)
    {
        LuceneStoreFilterBuilder.AddToFilters(searchType, docRef, AllRepositoryEntitiesFilter);
        LuceneStoreFilterBuilder.AddToFilters(searchType, docRef, additionalStoredFilters);
    }

    public void OnAdding(SearchType<IProjectSearchModel> searchType, IProjectSearchModel entity)
    {
        if (SearchTypes.Project == searchType)
        {
            ProjectReferenceCountSketch.Add(entity.Project.ProjectId);

            foreach (var reference in entity.Project.ProjectReferences)
            {
                if (reference.Definitions.Count != 0)
                {
                    // Only add projects with actual references
                    ProjectReferenceCountSketch.Add(reference.ProjectId);
                }
            }
        }
    }
}