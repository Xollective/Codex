using Codex.ObjectModel.Attributes;

namespace Codex.ObjectModel
{
    public interface IAnalyzedProjectInfo : IReferencedProject
    {
        /// <summary>
        /// The target framework/platform/configuration
        /// </summary>
        [Include(ObjectStage.Analysis)]
        string Qualifier { get; }

        /// <summary>
        /// The project kind (see <see cref="ObjectModel.ProjectKind"/>)
        /// </summary>
        StringEnum<ProjectKind> ProjectKind { get; }

        /// <summary>
        /// The primary file of the project (i.e. the .csproj file)
        /// </summary>
        IProjectFileScopeEntity PrimaryFile { get; }

        /// <summary>
        /// References to files in the project
        /// </summary>
        // TODO: Should this be serialized as a part of 
        [Exclude(ObjectStage.BlockIndex)]
        IReadOnlyList<IProjectFileScopeEntity> Files { get; }

        /// <summary>
        /// Descriptions of referenced projects and used definitions from the projects
        /// </summary>
        [Exclude(ObjectStage.BlockIndex)]
        IReadOnlyList<IReferencedProject> ProjectReferences { get; }
    }

    /// <summary>
    /// Defines standard set of project kinds
    /// </summary>
    public enum ProjectKind
    {
        Source,

        MetadataAsSource,

        Decompilation,

        Repo,

        MSBuild
    }

    public static class ProjectKindExtensions
    {
        /// <summary>
        /// Gets whether the project is a distributed project (i.e. distributed across
        /// multiple repositories)
        /// </summary>
        public static bool IsDistributedProject(this ProjectKind kind)
        {
            return kind == ProjectKind.MSBuild;
        }
    }

    public interface IReferencedProject : IProjectScopeEntity
    {
        /// <summary>
        /// Used definitions for the project. Sorted.
        /// </summary>
        [Include(ObjectStage.Analysis)]
        IReadOnlyList<IDefinitionSymbol> Definitions { get; }

        /// <summary>
        /// The number of referenced definitions from the project
        /// </summary>
        [CoerceGet(typeof(int?))]
        [Include(ObjectStage.None)]
        int DefinitionCount { get; }

        /// <summary>
        /// The display name of the project
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// The properties of the project. Such as Version, PublicKey, etc.
        /// </summary>
        IPropertyMap Properties { get; }
    }

    /// <summary>
    /// NOTE: Do not set <see cref="IRepoScopeEntity.RepositoryName"/>
    /// </summary>
    public interface IProjectFileLink : IProjectFileScopeEntity
    {
        /// <summary>
        /// Unique identifier for file
        /// TODO: Make this checksum and searchable and use for discovering commit from PDB.
        /// TODO: What is this?
        /// </summary>
        string FileId { get; }
    }
}