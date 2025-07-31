using System.Collections.Immutable;
using System.Diagnostics.ContractsLight;

namespace Codex.ObjectModel;

public interface IStoredRepositorySettings
{
    RepoAccess Access { get; }

    /// <summary>
    /// Indicates whether repo should 
    /// </summary>
    bool ExplicitGroupsOnly { get; set; }

    /// <summary>
    /// Gets list of groups to add 
    /// </summary>
    ImmutableSortedSet<string> Groups { get; }
}

public interface IStoredRepositoryGroupSettings
{
    /// <summary>
    /// The name of the group which this group is based on
    /// </summary>
    public string Base { get; }

    /// <summary>
    /// Gets an explicit list of repositories to exclude from the group
    /// </summary>
    ImmutableHashSet<RepoName> Excludes { get; }
}

public interface IUserSettings
{
    RepoAccess? Access { get; }

    DateTime ExpirationUtc { get; }
}

public interface IGlobalStoredRepositorySettings
{
    ImmutableDictionary<RepoName, IStoredRepositorySettings> Repositories { get; }

    ImmutableDictionary<RepoName, IStoredRepositoryGroupSettings> Groups { get; }
}

public interface IStoredRepositoryInfo
{
    /// <summary>
    /// Gets the set of groups the repository is currently present in.
    /// </summary>
    ImmutableSortedSet<string> Groups { get; }
}

public interface IStoredRepositoryGroupInfo
{
    /// <summary>
    /// Gets the set of repositories currently contained in the group.
    /// </summary>
    ImmutableSortedSet<string> ActiveRepos { get; }
}

public enum RepoAccess
{
    /// <summary>
    /// Only visible to administrators.
    /// </summary>
    AdminOnly = -1000,

    /// <summary>
    /// Repos in <see cref="Internal"/> excluding <see cref="Public"/>
    /// </summary>
    InternalOnly = -100,

    /// <summary>
    /// Only visible to sponsors.
    /// </summary>
    Internal = 0,

    /// <summary>
    /// Visible to everyone.
    /// </summary>
    Public = 1000,
}