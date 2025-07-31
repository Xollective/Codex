using Codex.Sdk;
using Codex.Utilities;
using Codex.Utilities.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Codex.ObjectModel
{
    /*
     * Types in this file define search behaviors. Changes should be made with caution as they can affect
     * the mapping schema for indices and will generally need to be backward compatible.
     * Additions should be generally safe.
     * 
     * WARNING: Changing routing key by changed input to Route() function is extremely destructive as it causes entities
     * to be routed to different shards and thereby invalidates most stored documents. Generally, these should never be changed
     * unless the entire index will be recreated.
     * 
     * TODO: Maybe there should be some sort of validation on this.
     */
    public static class SearchTypes
    {
        static SearchTypes()
        {
            _searchTypesById = new SearchType[RegisteredSearchTypes.Select(s => (int)s.TypeId).Max() + 1];
            RegisteredSearchTypes.ForEach(s => _searchTypesById[(int)s.TypeId]  = s);
        }

        public static SearchType GetSearchType(this SearchTypeId typeId)
        {
            return _searchTypesById[(int)typeId];
        }

        public static SearchType<T> GetSearchType<T>()
            where T : class, ISearchEntity<T>
        {
            return SearchType<T>.Instance;
        }

        private static SearchType[] _searchTypesById;

        public static readonly List<SearchType> RegisteredSearchTypes = new List<SearchType>();

        public static SearchType<IDefinitionSearchModel> Definition = SearchType.Create<IDefinitionSearchModel>(RegisteredSearchTypes)
            .WithObjectPath(ObjectPaths.GetPath)
            //.ExternalLink(IDefinitionSearchModel.GetExternalLink)

            .SearchField(s => s.Definition.ProjectId, SearchBehavior.Sortword)
            //.SearchMultiField(s => s.Definition.ConstantValue.SelectOrDefault(cv => cv.GetTriangulationValues(), default), SearchBehavior.Term, nameof(IDefinitionSymbol.ExtendedSearchInfo.ConstantValue))
            .SearchNamedField(s => s.Definition.ExtensionInfo?.ProjectId, SearchBehavior.PrefixFullName, "ExtensionProjectId")
            .SearchField(s => s.Definition.Id, SearchBehavior.NormalizedKeyword)
            .SearchField(s => s.ExcludeFromDefaultSearch, SearchBehavior.Term)
            .SearchField(s => s.Definition.ContainerTypeSymbolId, SearchBehavior.NormalizedKeyword)
            .SearchField(s => s.ExtendedSearchInfo.ConstantValue.Value, SearchBehavior.NormalizedKeyword, 
                nameof(IDefinitionSymbolExtendedSearchInfo.ConstantValue), 
                isValid: s => s.ExtendedSearchInfo?.ConstantValue != null)

            .SetShouldExclude(s => s.Definition.ExcludeFromSearch)
            .SearchField(s => s.Definition.ContainerQualifiedName, SearchBehavior.PrefixFullName)

            .SetShouldExclude(s => s.Definition.ExcludeFromSearch || s.ExtendedSearchInfo != null)
            .SearchField(s => s.Definition.Kind, SearchBehavior.Sortword, configure: s => s.BehaviorInfo = s.BehaviorInfo with { LowCardinalityTermOptimization = true })
            .SearchField(s => s.Definition.ShortName, SearchBehavior.PrefixShortName)
            .SearchField(s => s.Definition.AbbreviatedName, SearchBehavior.PrefixTerm)
            .SearchNamedField(s => s.Definition.ExtensionInfo?.ContainerQualifiedName, SearchBehavior.PrefixFullName, "ExtensionContainerQualifiedName")
            .SearchMultiField(s => s.Definition.Modifiers, SearchBehavior.NormalizedKeyword)
            .SearchMultiField(s => s.Definition.Keywords, SearchBehavior.NormalizedKeyword, configure: s => s.BehaviorInfo = s.BehaviorInfo with { LowCardinalityTermOptimization = true })
            ;
        //.CopyTo(ds => ds.Definition.Modifiers, ds => ds.Keywords)
        //.CopyTo(ds => ds.Definition.Kind, ds => ds.Kind)
        //.CopyTo(ds => ds.Definition.ExcludeFromDefaultSearch, ds => ds.ExcludeFromDefaultSearch)
        //.CopyTo(ds => ds.Definition.ShortName, ds => ds.ShortName)
        ////.CopyTo(ds => ds.Language, ds => ds.Keywords)
        //.CopyTo(ds => ds.Definition.ProjectId, ds => ds.ProjectId)
        //.CopyTo(ds => ds.Definition.ProjectId, ds => ds.Keywords);

        public static SearchType<IReferenceSearchModel> Reference = SearchType.Create<IReferenceSearchModel>(RegisteredSearchTypes)
            .WithObjectPath(ObjectPaths.GetPath)
            //.ExternalLink(IReferenceSearchModel.GetExternalLink, nameof(IReferenceSearchModel.FileInfo), nameof(IReferenceSearchModel.References))
            .Route(rs => rs.Symbol.Id.Value)
            .SearchField(s => s.FileInfo.RepositoryName, SearchBehavior.Sortword)
            .SearchField(s => s.FileInfo.ProjectId, SearchBehavior.Sortword, "ReferencingProjectId")
            .SearchField(s => s.FileInfo.ProjectRelativePath, SearchBehavior.None)
            .SearchField(s => s.Symbol.ProjectId, SearchBehavior.NormalizedKeyword)
            .SearchField(s => s.Symbol.Id, SearchBehavior.NormalizedKeyword)
            .MarkForRemoval("Do we need a primary reference kind for sorting?")
            .SearchField(s => s.ReferenceKind.GetPreference(), SearchBehavior.SortValue, name: "Rank")
            .SearchField(s => s.ReferenceKind.Value.CastToSigned(), SearchBehavior.SortValue, name: "SortedReferenceKey")
            .SearchMultiField(s => s.ReferenceKind.Enumerate(), SearchBehavior.NormalizedKeyword, name: nameof(IReferenceSymbol.ReferenceKind))
            .SearchMultiField(s => s.RelatedDefinition, SearchBehavior.NormalizedKeyword, name: nameof(IReferenceSpan.RelatedDefinition))
            ;
        //.CopyTo(rs => rs.Spans.First().Reference, rs => rs.Reference);


        public static SearchType<ITextChunkSearchModel> TextChunk = SearchType.Create<ITextChunkSearchModel>(RegisteredSearchTypes)
            .WithObjectPath(ObjectPaths.GetPath)
            //.ExternalLink(ITextChunkSearchModel.GetExternalLink)
            .SearchField(s => s.Content, SearchBehavior.FullText)
            ;

        public static SearchType<ITextSourceSearchModel> TextSource = SearchType.Create<ITextSourceSearchModel>(RegisteredSearchTypes)
            .WithObjectPath(ObjectPaths.GetPath)
            //.ExternalLink(ITextSourceSearchModel.GetExternalLink)
            .SearchField(s => s.Chunk.Id, SearchBehavior.Term, "ChunkId")

            // Add file location fields with SearchBehavior.None so they are included
            // for index hash, but not actually indexed
            .SearchField(s => s.Chunk.StartLineNumber, SearchBehavior.None)
            .SearchField(s => s.File.RepositoryName, SearchBehavior.None)
            .SearchField(s => s.File.ProjectId, SearchBehavior.None)
            .SearchField(s => s.File.ProjectRelativePath, SearchBehavior.None)
            ;
        //.CopyTo(ss => ss.File.SourceFile.Content, ss => ss.Content)
        //.CopyTo(ss => ss.File.SourceFile.Info.RepoRelativePath, ss => ss.RepoRelativePath)
        //.CopyTo(ss => ss.File.ProjectId, ss => ss.ProjectId)
        //.CopyTo(ss => ss.File.Info.Path, ss => ss.FilePath);

        public static SearchType<IBoundSourceSearchModel> BoundSource = SearchType.Create<IBoundSourceSearchModel>(RegisteredSearchTypes)
            .WithObjectPath(ObjectPaths.GetPath)
            //.ExternalLink(IBoundSourceSearchModel.GetExternalLink, nameof(IBoundSourceSearchModel.Content))
            .Route(ss => PathUtilities.GetFileName(ss.File.Info.RepoRelativePath))
            .SearchField(s => s.File.Info.RepositoryName, SearchBehavior.Sortword)
            .SearchField(s => s.File.Info.ProjectId, SearchBehavior.Sortword)
            .SearchField(s => s.File.Info.ProjectRelativePath, SearchBehavior.NormalizedKeyword)
            ;
        //.CopyTo(ss => ss.File.SourceFile.Content, ss => ss.Content)
        //.CopyTo(ss => ss.File.SourceFile.Info.RepoRelativePath, ss => ss.RepoRelativePath)
        //.CopyTo(ss => ss.BindingInfo.ProjectId, ss => ss.ProjectId)
        //.CopyTo(ss => ss.FilePath, ss => ss.FilePath);

        public static SearchType<ILanguageSearchModel> Language = SearchType.Create<ILanguageSearchModel>(RegisteredSearchTypes)
            .Route(ls => ls.Language.Name);

        public static SearchType<IRepositorySearchModel> Repository = SearchType.Create<IRepositorySearchModel>(RegisteredSearchTypes)
            .Route(rs => rs.Repository.Name)
            .WithObjectPath(rs => new[] { rs.Repository.Name })
            .SearchField(s => s.Repository.Name, SearchBehavior.NormalizedKeyword)
            ;

        public static SearchType<IProjectSearchModel> Project = SearchType.Create<IProjectSearchModel>(RegisteredSearchTypes)
            .Route(sm => sm.Project.ProjectId)
            .SearchField(s => s.Project.RepositoryName, SearchBehavior.Sortword)
            .SearchField(s => s.Project.ProjectId, SearchBehavior.Sortword)

            .Exclude(sm => sm.Project.ProjectReferences.First().Definitions.MarkForRemoval());

        public static SearchType<ICommitSearchModel> Commit = SearchType.Create<ICommitSearchModel>(RegisteredSearchTypes)
            .SearchField(s => s.Commit.CommitId, SearchBehavior.NormalizedKeyword)
            .SearchField(s => s.Commit.RepositoryName, SearchBehavior.Sortword)
            .SearchField(s => s.Commit.DateCommitted, SearchBehavior.Sortword)
            .SearchField(s => s.Commit.DateUploaded, SearchBehavior.Sortword)
            ;

        public static SearchType<IProjectReferenceSearchModel> ProjectReference = SearchType.Create<IProjectReferenceSearchModel>(RegisteredSearchTypes)
            .SearchField(s => s.ProjectReference.ProjectId, SearchBehavior.NormalizedKeyword, name: "ReferencedProjectId")
            .SearchField(s => s.ProjectId, SearchBehavior.Sortword)
            .SearchField(s => s.RepositoryName, SearchBehavior.Sortword)
            ;

        public static SearchType<IPropertySearchModel> Property = SearchType.Create<IPropertySearchModel>(RegisteredSearchTypes)
            .SearchField(s => s.OwnerId, SearchBehavior.NormalizedKeyword)
            .SearchField(s => s.Key, SearchBehavior.NormalizedKeyword)
            .SearchField(s => s.Value, SearchBehavior.NormalizedKeyword)
            
            ;

        public static SearchType<IStoredFilter> StoredFilter = SearchType.Create<IStoredFilter>(RegisteredSearchTypes);
    }

    /// <summary>
    /// Defines a stored filter which matches entities in a particular index shard in a stable manner
    /// </summary>
    public interface IStoredFilter : ISearchEntity<IStoredFilter>
    {
        ICommitInfo CommitInfo { get; }

        /// <summary>
        /// Stored filter bit set
        /// </summary>
        [SearchBehavior(SearchBehavior.None)]
        byte[] StableIds { get; }

        /// <summary>
        /// The hash of <see cref="Filter"/>
        /// </summary>
        [SearchBehavior(SearchBehavior.Term)]
        string FilterHash { get; }

        /// <summary>
        /// The count of elements matched by <see cref="Filter"/>
        /// </summary>
        int Cardinality { get; }
    }

    public interface IDefinitionSearchModel : ISearchEntity<IDefinitionSearchModel>
    {
        [CoerceGet(typeof(bool?))]
        bool ExcludeFromDefaultSearch { get; }

        [UseInterface]
        IDefinitionSymbol Definition { get; }

        [UseInterface]
        IDefinitionSymbolExtendedSearchInfo ExtendedSearchInfo { get; }
    }

    public interface ILanguageSearchModel : ISearchEntity<ILanguageSearchModel>
    {
        ILanguageInfo Language { get; }
    }

    public interface IReferenceSearchModel : ISearchEntity<IReferenceSearchModel>
    {
        /// <summary>
        /// The project location of this reference
        /// </summary>
        [UseInterface]
        IProjectFileScopeEntity FileInfo { get; }

        ReferenceKindSet ReferenceKind { get; }

        [UseInterface]
        ICodeSymbol Symbol { get; }

        [Include(ObjectStage.None)]
        IEnumerable<SymbolId> RelatedDefinition { get; }

        // Below is excluded from indexing for chunk model.

        ISymbolReferenceList References { get; }

        [UseInterface]
        [ReadOnlyList]
        [CoerceGet]
        [Include(ObjectStage.None)]
        IReadOnlyList<IReferenceSpan> Spans { get; }
    }

    public interface ISymbolReferenceList
    {
        [UseInterface]
        ICodeSymbol Symbol { get; }

        [ReadOnlyList]
        [CoerceGet]
        IReadOnlyList<ISharedReferenceInfoSpan> Spans { get; }

        /// <summary>
        /// The references in this file
        /// NOTE: Not serialized when indexing. since pointer to the content
        /// in the bound source file is included
        /// </summary>
        ISharedReferenceInfoSpanModel CompressedSpans { get; }
    }

    /// <summary>
    /// Defines the location and byte range of an object
    /// </summary>
    /// <param name="ObjectId">The object id of the blob containing the content. Typically a git sha.</param>
    /// <param name="ByteSpan">The byte span inside the blob representing the content of the referencing object.</param>
    [DataContract]
    public record ObjectContentLink<T>([property: DataMember] string ObjectId, [property: DataMember] Extent ByteSpan)
    {
        public ObjectContentLink<TNew> As<TNew>()
        {
            return new ObjectContentLink<TNew>(ObjectId, ByteSpan);
        }

        public ObjectContentLink<TNew> WithRange<TNew>(Extent<TNew> range)
        {
            return range.AsLink(ObjectId);
        }

        public ObjectContentLink<T> WithRange(Extent range)
        {
            return new ObjectContentLink<T>(ObjectId, range);
        }
    }

    public interface ISourceSearchModelBase : ISearchEntity
    {
    }

    public interface IBoundSourceSearchModel : ISourceSearchModelBase, ISearchEntity<IBoundSourceSearchModel>,
        IExternalEntity<IBoundSourceSearchModel, ObjectContentLink<IBoundSourceSearchModel>>
    {
        /// <summary>
        /// The binding info
        /// </summary>
        IBoundSourceInfo BindingInfo { get; }


        ISourceFileBase File { get; }

        /// <summary>
        /// Compressed list of classification spans
        /// </summary>
        [SearchBehavior(SearchBehavior.None)]
        IClassificationListModel CompressedClassifications { get; }

        /// <summary>
        /// Compressed list of reference spans
        /// </summary>
        IReadOnlyList<ISymbolReferenceList> References { get; }

        /// <summary>
        /// The content of the associated file. This is never persisted.
        /// </summary>
        //[Include(ObjectStage.Index, WhenDisabled = true, WhenFeature = nameof(Features.UseExternalStorage))]
        string Content { get; }
    }

    public interface ITextSourceSearchModel : ISourceSearchModelBase, ISearchEntity<ITextSourceSearchModel>
    {
        [Exclude(ObjectStage.BlockIndex)]
        [UseInterface]
        IProjectFileScopeEntity File { get; }

        [UseInterface]
        IChunkReference Chunk { get; }
    }

    public interface ITextChunkSearchModel : ISearchEntity<ITextChunkSearchModel>
    {
        /// <summary>
        /// The content of the chunk
        /// </summary>
        TextSourceBase Content { get; }
    }

    public interface IRepositorySearchModel : ISearchEntity<IRepositorySearchModel>
    {
        IRepository Repository { get; }
    }

    public interface IProjectSearchModel : ISearchEntity<IProjectSearchModel>
    {
        [UseInterface]
        IAnalyzedProjectInfo Project { get; }
    }

    public interface IProjectReferenceSearchModel : IProjectScopeEntity, ISearchEntity<IProjectReferenceSearchModel>
    {
        [UseInterface]
        IReferencedProject ProjectReference { get; }
    }

    public interface ICommitSearchModel : ISearchEntity<ICommitSearchModel>
    {
        ICommit Commit { get; }
    }
}

