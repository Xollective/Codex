using System.Collections.Immutable;
using Codex.Logging;
using Codex.Storage;
using Codex.Utilities;

namespace Codex.Lucene.Search
{
    using static LuceneConstants;

    public partial record StoredFilterUpdater(IObjectStorage DiskStorage, Logger Logger, string SettingsRoot = null)
    {
        public StoredFile<StableIdStorageHeader> HeaderFile { get; } = new StoredFile<StableIdStorageHeader>(DiskStorage, StableIdStorageHeaderPath);
        public StoredFile<StableIdStorageHeader> HeaderLegacyFile { get; } = new StoredFile<StableIdStorageHeader>(DiskStorage, StableIdStorageHeaderPath);
        public StableIdStorageHeader Header { get; private set; }

        public IStoredRepositorySettings DefaultRepoSettings { get; set; } = new StoredRepositorySettings();
        public GlobalStoredRepositorySettings GlobalSettings { get; private set; }

        private string RepoSettingsPath = PathUtilities.UriCombine(SettingsRoot, RepoSettingsRelativePath, normalize: true); 

        public async ValueTask InitializeAsync()
        {
            Header = await LoadHeaderAsync();

            var settingsFile = new StoredFile<GlobalStoredRepositorySettings>(DiskStorage, RepoSettingsPath);
            GlobalSettings = await settingsFile.LoadAsync();
        }

        public async ValueTask FinalizeAsync()
        {
            await HeaderFile.WriteAsync(Header);
        }

        public async ValueTask<StableIdStorageHeader> LoadHeaderAsync()
        {
            return await DiskStorage.LoadValueAsync<StableIdStorageHeader>(LuceneConstants.StableIdStorageHeaderPath)
                ?? await DiskStorage.CreateOrLoadValueAsync<StableIdStorageHeader>(LuceneConstants.StableIdStorageHeaderLegacyPath);
        }

        public struct RepoInfo<T>(T? info, string? repoName = null) where T : class, IRepositoryStoreInfo
        {
            public string Name { get; } = (info?.Repository.Name ?? repoName).ToLowerInvariant();
            public T? Info { get; } = info;

            public string? Branch => Info.Branch.Name;

            public string? Commit => Info.Commit.CommitId;

            public static implicit operator RepoInfo<T>(T info)
            {
                return new RepoInfo<T>(info, null);
            }

            public static implicit operator RepoInfo<T>(string repoName)
            {
                return new RepoInfo<T>(null, repoName);
            }
        }

        public async ValueTask UpdateRepoAsync(RepoInfo<IRepositoryStoreInfo> repoName, PersistedStoredFilter? newFilter, bool reuseOldFilter = false)
        {
            IStoredRepositorySettings settings = GetRepositorySettings(repoName);

            StoredFilterFiles storedRepoFilter = GetRepoFilter(repoName);

            var addGroupFilters = (newFilter == null && !reuseOldFilter) ? new StoredFilterFiles[0] : await GetAddGroupFiltersAsync(repoName.Name, settings);

            (var oldFilter, var oldGroups) = await storedRepoFilter.GetAndUpdateRepoFilterAsync(newFilter, addGroupFilters.SelectArray(g => g.Name));

            if (reuseOldFilter)
            {
                newFilter = oldFilter;
            }

            var removeGroupFilters = GetGroupFilters(oldGroups.Except(addGroupFilters.Select(g => g.Name)));

            PersistedStoredFilter addFilter = newFilter;
            PersistedStoredFilter removeFilter = oldFilter;
            
            if (reuseOldFilter)
            {
                (addFilter, removeFilter) = (new(), new());
            }
            else if (newFilter != null && !oldFilter.IsEmpty)
            {
                (addFilter, removeFilter) = Diffable.Diff(newFilter, oldFilter);
            }

            await Parallel.ForEachAsync(addGroupFilters, async (groupFilter, token) =>
            {
                Logger.LogMessage($"Adding repository '{repoName}' to group '{groupFilter.Name}'.");
                await groupFilter.UpdateGroupFilterAsync(storedRepoFilter, newFilter, addFilter: addFilter, removeFilter: removeFilter);
            });

            await Parallel.ForEachAsync(removeGroupFilters, async (groupFilter, token) =>
            {
                Logger.LogMessage($"Removing repository '{repoName}' from group '{groupFilter.Name}'.");

                // Specify new filter as null, to indicate we are removing the repo.
                await groupFilter.UpdateGroupFilterAsync(storedRepoFilter, newFilter: null, addFilter: null, removeFilter: oldFilter);
            });
        }

        private IStoredRepositorySettings GetRepositorySettings(RepoInfo<IRepositoryStoreInfo> info)
        {
            foreach (var suffix in new string[] { info.Commit, info.Branch, "" }.WhereNotNull())
            {
                var name = info.Name;
                if (suffix.IsNonEmpty()) name = $"{name}@{suffix}";

                if (GlobalSettings.Repositories.TryGetValue(name, out var settings))
                {
                    return settings;
                }
            }

            return DefaultRepoSettings;
        }

        public StoredFilterFiles GetRepoFilter(RepoInfo<IRepositoryStoreInfo> repoName)
        {
            return new StoredFilterFiles(DiskStorage, repoName.Name, StoredFilterKinds.repo);
        }

        private IReadOnlyList<StoredFilterFiles> GetGroupFilters(IEnumerable<string> groupNames)
        {
            return groupNames.Select(name => new StoredFilterFiles(DiskStorage, name, StoredFilterKinds.group)).ToArray();
        }

        private async Task<IReadOnlyList<StoredFilterFiles>> GetAddGroupFiltersAsync(string repoName, IStoredRepositorySettings repoSettings)
        {
            IEnumerable<RepoName> getGroupNames()
            {
                foreach (var access in Enum.GetValues<RepoAccess>())
                {
                    if (access == RepoAccess.InternalOnly && repoSettings.Access == RepoAccess.Public)
                    {
                        // Public repos should not be included in InternalOnly
                        continue;
                    }

                    if (access <= repoSettings.Access)
                    {
                        yield return access.GetGroupName();
                    }
                }

                if (repoSettings.ExplicitGroupsOnly == false || repoSettings.Groups.Count == 0)
                {
                    yield return AllGroupName;
                }

                foreach (var group in repoSettings.Groups)
                {
                    if (IsReservedGroupName(group))
                    {
                        Logger.LogWarning($"{group} is a reserved name and cannot be specified in explicit groups. Skipping.");
                        continue;
                    }

                    yield return group;
                }
            }

            var groupNames = getGroupNames().ToImmutableHashSet();

            var groupSettingsByBase = GlobalSettings.Groups.ToLookup(g => g.Value.Base);

            foreach (var groupName in groupNames)
            {
                foreach (var derivedGroup in groupSettingsByBase[groupName])
                {
                    if (!derivedGroup.Value.Excludes.Contains(repoName) && !derivedGroup.Value.Excludes.Any(e => groupNames.Contains(e)))
                    {
                        groupNames = groupNames.Add(derivedGroup.Key);
                    }
                }
            }

            return GetGroupFilters(groupNames.Select(s => s.Value));
        }
    }
}