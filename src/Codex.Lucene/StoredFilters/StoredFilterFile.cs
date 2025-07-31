using System.Collections.Immutable;
using System.Diagnostics.ContractsLight;
using Codex.Sdk.Utilities;
using Codex.Storage;
using Codex.Utilities;

namespace Codex.Lucene.Search;

using static LuceneConstants;

public record StoredFilterFiles(IAsyncObjectStorage Storage, string Name, StoredFilterKinds Kind, None _)
{
    public string Name { get; } = Name.ToLowerInvariant();

    public StoredFilterFiles(IAsyncObjectStorage storage, string name, StoredFilterKinds kind) : this(storage, name, kind, default)
    {
        if (IsAggregate)
        {
            if (IsAccessGroupName(name))
            {
                Kind = StoredFilterKinds.access;
            }
            else
            {
                Contract.Check(Kind != StoredFilterKinds.access)?.Assert($"{name} is not an access name.");
            }

            Contract.Assert(PathUtilities.IsValidFileName(name), "Group names must be valid file names.");
            Contract.Check(!name.Contains(SourceControlUri.FileNameSafeSlash))?.Assert($"Group names cannot contain '{SourceControlUri.FileNameSafeSlash}'.");
            GroupInfoFile = GetFile<StoredRepositoryGroupInfo>(".info.json");
        }
        else
        {
            Name = CoerceRepoFilterName(Name);
            RepoInfoFile = GetFile<StoredRepositoryInfo>(".info.json");
        }

        ActiveFile = GetFile<PersistedStoredFilter>(".json");
        CumulativeFile = GetFile<PersistedStoredFilter>(".cumulative.json");
        BuilderFile = GetFile<PersistedStoredFilter>(".builder.json");
    }

    private bool IsAggregate => Kind != StoredFilterKinds.repo;

    public StoredFile<PersistedStoredFilter> ActiveFile { get; }

    public StoredFile<PersistedStoredFilter> CumulativeFile { get; }

    public StoredFile<PersistedStoredFilter> BuilderFile { get; }

    public StoredFile<StoredRepositoryInfo> RepoInfoFile { get; }
    public StoredFile<StoredRepositoryGroupInfo> GroupInfoFile { get; }

    private StoredFile<T> GetFile<T>(string extension)
        where T : class, new()
    {
        return new(Storage, $"{FilterRoot}/{Kind}/{SourceControlUri.GetUnicodeRepoFileName(Name)}{extension}");
    }

    /// <summary>
    /// Updates the filter and returns the original filter for repo filters
    /// </summary>
    public async ValueTask<(PersistedStoredFilter oldFilter, IReadOnlyList<string> oldGroups)> GetAndUpdateRepoFilterAsync(PersistedStoredFilter? newFilter, string[] groups)
    {
        Contract.Assert(Kind == StoredFilterKinds.repo);
        var info = await RepoInfoFile.LoadAsync();
        var oldGroups = info.Groups;
        info.Groups = info.Groups.Clear().Union(groups);

        var oldFilter = await ActiveFile.LoadAsync();

        if (newFilter != null)
        {
            await ActiveFile.WriteAsync(newFilter);
        }

        await RepoInfoFile.WriteAsync(info);
        
        return (oldFilter, oldGroups);
    }

    public async ValueTask UpdateGroupFilterAsync(StoredFilterFiles repoFilter, PersistedStoredFilter? newFilter, PersistedStoredFilter? addFilter, PersistedStoredFilter removeFilter)
    {
        Contract.Assert(IsAggregate);
        Contract.Assert(removeFilter != null, "Group filters require old filter for update. Old filter must be subtracted before adding new filter.");
        Contract.Check((newFilter == null) == (addFilter == null))?.Assert($"newFilter == null: {newFilter == null}, addFilter == null: {addFilter == null}.");

        var info = await GroupInfoFile.LoadAsync();

        var builderFilter = await BuilderFile.LoadAsync();
        var cumulativeFilter = await CumulativeFile.LoadAsync();

        if (info.ActiveRepos.Contains(repoFilter.Name))
        {
            if (!removeFilter.IsEmpty)
            {
                builderFilter.Subtract(removeFilter, recomputeAggregates: addFilter == null);
            }

            if (addFilter != null)
            {
                builderFilter.Add(addFilter, recomputeAggregates: true);
            }
        }
        else if (newFilter != null)
        {
            builderFilter.Add(newFilter, recomputeAggregates: true);
        }

        if (newFilter != null)
        {
            info.ActiveRepos = info.ActiveRepos.Add(repoFilter.Name);
            cumulativeFilter.Add(newFilter, recomputeAggregates: true);
        }
        else
        {
            info.ActiveRepos = info.ActiveRepos.Remove(repoFilter.Name);
        }

        await CumulativeFile.WriteAsync(cumulativeFilter);

        await BuilderFile.WriteAsync(builderFilter);

        builderFilter.ClearCountingFilters();

        await ActiveFile.WriteAsync(builderFilter);

        await GroupInfoFile.WriteAsync(info);
    }
}

public enum StoredFilterKinds
{
    repo,
    group,
    access
}
