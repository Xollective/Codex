using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codex.ObjectModel.Attributes;

namespace Codex.ObjectModel
{
    /// <summary>
    /// Describes basic info about a commit in version control
    /// </summary>
    public interface ICommitInfo : ICommitScopeEntity
    {
        /// <summary>
        /// The date the commit was stored to the index
        /// </summary>
        [SearchBehavior(SearchBehavior.Sortword)]
        DateTime DateUploaded { get; }

        /// <summary>
        /// The date of the commit
        /// </summary>
        [SearchBehavior(SearchBehavior.Sortword)]
        DateTime DateCommitted { get; }

        /// <summary>
        /// The URI of the build where the source was analyzed
        /// </summary>
        string BuildUri { get; }
    }

    /// <summary>
    /// Describes a commit in version control
    /// </summary>
    public interface ICommit : ICommitInfo
    {
        /// <summary>
        /// The commit description describing the changes
        /// </summary>
        [SearchBehavior(SearchBehavior.FullText)]
        string Description { get; }

        /// <summary>
        /// The <see cref="ICommitScopeEntity.CommitId"/> of the parent commits
        /// </summary>
        IReadOnlyList<string> ParentCommitIds { get; }
    }

    /// <summary>
    /// Describes a branch in a repository
    /// </summary>
    public interface IBranch
    {
        /// <summary>
        /// The name of the branch
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The branch description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// The head commit of the branch
        /// </summary>
        [SearchBehavior(SearchBehavior.NormalizedKeyword)]
        string HeadCommitId { get; }
    }
}
