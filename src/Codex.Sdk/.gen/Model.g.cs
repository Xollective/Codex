using Codex.ObjectModel.CompilerServices;

namespace Codex.ObjectModel.Implementation
{
    using static PropertyTarget;
    using Codex.Utilities.Serialization;
    using Codex.Sdk.Search;
    using static Descriptors;

    public interface IClient
    {
        IIndex<IBoundSourceSearchModel> BoundSourceIndex { get; }

        IIndex<ICommitSearchModel> CommitIndex { get; }

        IIndex<IDefinitionSearchModel> DefinitionIndex { get; }

        IIndex<ILanguageSearchModel> LanguageIndex { get; }

        IIndex<IProjectSearchModel> ProjectIndex { get; }

        IIndex<IProjectReferenceSearchModel> ProjectReferenceIndex { get; }

        IIndex<IPropertySearchModel> PropertyIndex { get; }

        IIndex<IReferenceSearchModel> ReferenceIndex { get; }

        IIndex<IRepositorySearchModel> RepositoryIndex { get; }

        IIndex<IStoredFilter> StoredFilterIndex { get; }

        IIndex<ITextChunkSearchModel> TextChunkIndex { get; }

        IIndex<ITextSourceSearchModel> TextSourceIndex { get; }
    }

    partial class ClientBase
    {
        protected ClientBase()
        {
            this._lazyBoundSourceIndex = GetIndexFactory(Codex.ObjectModel.SearchTypes.BoundSource);
            this._lazyCommitIndex = GetIndexFactory(Codex.ObjectModel.SearchTypes.Commit);
            this._lazyDefinitionIndex = GetIndexFactory(Codex.ObjectModel.SearchTypes.Definition);
            this._lazyLanguageIndex = GetIndexFactory(Codex.ObjectModel.SearchTypes.Language);
            this._lazyProjectIndex = GetIndexFactory(Codex.ObjectModel.SearchTypes.Project);
            this._lazyProjectReferenceIndex = GetIndexFactory(Codex.ObjectModel.SearchTypes.ProjectReference);
            this._lazyPropertyIndex = GetIndexFactory(Codex.ObjectModel.SearchTypes.Property);
            this._lazyReferenceIndex = GetIndexFactory(Codex.ObjectModel.SearchTypes.Reference);
            this._lazyRepositoryIndex = GetIndexFactory(Codex.ObjectModel.SearchTypes.Repository);
            this._lazyStoredFilterIndex = GetIndexFactory(Codex.ObjectModel.SearchTypes.StoredFilter);
            this._lazyTextChunkIndex = GetIndexFactory(Codex.ObjectModel.SearchTypes.TextChunk);
            this._lazyTextSourceIndex = GetIndexFactory(Codex.ObjectModel.SearchTypes.TextSource);
        }

        private readonly Lazy<IIndex<IBoundSourceSearchModel>> _lazyBoundSourceIndex;
        public IIndex<IBoundSourceSearchModel> BoundSourceIndex
        {
            get
            {
                return this._lazyBoundSourceIndex.Value;
            }
        }

        private readonly Lazy<IIndex<ICommitSearchModel>> _lazyCommitIndex;
        public IIndex<ICommitSearchModel> CommitIndex
        {
            get
            {
                return this._lazyCommitIndex.Value;
            }
        }

        private readonly Lazy<IIndex<IDefinitionSearchModel>> _lazyDefinitionIndex;
        public IIndex<IDefinitionSearchModel> DefinitionIndex
        {
            get
            {
                return this._lazyDefinitionIndex.Value;
            }
        }

        private readonly Lazy<IIndex<ILanguageSearchModel>> _lazyLanguageIndex;
        public IIndex<ILanguageSearchModel> LanguageIndex
        {
            get
            {
                return this._lazyLanguageIndex.Value;
            }
        }

        private readonly Lazy<IIndex<IProjectSearchModel>> _lazyProjectIndex;
        public IIndex<IProjectSearchModel> ProjectIndex
        {
            get
            {
                return this._lazyProjectIndex.Value;
            }
        }

        private readonly Lazy<IIndex<IProjectReferenceSearchModel>> _lazyProjectReferenceIndex;
        public IIndex<IProjectReferenceSearchModel> ProjectReferenceIndex
        {
            get
            {
                return this._lazyProjectReferenceIndex.Value;
            }
        }

        private readonly Lazy<IIndex<IPropertySearchModel>> _lazyPropertyIndex;
        public IIndex<IPropertySearchModel> PropertyIndex
        {
            get
            {
                return this._lazyPropertyIndex.Value;
            }
        }

        private readonly Lazy<IIndex<IReferenceSearchModel>> _lazyReferenceIndex;
        public IIndex<IReferenceSearchModel> ReferenceIndex
        {
            get
            {
                return this._lazyReferenceIndex.Value;
            }
        }

        private readonly Lazy<IIndex<IRepositorySearchModel>> _lazyRepositoryIndex;
        public IIndex<IRepositorySearchModel> RepositoryIndex
        {
            get
            {
                return this._lazyRepositoryIndex.Value;
            }
        }

        private readonly Lazy<IIndex<IStoredFilter>> _lazyStoredFilterIndex;
        public IIndex<IStoredFilter> StoredFilterIndex
        {
            get
            {
                return this._lazyStoredFilterIndex.Value;
            }
        }

        private readonly Lazy<IIndex<ITextChunkSearchModel>> _lazyTextChunkIndex;
        public IIndex<ITextChunkSearchModel> TextChunkIndex
        {
            get
            {
                return this._lazyTextChunkIndex.Value;
            }
        }

        private readonly Lazy<IIndex<ITextSourceSearchModel>> _lazyTextSourceIndex;
        public IIndex<ITextSourceSearchModel> TextSourceIndex
        {
            get
            {
                return this._lazyTextSourceIndex.Value;
            }
        }
    }

    public class SearchMappings
    {
        public class BoundSource
        {
            public static IMappingField<IBoundSourceSearchModel, string> StoredFilterTag { get; } = Codex.ObjectModel.SearchTypes.BoundSource.GetMappingField<string>();
            public static ISortField<IBoundSourceSearchModel, int> StableId { get; } = Codex.ObjectModel.SearchTypes.BoundSource.GetMappingField<int>();
            public static ISortField<IBoundSourceSearchModel, string> RepositoryName { get; } = Codex.ObjectModel.SearchTypes.BoundSource.GetMappingField<string>();
            public static ISortField<IBoundSourceSearchModel, string> ProjectId { get; } = Codex.ObjectModel.SearchTypes.BoundSource.GetMappingField<string>();
            public static IMappingField<IBoundSourceSearchModel, string> ProjectRelativePath { get; } = Codex.ObjectModel.SearchTypes.BoundSource.GetMappingField<string>();
        }

        public class Commit
        {
            public static IMappingField<ICommitSearchModel, string> StoredFilterTag { get; } = Codex.ObjectModel.SearchTypes.Commit.GetMappingField<string>();
            public static ISortField<ICommitSearchModel, int> StableId { get; } = Codex.ObjectModel.SearchTypes.Commit.GetMappingField<int>();
            public static IMappingField<ICommitSearchModel, string> CommitId { get; } = Codex.ObjectModel.SearchTypes.Commit.GetMappingField<string>();
            public static ISortField<ICommitSearchModel, string> RepositoryName { get; } = Codex.ObjectModel.SearchTypes.Commit.GetMappingField<string>();
            public static ISortField<ICommitSearchModel, DateTime> DateCommitted { get; } = Codex.ObjectModel.SearchTypes.Commit.GetMappingField<DateTime>();
            public static ISortField<ICommitSearchModel, DateTime> DateUploaded { get; } = Codex.ObjectModel.SearchTypes.Commit.GetMappingField<DateTime>();
        }

        public class Definition
        {
            public static IMappingField<IDefinitionSearchModel, string> StoredFilterTag { get; } = Codex.ObjectModel.SearchTypes.Definition.GetMappingField<string>();
            public static ISortField<IDefinitionSearchModel, int> StableId { get; } = Codex.ObjectModel.SearchTypes.Definition.GetMappingField<int>();
            public static ISortField<IDefinitionSearchModel, string> ProjectId { get; } = Codex.ObjectModel.SearchTypes.Definition.GetMappingField<string>();
            public static IMappingField<IDefinitionSearchModel, string> ExtensionProjectId { get; } = Codex.ObjectModel.SearchTypes.Definition.GetMappingField<string>();
            public static IMappingField<IDefinitionSearchModel, string> Id { get; } = Codex.ObjectModel.SearchTypes.Definition.GetMappingField<string>();
            public static IMappingField<IDefinitionSearchModel, bool> ExcludeFromDefaultSearch { get; } = Codex.ObjectModel.SearchTypes.Definition.GetMappingField<bool>();
            public static IMappingField<IDefinitionSearchModel, string> ContainerTypeSymbolId { get; } = Codex.ObjectModel.SearchTypes.Definition.GetMappingField<string>();
            public static IMappingField<IDefinitionSearchModel, long> ConstantValue { get; } = Codex.ObjectModel.SearchTypes.Definition.GetMappingField<long>();
            public static IMappingField<IDefinitionSearchModel, string> ContainerQualifiedName { get; } = Codex.ObjectModel.SearchTypes.Definition.GetMappingField<string>();
            public static ISortField<IDefinitionSearchModel, StringEnum<SymbolKinds>> Kind { get; } = Codex.ObjectModel.SearchTypes.Definition.GetMappingField<StringEnum<SymbolKinds>>();
            public static IMappingField<IDefinitionSearchModel, string> ShortName { get; } = Codex.ObjectModel.SearchTypes.Definition.GetMappingField<string>();
            public static IMappingField<IDefinitionSearchModel, string> AbbreviatedName { get; } = Codex.ObjectModel.SearchTypes.Definition.GetMappingField<string>();
            public static IMappingField<IDefinitionSearchModel, string> ExtensionContainerQualifiedName { get; } = Codex.ObjectModel.SearchTypes.Definition.GetMappingField<string>();
            public static IMappingField<IDefinitionSearchModel, string> Modifiers { get; } = Codex.ObjectModel.SearchTypes.Definition.GetMappingField<string>();
            public static IMappingField<IDefinitionSearchModel, string> Keywords { get; } = Codex.ObjectModel.SearchTypes.Definition.GetMappingField<string>();
        }

        public class Language
        {
            public static IMappingField<ILanguageSearchModel, string> StoredFilterTag { get; } = Codex.ObjectModel.SearchTypes.Language.GetMappingField<string>();
            public static ISortField<ILanguageSearchModel, int> StableId { get; } = Codex.ObjectModel.SearchTypes.Language.GetMappingField<int>();
        }

        public class Project
        {
            public static IMappingField<IProjectSearchModel, string> StoredFilterTag { get; } = Codex.ObjectModel.SearchTypes.Project.GetMappingField<string>();
            public static ISortField<IProjectSearchModel, int> StableId { get; } = Codex.ObjectModel.SearchTypes.Project.GetMappingField<int>();
            public static ISortField<IProjectSearchModel, string> RepositoryName { get; } = Codex.ObjectModel.SearchTypes.Project.GetMappingField<string>();
            public static ISortField<IProjectSearchModel, string> ProjectId { get; } = Codex.ObjectModel.SearchTypes.Project.GetMappingField<string>();
        }

        public class ProjectReference
        {
            public static IMappingField<IProjectReferenceSearchModel, string> StoredFilterTag { get; } = Codex.ObjectModel.SearchTypes.ProjectReference.GetMappingField<string>();
            public static ISortField<IProjectReferenceSearchModel, int> StableId { get; } = Codex.ObjectModel.SearchTypes.ProjectReference.GetMappingField<int>();
            public static IMappingField<IProjectReferenceSearchModel, string> ReferencedProjectId { get; } = Codex.ObjectModel.SearchTypes.ProjectReference.GetMappingField<string>();
            public static ISortField<IProjectReferenceSearchModel, string> ProjectId { get; } = Codex.ObjectModel.SearchTypes.ProjectReference.GetMappingField<string>();
            public static ISortField<IProjectReferenceSearchModel, string> RepositoryName { get; } = Codex.ObjectModel.SearchTypes.ProjectReference.GetMappingField<string>();
        }

        public class Property
        {
            public static IMappingField<IPropertySearchModel, string> StoredFilterTag { get; } = Codex.ObjectModel.SearchTypes.Property.GetMappingField<string>();
            public static ISortField<IPropertySearchModel, int> StableId { get; } = Codex.ObjectModel.SearchTypes.Property.GetMappingField<int>();
            public static IMappingField<IPropertySearchModel, int> OwnerId { get; } = Codex.ObjectModel.SearchTypes.Property.GetMappingField<int>();
            public static IMappingField<IPropertySearchModel, StringEnum<PropertyKey>> Key { get; } = Codex.ObjectModel.SearchTypes.Property.GetMappingField<StringEnum<PropertyKey>>();
            public static IMappingField<IPropertySearchModel, string> Value { get; } = Codex.ObjectModel.SearchTypes.Property.GetMappingField<string>();
        }

        public class Reference
        {
            public static IMappingField<IReferenceSearchModel, string> StoredFilterTag { get; } = Codex.ObjectModel.SearchTypes.Reference.GetMappingField<string>();
            public static ISortField<IReferenceSearchModel, int> StableId { get; } = Codex.ObjectModel.SearchTypes.Reference.GetMappingField<int>();
            public static ISortField<IReferenceSearchModel, string> RepositoryName { get; } = Codex.ObjectModel.SearchTypes.Reference.GetMappingField<string>();
            public static ISortField<IReferenceSearchModel, string> ReferencingProjectId { get; } = Codex.ObjectModel.SearchTypes.Reference.GetMappingField<string>();
            public static IMappingField<IReferenceSearchModel, string> ProjectId { get; } = Codex.ObjectModel.SearchTypes.Reference.GetMappingField<string>();
            public static IMappingField<IReferenceSearchModel, string> Id { get; } = Codex.ObjectModel.SearchTypes.Reference.GetMappingField<string>();
            public static ISortField<IReferenceSearchModel, int> Rank { get; } = Codex.ObjectModel.SearchTypes.Reference.GetMappingField<int>();
            public static ISortField<IReferenceSearchModel, long> SortedReferenceKey { get; } = Codex.ObjectModel.SearchTypes.Reference.GetMappingField<long>();
            public static IMappingField<IReferenceSearchModel, ReferenceKind> ReferenceKind { get; } = Codex.ObjectModel.SearchTypes.Reference.GetMappingField<ReferenceKind>();
            public static IMappingField<IReferenceSearchModel, SymbolId> RelatedDefinition { get; } = Codex.ObjectModel.SearchTypes.Reference.GetMappingField<SymbolId>();
        }

        public class Repository
        {
            public static IMappingField<IRepositorySearchModel, string> StoredFilterTag { get; } = Codex.ObjectModel.SearchTypes.Repository.GetMappingField<string>();
            public static ISortField<IRepositorySearchModel, int> StableId { get; } = Codex.ObjectModel.SearchTypes.Repository.GetMappingField<int>();
            public static IMappingField<IRepositorySearchModel, string> Name { get; } = Codex.ObjectModel.SearchTypes.Repository.GetMappingField<string>();
        }

        public class StoredFilter
        {
            public static IMappingField<IStoredFilter, string> StoredFilterTag { get; } = Codex.ObjectModel.SearchTypes.StoredFilter.GetMappingField<string>();
            public static ISortField<IStoredFilter, int> StableId { get; } = Codex.ObjectModel.SearchTypes.StoredFilter.GetMappingField<int>();
        }

        public class TextChunk
        {
            public static IMappingField<ITextChunkSearchModel, string> StoredFilterTag { get; } = Codex.ObjectModel.SearchTypes.TextChunk.GetMappingField<string>();
            public static ISortField<ITextChunkSearchModel, int> StableId { get; } = Codex.ObjectModel.SearchTypes.TextChunk.GetMappingField<int>();
            public static IMappingField<ITextChunkSearchModel, TextSourceBase> Content { get; } = Codex.ObjectModel.SearchTypes.TextChunk.GetMappingField<TextSourceBase>();
        }

        public class TextSource
        {
            public static IMappingField<ITextSourceSearchModel, string> StoredFilterTag { get; } = Codex.ObjectModel.SearchTypes.TextSource.GetMappingField<string>();
            public static ISortField<ITextSourceSearchModel, int> StableId { get; } = Codex.ObjectModel.SearchTypes.TextSource.GetMappingField<int>();
            public static IMappingField<ITextSourceSearchModel, int> ChunkId { get; } = Codex.ObjectModel.SearchTypes.TextSource.GetMappingField<int>();
        }
    }

    public partial class AnalyzedProjectInfo : ReferencedProject, IAnalyzedProjectInfo, IProjectScopeEntity, IRepoScopeEntity, IPropertyTarget<IAnalyzedProjectInfo, AnalyzedProjectInfo>, IEntity<AnalyzedProjectInfo, IAnalyzedProjectInfo, AnalyzedProjectInfoDescriptor>
    {
        public AnalyzedProjectInfo()
        {
        }

        public AnalyzedProjectInfo(IAnalyzedProjectInfo source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public AnalyzedProjectInfo(IProjectScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public AnalyzedProjectInfo(IReferencedProject source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public AnalyzedProjectInfo(IRepoScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static AnalyzedProjectInfo ICreate<AnalyzedProjectInfo>.Create()
        {
            return new AnalyzedProjectInfo();
        }

        public List<ProjectFileScopeEntity> Files
        {
            get => Coerce(ref this.m_Files);
            set
            {
                this.m_Files = value;
            }
        }

        private List<ProjectFileScopeEntity> m_Files;
        IReadOnlyList<IProjectFileScopeEntity> IAnalyzedProjectInfo.Files { get => CoerceReadOnly(ref this.m_Files); }
        public ProjectFileScopeEntity PrimaryFile { get; set; }

        IProjectFileScopeEntity IAnalyzedProjectInfo.PrimaryFile { get => PrimaryFile; }
        public StringEnum<ProjectKind> ProjectKind { get; set; }

        public List<ReferencedProject> ProjectReferences
        {
            get => Coerce(ref this.m_ProjectReferences);
            set
            {
                this.m_ProjectReferences = value;
            }
        }

        private List<ReferencedProject> m_ProjectReferences;
        IReadOnlyList<IReferencedProject> IAnalyzedProjectInfo.ProjectReferences { get => CoerceReadOnly(ref this.m_ProjectReferences); }
        public string Qualifier { get; set; }
    }

    public partial class BoundSourceFile : BoundSourceInfo, IBoundSourceFile, IPropertyTarget<IBoundSourceFile, BoundSourceFile>, IEntity<BoundSourceFile, IBoundSourceFile, BoundSourceFileDescriptor>
    {
        public BoundSourceFile()
        {
        }

        public BoundSourceFile(IBoundSourceFile source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public BoundSourceFile(IBoundSourceInfo source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static BoundSourceFile ICreate<BoundSourceFile>.Create()
        {
            return new BoundSourceFile();
        }

        public Commit Commit { get; set; }

        ICommit IBoundSourceFile.Commit { get => Commit; }
        public SourceFile SourceFile { get; set; }

        ISourceFile IBoundSourceFile.SourceFile { get => SourceFile; }
    }

    public partial class BoundSourceInfo : EntityBase, IBoundSourceInfo, IPropertyTarget<IBoundSourceInfo, BoundSourceInfo>, IEntity<BoundSourceInfo, IBoundSourceInfo, BoundSourceInfoDescriptor>
    {
        public BoundSourceInfo()
        {
        }

        public BoundSourceInfo(IBoundSourceInfo source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static BoundSourceInfo ICreate<BoundSourceInfo>.Create()
        {
            return new BoundSourceInfo();
        }

        public IReadOnlyList<ClassificationSpan> Classifications { get; set; } = System.Array.Empty<ClassificationSpan>();

        IReadOnlyList<IClassificationSpan> IBoundSourceInfo.Classifications { get => Classifications; }

        public int DefinitionCount
        {
            get => CoerceDefinitionCount(this.m_DefinitionCount);
            set
            {
                this.m_DefinitionCount = value;
            }
        }

        private Nullable<int> m_DefinitionCount;
        public IReadOnlyList<DefinitionSpan> Definitions { get; set; } = System.Array.Empty<DefinitionSpan>();

        IReadOnlyList<IDefinitionSpan> IBoundSourceInfo.Definitions { get => Definitions; }

        public int ReferenceCount
        {
            get => CoerceReferenceCount(this.m_ReferenceCount);
            set
            {
                this.m_ReferenceCount = value;
            }
        }

        private Nullable<int> m_ReferenceCount;
        public IReadOnlyList<ReferenceSpan> References { get; set; } = System.Array.Empty<ReferenceSpan>();

        IReadOnlyList<IReferenceSpan> IBoundSourceInfo.References { get => References; }
    }

    public partial class BoundSourceSearchModel : SourceSearchModelBase, IBoundSourceSearchModel, ISearchEntity, IExternalEntity<IBoundSourceSearchModel, ObjectContentLink<IBoundSourceSearchModel>>, IExternalSearchEntity<ObjectContentLink<IBoundSourceSearchModel>>, IExternalEntity<ObjectContentLink<IBoundSourceSearchModel>>, IPropertyTarget<IBoundSourceSearchModel, BoundSourceSearchModel>, IEntity<BoundSourceSearchModel, IBoundSourceSearchModel, BoundSourceSearchModelDescriptor>
    {
        public BoundSourceSearchModel()
        {
        }

        public BoundSourceSearchModel(IBoundSourceSearchModel source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public BoundSourceSearchModel(ISearchEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public BoundSourceSearchModel(ISourceSearchModelBase source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static BoundSourceSearchModel ICreate<BoundSourceSearchModel>.Create()
        {
            return new BoundSourceSearchModel();
        }

        public BoundSourceInfo BindingInfo { get; set; }

        IBoundSourceInfo IBoundSourceSearchModel.BindingInfo { get => BindingInfo; }
        public ClassificationListModel CompressedClassifications { get; set; }

        IClassificationListModel IBoundSourceSearchModel.CompressedClassifications { get => CompressedClassifications; }
        public string Content { get; set; }
        public SourceFileBase File { get; set; }

        ISourceFileBase IBoundSourceSearchModel.File { get => File; }

        public List<SymbolReferenceList> References
        {
            get => Coerce(ref this.m_References);
            set
            {
                this.m_References = value;
            }
        }

        private List<SymbolReferenceList> m_References;
        IReadOnlyList<ISymbolReferenceList> IBoundSourceSearchModel.References { get => CoerceReadOnly(ref this.m_References); }
        public ObjectContentLink<IBoundSourceSearchModel> ExternalLink { get; set; }
    }

    public partial class Branch : EntityBase, IBranch, IPropertyTarget<IBranch, Branch>, IEntity<Branch, IBranch, BranchDescriptor>
    {
        public Branch()
        {
        }

        public Branch(IBranch source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static Branch ICreate<Branch>.Create()
        {
            return new Branch();
        }

        public string Description { get; set; }
        public string HeadCommitId { get; set; }
        public string Name { get; set; }
    }

    public partial class ChunkedSourceFile : SourceFileBase, IChunkedSourceFile, IPropertyTarget<IChunkedSourceFile, ChunkedSourceFile>, IEntity<ChunkedSourceFile, IChunkedSourceFile, ChunkedSourceFileDescriptor>
    {
        public ChunkedSourceFile()
        {
        }

        public ChunkedSourceFile(IChunkedSourceFile source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ChunkedSourceFile(ISourceFileBase source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static ChunkedSourceFile ICreate<ChunkedSourceFile>.Create()
        {
            return new ChunkedSourceFile();
        }

        public List<ChunkReference> Chunks
        {
            get => Coerce(ref this.m_Chunks);
            set
            {
                this.m_Chunks = value;
            }
        }

        private List<ChunkReference> m_Chunks;
        IReadOnlyList<IChunkReference> IChunkedSourceFile.Chunks { get => CoerceReadOnly(ref this.m_Chunks); }
    }

    public partial class ChunkReference : EntityBase, IChunkReference, IPropertyTarget<IChunkReference, ChunkReference>, IEntity<ChunkReference, IChunkReference, ChunkReferenceDescriptor>
    {
        public ChunkReference()
        {
        }

        public ChunkReference(IChunkReference source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static ChunkReference ICreate<ChunkReference>.Create()
        {
            return new ChunkReference();
        }

        public int Id { get; set; }
        public int StartLineNumber { get; set; }
    }

    public partial class ClassificationSpan : Span, IClassificationSpan, IPropertyTarget<IClassificationSpan, ClassificationSpan>, IEntity<ClassificationSpan, IClassificationSpan, ClassificationSpanDescriptor>
    {
        public ClassificationSpan()
        {
        }

        public ClassificationSpan(IClassificationSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ClassificationSpan(ISpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static ClassificationSpan ICreate<ClassificationSpan>.Create()
        {
            return new ClassificationSpan();
        }

        public StringEnum<ClassificationName> Classification { get; set; }
        public int DefaultClassificationColor { get; set; }
        public int LocalGroupId { get; set; }
        public int SymbolDepth { get; set; }
    }

    public partial class ClassificationStyle : EntityBase, IClassificationStyle, IPropertyTarget<IClassificationStyle, ClassificationStyle>, IEntity<ClassificationStyle, IClassificationStyle, ClassificationStyleDescriptor>
    {
        public ClassificationStyle()
        {
        }

        public ClassificationStyle(IClassificationStyle source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static ClassificationStyle ICreate<ClassificationStyle>.Create()
        {
            return new ClassificationStyle();
        }

        public int Color { get; set; }
        public bool Italic { get; set; }
        public StringEnum<ClassificationName> Name { get; set; }
    }

    public partial class CodeSymbol : EntityBase, ICodeSymbol, IPropertyTarget<ICodeSymbol, CodeSymbol>, IEntity<CodeSymbol, ICodeSymbol, CodeSymbolDescriptor>
    {
        public CodeSymbol()
        {
        }

        public CodeSymbol(ICodeSymbol source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static CodeSymbol ICreate<CodeSymbol>.Create()
        {
            return new CodeSymbol();
        }

        public SymbolId Id { get; set; }
        public StringEnum<SymbolKinds> Kind { get; set; }
        public string ProjectId { get; set; }
    }

    public partial class Commit : CommitInfo, ICommit, ICommitScopeEntity, IRepoScopeEntity, IPropertyTarget<ICommit, Commit>, IEntity<Commit, ICommit, CommitDescriptor>
    {
        public Commit()
        {
        }

        public Commit(ICommit source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public Commit(ICommitInfo source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public Commit(ICommitScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public Commit(IRepoScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static Commit ICreate<Commit>.Create()
        {
            return new Commit();
        }

        public string Description { get; set; }

        public List<string> ParentCommitIds
        {
            get => Coerce(ref this.m_ParentCommitIds);
            set
            {
                this.m_ParentCommitIds = value;
            }
        }

        private List<string> m_ParentCommitIds;
        IReadOnlyList<string> ICommit.ParentCommitIds { get => CoerceReadOnly(ref this.m_ParentCommitIds); }
    }

    public partial class CommitInfo : CommitScopeEntity, ICommitInfo, IRepoScopeEntity, IPropertyTarget<ICommitInfo, CommitInfo>, IEntity<CommitInfo, ICommitInfo, CommitInfoDescriptor>
    {
        public CommitInfo()
        {
        }

        public CommitInfo(ICommitInfo source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public CommitInfo(ICommitScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public CommitInfo(IRepoScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static CommitInfo ICreate<CommitInfo>.Create()
        {
            return new CommitInfo();
        }

        public string Alias { get; set; }
        public string BuildUri { get; set; }
        public DateTime DateCommitted { get; set; }
        public DateTime DateUploaded { get; set; }
    }

    public partial class CommitScopeEntity : RepoScopeEntity, ICommitScopeEntity, IPropertyTarget<ICommitScopeEntity, CommitScopeEntity>, IEntity<CommitScopeEntity, ICommitScopeEntity, CommitScopeEntityDescriptor>
    {
        public CommitScopeEntity()
        {
        }

        public CommitScopeEntity(ICommitScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public CommitScopeEntity(IRepoScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static CommitScopeEntity ICreate<CommitScopeEntity>.Create()
        {
            return new CommitScopeEntity();
        }

        public string CommitId { get; set; }
    }

    public partial class CommitSearchModel : SearchEntity, ICommitSearchModel, IPropertyTarget<ICommitSearchModel, CommitSearchModel>, IEntity<CommitSearchModel, ICommitSearchModel, CommitSearchModelDescriptor>
    {
        public CommitSearchModel()
        {
        }

        public CommitSearchModel(ICommitSearchModel source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public CommitSearchModel(ISearchEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static CommitSearchModel ICreate<CommitSearchModel>.Create()
        {
            return new CommitSearchModel();
        }

        public Commit Commit { get; set; }

        ICommit ICommitSearchModel.Commit { get => Commit; }
    }

    public partial class DefinitionSearchModel : SearchEntity, IDefinitionSearchModel, IPropertyTarget<IDefinitionSearchModel, DefinitionSearchModel>, IEntity<DefinitionSearchModel, IDefinitionSearchModel, DefinitionSearchModelDescriptor>
    {
        public DefinitionSearchModel()
        {
        }

        public DefinitionSearchModel(IDefinitionSearchModel source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public DefinitionSearchModel(ISearchEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static DefinitionSearchModel ICreate<DefinitionSearchModel>.Create()
        {
            return new DefinitionSearchModel();
        }

        public IDefinitionSymbol Definition { get; set; }

        public bool ExcludeFromDefaultSearch
        {
            get => CoerceExcludeFromDefaultSearch(this.m_ExcludeFromDefaultSearch);
            set
            {
                this.m_ExcludeFromDefaultSearch = value;
            }
        }

        private Nullable<bool> m_ExcludeFromDefaultSearch;
        public IDefinitionSymbolExtendedSearchInfo ExtendedSearchInfo { get; set; }
    }

    public partial class DefinitionSpan : Span, IDefinitionSpan, IPropertyTarget<IDefinitionSpan, DefinitionSpan>, IEntity<DefinitionSpan, IDefinitionSpan, DefinitionSpanDescriptor>
    {
        public DefinitionSpan()
        {
        }

        public DefinitionSpan(IDefinitionSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public DefinitionSpan(ISpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static DefinitionSpan ICreate<DefinitionSpan>.Create()
        {
            return new DefinitionSpan();
        }

        public DefinitionSymbol Definition { get; set; }

        IDefinitionSymbol IDefinitionSpan.Definition { get => Definition; }
        public Extent FullSpan { get; set; }
        public IReadOnlyList<ParameterDefinitionSpan> Parameters { get; set; } = System.Array.Empty<ParameterDefinitionSpan>();

        IReadOnlyList<IParameterDefinitionSpan> IDefinitionSpan.Parameters { get => Parameters; }
    }

    public partial class DefinitionSymbol : ReferenceSymbol, IDefinitionSymbol, ICodeSymbol, IDisplayCodeSymbol, IJsonRangeTracking<IDefinitionSymbol>, IPropertyTarget<IDefinitionSymbol, DefinitionSymbol>, IPropertyTarget<IDisplayCodeSymbol, DisplayCodeSymbol>, IEntity<DefinitionSymbol, IDefinitionSymbol, DefinitionSymbolDescriptor>
    {
        public DefinitionSymbol(IReferenceSymbol source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public DefinitionSymbol()
        {
        }

        public DefinitionSymbol(ICodeSymbol source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public DefinitionSymbol(IDefinitionSymbol source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public DefinitionSymbol(IDisplayCodeSymbol source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static DefinitionSymbol ICreate<DefinitionSymbol>.Create()
        {
            return new DefinitionSymbol();
        }

        public string Comment { get; set; }
        public string DisplayName { get; set; }

        IReadOnlyList<ClassifiedExtent> IDisplayCodeSymbol.Classifications { get => Classifications; }
        public IReadOnlyList<ClassifiedExtent> Classifications { get; set; } = System.Array.Empty<ClassifiedExtent>();

        public string AbbreviatedName
        {
            get => CoerceAbbreviatedName(this.m_AbbreviatedName);
            set
            {
                this.m_AbbreviatedName = value;
            }
        }

        public string Uid { get; set; }
        public string TypeName { get; set; }
        public int SymbolDepth { get; set; }

        private string m_ShortName;
        public string ShortName
        {
            get => CoerceShortName(this.m_ShortName);
            set
            {
                this.m_ShortName = value;
            }
        }

        private int m_ReferenceCount;
        public int ReferenceCount
        {
            get => CoerceReferenceCount(this.m_ReferenceCount);
            set
            {
                this.m_ReferenceCount = value;
            }
        }

        IReadOnlyList<string> IDefinitionSymbol.Modifiers { get => CoerceReadOnly(ref this.m_Modifiers); }

        private List<string> m_Modifiers;
        public List<string> Modifiers
        {
            get => Coerce(ref this.m_Modifiers);
            set
            {
                this.m_Modifiers = value;
            }
        }

        IReadOnlyList<string> IDefinitionSymbol.Keywords { get => CoerceReadOnly(ref this.m_Keywords); }

        private List<string> m_Keywords;
        public Nullable<Extent<IDefinitionSymbol>> JsonRange { get; set; }
        public Glyph Glyph { get; set; }

        IDefinitionSymbolExtensionInfo IDefinitionSymbol.ExtensionInfo { get => ExtensionInfo; }
        public DefinitionSymbolExtensionInfo ExtensionInfo { get; set; }

        IReadOnlyList<IDefinitionSymbolExtendedSearchInfo> IDefinitionSymbol.ExtendedSearchInfo { get => CoerceReadOnly(ref this.m_ExtendedSearchInfo); }

        private List<DefinitionSymbolExtendedSearchInfo> m_ExtendedSearchInfo;
        public List<DefinitionSymbolExtendedSearchInfo> ExtendedSearchInfo
        {
            get => Coerce(ref this.m_ExtendedSearchInfo);
            set
            {
                this.m_ExtendedSearchInfo = value;
            }
        }

        public bool ExcludeFromDefaultSearch { get; set; }
        public string DeclarationName { get; set; }
        public SymbolId ContainerTypeSymbolId { get; set; }
        public string ContainerQualifiedName { get; set; }

        private string m_AbbreviatedName;
        public List<string> Keywords
        {
            get => Coerce(ref this.m_Keywords);
            set
            {
                this.m_Keywords = value;
            }
        }
    }

    public partial class DefinitionSymbolExtendedSearchInfo : EntityBase, IDefinitionSymbolExtendedSearchInfo, IPropertyTarget<IDefinitionSymbolExtendedSearchInfo, DefinitionSymbolExtendedSearchInfo>, IEntity<DefinitionSymbolExtendedSearchInfo, IDefinitionSymbolExtendedSearchInfo, DefinitionSymbolExtendedSearchInfoDescriptor>
    {
        public DefinitionSymbolExtendedSearchInfo()
        {
        }

        public DefinitionSymbolExtendedSearchInfo(IDefinitionSymbolExtendedSearchInfo source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static DefinitionSymbolExtendedSearchInfo ICreate<DefinitionSymbolExtendedSearchInfo>.Create()
        {
            return new DefinitionSymbolExtendedSearchInfo();
        }

        public Nullable<long> ConstantValue { get; set; }
    }

    public partial class DefinitionSymbolExtensionInfo : EntityBase, IDefinitionSymbolExtensionInfo, IPropertyTarget<IDefinitionSymbolExtensionInfo, DefinitionSymbolExtensionInfo>, IEntity<DefinitionSymbolExtensionInfo, IDefinitionSymbolExtensionInfo, DefinitionSymbolExtensionInfoDescriptor>
    {
        public DefinitionSymbolExtensionInfo()
        {
        }

        public DefinitionSymbolExtensionInfo(IDefinitionSymbolExtensionInfo source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static DefinitionSymbolExtensionInfo ICreate<DefinitionSymbolExtensionInfo>.Create()
        {
            return new DefinitionSymbolExtensionInfo();
        }

        public string ContainerQualifiedName { get; set; }
        public string ProjectId { get; set; }
    }

    public partial class DirectoryRepositoryStoreInfo : RepositoryStoreInfo, IDirectoryRepositoryStoreInfo, IPropertyTarget<IDirectoryRepositoryStoreInfo, DirectoryRepositoryStoreInfo>, IEntity<DirectoryRepositoryStoreInfo, IDirectoryRepositoryStoreInfo, DirectoryRepositoryStoreInfoDescriptor>
    {
        public DirectoryRepositoryStoreInfo()
        {
        }

        public DirectoryRepositoryStoreInfo(IDirectoryRepositoryStoreInfo source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public DirectoryRepositoryStoreInfo(IRepositoryStoreInfo source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static DirectoryRepositoryStoreInfo ICreate<DirectoryRepositoryStoreInfo>.Create()
        {
            return new DirectoryRepositoryStoreInfo();
        }

        public DirectoryStoreFormat Format { get; set; }
    }

    public partial class DisplayCodeSymbol : CodeSymbol, IDisplayCodeSymbol, IPropertyTarget<IDisplayCodeSymbol, DisplayCodeSymbol>, IEntity<DisplayCodeSymbol, IDisplayCodeSymbol, DisplayCodeSymbolDescriptor>
    {
        public DisplayCodeSymbol()
        {
        }

        public DisplayCodeSymbol(ICodeSymbol source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public DisplayCodeSymbol(IDisplayCodeSymbol source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static DisplayCodeSymbol ICreate<DisplayCodeSymbol>.Create()
        {
            return new DisplayCodeSymbol();
        }

        public IReadOnlyList<ClassifiedExtent> Classifications { get; set; } = System.Array.Empty<ClassifiedExtent>();

        IReadOnlyList<ClassifiedExtent> IDisplayCodeSymbol.Classifications { get => Classifications; }
        public string DisplayName { get; set; }
    }

    public partial class DocumentationReferenceSymbol : ReferenceSymbol, IDocumentationReferenceSymbol, ICodeSymbol, IPropertyTarget<IDocumentationReferenceSymbol, DocumentationReferenceSymbol>, IEntity<DocumentationReferenceSymbol, IDocumentationReferenceSymbol, DocumentationReferenceSymbolDescriptor>
    {
        public DocumentationReferenceSymbol()
        {
        }

        public DocumentationReferenceSymbol(ICodeSymbol source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public DocumentationReferenceSymbol(IDocumentationReferenceSymbol source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public DocumentationReferenceSymbol(IReferenceSymbol source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static DocumentationReferenceSymbol ICreate<DocumentationReferenceSymbol>.Create()
        {
            return new DocumentationReferenceSymbol();
        }

        public string Comment { get; set; }
        public string DisplayName { get; set; }
    }

    public partial class FileSpanResult : EntityBase, IFileSpanResult, IPropertyTarget<IFileSpanResult, FileSpanResult>, IEntity<FileSpanResult, IFileSpanResult, FileSpanResultDescriptor>
    {
        public FileSpanResult()
        {
        }

        public FileSpanResult(IFileSpanResult source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static FileSpanResult ICreate<FileSpanResult>.Create()
        {
            return new FileSpanResult();
        }

        public IProjectFileScopeEntity File { get; set; }
    }

    public partial class GlobalStoredRepositorySettings : EntityBase, IGlobalStoredRepositorySettings, IPropertyTarget<IGlobalStoredRepositorySettings, GlobalStoredRepositorySettings>, IEntity<GlobalStoredRepositorySettings, IGlobalStoredRepositorySettings, GlobalStoredRepositorySettingsDescriptor>
    {
        public GlobalStoredRepositorySettings()
        {
        }

        public GlobalStoredRepositorySettings(IGlobalStoredRepositorySettings source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static GlobalStoredRepositorySettings ICreate<GlobalStoredRepositorySettings>.Create()
        {
            return new GlobalStoredRepositorySettings();
        }

        public ImmutableDictionary<RepoName, IStoredRepositoryGroupSettings> Groups { get; set; } = System.Collections.Immutable.ImmutableDictionary<RepoName, IStoredRepositoryGroupSettings>.Empty;
        public ImmutableDictionary<RepoName, IStoredRepositorySettings> Repositories { get; set; } = System.Collections.Immutable.ImmutableDictionary<RepoName, IStoredRepositorySettings>.Empty;
    }

    public partial class HeaderInfo : EntityBase, IHeaderInfo, IPropertyTarget<IHeaderInfo, HeaderInfo>, IEntity<HeaderInfo, IHeaderInfo, HeaderInfoDescriptor>
    {
        public HeaderInfo()
        {
        }

        public HeaderInfo(IHeaderInfo source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static HeaderInfo ICreate<HeaderInfo>.Create()
        {
            return new HeaderInfo();
        }

        public int FormatVersion { get; set; }
    }

    public partial class LanguageInfo : EntityBase, ILanguageInfo, IPropertyTarget<ILanguageInfo, LanguageInfo>, IEntity<LanguageInfo, ILanguageInfo, LanguageInfoDescriptor>
    {
        public LanguageInfo()
        {
        }

        public LanguageInfo(ILanguageInfo source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static LanguageInfo ICreate<LanguageInfo>.Create()
        {
            return new LanguageInfo();
        }

        public List<ClassificationStyle> Classifications
        {
            get => Coerce(ref this.m_Classifications);
            set
            {
                this.m_Classifications = value;
            }
        }

        private List<ClassificationStyle> m_Classifications;
        IReadOnlyList<IClassificationStyle> ILanguageInfo.Classifications { get => CoerceReadOnly(ref this.m_Classifications); }
        public string Name { get; set; }
    }

    public partial class LanguageSearchModel : SearchEntity, ILanguageSearchModel, IPropertyTarget<ILanguageSearchModel, LanguageSearchModel>, IEntity<LanguageSearchModel, ILanguageSearchModel, LanguageSearchModelDescriptor>
    {
        public LanguageSearchModel()
        {
        }

        public LanguageSearchModel(ILanguageSearchModel source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public LanguageSearchModel(ISearchEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static LanguageSearchModel ICreate<LanguageSearchModel>.Create()
        {
            return new LanguageSearchModel();
        }

        public LanguageInfo Language { get; set; }

        ILanguageInfo ILanguageSearchModel.Language { get => Language; }
    }

    public partial class LineSpan : Span, ILineSpan, IPropertyTarget<ILineSpan, LineSpan>, IEntity<LineSpan, ILineSpan, LineSpanDescriptor>
    {
        public LineSpan()
        {
        }

        public LineSpan(ILineSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public LineSpan(ISpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static LineSpan ICreate<LineSpan>.Create()
        {
            return new LineSpan();
        }

        public int LineIndex
        {
            get => CoerceLineIndex(this.m_LineIndex);
            set
            {
                this.m_LineIndex = value;
            }
        }

        private Nullable<int> m_LineIndex;
        public int LineNumber
        {
            get => CoerceLineNumber(this.m_LineNumber);
            set
            {
                this.m_LineNumber = value;
            }
        }

        private Nullable<int> m_LineNumber;
        public int LineOffset { get; set; }
        public int LineSpanStart { get; set; }
    }

    public partial class NewBoundSourceFile : EntityBase, INewBoundSourceFile, IPropertyTarget<INewBoundSourceFile, NewBoundSourceFile>, IEntity<NewBoundSourceFile, INewBoundSourceFile, NewBoundSourceFileDescriptor>
    {
        public NewBoundSourceFile()
        {
        }

        public NewBoundSourceFile(INewBoundSourceFile source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static NewBoundSourceFile ICreate<NewBoundSourceFile>.Create()
        {
            return new NewBoundSourceFile();
        }

        public ProjectFileScopeEntity FileInfo { get; set; }

        IProjectFileScopeEntity INewBoundSourceFile.FileInfo { get => FileInfo; }
        public SourceFileBase SourceFile { get; set; }

        ISourceFileBase INewBoundSourceFile.SourceFile { get => SourceFile; }
    }

    public partial class OutliningRegion : EntityBase, IOutliningRegion, IPropertyTarget<IOutliningRegion, OutliningRegion>, IEntity<OutliningRegion, IOutliningRegion, OutliningRegionDescriptor>
    {
        public OutliningRegion()
        {
        }

        public OutliningRegion(IOutliningRegion source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static OutliningRegion ICreate<OutliningRegion>.Create()
        {
            return new OutliningRegion();
        }

        public LineSpan Content { get; set; }

        ILineSpan IOutliningRegion.Content { get => Content; }
        public LineSpan Header { get; set; }

        ILineSpan IOutliningRegion.Header { get => Header; }
    }

    public partial class ParameterDefinitionSpan : LineSpan, IParameterDefinitionSpan, ISpan, IPropertyTarget<IParameterDefinitionSpan, ParameterDefinitionSpan>, IEntity<ParameterDefinitionSpan, IParameterDefinitionSpan, ParameterDefinitionSpanDescriptor>
    {
        public ParameterDefinitionSpan()
        {
        }

        public ParameterDefinitionSpan(ILineSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ParameterDefinitionSpan(IParameterDefinitionSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ParameterDefinitionSpan(ISpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static ParameterDefinitionSpan ICreate<ParameterDefinitionSpan>.Create()
        {
            return new ParameterDefinitionSpan();
        }

        public string Name { get; set; }
        public int ParameterIndex { get; set; }
    }

    public partial class ParameterDocumentation : EntityBase, IParameterDocumentation, IPropertyTarget<IParameterDocumentation, ParameterDocumentation>, IEntity<ParameterDocumentation, IParameterDocumentation, ParameterDocumentationDescriptor>
    {
        public ParameterDocumentation()
        {
        }

        public ParameterDocumentation(IParameterDocumentation source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static ParameterDocumentation ICreate<ParameterDocumentation>.Create()
        {
            return new ParameterDocumentation();
        }

        public string Comment { get; set; }
        public string Name { get; set; }
    }

    public partial class ParameterReferenceSpan : SymbolSpan, IParameterReferenceSpan, ITextLineSpan, ILineSpan, ISpan, IPropertyTarget<IParameterReferenceSpan, ParameterReferenceSpan>, IEntity<ParameterReferenceSpan, IParameterReferenceSpan, ParameterReferenceSpanDescriptor>
    {
        public ParameterReferenceSpan()
        {
        }

        public ParameterReferenceSpan(ILineSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ParameterReferenceSpan(IParameterReferenceSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ParameterReferenceSpan(ISpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ParameterReferenceSpan(ISymbolSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ParameterReferenceSpan(ITextLineSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static ParameterReferenceSpan ICreate<ParameterReferenceSpan>.Create()
        {
            return new ParameterReferenceSpan();
        }

        public int ParameterIndex { get; set; }
    }

    public partial class ProjectFileLink : ProjectFileScopeEntity, IProjectFileLink, IRepoFileScopeEntity, IRepoScopeEntity, IProjectScopeEntity, IPropertyTarget<IProjectFileLink, ProjectFileLink>, IEntity<ProjectFileLink, IProjectFileLink, ProjectFileLinkDescriptor>
    {
        public ProjectFileLink()
        {
        }

        public ProjectFileLink(IProjectFileLink source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ProjectFileLink(IProjectFileScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ProjectFileLink(IProjectScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ProjectFileLink(IRepoFileScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ProjectFileLink(IRepoScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static ProjectFileLink ICreate<ProjectFileLink>.Create()
        {
            return new ProjectFileLink();
        }

        public string FileId { get; set; }
    }

    public partial class ProjectFileScopeEntity : RepoFileScopeEntity, IProjectFileScopeEntity, IRepoScopeEntity, IProjectScopeEntity, IPropertyTarget<IProjectFileScopeEntity, ProjectFileScopeEntity>, IPropertyTarget<IProjectScopeEntity, ProjectScopeEntity>, IEntity<ProjectFileScopeEntity, IProjectFileScopeEntity, ProjectFileScopeEntityDescriptor>
    {
        public ProjectFileScopeEntity()
        {
        }

        public ProjectFileScopeEntity(IProjectFileScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ProjectFileScopeEntity(IProjectScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ProjectFileScopeEntity(IRepoFileScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ProjectFileScopeEntity(IRepoScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static ProjectFileScopeEntity ICreate<ProjectFileScopeEntity>.Create()
        {
            return new ProjectFileScopeEntity();
        }

        public string ProjectRelativePath { get; set; }
        public string ProjectId { get; set; }
    }

    public partial class ProjectReferenceSearchModel : ProjectScopeEntity, IProjectReferenceSearchModel, IRepoScopeEntity, ISearchEntity, IPropertyTarget<IProjectReferenceSearchModel, ProjectReferenceSearchModel>, IPropertyTarget<ISearchEntity, SearchEntity>, IEntity<ProjectReferenceSearchModel, IProjectReferenceSearchModel, ProjectReferenceSearchModelDescriptor>
    {
        public ProjectReferenceSearchModel()
        {
        }

        public ProjectReferenceSearchModel(IProjectReferenceSearchModel source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ProjectReferenceSearchModel(IProjectScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ProjectReferenceSearchModel(IRepoScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ProjectReferenceSearchModel(ISearchEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static ProjectReferenceSearchModel ICreate<ProjectReferenceSearchModel>.Create()
        {
            return new ProjectReferenceSearchModel();
        }

        public IReferencedProject ProjectReference { get; set; }
        public MurmurHash EntityContentId { get; set; }
        public int EntityContentSize { get; set; }
        public bool IsAdded { get; set; }
        public int StableId { get; set; }
        public MurmurHash Uid { get; set; }
    }

    public partial class ProjectScopeEntity : RepoScopeEntity, IProjectScopeEntity, IPropertyTarget<IProjectScopeEntity, ProjectScopeEntity>, IEntity<ProjectScopeEntity, IProjectScopeEntity, ProjectScopeEntityDescriptor>
    {
        public ProjectScopeEntity()
        {
        }

        public ProjectScopeEntity(IProjectScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ProjectScopeEntity(IRepoScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static ProjectScopeEntity ICreate<ProjectScopeEntity>.Create()
        {
            return new ProjectScopeEntity();
        }

        public string ProjectId { get; set; }
    }

    public partial class ProjectSearchModel : SearchEntity, IProjectSearchModel, IPropertyTarget<IProjectSearchModel, ProjectSearchModel>, IEntity<ProjectSearchModel, IProjectSearchModel, ProjectSearchModelDescriptor>
    {
        public ProjectSearchModel()
        {
        }

        public ProjectSearchModel(IProjectSearchModel source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ProjectSearchModel(ISearchEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static ProjectSearchModel ICreate<ProjectSearchModel>.Create()
        {
            return new ProjectSearchModel();
        }

        public IAnalyzedProjectInfo Project { get; set; }
    }

    public partial class PropertySearchModel : SearchEntity, IPropertySearchModel, IPropertyTarget<IPropertySearchModel, PropertySearchModel>, IEntity<PropertySearchModel, IPropertySearchModel, PropertySearchModelDescriptor>
    {
        public PropertySearchModel()
        {
        }

        public PropertySearchModel(IPropertySearchModel source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public PropertySearchModel(ISearchEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static PropertySearchModel ICreate<PropertySearchModel>.Create()
        {
            return new PropertySearchModel();
        }

        public StringEnum<PropertyKey> Key { get; set; }
        public int OwnerId { get; set; }
        public string Value { get; set; }
    }

    public partial class QualifierScopeEntity : EntityBase, IQualifierScopeEntity, IPropertyTarget<IQualifierScopeEntity, QualifierScopeEntity>, IEntity<QualifierScopeEntity, IQualifierScopeEntity, QualifierScopeEntityDescriptor>
    {
        public QualifierScopeEntity()
        {
        }

        public QualifierScopeEntity(IQualifierScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static QualifierScopeEntity ICreate<QualifierScopeEntity>.Create()
        {
            return new QualifierScopeEntity();
        }

        public string Qualifier { get; set; }
    }

    public partial class ReferencedProject : ProjectScopeEntity, IReferencedProject, IRepoScopeEntity, IPropertyTarget<IReferencedProject, ReferencedProject>, IEntity<ReferencedProject, IReferencedProject, ReferencedProjectDescriptor>
    {
        public ReferencedProject()
        {
        }

        public ReferencedProject(IProjectScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ReferencedProject(IReferencedProject source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ReferencedProject(IRepoScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static ReferencedProject ICreate<ReferencedProject>.Create()
        {
            return new ReferencedProject();
        }

        public int DefinitionCount
        {
            get => CoerceDefinitionCount(this.m_DefinitionCount);
            set
            {
                this.m_DefinitionCount = value;
            }
        }

        private Nullable<int> m_DefinitionCount;
        public List<DefinitionSymbol> Definitions
        {
            get => Coerce(ref this.m_Definitions);
            set
            {
                this.m_Definitions = value;
            }
        }

        private List<DefinitionSymbol> m_Definitions;
        IReadOnlyList<IDefinitionSymbol> IReferencedProject.Definitions { get => CoerceReadOnly(ref this.m_Definitions); }
        public string DisplayName { get; set; }
        public PropertyMap Properties { get; set; }

        IPropertyMap IReferencedProject.Properties { get => Properties; }
    }

    public partial class ReferenceSearchModel : SearchEntity, IReferenceSearchModel, IPropertyTarget<IReferenceSearchModel, ReferenceSearchModel>, IEntity<ReferenceSearchModel, IReferenceSearchModel, ReferenceSearchModelDescriptor>
    {
        public ReferenceSearchModel()
        {
        }

        public ReferenceSearchModel(IReferenceSearchModel source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ReferenceSearchModel(ISearchEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static ReferenceSearchModel ICreate<ReferenceSearchModel>.Create()
        {
            return new ReferenceSearchModel();
        }

        public IProjectFileScopeEntity FileInfo { get; set; }
        public ReferenceKindSet ReferenceKind { get; set; }
        public SymbolReferenceList References { get; set; }

        ISymbolReferenceList IReferenceSearchModel.References { get => References; }
        public IEnumerable<SymbolId> RelatedDefinition { get; set; }

        public IReadOnlyList<IReferenceSpan> Spans
        {
            get => CoerceSpans(this.m_Spans);
            set
            {
                this.m_Spans = value;
            }
        }

        private IReadOnlyList<IReferenceSpan> m_Spans;
        IReadOnlyList<IReferenceSpan> IReferenceSearchModel.Spans { get => Spans; }
        public ICodeSymbol Symbol { get; set; }
    }

    public partial class ReferenceSearchResult : FileSpanResult, IReferenceSearchResult, IPropertyTarget<IReferenceSearchResult, ReferenceSearchResult>, IEntity<ReferenceSearchResult, IReferenceSearchResult, ReferenceSearchResultDescriptor>
    {
        public ReferenceSearchResult()
        {
        }

        public ReferenceSearchResult(IFileSpanResult source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ReferenceSearchResult(IReferenceSearchResult source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static ReferenceSearchResult ICreate<ReferenceSearchResult>.Create()
        {
            return new ReferenceSearchResult();
        }

        public IReferenceSpan ReferenceSpan { get; set; }
    }

    public partial class ReferenceSpan : SymbolSpan, IReferenceSpan, ITextLineSpan, ILineSpan, ISpan, IPropertyTarget<IReferenceSpan, ReferenceSpan>, IEntity<ReferenceSpan, IReferenceSpan, ReferenceSpanDescriptor>
    {
        public ReferenceSpan()
        {
        }

        public ReferenceSpan(ILineSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ReferenceSpan(IReferenceSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ReferenceSpan(ISpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ReferenceSpan(ISymbolSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ReferenceSpan(ITextLineSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static ReferenceSpan ICreate<ReferenceSpan>.Create()
        {
            return new ReferenceSpan();
        }

        public IDisplayCodeSymbol ContainerSymbol { get; set; }
        public bool IsImplicitlyDeclared { get; set; }
        public IReadOnlyList<ParameterReferenceSpan> Parameters { get; set; } = System.Array.Empty<ParameterReferenceSpan>();

        IReadOnlyList<IParameterReferenceSpan> IReferenceSpan.Parameters { get => Parameters; }
        public ReferenceSymbol Reference { get; set; }

        IReferenceSymbol IReferenceSpan.Reference { get => Reference; }
        public SymbolId RelatedDefinition { get; set; }
    }

    public partial class ReferenceSymbol : CodeSymbol, IReferenceSymbol, IPropertyTarget<IReferenceSymbol, ReferenceSymbol>, IEntity<ReferenceSymbol, IReferenceSymbol, ReferenceSymbolDescriptor>
    {
        public ReferenceSymbol()
        {
        }

        public ReferenceSymbol(ICodeSymbol source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public ReferenceSymbol(IReferenceSymbol source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static ReferenceSymbol ICreate<ReferenceSymbol>.Create()
        {
            return new ReferenceSymbol();
        }

        public bool ExcludeFromSearch { get; set; }

        public ReferenceKind ReferenceKind
        {
            get => CoerceReferenceKind(this.m_ReferenceKind);
            set
            {
                this.m_ReferenceKind = value;
            }
        }

        private ReferenceKind m_ReferenceKind;
    }

    public partial class RepoFileScopeEntity : RepoScopeEntity, IRepoFileScopeEntity, IPropertyTarget<IRepoFileScopeEntity, RepoFileScopeEntity>, IEntity<RepoFileScopeEntity, IRepoFileScopeEntity, RepoFileScopeEntityDescriptor>
    {
        public RepoFileScopeEntity()
        {
        }

        public RepoFileScopeEntity(IRepoFileScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public RepoFileScopeEntity(IRepoScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static RepoFileScopeEntity ICreate<RepoFileScopeEntity>.Create()
        {
            return new RepoFileScopeEntity();
        }

        public string RepoRelativePath { get; set; }
    }

    public partial class RepoScopeEntity : EntityBase, IRepoScopeEntity, IPropertyTarget<IRepoScopeEntity, RepoScopeEntity>, IEntity<RepoScopeEntity, IRepoScopeEntity, RepoScopeEntityDescriptor>
    {
        public RepoScopeEntity()
        {
        }

        public RepoScopeEntity(IRepoScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static RepoScopeEntity ICreate<RepoScopeEntity>.Create()
        {
            return new RepoScopeEntity();
        }

        public string RepositoryName { get; set; }
    }

    public partial class Repository : EntityBase, IRepository, IPropertyTarget<IRepository, Repository>, IEntity<Repository, IRepository, RepositoryDescriptor>
    {
        public Repository()
        {
        }

        public Repository(IRepository source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static Repository ICreate<Repository>.Create()
        {
            return new Repository();
        }

        public string Description { get; set; }
        public string Name { get; set; }
        public string PrimaryBranch { get; set; }

        public List<RepositoryReference> RepositoryReferences
        {
            get => Coerce(ref this.m_RepositoryReferences);
            set
            {
                this.m_RepositoryReferences = value;
            }
        }

        private List<RepositoryReference> m_RepositoryReferences;
        IReadOnlyList<IRepositoryReference> IRepository.RepositoryReferences { get => CoerceReadOnly(ref this.m_RepositoryReferences); }
        public string SourceControlWebAddress { get; set; }
    }

    public partial class RepositoryReference : EntityBase, IRepositoryReference, IPropertyTarget<IRepositoryReference, RepositoryReference>, IEntity<RepositoryReference, IRepositoryReference, RepositoryReferenceDescriptor>
    {
        public RepositoryReference()
        {
        }

        public RepositoryReference(IRepositoryReference source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static RepositoryReference ICreate<RepositoryReference>.Create()
        {
            return new RepositoryReference();
        }

        public string Id { get; set; }
        public string Name { get; set; }
    }

    public partial class RepositorySearchModel : SearchEntity, IRepositorySearchModel, IPropertyTarget<IRepositorySearchModel, RepositorySearchModel>, IEntity<RepositorySearchModel, IRepositorySearchModel, RepositorySearchModelDescriptor>
    {
        public RepositorySearchModel()
        {
        }

        public RepositorySearchModel(IRepositorySearchModel source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public RepositorySearchModel(ISearchEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static RepositorySearchModel ICreate<RepositorySearchModel>.Create()
        {
            return new RepositorySearchModel();
        }

        public Repository Repository { get; set; }

        IRepository IRepositorySearchModel.Repository { get => Repository; }
    }

    public partial class RepositoryStoreInfo : EntityBase, IRepositoryStoreInfo, IPropertyTarget<IRepositoryStoreInfo, RepositoryStoreInfo>, IEntity<RepositoryStoreInfo, IRepositoryStoreInfo, RepositoryStoreInfoDescriptor>
    {
        public RepositoryStoreInfo()
        {
        }

        public RepositoryStoreInfo(IRepositoryStoreInfo source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static RepositoryStoreInfo ICreate<RepositoryStoreInfo>.Create()
        {
            return new RepositoryStoreInfo();
        }

        public Branch Branch { get; set; }

        IBranch IRepositoryStoreInfo.Branch { get => Branch; }
        public Commit Commit { get; set; }

        ICommit IRepositoryStoreInfo.Commit { get => Commit; }
        public Repository Repository { get; set; }

        IRepository IRepositoryStoreInfo.Repository { get => Repository; }
    }

    public partial class SearchEntity : EntityBase, ISearchEntity, IPropertyTarget<ISearchEntity, SearchEntity>, IEntity<SearchEntity, ISearchEntity, SearchEntityDescriptor>
    {
        public SearchEntity()
        {
        }

        public SearchEntity(ISearchEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static SearchEntity ICreate<SearchEntity>.Create()
        {
            return new SearchEntity();
        }

        public MurmurHash EntityContentId { get; set; }
        public int EntityContentSize { get; set; }
        public bool IsAdded { get; set; }
        public int StableId { get; set; }
        public MurmurHash Uid { get; set; }
    }

    public partial class SearchResult : EntityBase, ISearchResult, IPropertyTarget<ISearchResult, SearchResult>, IEntity<SearchResult, ISearchResult, SearchResultDescriptor>
    {
        public SearchResult()
        {
        }

        public SearchResult(ISearchResult source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static SearchResult ICreate<SearchResult>.Create()
        {
            return new SearchResult();
        }

        public DefinitionSymbol Definition { get; set; }

        IDefinitionSymbol ISearchResult.Definition { get => Definition; }
        public TextLineSpanResult TextLine { get; set; }

        ITextLineSpanResult ISearchResult.TextLine { get => TextLine; }
    }

    public partial class SharedReferenceInfo : EntityBase, ISharedReferenceInfo, IPropertyTarget<ISharedReferenceInfo, SharedReferenceInfo>, IEntity<SharedReferenceInfo, ISharedReferenceInfo, SharedReferenceInfoDescriptor>
    {
        public SharedReferenceInfo()
        {
        }

        public SharedReferenceInfo(ISharedReferenceInfo source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static SharedReferenceInfo ICreate<SharedReferenceInfo>.Create()
        {
            return new SharedReferenceInfo();
        }

        public bool ExcludeFromSearch { get; set; }
        public ReferenceKind ReferenceKind { get; set; }
        public SymbolId RelatedDefinition { get; set; }
    }

    public partial class SharedReferenceInfoSpan : SymbolSpan, ISharedReferenceInfoSpan, ITextLineSpan, ILineSpan, ISpan, IPropertyTarget<ISharedReferenceInfoSpan, SharedReferenceInfoSpan>, IEntity<SharedReferenceInfoSpan, ISharedReferenceInfoSpan, SharedReferenceInfoSpanDescriptor>
    {
        public SharedReferenceInfoSpan()
        {
        }

        public SharedReferenceInfoSpan(ILineSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public SharedReferenceInfoSpan(ISharedReferenceInfoSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public SharedReferenceInfoSpan(ISpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public SharedReferenceInfoSpan(ISymbolSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public SharedReferenceInfoSpan(ITextLineSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static SharedReferenceInfoSpan ICreate<SharedReferenceInfoSpan>.Create()
        {
            return new SharedReferenceInfoSpan();
        }

        public SharedReferenceInfo Info { get; set; }

        ISharedReferenceInfo ISharedReferenceInfoSpan.Info { get => Info; }
    }

    public partial class SourceControlFileInfo : EntityBase, ISourceControlFileInfo, IPropertyTarget<ISourceControlFileInfo, SourceControlFileInfo>, IEntity<SourceControlFileInfo, ISourceControlFileInfo, SourceControlFileInfoDescriptor>
    {
        public SourceControlFileInfo()
        {
        }

        public SourceControlFileInfo(ISourceControlFileInfo source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static SourceControlFileInfo ICreate<SourceControlFileInfo>.Create()
        {
            return new SourceControlFileInfo();
        }

        public SourceEncodingInfo EncodingInfo { get; set; }
        public int Lines { get; set; }
        public int Size { get; set; }
        public string SourceControlContentId { get; set; }
    }

    public partial class SourceFile : SourceFileBase, ISourceFile, IPropertyTarget<ISourceFile, SourceFile>, IEntity<SourceFile, ISourceFile, SourceFileDescriptor>
    {
        public SourceFile()
        {
        }

        public SourceFile(ISourceFile source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public SourceFile(ISourceFileBase source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static SourceFile ICreate<SourceFile>.Create()
        {
            return new SourceFile();
        }

        public string Content
        {
            get => CoerceContent(this.m_Content);
            set
            {
                this.m_Content = value;
            }
        }

        private string m_Content;
        public TextSourceBase ContentSource
        {
            get => CoerceContentSource(this.m_ContentSource);
            set
            {
                this.m_ContentSource = value;
            }
        }

        private TextSourceBase m_ContentSource;
    }

    public partial class SourceFileBase : EntityBase, ISourceFileBase, IPropertyTarget<ISourceFileBase, SourceFileBase>, IEntity<SourceFileBase, ISourceFileBase, SourceFileBaseDescriptor>
    {
        public SourceFileBase()
        {
        }

        public SourceFileBase(ISourceFileBase source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static SourceFileBase ICreate<SourceFileBase>.Create()
        {
            return new SourceFileBase();
        }

        public bool ExcludeFromSearch { get; set; }
        public BoundSourceFlags Flags { get; set; }
        public SourceFileInfo Info { get; set; }

        ISourceFileInfo ISourceFileBase.Info { get => Info; }
    }

    public partial class SourceFileInfo : ProjectFileScopeEntity, ISourceFileInfo, IRepoFileScopeEntity, IRepoScopeEntity, IProjectScopeEntity, ISourceControlFileInfo, IQualifierScopeEntity, IPropertyTarget<IQualifierScopeEntity, QualifierScopeEntity>, IPropertyTarget<ISourceControlFileInfo, SourceControlFileInfo>, IPropertyTarget<ISourceFileInfo, SourceFileInfo>, IEntity<SourceFileInfo, ISourceFileInfo, SourceFileInfoDescriptor>
    {
        public SourceFileInfo()
        {
        }

        public SourceFileInfo(IProjectFileScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public SourceFileInfo(IProjectScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public SourceFileInfo(IQualifierScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public SourceFileInfo(IRepoFileScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public SourceFileInfo(IRepoScopeEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public SourceFileInfo(ISourceControlFileInfo source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public SourceFileInfo(ISourceFileInfo source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static SourceFileInfo ICreate<SourceFileInfo>.Create()
        {
            return new SourceFileInfo();
        }

        public PropertyMap Properties { get; set; }
        public string Language { get; set; }
        public string DownloadAddress { get; set; }
        public string CommitId { get; set; }
        public SourceEncodingInfo EncodingInfo { get; set; }
        public int Size { get; set; }
        public int Lines { get; set; }

        IPropertyMap ISourceFileInfo.Properties { get => Properties; }
        public string Qualifier { get; set; }
        public string SourceControlContentId { get; set; }
        public string WebAddress { get; set; }
    }

    public partial class SourceSearchModelBase : SearchEntity, ISourceSearchModelBase, IPropertyTarget<ISourceSearchModelBase, SourceSearchModelBase>, IEntity<SourceSearchModelBase, ISourceSearchModelBase, SourceSearchModelBaseDescriptor>
    {
        public SourceSearchModelBase()
        {
        }

        public SourceSearchModelBase(ISearchEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public SourceSearchModelBase(ISourceSearchModelBase source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static SourceSearchModelBase ICreate<SourceSearchModelBase>.Create()
        {
            return new SourceSearchModelBase();
        }
    }

    public partial class Span : EntityBase, ISpan, IPropertyTarget<ISpan, Span>, IEntity<Span, ISpan, SpanDescriptor>
    {
        public Span()
        {
        }

        public Span(ISpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static Span ICreate<Span>.Create()
        {
            return new Span();
        }

        public int Length { get; set; }
        public int Start { get; set; }
    }

    public partial class StoredBoundSourceFile : EntityBase, IStoredBoundSourceFile, IPropertyTarget<IStoredBoundSourceFile, StoredBoundSourceFile>, IEntity<StoredBoundSourceFile, IStoredBoundSourceFile, StoredBoundSourceFileDescriptor>
    {
        public StoredBoundSourceFile()
        {
        }

        public StoredBoundSourceFile(IStoredBoundSourceFile source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static StoredBoundSourceFile ICreate<StoredBoundSourceFile>.Create()
        {
            return new StoredBoundSourceFile();
        }

        public List<string> SourceFileContentLines
        {
            get => Coerce(ref this.m_SourceFileContentLines);
            set
            {
                this.m_SourceFileContentLines = value;
            }
        }

        IReadOnlyList<ReadOnlyMemory<byte>> IStoredBoundSourceFile.SemanticData { get => CoerceReadOnly(ref this.m_SemanticData); }

        private List<ReadOnlyMemory<byte>> m_SemanticData;
        public List<ReadOnlyMemory<byte>> SemanticData
        {
            get => Coerce(ref this.m_SemanticData);
            set
            {
                this.m_SemanticData = value;
            }
        }

        IReadOnlyList<ICodeSymbol> IStoredBoundSourceFile.References { get => CoerceReadOnly(ref this.m_References); }

        private List<CodeSymbol> m_References;
        IReferenceListModel IStoredBoundSourceFile.CompressedReferences { get => CompressedReferences; }

        private List<string> m_SourceFileContentLines;
        public ReferenceListModel CompressedReferences { get; set; }

        IClassificationListModel IStoredBoundSourceFile.CompressedClassifications { get => CompressedClassifications; }
        public ClassificationListModel CompressedClassifications { get; set; }

        IBoundSourceFile IStoredBoundSourceFile.BoundSourceFile { get => BoundSourceFile; }
        public BoundSourceFile BoundSourceFile { get; set; }

        public List<CodeSymbol> References
        {
            get => Coerce(ref this.m_References);
            set
            {
                this.m_References = value;
            }
        }

        IReadOnlyList<string> IStoredBoundSourceFile.SourceFileContentLines { get => CoerceReadOnly(ref this.m_SourceFileContentLines); }
    }

    public partial class StoredFilter : SearchEntity, IStoredFilter, IPropertyTarget<IStoredFilter, StoredFilter>, IEntity<StoredFilter, IStoredFilter, StoredFilterDescriptor>
    {
        public StoredFilter()
        {
        }

        public StoredFilter(ISearchEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public StoredFilter(IStoredFilter source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static StoredFilter ICreate<StoredFilter>.Create()
        {
            return new StoredFilter();
        }

        public int Cardinality { get; set; }
        public CommitInfo CommitInfo { get; set; }

        ICommitInfo IStoredFilter.CommitInfo { get => CommitInfo; }
        public string FilterHash { get; set; }
        public byte[] StableIds { get; set; }
    }

    public partial class StoredRepositoryGroupInfo : EntityBase, IStoredRepositoryGroupInfo, IPropertyTarget<IStoredRepositoryGroupInfo, StoredRepositoryGroupInfo>, IEntity<StoredRepositoryGroupInfo, IStoredRepositoryGroupInfo, StoredRepositoryGroupInfoDescriptor>
    {
        public StoredRepositoryGroupInfo()
        {
        }

        public StoredRepositoryGroupInfo(IStoredRepositoryGroupInfo source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static StoredRepositoryGroupInfo ICreate<StoredRepositoryGroupInfo>.Create()
        {
            return new StoredRepositoryGroupInfo();
        }

        public ImmutableSortedSet<string> ActiveRepos { get; set; } = System.Collections.Immutable.ImmutableSortedSet<string>.Empty;
    }

    public partial class StoredRepositoryGroupSettings : EntityBase, IStoredRepositoryGroupSettings, IPropertyTarget<IStoredRepositoryGroupSettings, StoredRepositoryGroupSettings>, IEntity<StoredRepositoryGroupSettings, IStoredRepositoryGroupSettings, StoredRepositoryGroupSettingsDescriptor>
    {
        public StoredRepositoryGroupSettings()
        {
        }

        public StoredRepositoryGroupSettings(IStoredRepositoryGroupSettings source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static StoredRepositoryGroupSettings ICreate<StoredRepositoryGroupSettings>.Create()
        {
            return new StoredRepositoryGroupSettings();
        }

        public string Base { get; set; }
        public ImmutableHashSet<RepoName> Excludes { get; set; } = System.Collections.Immutable.ImmutableHashSet<RepoName>.Empty;
    }

    public partial class StoredRepositoryInfo : EntityBase, IStoredRepositoryInfo, IPropertyTarget<IStoredRepositoryInfo, StoredRepositoryInfo>, IEntity<StoredRepositoryInfo, IStoredRepositoryInfo, StoredRepositoryInfoDescriptor>
    {
        public StoredRepositoryInfo()
        {
        }

        public StoredRepositoryInfo(IStoredRepositoryInfo source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static StoredRepositoryInfo ICreate<StoredRepositoryInfo>.Create()
        {
            return new StoredRepositoryInfo();
        }

        public ImmutableSortedSet<string> Groups { get; set; } = System.Collections.Immutable.ImmutableSortedSet<string>.Empty;
    }

    public partial class StoredRepositorySettings : EntityBase, IStoredRepositorySettings, IPropertyTarget<IStoredRepositorySettings, StoredRepositorySettings>, IEntity<StoredRepositorySettings, IStoredRepositorySettings, StoredRepositorySettingsDescriptor>
    {
        public StoredRepositorySettings()
        {
        }

        public StoredRepositorySettings(IStoredRepositorySettings source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static StoredRepositorySettings ICreate<StoredRepositorySettings>.Create()
        {
            return new StoredRepositorySettings();
        }

        public RepoAccess Access { get; set; }
        public bool ExplicitGroupsOnly { get; set; }
        public ImmutableSortedSet<string> Groups { get; set; } = System.Collections.Immutable.ImmutableSortedSet<string>.Empty;
    }

    public partial class SymbolReferenceList : EntityBase, ISymbolReferenceList, IPropertyTarget<ISymbolReferenceList, SymbolReferenceList>, IEntity<SymbolReferenceList, ISymbolReferenceList, SymbolReferenceListDescriptor>
    {
        public SymbolReferenceList()
        {
        }

        public SymbolReferenceList(ISymbolReferenceList source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static SymbolReferenceList ICreate<SymbolReferenceList>.Create()
        {
            return new SymbolReferenceList();
        }

        public SharedReferenceInfoSpanModel CompressedSpans { get; set; }

        ISharedReferenceInfoSpanModel ISymbolReferenceList.CompressedSpans { get => CompressedSpans; }

        public IReadOnlyList<SharedReferenceInfoSpan> Spans
        {
            get => CoerceSpans(this.m_Spans);
            set
            {
                this.m_Spans = value;
            }
        }

        private IReadOnlyList<SharedReferenceInfoSpan> m_Spans;
        IReadOnlyList<ISharedReferenceInfoSpan> ISymbolReferenceList.Spans { get => Spans; }
        public ICodeSymbol Symbol { get; set; }
    }

    public partial class SymbolSpan : TextLineSpan, ISymbolSpan, ILineSpan, ISpan, IPropertyTarget<ISymbolSpan, SymbolSpan>, IEntity<SymbolSpan, ISymbolSpan, SymbolSpanDescriptor>
    {
        public SymbolSpan()
        {
        }

        public SymbolSpan(ILineSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public SymbolSpan(ISpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public SymbolSpan(ISymbolSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public SymbolSpan(ITextLineSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static SymbolSpan ICreate<SymbolSpan>.Create()
        {
            return new SymbolSpan();
        }
    }

    public partial class TextChunkSearchModel : SearchEntity, ITextChunkSearchModel, IPropertyTarget<ITextChunkSearchModel, TextChunkSearchModel>, IEntity<TextChunkSearchModel, ITextChunkSearchModel, TextChunkSearchModelDescriptor>
    {
        public TextChunkSearchModel()
        {
        }

        public TextChunkSearchModel(ISearchEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public TextChunkSearchModel(ITextChunkSearchModel source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static TextChunkSearchModel ICreate<TextChunkSearchModel>.Create()
        {
            return new TextChunkSearchModel();
        }

        public TextSourceBase Content { get; set; }
    }

    public partial class TextLineSpan : LineSpan, ITextLineSpan, ISpan, IPropertyTarget<ITextLineSpan, TextLineSpan>, IEntity<TextLineSpan, ITextLineSpan, TextLineSpanDescriptor>
    {
        public TextLineSpan()
        {
        }

        public TextLineSpan(ILineSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public TextLineSpan(ISpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public TextLineSpan(ITextLineSpan source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static TextLineSpan ICreate<TextLineSpan>.Create()
        {
            return new TextLineSpan();
        }

        public CharString LineSpanText { get; set; }
    }

    public partial class TextLineSpanResult : FileSpanResult, ITextLineSpanResult, IPropertyTarget<ITextLineSpanResult, TextLineSpanResult>, IEntity<TextLineSpanResult, ITextLineSpanResult, TextLineSpanResultDescriptor>
    {
        public TextLineSpanResult()
        {
        }

        public TextLineSpanResult(IFileSpanResult source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public TextLineSpanResult(ITextLineSpanResult source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static TextLineSpanResult ICreate<TextLineSpanResult>.Create()
        {
            return new TextLineSpanResult();
        }

        public TextLineSpan TextSpan { get; set; }

        ITextLineSpan ITextLineSpanResult.TextSpan { get => TextSpan; }
    }

    public partial class TextSourceSearchModel : SourceSearchModelBase, ITextSourceSearchModel, ISearchEntity, IPropertyTarget<ITextSourceSearchModel, TextSourceSearchModel>, IEntity<TextSourceSearchModel, ITextSourceSearchModel, TextSourceSearchModelDescriptor>
    {
        public TextSourceSearchModel()
        {
        }

        public TextSourceSearchModel(ISearchEntity source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public TextSourceSearchModel(ISourceSearchModelBase source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        public TextSourceSearchModel(ITextSourceSearchModel source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static TextSourceSearchModel ICreate<TextSourceSearchModel>.Create()
        {
            return new TextSourceSearchModel();
        }

        public IChunkReference Chunk { get; set; }
        public IProjectFileScopeEntity File { get; set; }
    }

    public partial class UserSettings : EntityBase, IUserSettings, IPropertyTarget<IUserSettings, UserSettings>, IEntity<UserSettings, IUserSettings, UserSettingsDescriptor>
    {
        public UserSettings()
        {
        }

        public UserSettings(IUserSettings source, bool shallowCopy = false)
        {
            this.Apply(source, shallowCopy);
        }

        static UserSettings ICreate<UserSettings>.Create()
        {
            return new UserSettings();
        }

        public Nullable<RepoAccess> Access { get; set; }
        public DateTime ExpirationUtc { get; set; }
    }

    public static partial class EntityTypes
    {
        public static IReadOnlyDictionary<Type, Type> ToImplementationMap { get; } = new Dictionary<Type, Type>()
        {
            {
                typeof(IAnalyzedProjectInfo),
                typeof(AnalyzedProjectInfo)
            },
            {
                typeof(IBoundSourceFile),
                typeof(BoundSourceFile)
            },
            {
                typeof(IBoundSourceInfo),
                typeof(BoundSourceInfo)
            },
            {
                typeof(IBoundSourceSearchModel),
                typeof(BoundSourceSearchModel)
            },
            {
                typeof(IBranch),
                typeof(Branch)
            },
            {
                typeof(IChunkedSourceFile),
                typeof(ChunkedSourceFile)
            },
            {
                typeof(IChunkReference),
                typeof(ChunkReference)
            },
            {
                typeof(IClassificationSpan),
                typeof(ClassificationSpan)
            },
            {
                typeof(IClassificationStyle),
                typeof(ClassificationStyle)
            },
            {
                typeof(ICodeSymbol),
                typeof(CodeSymbol)
            },
            {
                typeof(ICommit),
                typeof(Commit)
            },
            {
                typeof(ICommitInfo),
                typeof(CommitInfo)
            },
            {
                typeof(ICommitScopeEntity),
                typeof(CommitScopeEntity)
            },
            {
                typeof(ICommitSearchModel),
                typeof(CommitSearchModel)
            },
            {
                typeof(IDefinitionSearchModel),
                typeof(DefinitionSearchModel)
            },
            {
                typeof(IDefinitionSpan),
                typeof(DefinitionSpan)
            },
            {
                typeof(IDefinitionSymbol),
                typeof(DefinitionSymbol)
            },
            {
                typeof(IDefinitionSymbolExtendedSearchInfo),
                typeof(DefinitionSymbolExtendedSearchInfo)
            },
            {
                typeof(IDefinitionSymbolExtensionInfo),
                typeof(DefinitionSymbolExtensionInfo)
            },
            {
                typeof(IDirectoryRepositoryStoreInfo),
                typeof(DirectoryRepositoryStoreInfo)
            },
            {
                typeof(IDisplayCodeSymbol),
                typeof(DisplayCodeSymbol)
            },
            {
                typeof(IDocumentationReferenceSymbol),
                typeof(DocumentationReferenceSymbol)
            },
            {
                typeof(IFileSpanResult),
                typeof(FileSpanResult)
            },
            {
                typeof(IGlobalStoredRepositorySettings),
                typeof(GlobalStoredRepositorySettings)
            },
            {
                typeof(IHeaderInfo),
                typeof(HeaderInfo)
            },
            {
                typeof(ILanguageInfo),
                typeof(LanguageInfo)
            },
            {
                typeof(ILanguageSearchModel),
                typeof(LanguageSearchModel)
            },
            {
                typeof(ILineSpan),
                typeof(LineSpan)
            },
            {
                typeof(INewBoundSourceFile),
                typeof(NewBoundSourceFile)
            },
            {
                typeof(IOutliningRegion),
                typeof(OutliningRegion)
            },
            {
                typeof(IParameterDefinitionSpan),
                typeof(ParameterDefinitionSpan)
            },
            {
                typeof(IParameterDocumentation),
                typeof(ParameterDocumentation)
            },
            {
                typeof(IParameterReferenceSpan),
                typeof(ParameterReferenceSpan)
            },
            {
                typeof(IProjectFileLink),
                typeof(ProjectFileLink)
            },
            {
                typeof(IProjectFileScopeEntity),
                typeof(ProjectFileScopeEntity)
            },
            {
                typeof(IProjectReferenceSearchModel),
                typeof(ProjectReferenceSearchModel)
            },
            {
                typeof(IProjectScopeEntity),
                typeof(ProjectScopeEntity)
            },
            {
                typeof(IProjectSearchModel),
                typeof(ProjectSearchModel)
            },
            {
                typeof(IPropertySearchModel),
                typeof(PropertySearchModel)
            },
            {
                typeof(IQualifierScopeEntity),
                typeof(QualifierScopeEntity)
            },
            {
                typeof(IReferencedProject),
                typeof(ReferencedProject)
            },
            {
                typeof(IReferenceSearchModel),
                typeof(ReferenceSearchModel)
            },
            {
                typeof(IReferenceSearchResult),
                typeof(ReferenceSearchResult)
            },
            {
                typeof(IReferenceSpan),
                typeof(ReferenceSpan)
            },
            {
                typeof(IReferenceSymbol),
                typeof(ReferenceSymbol)
            },
            {
                typeof(IRepoFileScopeEntity),
                typeof(RepoFileScopeEntity)
            },
            {
                typeof(IRepoScopeEntity),
                typeof(RepoScopeEntity)
            },
            {
                typeof(IRepository),
                typeof(Repository)
            },
            {
                typeof(IRepositoryReference),
                typeof(RepositoryReference)
            },
            {
                typeof(IRepositorySearchModel),
                typeof(RepositorySearchModel)
            },
            {
                typeof(IRepositoryStoreInfo),
                typeof(RepositoryStoreInfo)
            },
            {
                typeof(ISearchEntity),
                typeof(SearchEntity)
            },
            {
                typeof(ISearchResult),
                typeof(SearchResult)
            },
            {
                typeof(ISharedReferenceInfo),
                typeof(SharedReferenceInfo)
            },
            {
                typeof(ISharedReferenceInfoSpan),
                typeof(SharedReferenceInfoSpan)
            },
            {
                typeof(ISourceControlFileInfo),
                typeof(SourceControlFileInfo)
            },
            {
                typeof(ISourceFile),
                typeof(SourceFile)
            },
            {
                typeof(ISourceFileBase),
                typeof(SourceFileBase)
            },
            {
                typeof(ISourceFileInfo),
                typeof(SourceFileInfo)
            },
            {
                typeof(ISourceSearchModelBase),
                typeof(SourceSearchModelBase)
            },
            {
                typeof(ISpan),
                typeof(Span)
            },
            {
                typeof(IStoredBoundSourceFile),
                typeof(StoredBoundSourceFile)
            },
            {
                typeof(IStoredFilter),
                typeof(StoredFilter)
            },
            {
                typeof(IStoredRepositoryGroupInfo),
                typeof(StoredRepositoryGroupInfo)
            },
            {
                typeof(IStoredRepositoryGroupSettings),
                typeof(StoredRepositoryGroupSettings)
            },
            {
                typeof(IStoredRepositoryInfo),
                typeof(StoredRepositoryInfo)
            },
            {
                typeof(IStoredRepositorySettings),
                typeof(StoredRepositorySettings)
            },
            {
                typeof(ISymbolReferenceList),
                typeof(SymbolReferenceList)
            },
            {
                typeof(ISymbolSpan),
                typeof(SymbolSpan)
            },
            {
                typeof(ITextChunkSearchModel),
                typeof(TextChunkSearchModel)
            },
            {
                typeof(ITextLineSpan),
                typeof(TextLineSpan)
            },
            {
                typeof(ITextLineSpanResult),
                typeof(TextLineSpanResult)
            },
            {
                typeof(ITextSourceSearchModel),
                typeof(TextSourceSearchModel)
            },
            {
                typeof(IUserSettings),
                typeof(UserSettings)
            },
        };
        public static IReadOnlyDictionary<Type, Type> ToAdapterImplementationMap { get; } = new Dictionary<Type, Type>()
        {
            {
                typeof(IClassificationListModel),
                typeof(ClassificationListModel)
            },
            {
                typeof(IIntArray),
                typeof(IntArray)
            },
            {
                typeof(IPropertyMap),
                typeof(PropertyMap)
            },
            {
                typeof(IReferenceListModel),
                typeof(ReferenceListModel)
            },
            {
                typeof(ISharedReferenceInfoSpanModel),
                typeof(SharedReferenceInfoSpanModel)
            },
            {
                typeof(ISymbolLineSpanListModel),
                typeof(SymbolLineSpanListModel)
            },
        };
        public static IReadOnlyDictionary<Type, Type> FromImplementationMap { get; } = new Dictionary<Type, Type>()
        {
            {
                typeof(AnalyzedProjectInfo),
                typeof(IAnalyzedProjectInfo)
            },
            {
                typeof(BoundSourceFile),
                typeof(IBoundSourceFile)
            },
            {
                typeof(BoundSourceInfo),
                typeof(IBoundSourceInfo)
            },
            {
                typeof(BoundSourceSearchModel),
                typeof(IBoundSourceSearchModel)
            },
            {
                typeof(Branch),
                typeof(IBranch)
            },
            {
                typeof(ChunkedSourceFile),
                typeof(IChunkedSourceFile)
            },
            {
                typeof(ChunkReference),
                typeof(IChunkReference)
            },
            {
                typeof(ClassificationSpan),
                typeof(IClassificationSpan)
            },
            {
                typeof(ClassificationStyle),
                typeof(IClassificationStyle)
            },
            {
                typeof(CodeSymbol),
                typeof(ICodeSymbol)
            },
            {
                typeof(Commit),
                typeof(ICommit)
            },
            {
                typeof(CommitInfo),
                typeof(ICommitInfo)
            },
            {
                typeof(CommitScopeEntity),
                typeof(ICommitScopeEntity)
            },
            {
                typeof(CommitSearchModel),
                typeof(ICommitSearchModel)
            },
            {
                typeof(DefinitionSearchModel),
                typeof(IDefinitionSearchModel)
            },
            {
                typeof(DefinitionSpan),
                typeof(IDefinitionSpan)
            },
            {
                typeof(DefinitionSymbol),
                typeof(IDefinitionSymbol)
            },
            {
                typeof(DefinitionSymbolExtendedSearchInfo),
                typeof(IDefinitionSymbolExtendedSearchInfo)
            },
            {
                typeof(DefinitionSymbolExtensionInfo),
                typeof(IDefinitionSymbolExtensionInfo)
            },
            {
                typeof(DirectoryRepositoryStoreInfo),
                typeof(IDirectoryRepositoryStoreInfo)
            },
            {
                typeof(DisplayCodeSymbol),
                typeof(IDisplayCodeSymbol)
            },
            {
                typeof(DocumentationReferenceSymbol),
                typeof(IDocumentationReferenceSymbol)
            },
            {
                typeof(FileSpanResult),
                typeof(IFileSpanResult)
            },
            {
                typeof(GlobalStoredRepositorySettings),
                typeof(IGlobalStoredRepositorySettings)
            },
            {
                typeof(HeaderInfo),
                typeof(IHeaderInfo)
            },
            {
                typeof(LanguageInfo),
                typeof(ILanguageInfo)
            },
            {
                typeof(LanguageSearchModel),
                typeof(ILanguageSearchModel)
            },
            {
                typeof(LineSpan),
                typeof(ILineSpan)
            },
            {
                typeof(NewBoundSourceFile),
                typeof(INewBoundSourceFile)
            },
            {
                typeof(OutliningRegion),
                typeof(IOutliningRegion)
            },
            {
                typeof(ParameterDefinitionSpan),
                typeof(IParameterDefinitionSpan)
            },
            {
                typeof(ParameterDocumentation),
                typeof(IParameterDocumentation)
            },
            {
                typeof(ParameterReferenceSpan),
                typeof(IParameterReferenceSpan)
            },
            {
                typeof(ProjectFileLink),
                typeof(IProjectFileLink)
            },
            {
                typeof(ProjectFileScopeEntity),
                typeof(IProjectFileScopeEntity)
            },
            {
                typeof(ProjectReferenceSearchModel),
                typeof(IProjectReferenceSearchModel)
            },
            {
                typeof(ProjectScopeEntity),
                typeof(IProjectScopeEntity)
            },
            {
                typeof(ProjectSearchModel),
                typeof(IProjectSearchModel)
            },
            {
                typeof(PropertySearchModel),
                typeof(IPropertySearchModel)
            },
            {
                typeof(QualifierScopeEntity),
                typeof(IQualifierScopeEntity)
            },
            {
                typeof(ReferencedProject),
                typeof(IReferencedProject)
            },
            {
                typeof(ReferenceSearchModel),
                typeof(IReferenceSearchModel)
            },
            {
                typeof(ReferenceSearchResult),
                typeof(IReferenceSearchResult)
            },
            {
                typeof(ReferenceSpan),
                typeof(IReferenceSpan)
            },
            {
                typeof(ReferenceSymbol),
                typeof(IReferenceSymbol)
            },
            {
                typeof(RepoFileScopeEntity),
                typeof(IRepoFileScopeEntity)
            },
            {
                typeof(RepoScopeEntity),
                typeof(IRepoScopeEntity)
            },
            {
                typeof(Repository),
                typeof(IRepository)
            },
            {
                typeof(RepositoryReference),
                typeof(IRepositoryReference)
            },
            {
                typeof(RepositorySearchModel),
                typeof(IRepositorySearchModel)
            },
            {
                typeof(RepositoryStoreInfo),
                typeof(IRepositoryStoreInfo)
            },
            {
                typeof(SearchEntity),
                typeof(ISearchEntity)
            },
            {
                typeof(SearchResult),
                typeof(ISearchResult)
            },
            {
                typeof(SharedReferenceInfo),
                typeof(ISharedReferenceInfo)
            },
            {
                typeof(SharedReferenceInfoSpan),
                typeof(ISharedReferenceInfoSpan)
            },
            {
                typeof(SourceControlFileInfo),
                typeof(ISourceControlFileInfo)
            },
            {
                typeof(SourceFile),
                typeof(ISourceFile)
            },
            {
                typeof(SourceFileBase),
                typeof(ISourceFileBase)
            },
            {
                typeof(SourceFileInfo),
                typeof(ISourceFileInfo)
            },
            {
                typeof(SourceSearchModelBase),
                typeof(ISourceSearchModelBase)
            },
            {
                typeof(Span),
                typeof(ISpan)
            },
            {
                typeof(StoredBoundSourceFile),
                typeof(IStoredBoundSourceFile)
            },
            {
                typeof(StoredFilter),
                typeof(IStoredFilter)
            },
            {
                typeof(StoredRepositoryGroupInfo),
                typeof(IStoredRepositoryGroupInfo)
            },
            {
                typeof(StoredRepositoryGroupSettings),
                typeof(IStoredRepositoryGroupSettings)
            },
            {
                typeof(StoredRepositoryInfo),
                typeof(IStoredRepositoryInfo)
            },
            {
                typeof(StoredRepositorySettings),
                typeof(IStoredRepositorySettings)
            },
            {
                typeof(SymbolReferenceList),
                typeof(ISymbolReferenceList)
            },
            {
                typeof(SymbolSpan),
                typeof(ISymbolSpan)
            },
            {
                typeof(TextChunkSearchModel),
                typeof(ITextChunkSearchModel)
            },
            {
                typeof(TextLineSpan),
                typeof(ITextLineSpan)
            },
            {
                typeof(TextLineSpanResult),
                typeof(ITextLineSpanResult)
            },
            {
                typeof(TextSourceSearchModel),
                typeof(ITextSourceSearchModel)
            },
            {
                typeof(UserSettings),
                typeof(IUserSettings)
            },
        };
        public static IReadOnlyDictionary<Type, Type> FromAdapterImplementationMap { get; } = new Dictionary<Type, Type>()
        {
            {
                typeof(ClassificationListModel),
                typeof(IClassificationListModel)
            },
            {
                typeof(IntArray),
                typeof(IIntArray)
            },
            {
                typeof(PropertyMap),
                typeof(IPropertyMap)
            },
            {
                typeof(ReferenceListModel),
                typeof(IReferenceListModel)
            },
            {
                typeof(SharedReferenceInfoSpanModel),
                typeof(ISharedReferenceInfoSpanModel)
            },
            {
                typeof(SymbolLineSpanListModel),
                typeof(ISymbolLineSpanListModel)
            },
        };

        public static AnalyzedProjectInfo ToImplementation(this IAnalyzedProjectInfo entity) => ((AnalyzedProjectInfo)entity);
        public static IAnalyzedProjectInfo FromImplementation(this AnalyzedProjectInfo entity) => ((IAnalyzedProjectInfo)entity);
        public static BoundSourceFile ToImplementation(this IBoundSourceFile entity) => ((BoundSourceFile)entity);
        public static IBoundSourceFile FromImplementation(this BoundSourceFile entity) => ((IBoundSourceFile)entity);
        public static BoundSourceInfo ToImplementation(this IBoundSourceInfo entity) => ((BoundSourceInfo)entity);
        public static IBoundSourceInfo FromImplementation(this BoundSourceInfo entity) => ((IBoundSourceInfo)entity);
        public static BoundSourceSearchModel ToImplementation(this IBoundSourceSearchModel entity) => ((BoundSourceSearchModel)entity);
        public static IBoundSourceSearchModel FromImplementation(this BoundSourceSearchModel entity) => ((IBoundSourceSearchModel)entity);
        public static Branch ToImplementation(this IBranch entity) => ((Branch)entity);
        public static IBranch FromImplementation(this Branch entity) => ((IBranch)entity);
        public static ChunkedSourceFile ToImplementation(this IChunkedSourceFile entity) => ((ChunkedSourceFile)entity);
        public static IChunkedSourceFile FromImplementation(this ChunkedSourceFile entity) => ((IChunkedSourceFile)entity);
        public static ChunkReference ToImplementation(this IChunkReference entity) => ((ChunkReference)entity);
        public static IChunkReference FromImplementation(this ChunkReference entity) => ((IChunkReference)entity);
        public static ClassificationListModel ToImplementation(this IClassificationListModel entity) => ((ClassificationListModel)entity);
        public static IClassificationListModel FromImplementation(this ClassificationListModel entity) => ((IClassificationListModel)entity);
        public static ClassificationSpan ToImplementation(this IClassificationSpan entity) => ((ClassificationSpan)entity);
        public static IClassificationSpan FromImplementation(this ClassificationSpan entity) => ((IClassificationSpan)entity);
        public static ClassificationStyle ToImplementation(this IClassificationStyle entity) => ((ClassificationStyle)entity);
        public static IClassificationStyle FromImplementation(this ClassificationStyle entity) => ((IClassificationStyle)entity);
        public static CodeSymbol ToImplementation(this ICodeSymbol entity) => ((CodeSymbol)entity);
        public static ICodeSymbol FromImplementation(this CodeSymbol entity) => ((ICodeSymbol)entity);
        public static Commit ToImplementation(this ICommit entity) => ((Commit)entity);
        public static ICommit FromImplementation(this Commit entity) => ((ICommit)entity);
        public static CommitInfo ToImplementation(this ICommitInfo entity) => ((CommitInfo)entity);
        public static ICommitInfo FromImplementation(this CommitInfo entity) => ((ICommitInfo)entity);
        public static CommitScopeEntity ToImplementation(this ICommitScopeEntity entity) => ((CommitScopeEntity)entity);
        public static ICommitScopeEntity FromImplementation(this CommitScopeEntity entity) => ((ICommitScopeEntity)entity);
        public static CommitSearchModel ToImplementation(this ICommitSearchModel entity) => ((CommitSearchModel)entity);
        public static ICommitSearchModel FromImplementation(this CommitSearchModel entity) => ((ICommitSearchModel)entity);
        public static DefinitionSearchModel ToImplementation(this IDefinitionSearchModel entity) => ((DefinitionSearchModel)entity);
        public static IDefinitionSearchModel FromImplementation(this DefinitionSearchModel entity) => ((IDefinitionSearchModel)entity);
        public static DefinitionSpan ToImplementation(this IDefinitionSpan entity) => ((DefinitionSpan)entity);
        public static IDefinitionSpan FromImplementation(this DefinitionSpan entity) => ((IDefinitionSpan)entity);
        public static DefinitionSymbol ToImplementation(this IDefinitionSymbol entity) => ((DefinitionSymbol)entity);
        public static IDefinitionSymbol FromImplementation(this DefinitionSymbol entity) => ((IDefinitionSymbol)entity);
        public static DefinitionSymbolExtendedSearchInfo ToImplementation(this IDefinitionSymbolExtendedSearchInfo entity) => ((DefinitionSymbolExtendedSearchInfo)entity);
        public static IDefinitionSymbolExtendedSearchInfo FromImplementation(this DefinitionSymbolExtendedSearchInfo entity) => ((IDefinitionSymbolExtendedSearchInfo)entity);
        public static DefinitionSymbolExtensionInfo ToImplementation(this IDefinitionSymbolExtensionInfo entity) => ((DefinitionSymbolExtensionInfo)entity);
        public static IDefinitionSymbolExtensionInfo FromImplementation(this DefinitionSymbolExtensionInfo entity) => ((IDefinitionSymbolExtensionInfo)entity);
        public static DirectoryRepositoryStoreInfo ToImplementation(this IDirectoryRepositoryStoreInfo entity) => ((DirectoryRepositoryStoreInfo)entity);
        public static IDirectoryRepositoryStoreInfo FromImplementation(this DirectoryRepositoryStoreInfo entity) => ((IDirectoryRepositoryStoreInfo)entity);
        public static DisplayCodeSymbol ToImplementation(this IDisplayCodeSymbol entity) => ((DisplayCodeSymbol)entity);
        public static IDisplayCodeSymbol FromImplementation(this DisplayCodeSymbol entity) => ((IDisplayCodeSymbol)entity);
        public static DocumentationReferenceSymbol ToImplementation(this IDocumentationReferenceSymbol entity) => ((DocumentationReferenceSymbol)entity);
        public static IDocumentationReferenceSymbol FromImplementation(this DocumentationReferenceSymbol entity) => ((IDocumentationReferenceSymbol)entity);
        public static FileSpanResult ToImplementation(this IFileSpanResult entity) => ((FileSpanResult)entity);
        public static IFileSpanResult FromImplementation(this FileSpanResult entity) => ((IFileSpanResult)entity);
        public static GlobalStoredRepositorySettings ToImplementation(this IGlobalStoredRepositorySettings entity) => ((GlobalStoredRepositorySettings)entity);
        public static IGlobalStoredRepositorySettings FromImplementation(this GlobalStoredRepositorySettings entity) => ((IGlobalStoredRepositorySettings)entity);
        public static HeaderInfo ToImplementation(this IHeaderInfo entity) => ((HeaderInfo)entity);
        public static IHeaderInfo FromImplementation(this HeaderInfo entity) => ((IHeaderInfo)entity);
        public static IntArray ToImplementation(this IIntArray entity) => ((IntArray)entity);
        public static IIntArray FromImplementation(this IntArray entity) => ((IIntArray)entity);
        public static LanguageInfo ToImplementation(this ILanguageInfo entity) => ((LanguageInfo)entity);
        public static ILanguageInfo FromImplementation(this LanguageInfo entity) => ((ILanguageInfo)entity);
        public static LanguageSearchModel ToImplementation(this ILanguageSearchModel entity) => ((LanguageSearchModel)entity);
        public static ILanguageSearchModel FromImplementation(this LanguageSearchModel entity) => ((ILanguageSearchModel)entity);
        public static LineSpan ToImplementation(this ILineSpan entity) => ((LineSpan)entity);
        public static ILineSpan FromImplementation(this LineSpan entity) => ((ILineSpan)entity);
        public static NewBoundSourceFile ToImplementation(this INewBoundSourceFile entity) => ((NewBoundSourceFile)entity);
        public static INewBoundSourceFile FromImplementation(this NewBoundSourceFile entity) => ((INewBoundSourceFile)entity);
        public static OutliningRegion ToImplementation(this IOutliningRegion entity) => ((OutliningRegion)entity);
        public static IOutliningRegion FromImplementation(this OutliningRegion entity) => ((IOutliningRegion)entity);
        public static ParameterDefinitionSpan ToImplementation(this IParameterDefinitionSpan entity) => ((ParameterDefinitionSpan)entity);
        public static IParameterDefinitionSpan FromImplementation(this ParameterDefinitionSpan entity) => ((IParameterDefinitionSpan)entity);
        public static ParameterDocumentation ToImplementation(this IParameterDocumentation entity) => ((ParameterDocumentation)entity);
        public static IParameterDocumentation FromImplementation(this ParameterDocumentation entity) => ((IParameterDocumentation)entity);
        public static ParameterReferenceSpan ToImplementation(this IParameterReferenceSpan entity) => ((ParameterReferenceSpan)entity);
        public static IParameterReferenceSpan FromImplementation(this ParameterReferenceSpan entity) => ((IParameterReferenceSpan)entity);
        public static ProjectFileLink ToImplementation(this IProjectFileLink entity) => ((ProjectFileLink)entity);
        public static IProjectFileLink FromImplementation(this ProjectFileLink entity) => ((IProjectFileLink)entity);
        public static ProjectFileScopeEntity ToImplementation(this IProjectFileScopeEntity entity) => ((ProjectFileScopeEntity)entity);
        public static IProjectFileScopeEntity FromImplementation(this ProjectFileScopeEntity entity) => ((IProjectFileScopeEntity)entity);
        public static ProjectReferenceSearchModel ToImplementation(this IProjectReferenceSearchModel entity) => ((ProjectReferenceSearchModel)entity);
        public static IProjectReferenceSearchModel FromImplementation(this ProjectReferenceSearchModel entity) => ((IProjectReferenceSearchModel)entity);
        public static ProjectScopeEntity ToImplementation(this IProjectScopeEntity entity) => ((ProjectScopeEntity)entity);
        public static IProjectScopeEntity FromImplementation(this ProjectScopeEntity entity) => ((IProjectScopeEntity)entity);
        public static ProjectSearchModel ToImplementation(this IProjectSearchModel entity) => ((ProjectSearchModel)entity);
        public static IProjectSearchModel FromImplementation(this ProjectSearchModel entity) => ((IProjectSearchModel)entity);
        public static PropertyMap ToImplementation(this IPropertyMap entity) => ((PropertyMap)entity);
        public static IPropertyMap FromImplementation(this PropertyMap entity) => ((IPropertyMap)entity);
        public static PropertySearchModel ToImplementation(this IPropertySearchModel entity) => ((PropertySearchModel)entity);
        public static IPropertySearchModel FromImplementation(this PropertySearchModel entity) => ((IPropertySearchModel)entity);
        public static QualifierScopeEntity ToImplementation(this IQualifierScopeEntity entity) => ((QualifierScopeEntity)entity);
        public static IQualifierScopeEntity FromImplementation(this QualifierScopeEntity entity) => ((IQualifierScopeEntity)entity);
        public static ReferencedProject ToImplementation(this IReferencedProject entity) => ((ReferencedProject)entity);
        public static IReferencedProject FromImplementation(this ReferencedProject entity) => ((IReferencedProject)entity);
        public static ReferenceListModel ToImplementation(this IReferenceListModel entity) => ((ReferenceListModel)entity);
        public static IReferenceListModel FromImplementation(this ReferenceListModel entity) => ((IReferenceListModel)entity);
        public static ReferenceSearchModel ToImplementation(this IReferenceSearchModel entity) => ((ReferenceSearchModel)entity);
        public static IReferenceSearchModel FromImplementation(this ReferenceSearchModel entity) => ((IReferenceSearchModel)entity);
        public static ReferenceSearchResult ToImplementation(this IReferenceSearchResult entity) => ((ReferenceSearchResult)entity);
        public static IReferenceSearchResult FromImplementation(this ReferenceSearchResult entity) => ((IReferenceSearchResult)entity);
        public static ReferenceSpan ToImplementation(this IReferenceSpan entity) => ((ReferenceSpan)entity);
        public static IReferenceSpan FromImplementation(this ReferenceSpan entity) => ((IReferenceSpan)entity);
        public static ReferenceSymbol ToImplementation(this IReferenceSymbol entity) => ((ReferenceSymbol)entity);
        public static IReferenceSymbol FromImplementation(this ReferenceSymbol entity) => ((IReferenceSymbol)entity);
        public static RepoFileScopeEntity ToImplementation(this IRepoFileScopeEntity entity) => ((RepoFileScopeEntity)entity);
        public static IRepoFileScopeEntity FromImplementation(this RepoFileScopeEntity entity) => ((IRepoFileScopeEntity)entity);
        public static RepoScopeEntity ToImplementation(this IRepoScopeEntity entity) => ((RepoScopeEntity)entity);
        public static IRepoScopeEntity FromImplementation(this RepoScopeEntity entity) => ((IRepoScopeEntity)entity);
        public static Repository ToImplementation(this IRepository entity) => ((Repository)entity);
        public static IRepository FromImplementation(this Repository entity) => ((IRepository)entity);
        public static RepositoryReference ToImplementation(this IRepositoryReference entity) => ((RepositoryReference)entity);
        public static IRepositoryReference FromImplementation(this RepositoryReference entity) => ((IRepositoryReference)entity);
        public static RepositorySearchModel ToImplementation(this IRepositorySearchModel entity) => ((RepositorySearchModel)entity);
        public static IRepositorySearchModel FromImplementation(this RepositorySearchModel entity) => ((IRepositorySearchModel)entity);
        public static RepositoryStoreInfo ToImplementation(this IRepositoryStoreInfo entity) => ((RepositoryStoreInfo)entity);
        public static IRepositoryStoreInfo FromImplementation(this RepositoryStoreInfo entity) => ((IRepositoryStoreInfo)entity);
        public static SearchEntity ToImplementation(this ISearchEntity entity) => ((SearchEntity)entity);
        public static ISearchEntity FromImplementation(this SearchEntity entity) => ((ISearchEntity)entity);
        public static SearchResult ToImplementation(this ISearchResult entity) => ((SearchResult)entity);
        public static ISearchResult FromImplementation(this SearchResult entity) => ((ISearchResult)entity);
        public static SharedReferenceInfo ToImplementation(this ISharedReferenceInfo entity) => ((SharedReferenceInfo)entity);
        public static ISharedReferenceInfo FromImplementation(this SharedReferenceInfo entity) => ((ISharedReferenceInfo)entity);
        public static SharedReferenceInfoSpan ToImplementation(this ISharedReferenceInfoSpan entity) => ((SharedReferenceInfoSpan)entity);
        public static ISharedReferenceInfoSpan FromImplementation(this SharedReferenceInfoSpan entity) => ((ISharedReferenceInfoSpan)entity);
        public static SharedReferenceInfoSpanModel ToImplementation(this ISharedReferenceInfoSpanModel entity) => ((SharedReferenceInfoSpanModel)entity);
        public static ISharedReferenceInfoSpanModel FromImplementation(this SharedReferenceInfoSpanModel entity) => ((ISharedReferenceInfoSpanModel)entity);
        public static SourceControlFileInfo ToImplementation(this ISourceControlFileInfo entity) => ((SourceControlFileInfo)entity);
        public static ISourceControlFileInfo FromImplementation(this SourceControlFileInfo entity) => ((ISourceControlFileInfo)entity);
        public static SourceFile ToImplementation(this ISourceFile entity) => ((SourceFile)entity);
        public static ISourceFile FromImplementation(this SourceFile entity) => ((ISourceFile)entity);
        public static SourceFileBase ToImplementation(this ISourceFileBase entity) => ((SourceFileBase)entity);
        public static ISourceFileBase FromImplementation(this SourceFileBase entity) => ((ISourceFileBase)entity);
        public static SourceFileInfo ToImplementation(this ISourceFileInfo entity) => ((SourceFileInfo)entity);
        public static ISourceFileInfo FromImplementation(this SourceFileInfo entity) => ((ISourceFileInfo)entity);
        public static SourceSearchModelBase ToImplementation(this ISourceSearchModelBase entity) => ((SourceSearchModelBase)entity);
        public static ISourceSearchModelBase FromImplementation(this SourceSearchModelBase entity) => ((ISourceSearchModelBase)entity);
        public static Span ToImplementation(this ISpan entity) => ((Span)entity);
        public static ISpan FromImplementation(this Span entity) => ((ISpan)entity);
        public static StoredBoundSourceFile ToImplementation(this IStoredBoundSourceFile entity) => ((StoredBoundSourceFile)entity);
        public static IStoredBoundSourceFile FromImplementation(this StoredBoundSourceFile entity) => ((IStoredBoundSourceFile)entity);
        public static StoredFilter ToImplementation(this IStoredFilter entity) => ((StoredFilter)entity);
        public static IStoredFilter FromImplementation(this StoredFilter entity) => ((IStoredFilter)entity);
        public static StoredRepositoryGroupInfo ToImplementation(this IStoredRepositoryGroupInfo entity) => ((StoredRepositoryGroupInfo)entity);
        public static IStoredRepositoryGroupInfo FromImplementation(this StoredRepositoryGroupInfo entity) => ((IStoredRepositoryGroupInfo)entity);
        public static StoredRepositoryGroupSettings ToImplementation(this IStoredRepositoryGroupSettings entity) => ((StoredRepositoryGroupSettings)entity);
        public static IStoredRepositoryGroupSettings FromImplementation(this StoredRepositoryGroupSettings entity) => ((IStoredRepositoryGroupSettings)entity);
        public static StoredRepositoryInfo ToImplementation(this IStoredRepositoryInfo entity) => ((StoredRepositoryInfo)entity);
        public static IStoredRepositoryInfo FromImplementation(this StoredRepositoryInfo entity) => ((IStoredRepositoryInfo)entity);
        public static StoredRepositorySettings ToImplementation(this IStoredRepositorySettings entity) => ((StoredRepositorySettings)entity);
        public static IStoredRepositorySettings FromImplementation(this StoredRepositorySettings entity) => ((IStoredRepositorySettings)entity);
        public static SymbolLineSpanListModel ToImplementation(this ISymbolLineSpanListModel entity) => ((SymbolLineSpanListModel)entity);
        public static ISymbolLineSpanListModel FromImplementation(this SymbolLineSpanListModel entity) => ((ISymbolLineSpanListModel)entity);
        public static SymbolReferenceList ToImplementation(this ISymbolReferenceList entity) => ((SymbolReferenceList)entity);
        public static ISymbolReferenceList FromImplementation(this SymbolReferenceList entity) => ((ISymbolReferenceList)entity);
        public static SymbolSpan ToImplementation(this ISymbolSpan entity) => ((SymbolSpan)entity);
        public static ISymbolSpan FromImplementation(this SymbolSpan entity) => ((ISymbolSpan)entity);
        public static TextChunkSearchModel ToImplementation(this ITextChunkSearchModel entity) => ((TextChunkSearchModel)entity);
        public static ITextChunkSearchModel FromImplementation(this TextChunkSearchModel entity) => ((ITextChunkSearchModel)entity);
        public static TextLineSpan ToImplementation(this ITextLineSpan entity) => ((TextLineSpan)entity);
        public static ITextLineSpan FromImplementation(this TextLineSpan entity) => ((ITextLineSpan)entity);
        public static TextLineSpanResult ToImplementation(this ITextLineSpanResult entity) => ((TextLineSpanResult)entity);
        public static ITextLineSpanResult FromImplementation(this TextLineSpanResult entity) => ((ITextLineSpanResult)entity);
        public static TextSourceSearchModel ToImplementation(this ITextSourceSearchModel entity) => ((TextSourceSearchModel)entity);
        public static ITextSourceSearchModel FromImplementation(this TextSourceSearchModel entity) => ((ITextSourceSearchModel)entity);
        public static UserSettings ToImplementation(this IUserSettings entity) => ((UserSettings)entity);
        public static IUserSettings FromImplementation(this UserSettings entity) => ((IUserSettings)entity);
    }

    public class Descriptors
    {
        public partial class AnalyzedProjectInfoDescriptor : SingletonDescriptorBase<AnalyzedProjectInfo, IAnalyzedProjectInfo, AnalyzedProjectInfoDescriptor>, ICreate<AnalyzedProjectInfoDescriptor>
        {
            static AnalyzedProjectInfoDescriptor ICreate<AnalyzedProjectInfoDescriptor>.Create() => new AnalyzedProjectInfoDescriptor();
            AnalyzedProjectInfoDescriptor() : base(10, 11)
            {
                Add(new Property<int, int>(2, "DefinitionCount", e => e.DefinitionCount, (e, v) => e.DefinitionCount = v));
                Add(new ListProperty<DefinitionSymbol, IDefinitionSymbol>(3, "Definitions", e => e.Definitions, (e, v) => e.Definitions = v));
                Add(new Property<string, string>(4, "DisplayName", e => e.DisplayName, (e, v) => e.DisplayName = v));
                Add(new ListProperty<ProjectFileScopeEntity, IProjectFileScopeEntity>(6, "Files", e => e.Files, (e, v) => e.Files = v));
                Add(new Property<ProjectFileScopeEntity, IProjectFileScopeEntity>(7, "PrimaryFile", e => e.PrimaryFile, (e, v) => e.PrimaryFile = v));
                Add(new Property<string, string>(1, "ProjectId", e => e.ProjectId, (e, v) => e.ProjectId = v));
                Add(new Property<StringEnum<ProjectKind>, StringEnum<ProjectKind>>(8, "ProjectKind", e => e.ProjectKind, (e, v) => e.ProjectKind = v));
                Add(new ListProperty<ReferencedProject, IReferencedProject>(9, "ProjectReferences", e => e.ProjectReferences, (e, v) => e.ProjectReferences = v));
                Add(new Property<PropertyMap, IPropertyMap>(5, "Properties", e => e.Properties, (e, v) => e.Properties = v));
                Add(new Property<string, string>(10, "Qualifier", e => e.Qualifier, (e, v) => e.Qualifier = v));
                Add(new Property<string, string>(0, "RepositoryName", e => e.RepositoryName, (e, v) => e.RepositoryName = v));
            }
        }

        public partial class BoundSourceFileDescriptor : SingletonDescriptorBase<BoundSourceFile, IBoundSourceFile, BoundSourceFileDescriptor>, ICreate<BoundSourceFileDescriptor>
        {
            static BoundSourceFileDescriptor ICreate<BoundSourceFileDescriptor>.Create() => new BoundSourceFileDescriptor();
            BoundSourceFileDescriptor() : base(15, 7)
            {
                Add(new ListProperty<ClassificationSpan, IClassificationSpan>(11, "Classifications", e => e.Classifications, (e, v) => e.Classifications = v));
                Add(new Property<Commit, ICommit>(14, "Commit", e => e.Commit, (e, v) => e.Commit = v));
                Add(new Property<int, int>(2, "DefinitionCount", e => e.DefinitionCount, (e, v) => e.DefinitionCount = v));
                Add(new ListProperty<DefinitionSpan, IDefinitionSpan>(3, "Definitions", e => e.Definitions, (e, v) => e.Definitions = v));
                Add(new Property<int, int>(12, "ReferenceCount", e => e.ReferenceCount, (e, v) => e.ReferenceCount = v));
                Add(new ListProperty<ReferenceSpan, IReferenceSpan>(13, "References", e => e.References, (e, v) => e.References = v));
                Add(new Property<SourceFile, ISourceFile>(15, "SourceFile", e => e.SourceFile, (e, v) => e.SourceFile = v));
            }
        }

        public partial class BoundSourceInfoDescriptor : SingletonDescriptorBase<BoundSourceInfo, IBoundSourceInfo, BoundSourceInfoDescriptor>, ICreate<BoundSourceInfoDescriptor>
        {
            static BoundSourceInfoDescriptor ICreate<BoundSourceInfoDescriptor>.Create() => new BoundSourceInfoDescriptor();
            BoundSourceInfoDescriptor() : base(13, 5)
            {
                Add(new ListProperty<ClassificationSpan, IClassificationSpan>(11, "Classifications", e => e.Classifications, (e, v) => e.Classifications = v));
                Add(new Property<int, int>(2, "DefinitionCount", e => e.DefinitionCount, (e, v) => e.DefinitionCount = v));
                Add(new ListProperty<DefinitionSpan, IDefinitionSpan>(3, "Definitions", e => e.Definitions, (e, v) => e.Definitions = v));
                Add(new Property<int, int>(12, "ReferenceCount", e => e.ReferenceCount, (e, v) => e.ReferenceCount = v));
                Add(new ListProperty<ReferenceSpan, IReferenceSpan>(13, "References", e => e.References, (e, v) => e.References = v));
            }
        }

        public partial class BoundSourceSearchModelDescriptor : SingletonDescriptorBase<BoundSourceSearchModel, IBoundSourceSearchModel, BoundSourceSearchModelDescriptor>, ICreate<BoundSourceSearchModelDescriptor>
        {
            static BoundSourceSearchModelDescriptor ICreate<BoundSourceSearchModelDescriptor>.Create() => new BoundSourceSearchModelDescriptor();
            BoundSourceSearchModelDescriptor() : base(25, 11)
            {
                Add(new Property<BoundSourceInfo, IBoundSourceInfo>(21, "BindingInfo", e => e.BindingInfo, (e, v) => e.BindingInfo = v));
                Add(new Property<ClassificationListModel, IClassificationListModel>(22, "CompressedClassifications", e => e.CompressedClassifications, (e, v) => e.CompressedClassifications = v));
                Add(new Property<string, string>(23, "Content", e => e.Content, (e, v) => e.Content = v));
                Add(new Property<MurmurHash, MurmurHash>(16, "EntityContentId", e => e.EntityContentId, (e, v) => e.EntityContentId = v));
                Add(new Property<int, int>(17, "EntityContentSize", e => e.EntityContentSize, (e, v) => e.EntityContentSize = v));
                Add(new Property<ObjectContentLink<IBoundSourceSearchModel>, ObjectContentLink<IBoundSourceSearchModel>>(25, "ExternalLink", e => e.ExternalLink, (e, v) => e.ExternalLink = v));
                Add(new Property<SourceFileBase, ISourceFileBase>(24, "File", e => e.File, (e, v) => e.File = v));
                Add(new Property<bool, bool>(18, "IsAdded", e => e.IsAdded, (e, v) => e.IsAdded = v));
                Add(new ListProperty<SymbolReferenceList, ISymbolReferenceList>(13, "References", e => e.References, (e, v) => e.References = v));
                Add(new Property<int, int>(19, "StableId", e => e.StableId, (e, v) => e.StableId = v));
                Add(new Property<MurmurHash, MurmurHash>(20, "Uid", e => e.Uid, (e, v) => e.Uid = v));
            }
        }

        public partial class BranchDescriptor : SingletonDescriptorBase<Branch, IBranch, BranchDescriptor>, ICreate<BranchDescriptor>
        {
            static BranchDescriptor ICreate<BranchDescriptor>.Create() => new BranchDescriptor();
            BranchDescriptor() : base(28, 3)
            {
                Add(new Property<string, string>(26, "Description", e => e.Description, (e, v) => e.Description = v));
                Add(new Property<string, string>(27, "HeadCommitId", e => e.HeadCommitId, (e, v) => e.HeadCommitId = v));
                Add(new Property<string, string>(28, "Name", e => e.Name, (e, v) => e.Name = v));
            }
        }

        public partial class ChunkedSourceFileDescriptor : SingletonDescriptorBase<ChunkedSourceFile, IChunkedSourceFile, ChunkedSourceFileDescriptor>, ICreate<ChunkedSourceFileDescriptor>
        {
            static ChunkedSourceFileDescriptor ICreate<ChunkedSourceFileDescriptor>.Create() => new ChunkedSourceFileDescriptor();
            ChunkedSourceFileDescriptor() : base(32, 4)
            {
                Add(new ListProperty<ChunkReference, IChunkReference>(32, "Chunks", e => e.Chunks, (e, v) => e.Chunks = v));
                Add(new Property<bool, bool>(29, "ExcludeFromSearch", e => e.ExcludeFromSearch, (e, v) => e.ExcludeFromSearch = v));
                Add(new Property<BoundSourceFlags, BoundSourceFlags>(30, "Flags", e => e.Flags, (e, v) => e.Flags = v));
                Add(new Property<SourceFileInfo, ISourceFileInfo>(31, "Info", e => e.Info, (e, v) => e.Info = v));
            }
        }

        public partial class ChunkReferenceDescriptor : SingletonDescriptorBase<ChunkReference, IChunkReference, ChunkReferenceDescriptor>, ICreate<ChunkReferenceDescriptor>
        {
            static ChunkReferenceDescriptor ICreate<ChunkReferenceDescriptor>.Create() => new ChunkReferenceDescriptor();
            ChunkReferenceDescriptor() : base(34, 2)
            {
                Add(new Property<int, int>(33, "Id", e => e.Id, (e, v) => e.Id = v));
                Add(new Property<int, int>(34, "StartLineNumber", e => e.StartLineNumber, (e, v) => e.StartLineNumber = v));
            }
        }

        public partial class ClassificationSpanDescriptor : SingletonDescriptorBase<ClassificationSpan, IClassificationSpan, ClassificationSpanDescriptor>, ICreate<ClassificationSpanDescriptor>
        {
            static ClassificationSpanDescriptor ICreate<ClassificationSpanDescriptor>.Create() => new ClassificationSpanDescriptor();
            ClassificationSpanDescriptor() : base(40, 6)
            {
                Add(new Property<StringEnum<ClassificationName>, StringEnum<ClassificationName>>(37, "Classification", e => e.Classification, (e, v) => e.Classification = v));
                Add(new Property<int, int>(38, "DefaultClassificationColor", e => e.DefaultClassificationColor, (e, v) => e.DefaultClassificationColor = v));
                Add(new Property<int, int>(35, "Length", e => e.Length, (e, v) => e.Length = v));
                Add(new Property<int, int>(39, "LocalGroupId", e => e.LocalGroupId, (e, v) => e.LocalGroupId = v));
                Add(new Property<int, int>(36, "Start", e => e.Start, (e, v) => e.Start = v));
                Add(new Property<int, int>(40, "SymbolDepth", e => e.SymbolDepth, (e, v) => e.SymbolDepth = v));
            }
        }

        public partial class ClassificationStyleDescriptor : SingletonDescriptorBase<ClassificationStyle, IClassificationStyle, ClassificationStyleDescriptor>, ICreate<ClassificationStyleDescriptor>
        {
            static ClassificationStyleDescriptor ICreate<ClassificationStyleDescriptor>.Create() => new ClassificationStyleDescriptor();
            ClassificationStyleDescriptor() : base(42, 3)
            {
                Add(new Property<int, int>(41, "Color", e => e.Color, (e, v) => e.Color = v));
                Add(new Property<bool, bool>(42, "Italic", e => e.Italic, (e, v) => e.Italic = v));
                Add(new Property<StringEnum<ClassificationName>, StringEnum<ClassificationName>>(28, "Name", e => e.Name, (e, v) => e.Name = v));
            }
        }

        public partial class CodeSymbolDescriptor : SingletonDescriptorBase<CodeSymbol, ICodeSymbol, CodeSymbolDescriptor>, ICreate<CodeSymbolDescriptor>
        {
            static CodeSymbolDescriptor ICreate<CodeSymbolDescriptor>.Create() => new CodeSymbolDescriptor();
            CodeSymbolDescriptor() : base(43, 3)
            {
                Add(new Property<SymbolId, SymbolId>(33, "Id", e => e.Id, (e, v) => e.Id = v));
                Add(new Property<StringEnum<SymbolKinds>, StringEnum<SymbolKinds>>(43, "Kind", e => e.Kind, (e, v) => e.Kind = v));
                Add(new Property<string, string>(1, "ProjectId", e => e.ProjectId, (e, v) => e.ProjectId = v));
            }
        }

        public partial class CommitDescriptor : SingletonDescriptorBase<Commit, ICommit, CommitDescriptor>, ICreate<CommitDescriptor>
        {
            static CommitDescriptor ICreate<CommitDescriptor>.Create() => new CommitDescriptor();
            CommitDescriptor() : base(49, 8)
            {
                Add(new Property<string, string>(45, "Alias", e => e.Alias, (e, v) => e.Alias = v));
                Add(new Property<string, string>(46, "BuildUri", e => e.BuildUri, (e, v) => e.BuildUri = v));
                Add(new Property<string, string>(44, "CommitId", e => e.CommitId, (e, v) => e.CommitId = v));
                Add(new Property<DateTime, DateTime>(47, "DateCommitted", e => e.DateCommitted, (e, v) => e.DateCommitted = v));
                Add(new Property<DateTime, DateTime>(48, "DateUploaded", e => e.DateUploaded, (e, v) => e.DateUploaded = v));
                Add(new Property<string, string>(26, "Description", e => e.Description, (e, v) => e.Description = v));
                Add(new ListProperty<string, string>(49, "ParentCommitIds", e => e.ParentCommitIds, (e, v) => e.ParentCommitIds = v));
                Add(new Property<string, string>(0, "RepositoryName", e => e.RepositoryName, (e, v) => e.RepositoryName = v));
            }
        }

        public partial class CommitInfoDescriptor : SingletonDescriptorBase<CommitInfo, ICommitInfo, CommitInfoDescriptor>, ICreate<CommitInfoDescriptor>
        {
            static CommitInfoDescriptor ICreate<CommitInfoDescriptor>.Create() => new CommitInfoDescriptor();
            CommitInfoDescriptor() : base(48, 6)
            {
                Add(new Property<string, string>(45, "Alias", e => e.Alias, (e, v) => e.Alias = v));
                Add(new Property<string, string>(46, "BuildUri", e => e.BuildUri, (e, v) => e.BuildUri = v));
                Add(new Property<string, string>(44, "CommitId", e => e.CommitId, (e, v) => e.CommitId = v));
                Add(new Property<DateTime, DateTime>(47, "DateCommitted", e => e.DateCommitted, (e, v) => e.DateCommitted = v));
                Add(new Property<DateTime, DateTime>(48, "DateUploaded", e => e.DateUploaded, (e, v) => e.DateUploaded = v));
                Add(new Property<string, string>(0, "RepositoryName", e => e.RepositoryName, (e, v) => e.RepositoryName = v));
            }
        }

        public partial class CommitScopeEntityDescriptor : SingletonDescriptorBase<CommitScopeEntity, ICommitScopeEntity, CommitScopeEntityDescriptor>, ICreate<CommitScopeEntityDescriptor>
        {
            static CommitScopeEntityDescriptor ICreate<CommitScopeEntityDescriptor>.Create() => new CommitScopeEntityDescriptor();
            CommitScopeEntityDescriptor() : base(44, 2)
            {
                Add(new Property<string, string>(44, "CommitId", e => e.CommitId, (e, v) => e.CommitId = v));
                Add(new Property<string, string>(0, "RepositoryName", e => e.RepositoryName, (e, v) => e.RepositoryName = v));
            }
        }

        public partial class CommitSearchModelDescriptor : SingletonDescriptorBase<CommitSearchModel, ICommitSearchModel, CommitSearchModelDescriptor>, ICreate<CommitSearchModelDescriptor>
        {
            static CommitSearchModelDescriptor ICreate<CommitSearchModelDescriptor>.Create() => new CommitSearchModelDescriptor();
            CommitSearchModelDescriptor() : base(20, 6)
            {
                Add(new Property<Commit, ICommit>(14, "Commit", e => e.Commit, (e, v) => e.Commit = v));
                Add(new Property<MurmurHash, MurmurHash>(16, "EntityContentId", e => e.EntityContentId, (e, v) => e.EntityContentId = v));
                Add(new Property<int, int>(17, "EntityContentSize", e => e.EntityContentSize, (e, v) => e.EntityContentSize = v));
                Add(new Property<bool, bool>(18, "IsAdded", e => e.IsAdded, (e, v) => e.IsAdded = v));
                Add(new Property<int, int>(19, "StableId", e => e.StableId, (e, v) => e.StableId = v));
                Add(new Property<MurmurHash, MurmurHash>(20, "Uid", e => e.Uid, (e, v) => e.Uid = v));
            }
        }

        public partial class DefinitionSearchModelDescriptor : SingletonDescriptorBase<DefinitionSearchModel, IDefinitionSearchModel, DefinitionSearchModelDescriptor>, ICreate<DefinitionSearchModelDescriptor>
        {
            static DefinitionSearchModelDescriptor ICreate<DefinitionSearchModelDescriptor>.Create() => new DefinitionSearchModelDescriptor();
            DefinitionSearchModelDescriptor() : base(52, 8)
            {
                Add(new Property<IDefinitionSymbol, IDefinitionSymbol>(50, "Definition", e => e.Definition, (e, v) => e.Definition = v));
                Add(new Property<MurmurHash, MurmurHash>(16, "EntityContentId", e => e.EntityContentId, (e, v) => e.EntityContentId = v));
                Add(new Property<int, int>(17, "EntityContentSize", e => e.EntityContentSize, (e, v) => e.EntityContentSize = v));
                Add(new Property<bool, bool>(51, "ExcludeFromDefaultSearch", e => e.ExcludeFromDefaultSearch, (e, v) => e.ExcludeFromDefaultSearch = v));
                Add(new Property<IDefinitionSymbolExtendedSearchInfo, IDefinitionSymbolExtendedSearchInfo>(52, "ExtendedSearchInfo", e => e.ExtendedSearchInfo, (e, v) => e.ExtendedSearchInfo = v));
                Add(new Property<bool, bool>(18, "IsAdded", e => e.IsAdded, (e, v) => e.IsAdded = v));
                Add(new Property<int, int>(19, "StableId", e => e.StableId, (e, v) => e.StableId = v));
                Add(new Property<MurmurHash, MurmurHash>(20, "Uid", e => e.Uid, (e, v) => e.Uid = v));
            }
        }

        public partial class DefinitionSpanDescriptor : SingletonDescriptorBase<DefinitionSpan, IDefinitionSpan, DefinitionSpanDescriptor>, ICreate<DefinitionSpanDescriptor>
        {
            static DefinitionSpanDescriptor ICreate<DefinitionSpanDescriptor>.Create() => new DefinitionSpanDescriptor();
            DefinitionSpanDescriptor() : base(54, 5)
            {
                Add(new Property<DefinitionSymbol, IDefinitionSymbol>(50, "Definition", e => e.Definition, (e, v) => e.Definition = v));
                Add(new Property<Extent, Extent>(53, "FullSpan", e => e.FullSpan, (e, v) => e.FullSpan = v));
                Add(new Property<int, int>(35, "Length", e => e.Length, (e, v) => e.Length = v));
                Add(new ListProperty<ParameterDefinitionSpan, IParameterDefinitionSpan>(54, "Parameters", e => e.Parameters, (e, v) => e.Parameters = v));
                Add(new Property<int, int>(36, "Start", e => e.Start, (e, v) => e.Start = v));
            }
        }

        public partial class DefinitionSymbolDescriptor : SingletonDescriptorBase<DefinitionSymbol, IDefinitionSymbol, DefinitionSymbolDescriptor>, ICreate<DefinitionSymbolDescriptor>
        {
            static DefinitionSymbolDescriptor ICreate<DefinitionSymbolDescriptor>.Create() => new DefinitionSymbolDescriptor();
            DefinitionSymbolDescriptor() : base(67, 24)
            {
                Add(new Property<string, string>(56, "AbbreviatedName", e => e.AbbreviatedName, (e, v) => e.AbbreviatedName = v));
                Add(new ListProperty<ClassifiedExtent, ClassifiedExtent>(11, "Classifications", e => e.Classifications, (e, v) => e.Classifications = v));
                Add(new Property<string, string>(57, "Comment", e => e.Comment, (e, v) => e.Comment = v));
                Add(new Property<string, string>(58, "ContainerQualifiedName", e => e.ContainerQualifiedName, (e, v) => e.ContainerQualifiedName = v));
                Add(new Property<SymbolId, SymbolId>(59, "ContainerTypeSymbolId", e => e.ContainerTypeSymbolId, (e, v) => e.ContainerTypeSymbolId = v));
                Add(new Property<string, string>(60, "DeclarationName", e => e.DeclarationName, (e, v) => e.DeclarationName = v));
                Add(new Property<string, string>(4, "DisplayName", e => e.DisplayName, (e, v) => e.DisplayName = v));
                Add(new Property<bool, bool>(51, "ExcludeFromDefaultSearch", e => e.ExcludeFromDefaultSearch, (e, v) => e.ExcludeFromDefaultSearch = v));
                Add(new Property<bool, bool>(29, "ExcludeFromSearch", e => e.ExcludeFromSearch, (e, v) => e.ExcludeFromSearch = v));
                Add(new ListProperty<DefinitionSymbolExtendedSearchInfo, IDefinitionSymbolExtendedSearchInfo>(52, "ExtendedSearchInfo", e => e.ExtendedSearchInfo, (e, v) => e.ExtendedSearchInfo = v));
                Add(new Property<DefinitionSymbolExtensionInfo, IDefinitionSymbolExtensionInfo>(61, "ExtensionInfo", e => e.ExtensionInfo, (e, v) => e.ExtensionInfo = v));
                Add(new Property<Glyph, Glyph>(62, "Glyph", e => e.Glyph, (e, v) => e.Glyph = v));
                Add(new Property<SymbolId, SymbolId>(33, "Id", e => e.Id, (e, v) => e.Id = v));
                Add(new Property<Nullable<Extent<IDefinitionSymbol>>, Nullable<Extent<IDefinitionSymbol>>>(67, "JsonRange", e => e.JsonRange, (e, v) => e.JsonRange = v));
                Add(new ListProperty<string, string>(63, "Keywords", e => e.Keywords, (e, v) => e.Keywords = v));
                Add(new Property<StringEnum<SymbolKinds>, StringEnum<SymbolKinds>>(43, "Kind", e => e.Kind, (e, v) => e.Kind = v));
                Add(new ListProperty<string, string>(64, "Modifiers", e => e.Modifiers, (e, v) => e.Modifiers = v));
                Add(new Property<string, string>(1, "ProjectId", e => e.ProjectId, (e, v) => e.ProjectId = v));
                Add(new Property<int, int>(12, "ReferenceCount", e => e.ReferenceCount, (e, v) => e.ReferenceCount = v));
                Add(new Property<ReferenceKind, ReferenceKind>(55, "ReferenceKind", e => e.ReferenceKind, (e, v) => e.ReferenceKind = v));
                Add(new Property<string, string>(65, "ShortName", e => e.ShortName, (e, v) => e.ShortName = v));
                Add(new Property<int, int>(40, "SymbolDepth", e => e.SymbolDepth, (e, v) => e.SymbolDepth = v));
                Add(new Property<string, string>(66, "TypeName", e => e.TypeName, (e, v) => e.TypeName = v));
                Add(new Property<string, string>(20, "Uid", e => e.Uid, (e, v) => e.Uid = v));
            }
        }

        public partial class DefinitionSymbolExtendedSearchInfoDescriptor : SingletonDescriptorBase<DefinitionSymbolExtendedSearchInfo, IDefinitionSymbolExtendedSearchInfo, DefinitionSymbolExtendedSearchInfoDescriptor>, ICreate<DefinitionSymbolExtendedSearchInfoDescriptor>
        {
            static DefinitionSymbolExtendedSearchInfoDescriptor ICreate<DefinitionSymbolExtendedSearchInfoDescriptor>.Create() => new DefinitionSymbolExtendedSearchInfoDescriptor();
            DefinitionSymbolExtendedSearchInfoDescriptor() : base(68, 1)
            {
                Add(new Property<Nullable<long>, Nullable<long>>(68, "ConstantValue", e => e.ConstantValue, (e, v) => e.ConstantValue = v));
            }
        }

        public partial class DefinitionSymbolExtensionInfoDescriptor : SingletonDescriptorBase<DefinitionSymbolExtensionInfo, IDefinitionSymbolExtensionInfo, DefinitionSymbolExtensionInfoDescriptor>, ICreate<DefinitionSymbolExtensionInfoDescriptor>
        {
            static DefinitionSymbolExtensionInfoDescriptor ICreate<DefinitionSymbolExtensionInfoDescriptor>.Create() => new DefinitionSymbolExtensionInfoDescriptor();
            DefinitionSymbolExtensionInfoDescriptor() : base(58, 2)
            {
                Add(new Property<string, string>(58, "ContainerQualifiedName", e => e.ContainerQualifiedName, (e, v) => e.ContainerQualifiedName = v));
                Add(new Property<string, string>(1, "ProjectId", e => e.ProjectId, (e, v) => e.ProjectId = v));
            }
        }

        public partial class DirectoryRepositoryStoreInfoDescriptor : SingletonDescriptorBase<DirectoryRepositoryStoreInfo, IDirectoryRepositoryStoreInfo, DirectoryRepositoryStoreInfoDescriptor>, ICreate<DirectoryRepositoryStoreInfoDescriptor>
        {
            static DirectoryRepositoryStoreInfoDescriptor ICreate<DirectoryRepositoryStoreInfoDescriptor>.Create() => new DirectoryRepositoryStoreInfoDescriptor();
            DirectoryRepositoryStoreInfoDescriptor() : base(71, 4)
            {
                Add(new Property<Branch, IBranch>(69, "Branch", e => e.Branch, (e, v) => e.Branch = v));
                Add(new Property<Commit, ICommit>(14, "Commit", e => e.Commit, (e, v) => e.Commit = v));
                Add(new Property<DirectoryStoreFormat, DirectoryStoreFormat>(71, "Format", e => e.Format, (e, v) => e.Format = v));
                Add(new Property<Repository, IRepository>(70, "Repository", e => e.Repository, (e, v) => e.Repository = v));
            }
        }

        public partial class DisplayCodeSymbolDescriptor : SingletonDescriptorBase<DisplayCodeSymbol, IDisplayCodeSymbol, DisplayCodeSymbolDescriptor>, ICreate<DisplayCodeSymbolDescriptor>
        {
            static DisplayCodeSymbolDescriptor ICreate<DisplayCodeSymbolDescriptor>.Create() => new DisplayCodeSymbolDescriptor();
            DisplayCodeSymbolDescriptor() : base(43, 5)
            {
                Add(new ListProperty<ClassifiedExtent, ClassifiedExtent>(11, "Classifications", e => e.Classifications, (e, v) => e.Classifications = v));
                Add(new Property<string, string>(4, "DisplayName", e => e.DisplayName, (e, v) => e.DisplayName = v));
                Add(new Property<SymbolId, SymbolId>(33, "Id", e => e.Id, (e, v) => e.Id = v));
                Add(new Property<StringEnum<SymbolKinds>, StringEnum<SymbolKinds>>(43, "Kind", e => e.Kind, (e, v) => e.Kind = v));
                Add(new Property<string, string>(1, "ProjectId", e => e.ProjectId, (e, v) => e.ProjectId = v));
            }
        }

        public partial class DocumentationReferenceSymbolDescriptor : SingletonDescriptorBase<DocumentationReferenceSymbol, IDocumentationReferenceSymbol, DocumentationReferenceSymbolDescriptor>, ICreate<DocumentationReferenceSymbolDescriptor>
        {
            static DocumentationReferenceSymbolDescriptor ICreate<DocumentationReferenceSymbolDescriptor>.Create() => new DocumentationReferenceSymbolDescriptor();
            DocumentationReferenceSymbolDescriptor() : base(57, 7)
            {
                Add(new Property<string, string>(57, "Comment", e => e.Comment, (e, v) => e.Comment = v));
                Add(new Property<string, string>(4, "DisplayName", e => e.DisplayName, (e, v) => e.DisplayName = v));
                Add(new Property<bool, bool>(29, "ExcludeFromSearch", e => e.ExcludeFromSearch, (e, v) => e.ExcludeFromSearch = v));
                Add(new Property<SymbolId, SymbolId>(33, "Id", e => e.Id, (e, v) => e.Id = v));
                Add(new Property<StringEnum<SymbolKinds>, StringEnum<SymbolKinds>>(43, "Kind", e => e.Kind, (e, v) => e.Kind = v));
                Add(new Property<string, string>(1, "ProjectId", e => e.ProjectId, (e, v) => e.ProjectId = v));
                Add(new Property<ReferenceKind, ReferenceKind>(55, "ReferenceKind", e => e.ReferenceKind, (e, v) => e.ReferenceKind = v));
            }
        }

        public partial class FileSpanResultDescriptor : SingletonDescriptorBase<FileSpanResult, IFileSpanResult, FileSpanResultDescriptor>, ICreate<FileSpanResultDescriptor>
        {
            static FileSpanResultDescriptor ICreate<FileSpanResultDescriptor>.Create() => new FileSpanResultDescriptor();
            FileSpanResultDescriptor() : base(24, 1)
            {
                Add(new Property<IProjectFileScopeEntity, IProjectFileScopeEntity>(24, "File", e => e.File, (e, v) => e.File = v));
            }
        }

        public partial class GlobalStoredRepositorySettingsDescriptor : SingletonDescriptorBase<GlobalStoredRepositorySettings, IGlobalStoredRepositorySettings, GlobalStoredRepositorySettingsDescriptor>, ICreate<GlobalStoredRepositorySettingsDescriptor>
        {
            static GlobalStoredRepositorySettingsDescriptor ICreate<GlobalStoredRepositorySettingsDescriptor>.Create() => new GlobalStoredRepositorySettingsDescriptor();
            GlobalStoredRepositorySettingsDescriptor() : base(74, 2)
            {
                Add(new Property<ImmutableDictionary<RepoName, IStoredRepositoryGroupSettings>, ImmutableDictionary<RepoName, IStoredRepositoryGroupSettings>>(73, "Groups", e => e.Groups, (e, v) => e.Groups = v));
                Add(new Property<ImmutableDictionary<RepoName, IStoredRepositorySettings>, ImmutableDictionary<RepoName, IStoredRepositorySettings>>(74, "Repositories", e => e.Repositories, (e, v) => e.Repositories = v));
            }
        }

        public partial class HeaderInfoDescriptor : SingletonDescriptorBase<HeaderInfo, IHeaderInfo, HeaderInfoDescriptor>, ICreate<HeaderInfoDescriptor>
        {
            static HeaderInfoDescriptor ICreate<HeaderInfoDescriptor>.Create() => new HeaderInfoDescriptor();
            HeaderInfoDescriptor() : base(75, 1)
            {
                Add(new Property<int, int>(75, "FormatVersion", e => e.FormatVersion, (e, v) => e.FormatVersion = v));
            }
        }

        public partial class LanguageInfoDescriptor : SingletonDescriptorBase<LanguageInfo, ILanguageInfo, LanguageInfoDescriptor>, ICreate<LanguageInfoDescriptor>
        {
            static LanguageInfoDescriptor ICreate<LanguageInfoDescriptor>.Create() => new LanguageInfoDescriptor();
            LanguageInfoDescriptor() : base(28, 2)
            {
                Add(new ListProperty<ClassificationStyle, IClassificationStyle>(11, "Classifications", e => e.Classifications, (e, v) => e.Classifications = v));
                Add(new Property<string, string>(28, "Name", e => e.Name, (e, v) => e.Name = v));
            }
        }

        public partial class LanguageSearchModelDescriptor : SingletonDescriptorBase<LanguageSearchModel, ILanguageSearchModel, LanguageSearchModelDescriptor>, ICreate<LanguageSearchModelDescriptor>
        {
            static LanguageSearchModelDescriptor ICreate<LanguageSearchModelDescriptor>.Create() => new LanguageSearchModelDescriptor();
            LanguageSearchModelDescriptor() : base(76, 6)
            {
                Add(new Property<MurmurHash, MurmurHash>(16, "EntityContentId", e => e.EntityContentId, (e, v) => e.EntityContentId = v));
                Add(new Property<int, int>(17, "EntityContentSize", e => e.EntityContentSize, (e, v) => e.EntityContentSize = v));
                Add(new Property<bool, bool>(18, "IsAdded", e => e.IsAdded, (e, v) => e.IsAdded = v));
                Add(new Property<LanguageInfo, ILanguageInfo>(76, "Language", e => e.Language, (e, v) => e.Language = v));
                Add(new Property<int, int>(19, "StableId", e => e.StableId, (e, v) => e.StableId = v));
                Add(new Property<MurmurHash, MurmurHash>(20, "Uid", e => e.Uid, (e, v) => e.Uid = v));
            }
        }

        public partial class LineSpanDescriptor : SingletonDescriptorBase<LineSpan, ILineSpan, LineSpanDescriptor>, ICreate<LineSpanDescriptor>
        {
            static LineSpanDescriptor ICreate<LineSpanDescriptor>.Create() => new LineSpanDescriptor();
            LineSpanDescriptor() : base(80, 6)
            {
                Add(new Property<int, int>(35, "Length", e => e.Length, (e, v) => e.Length = v));
                Add(new Property<int, int>(77, "LineIndex", e => e.LineIndex, (e, v) => e.LineIndex = v));
                Add(new Property<int, int>(78, "LineNumber", e => e.LineNumber, (e, v) => e.LineNumber = v));
                Add(new Property<int, int>(79, "LineOffset", e => e.LineOffset, (e, v) => e.LineOffset = v));
                Add(new Property<int, int>(80, "LineSpanStart", e => e.LineSpanStart, (e, v) => e.LineSpanStart = v));
                Add(new Property<int, int>(36, "Start", e => e.Start, (e, v) => e.Start = v));
            }
        }

        public partial class NewBoundSourceFileDescriptor : SingletonDescriptorBase<NewBoundSourceFile, INewBoundSourceFile, NewBoundSourceFileDescriptor>, ICreate<NewBoundSourceFileDescriptor>
        {
            static NewBoundSourceFileDescriptor ICreate<NewBoundSourceFileDescriptor>.Create() => new NewBoundSourceFileDescriptor();
            NewBoundSourceFileDescriptor() : base(83, 2)
            {
                Add(new Property<ProjectFileScopeEntity, IProjectFileScopeEntity>(83, "FileInfo", e => e.FileInfo, (e, v) => e.FileInfo = v));
                Add(new Property<SourceFileBase, ISourceFileBase>(15, "SourceFile", e => e.SourceFile, (e, v) => e.SourceFile = v));
            }
        }

        public partial class OutliningRegionDescriptor : SingletonDescriptorBase<OutliningRegion, IOutliningRegion, OutliningRegionDescriptor>, ICreate<OutliningRegionDescriptor>
        {
            static OutliningRegionDescriptor ICreate<OutliningRegionDescriptor>.Create() => new OutliningRegionDescriptor();
            OutliningRegionDescriptor() : base(84, 2)
            {
                Add(new Property<LineSpan, ILineSpan>(23, "Content", e => e.Content, (e, v) => e.Content = v));
                Add(new Property<LineSpan, ILineSpan>(84, "Header", e => e.Header, (e, v) => e.Header = v));
            }
        }

        public partial class ParameterDefinitionSpanDescriptor : SingletonDescriptorBase<ParameterDefinitionSpan, IParameterDefinitionSpan, ParameterDefinitionSpanDescriptor>, ICreate<ParameterDefinitionSpanDescriptor>
        {
            static ParameterDefinitionSpanDescriptor ICreate<ParameterDefinitionSpanDescriptor>.Create() => new ParameterDefinitionSpanDescriptor();
            ParameterDefinitionSpanDescriptor() : base(85, 8)
            {
                Add(new Property<int, int>(35, "Length", e => e.Length, (e, v) => e.Length = v));
                Add(new Property<int, int>(77, "LineIndex", e => e.LineIndex, (e, v) => e.LineIndex = v));
                Add(new Property<int, int>(78, "LineNumber", e => e.LineNumber, (e, v) => e.LineNumber = v));
                Add(new Property<int, int>(79, "LineOffset", e => e.LineOffset, (e, v) => e.LineOffset = v));
                Add(new Property<int, int>(80, "LineSpanStart", e => e.LineSpanStart, (e, v) => e.LineSpanStart = v));
                Add(new Property<string, string>(28, "Name", e => e.Name, (e, v) => e.Name = v));
                Add(new Property<int, int>(85, "ParameterIndex", e => e.ParameterIndex, (e, v) => e.ParameterIndex = v));
                Add(new Property<int, int>(36, "Start", e => e.Start, (e, v) => e.Start = v));
            }
        }

        public partial class ParameterDocumentationDescriptor : SingletonDescriptorBase<ParameterDocumentation, IParameterDocumentation, ParameterDocumentationDescriptor>, ICreate<ParameterDocumentationDescriptor>
        {
            static ParameterDocumentationDescriptor ICreate<ParameterDocumentationDescriptor>.Create() => new ParameterDocumentationDescriptor();
            ParameterDocumentationDescriptor() : base(57, 2)
            {
                Add(new Property<string, string>(57, "Comment", e => e.Comment, (e, v) => e.Comment = v));
                Add(new Property<string, string>(28, "Name", e => e.Name, (e, v) => e.Name = v));
            }
        }

        public partial class ParameterReferenceSpanDescriptor : SingletonDescriptorBase<ParameterReferenceSpan, IParameterReferenceSpan, ParameterReferenceSpanDescriptor>, ICreate<ParameterReferenceSpanDescriptor>
        {
            static ParameterReferenceSpanDescriptor ICreate<ParameterReferenceSpanDescriptor>.Create() => new ParameterReferenceSpanDescriptor();
            ParameterReferenceSpanDescriptor() : base(86, 8)
            {
                Add(new Property<int, int>(35, "Length", e => e.Length, (e, v) => e.Length = v));
                Add(new Property<int, int>(77, "LineIndex", e => e.LineIndex, (e, v) => e.LineIndex = v));
                Add(new Property<int, int>(78, "LineNumber", e => e.LineNumber, (e, v) => e.LineNumber = v));
                Add(new Property<int, int>(79, "LineOffset", e => e.LineOffset, (e, v) => e.LineOffset = v));
                Add(new Property<int, int>(80, "LineSpanStart", e => e.LineSpanStart, (e, v) => e.LineSpanStart = v));
                Add(new Property<CharString, CharString>(86, "LineSpanText", e => e.LineSpanText, (e, v) => e.LineSpanText = v));
                Add(new Property<int, int>(85, "ParameterIndex", e => e.ParameterIndex, (e, v) => e.ParameterIndex = v));
                Add(new Property<int, int>(36, "Start", e => e.Start, (e, v) => e.Start = v));
            }
        }

        public partial class ProjectFileLinkDescriptor : SingletonDescriptorBase<ProjectFileLink, IProjectFileLink, ProjectFileLinkDescriptor>, ICreate<ProjectFileLinkDescriptor>
        {
            static ProjectFileLinkDescriptor ICreate<ProjectFileLinkDescriptor>.Create() => new ProjectFileLinkDescriptor();
            ProjectFileLinkDescriptor() : base(89, 5)
            {
                Add(new Property<string, string>(89, "FileId", e => e.FileId, (e, v) => e.FileId = v));
                Add(new Property<string, string>(1, "ProjectId", e => e.ProjectId, (e, v) => e.ProjectId = v));
                Add(new Property<string, string>(88, "ProjectRelativePath", e => e.ProjectRelativePath, (e, v) => e.ProjectRelativePath = v));
                Add(new Property<string, string>(87, "RepoRelativePath", e => e.RepoRelativePath, (e, v) => e.RepoRelativePath = v));
                Add(new Property<string, string>(0, "RepositoryName", e => e.RepositoryName, (e, v) => e.RepositoryName = v));
            }
        }

        public partial class ProjectFileScopeEntityDescriptor : SingletonDescriptorBase<ProjectFileScopeEntity, IProjectFileScopeEntity, ProjectFileScopeEntityDescriptor>, ICreate<ProjectFileScopeEntityDescriptor>
        {
            static ProjectFileScopeEntityDescriptor ICreate<ProjectFileScopeEntityDescriptor>.Create() => new ProjectFileScopeEntityDescriptor();
            ProjectFileScopeEntityDescriptor() : base(88, 4)
            {
                Add(new Property<string, string>(1, "ProjectId", e => e.ProjectId, (e, v) => e.ProjectId = v));
                Add(new Property<string, string>(88, "ProjectRelativePath", e => e.ProjectRelativePath, (e, v) => e.ProjectRelativePath = v));
                Add(new Property<string, string>(87, "RepoRelativePath", e => e.RepoRelativePath, (e, v) => e.RepoRelativePath = v));
                Add(new Property<string, string>(0, "RepositoryName", e => e.RepositoryName, (e, v) => e.RepositoryName = v));
            }
        }

        public partial class ProjectReferenceSearchModelDescriptor : SingletonDescriptorBase<ProjectReferenceSearchModel, IProjectReferenceSearchModel, ProjectReferenceSearchModelDescriptor>, ICreate<ProjectReferenceSearchModelDescriptor>
        {
            static ProjectReferenceSearchModelDescriptor ICreate<ProjectReferenceSearchModelDescriptor>.Create() => new ProjectReferenceSearchModelDescriptor();
            ProjectReferenceSearchModelDescriptor() : base(90, 8)
            {
                Add(new Property<MurmurHash, MurmurHash>(16, "EntityContentId", e => e.EntityContentId, (e, v) => e.EntityContentId = v));
                Add(new Property<int, int>(17, "EntityContentSize", e => e.EntityContentSize, (e, v) => e.EntityContentSize = v));
                Add(new Property<bool, bool>(18, "IsAdded", e => e.IsAdded, (e, v) => e.IsAdded = v));
                Add(new Property<string, string>(1, "ProjectId", e => e.ProjectId, (e, v) => e.ProjectId = v));
                Add(new Property<IReferencedProject, IReferencedProject>(90, "ProjectReference", e => e.ProjectReference, (e, v) => e.ProjectReference = v));
                Add(new Property<string, string>(0, "RepositoryName", e => e.RepositoryName, (e, v) => e.RepositoryName = v));
                Add(new Property<int, int>(19, "StableId", e => e.StableId, (e, v) => e.StableId = v));
                Add(new Property<MurmurHash, MurmurHash>(20, "Uid", e => e.Uid, (e, v) => e.Uid = v));
            }
        }

        public partial class ProjectScopeEntityDescriptor : SingletonDescriptorBase<ProjectScopeEntity, IProjectScopeEntity, ProjectScopeEntityDescriptor>, ICreate<ProjectScopeEntityDescriptor>
        {
            static ProjectScopeEntityDescriptor ICreate<ProjectScopeEntityDescriptor>.Create() => new ProjectScopeEntityDescriptor();
            ProjectScopeEntityDescriptor() : base(1, 2)
            {
                Add(new Property<string, string>(1, "ProjectId", e => e.ProjectId, (e, v) => e.ProjectId = v));
                Add(new Property<string, string>(0, "RepositoryName", e => e.RepositoryName, (e, v) => e.RepositoryName = v));
            }
        }

        public partial class ProjectSearchModelDescriptor : SingletonDescriptorBase<ProjectSearchModel, IProjectSearchModel, ProjectSearchModelDescriptor>, ICreate<ProjectSearchModelDescriptor>
        {
            static ProjectSearchModelDescriptor ICreate<ProjectSearchModelDescriptor>.Create() => new ProjectSearchModelDescriptor();
            ProjectSearchModelDescriptor() : base(91, 6)
            {
                Add(new Property<MurmurHash, MurmurHash>(16, "EntityContentId", e => e.EntityContentId, (e, v) => e.EntityContentId = v));
                Add(new Property<int, int>(17, "EntityContentSize", e => e.EntityContentSize, (e, v) => e.EntityContentSize = v));
                Add(new Property<bool, bool>(18, "IsAdded", e => e.IsAdded, (e, v) => e.IsAdded = v));
                Add(new Property<IAnalyzedProjectInfo, IAnalyzedProjectInfo>(91, "Project", e => e.Project, (e, v) => e.Project = v));
                Add(new Property<int, int>(19, "StableId", e => e.StableId, (e, v) => e.StableId = v));
                Add(new Property<MurmurHash, MurmurHash>(20, "Uid", e => e.Uid, (e, v) => e.Uid = v));
            }
        }

        public partial class PropertySearchModelDescriptor : SingletonDescriptorBase<PropertySearchModel, IPropertySearchModel, PropertySearchModelDescriptor>, ICreate<PropertySearchModelDescriptor>
        {
            static PropertySearchModelDescriptor ICreate<PropertySearchModelDescriptor>.Create() => new PropertySearchModelDescriptor();
            PropertySearchModelDescriptor() : base(93, 8)
            {
                Add(new Property<MurmurHash, MurmurHash>(16, "EntityContentId", e => e.EntityContentId, (e, v) => e.EntityContentId = v));
                Add(new Property<int, int>(17, "EntityContentSize", e => e.EntityContentSize, (e, v) => e.EntityContentSize = v));
                Add(new Property<bool, bool>(18, "IsAdded", e => e.IsAdded, (e, v) => e.IsAdded = v));
                Add(new Property<StringEnum<PropertyKey>, StringEnum<PropertyKey>>(92, "Key", e => e.Key, (e, v) => e.Key = v));
                Add(new Property<int, int>(93, "OwnerId", e => e.OwnerId, (e, v) => e.OwnerId = v));
                Add(new Property<int, int>(19, "StableId", e => e.StableId, (e, v) => e.StableId = v));
                Add(new Property<MurmurHash, MurmurHash>(20, "Uid", e => e.Uid, (e, v) => e.Uid = v));
                Add(new Property<string, string>(72, "Value", e => e.Value, (e, v) => e.Value = v));
            }
        }

        public partial class QualifierScopeEntityDescriptor : SingletonDescriptorBase<QualifierScopeEntity, IQualifierScopeEntity, QualifierScopeEntityDescriptor>, ICreate<QualifierScopeEntityDescriptor>
        {
            static QualifierScopeEntityDescriptor ICreate<QualifierScopeEntityDescriptor>.Create() => new QualifierScopeEntityDescriptor();
            QualifierScopeEntityDescriptor() : base(10, 1)
            {
                Add(new Property<string, string>(10, "Qualifier", e => e.Qualifier, (e, v) => e.Qualifier = v));
            }
        }

        public partial class ReferencedProjectDescriptor : SingletonDescriptorBase<ReferencedProject, IReferencedProject, ReferencedProjectDescriptor>, ICreate<ReferencedProjectDescriptor>
        {
            static ReferencedProjectDescriptor ICreate<ReferencedProjectDescriptor>.Create() => new ReferencedProjectDescriptor();
            ReferencedProjectDescriptor() : base(5, 6)
            {
                Add(new Property<int, int>(2, "DefinitionCount", e => e.DefinitionCount, (e, v) => e.DefinitionCount = v));
                Add(new ListProperty<DefinitionSymbol, IDefinitionSymbol>(3, "Definitions", e => e.Definitions, (e, v) => e.Definitions = v));
                Add(new Property<string, string>(4, "DisplayName", e => e.DisplayName, (e, v) => e.DisplayName = v));
                Add(new Property<string, string>(1, "ProjectId", e => e.ProjectId, (e, v) => e.ProjectId = v));
                Add(new Property<PropertyMap, IPropertyMap>(5, "Properties", e => e.Properties, (e, v) => e.Properties = v));
                Add(new Property<string, string>(0, "RepositoryName", e => e.RepositoryName, (e, v) => e.RepositoryName = v));
            }
        }

        public partial class ReferenceSearchModelDescriptor : SingletonDescriptorBase<ReferenceSearchModel, IReferenceSearchModel, ReferenceSearchModelDescriptor>, ICreate<ReferenceSearchModelDescriptor>
        {
            static ReferenceSearchModelDescriptor ICreate<ReferenceSearchModelDescriptor>.Create() => new ReferenceSearchModelDescriptor();
            ReferenceSearchModelDescriptor() : base(97, 11)
            {
                Add(new Property<MurmurHash, MurmurHash>(16, "EntityContentId", e => e.EntityContentId, (e, v) => e.EntityContentId = v));
                Add(new Property<int, int>(17, "EntityContentSize", e => e.EntityContentSize, (e, v) => e.EntityContentSize = v));
                Add(new Property<IProjectFileScopeEntity, IProjectFileScopeEntity>(83, "FileInfo", e => e.FileInfo, (e, v) => e.FileInfo = v));
                Add(new Property<bool, bool>(18, "IsAdded", e => e.IsAdded, (e, v) => e.IsAdded = v));
                Add(new Property<ReferenceKindSet, ReferenceKindSet>(55, "ReferenceKind", e => e.ReferenceKind, (e, v) => e.ReferenceKind = v));
                Add(new Property<SymbolReferenceList, ISymbolReferenceList>(13, "References", e => e.References, (e, v) => e.References = v));
                Add(new Property<IEnumerable<SymbolId>, IEnumerable<SymbolId>>(95, "RelatedDefinition", e => e.RelatedDefinition, (e, v) => e.RelatedDefinition = v));
                Add(new ListProperty<IReferenceSpan, IReferenceSpan>(96, "Spans", e => e.Spans, (e, v) => e.Spans = v));
                Add(new Property<int, int>(19, "StableId", e => e.StableId, (e, v) => e.StableId = v));
                Add(new Property<ICodeSymbol, ICodeSymbol>(97, "Symbol", e => e.Symbol, (e, v) => e.Symbol = v));
                Add(new Property<MurmurHash, MurmurHash>(20, "Uid", e => e.Uid, (e, v) => e.Uid = v));
            }
        }

        public partial class ReferenceSearchResultDescriptor : SingletonDescriptorBase<ReferenceSearchResult, IReferenceSearchResult, ReferenceSearchResultDescriptor>, ICreate<ReferenceSearchResultDescriptor>
        {
            static ReferenceSearchResultDescriptor ICreate<ReferenceSearchResultDescriptor>.Create() => new ReferenceSearchResultDescriptor();
            ReferenceSearchResultDescriptor() : base(98, 2)
            {
                Add(new Property<IProjectFileScopeEntity, IProjectFileScopeEntity>(24, "File", e => e.File, (e, v) => e.File = v));
                Add(new Property<IReferenceSpan, IReferenceSpan>(98, "ReferenceSpan", e => e.ReferenceSpan, (e, v) => e.ReferenceSpan = v));
            }
        }

        public partial class ReferenceSpanDescriptor : SingletonDescriptorBase<ReferenceSpan, IReferenceSpan, ReferenceSpanDescriptor>, ICreate<ReferenceSpanDescriptor>
        {
            static ReferenceSpanDescriptor ICreate<ReferenceSpanDescriptor>.Create() => new ReferenceSpanDescriptor();
            ReferenceSpanDescriptor() : base(101, 12)
            {
                Add(new Property<IDisplayCodeSymbol, IDisplayCodeSymbol>(99, "ContainerSymbol", e => e.ContainerSymbol, (e, v) => e.ContainerSymbol = v));
                Add(new Property<bool, bool>(100, "IsImplicitlyDeclared", e => e.IsImplicitlyDeclared, (e, v) => e.IsImplicitlyDeclared = v));
                Add(new Property<int, int>(35, "Length", e => e.Length, (e, v) => e.Length = v));
                Add(new Property<int, int>(77, "LineIndex", e => e.LineIndex, (e, v) => e.LineIndex = v));
                Add(new Property<int, int>(78, "LineNumber", e => e.LineNumber, (e, v) => e.LineNumber = v));
                Add(new Property<int, int>(79, "LineOffset", e => e.LineOffset, (e, v) => e.LineOffset = v));
                Add(new Property<int, int>(80, "LineSpanStart", e => e.LineSpanStart, (e, v) => e.LineSpanStart = v));
                Add(new Property<CharString, CharString>(86, "LineSpanText", e => e.LineSpanText, (e, v) => e.LineSpanText = v));
                Add(new ListProperty<ParameterReferenceSpan, IParameterReferenceSpan>(54, "Parameters", e => e.Parameters, (e, v) => e.Parameters = v));
                Add(new Property<ReferenceSymbol, IReferenceSymbol>(101, "Reference", e => e.Reference, (e, v) => e.Reference = v));
                Add(new Property<SymbolId, SymbolId>(95, "RelatedDefinition", e => e.RelatedDefinition, (e, v) => e.RelatedDefinition = v));
                Add(new Property<int, int>(36, "Start", e => e.Start, (e, v) => e.Start = v));
            }
        }

        public partial class ReferenceSymbolDescriptor : SingletonDescriptorBase<ReferenceSymbol, IReferenceSymbol, ReferenceSymbolDescriptor>, ICreate<ReferenceSymbolDescriptor>
        {
            static ReferenceSymbolDescriptor ICreate<ReferenceSymbolDescriptor>.Create() => new ReferenceSymbolDescriptor();
            ReferenceSymbolDescriptor() : base(55, 5)
            {
                Add(new Property<bool, bool>(29, "ExcludeFromSearch", e => e.ExcludeFromSearch, (e, v) => e.ExcludeFromSearch = v));
                Add(new Property<SymbolId, SymbolId>(33, "Id", e => e.Id, (e, v) => e.Id = v));
                Add(new Property<StringEnum<SymbolKinds>, StringEnum<SymbolKinds>>(43, "Kind", e => e.Kind, (e, v) => e.Kind = v));
                Add(new Property<string, string>(1, "ProjectId", e => e.ProjectId, (e, v) => e.ProjectId = v));
                Add(new Property<ReferenceKind, ReferenceKind>(55, "ReferenceKind", e => e.ReferenceKind, (e, v) => e.ReferenceKind = v));
            }
        }

        public partial class RepoFileScopeEntityDescriptor : SingletonDescriptorBase<RepoFileScopeEntity, IRepoFileScopeEntity, RepoFileScopeEntityDescriptor>, ICreate<RepoFileScopeEntityDescriptor>
        {
            static RepoFileScopeEntityDescriptor ICreate<RepoFileScopeEntityDescriptor>.Create() => new RepoFileScopeEntityDescriptor();
            RepoFileScopeEntityDescriptor() : base(87, 2)
            {
                Add(new Property<string, string>(87, "RepoRelativePath", e => e.RepoRelativePath, (e, v) => e.RepoRelativePath = v));
                Add(new Property<string, string>(0, "RepositoryName", e => e.RepositoryName, (e, v) => e.RepositoryName = v));
            }
        }

        public partial class RepoScopeEntityDescriptor : SingletonDescriptorBase<RepoScopeEntity, IRepoScopeEntity, RepoScopeEntityDescriptor>, ICreate<RepoScopeEntityDescriptor>
        {
            static RepoScopeEntityDescriptor ICreate<RepoScopeEntityDescriptor>.Create() => new RepoScopeEntityDescriptor();
            RepoScopeEntityDescriptor() : base(0, 1)
            {
                Add(new Property<string, string>(0, "RepositoryName", e => e.RepositoryName, (e, v) => e.RepositoryName = v));
            }
        }

        public partial class RepositoryDescriptor : SingletonDescriptorBase<Repository, IRepository, RepositoryDescriptor>, ICreate<RepositoryDescriptor>
        {
            static RepositoryDescriptor ICreate<RepositoryDescriptor>.Create() => new RepositoryDescriptor();
            RepositoryDescriptor() : base(104, 5)
            {
                Add(new Property<string, string>(26, "Description", e => e.Description, (e, v) => e.Description = v));
                Add(new Property<string, string>(28, "Name", e => e.Name, (e, v) => e.Name = v));
                Add(new Property<string, string>(102, "PrimaryBranch", e => e.PrimaryBranch, (e, v) => e.PrimaryBranch = v));
                Add(new ListProperty<RepositoryReference, IRepositoryReference>(103, "RepositoryReferences", e => e.RepositoryReferences, (e, v) => e.RepositoryReferences = v));
                Add(new Property<string, string>(104, "SourceControlWebAddress", e => e.SourceControlWebAddress, (e, v) => e.SourceControlWebAddress = v));
            }
        }

        public partial class RepositoryReferenceDescriptor : SingletonDescriptorBase<RepositoryReference, IRepositoryReference, RepositoryReferenceDescriptor>, ICreate<RepositoryReferenceDescriptor>
        {
            static RepositoryReferenceDescriptor ICreate<RepositoryReferenceDescriptor>.Create() => new RepositoryReferenceDescriptor();
            RepositoryReferenceDescriptor() : base(33, 2)
            {
                Add(new Property<string, string>(33, "Id", e => e.Id, (e, v) => e.Id = v));
                Add(new Property<string, string>(28, "Name", e => e.Name, (e, v) => e.Name = v));
            }
        }

        public partial class RepositorySearchModelDescriptor : SingletonDescriptorBase<RepositorySearchModel, IRepositorySearchModel, RepositorySearchModelDescriptor>, ICreate<RepositorySearchModelDescriptor>
        {
            static RepositorySearchModelDescriptor ICreate<RepositorySearchModelDescriptor>.Create() => new RepositorySearchModelDescriptor();
            RepositorySearchModelDescriptor() : base(70, 6)
            {
                Add(new Property<MurmurHash, MurmurHash>(16, "EntityContentId", e => e.EntityContentId, (e, v) => e.EntityContentId = v));
                Add(new Property<int, int>(17, "EntityContentSize", e => e.EntityContentSize, (e, v) => e.EntityContentSize = v));
                Add(new Property<bool, bool>(18, "IsAdded", e => e.IsAdded, (e, v) => e.IsAdded = v));
                Add(new Property<Repository, IRepository>(70, "Repository", e => e.Repository, (e, v) => e.Repository = v));
                Add(new Property<int, int>(19, "StableId", e => e.StableId, (e, v) => e.StableId = v));
                Add(new Property<MurmurHash, MurmurHash>(20, "Uid", e => e.Uid, (e, v) => e.Uid = v));
            }
        }

        public partial class RepositoryStoreInfoDescriptor : SingletonDescriptorBase<RepositoryStoreInfo, IRepositoryStoreInfo, RepositoryStoreInfoDescriptor>, ICreate<RepositoryStoreInfoDescriptor>
        {
            static RepositoryStoreInfoDescriptor ICreate<RepositoryStoreInfoDescriptor>.Create() => new RepositoryStoreInfoDescriptor();
            RepositoryStoreInfoDescriptor() : base(70, 3)
            {
                Add(new Property<Branch, IBranch>(69, "Branch", e => e.Branch, (e, v) => e.Branch = v));
                Add(new Property<Commit, ICommit>(14, "Commit", e => e.Commit, (e, v) => e.Commit = v));
                Add(new Property<Repository, IRepository>(70, "Repository", e => e.Repository, (e, v) => e.Repository = v));
            }
        }

        public partial class SearchEntityDescriptor : SingletonDescriptorBase<SearchEntity, ISearchEntity, SearchEntityDescriptor>, ICreate<SearchEntityDescriptor>
        {
            static SearchEntityDescriptor ICreate<SearchEntityDescriptor>.Create() => new SearchEntityDescriptor();
            SearchEntityDescriptor() : base(20, 5)
            {
                Add(new Property<MurmurHash, MurmurHash>(16, "EntityContentId", e => e.EntityContentId, (e, v) => e.EntityContentId = v));
                Add(new Property<int, int>(17, "EntityContentSize", e => e.EntityContentSize, (e, v) => e.EntityContentSize = v));
                Add(new Property<bool, bool>(18, "IsAdded", e => e.IsAdded, (e, v) => e.IsAdded = v));
                Add(new Property<int, int>(19, "StableId", e => e.StableId, (e, v) => e.StableId = v));
                Add(new Property<MurmurHash, MurmurHash>(20, "Uid", e => e.Uid, (e, v) => e.Uid = v));
            }
        }

        public partial class SearchResultDescriptor : SingletonDescriptorBase<SearchResult, ISearchResult, SearchResultDescriptor>, ICreate<SearchResultDescriptor>
        {
            static SearchResultDescriptor ICreate<SearchResultDescriptor>.Create() => new SearchResultDescriptor();
            SearchResultDescriptor() : base(105, 2)
            {
                Add(new Property<DefinitionSymbol, IDefinitionSymbol>(50, "Definition", e => e.Definition, (e, v) => e.Definition = v));
                Add(new Property<TextLineSpanResult, ITextLineSpanResult>(105, "TextLine", e => e.TextLine, (e, v) => e.TextLine = v));
            }
        }

        public partial class SharedReferenceInfoDescriptor : SingletonDescriptorBase<SharedReferenceInfo, ISharedReferenceInfo, SharedReferenceInfoDescriptor>, ICreate<SharedReferenceInfoDescriptor>
        {
            static SharedReferenceInfoDescriptor ICreate<SharedReferenceInfoDescriptor>.Create() => new SharedReferenceInfoDescriptor();
            SharedReferenceInfoDescriptor() : base(95, 3)
            {
                Add(new Property<bool, bool>(29, "ExcludeFromSearch", e => e.ExcludeFromSearch, (e, v) => e.ExcludeFromSearch = v));
                Add(new Property<ReferenceKind, ReferenceKind>(55, "ReferenceKind", e => e.ReferenceKind, (e, v) => e.ReferenceKind = v));
                Add(new Property<SymbolId, SymbolId>(95, "RelatedDefinition", e => e.RelatedDefinition, (e, v) => e.RelatedDefinition = v));
            }
        }

        public partial class SharedReferenceInfoSpanDescriptor : SingletonDescriptorBase<SharedReferenceInfoSpan, ISharedReferenceInfoSpan, SharedReferenceInfoSpanDescriptor>, ICreate<SharedReferenceInfoSpanDescriptor>
        {
            static SharedReferenceInfoSpanDescriptor ICreate<SharedReferenceInfoSpanDescriptor>.Create() => new SharedReferenceInfoSpanDescriptor();
            SharedReferenceInfoSpanDescriptor() : base(86, 8)
            {
                Add(new Property<SharedReferenceInfo, ISharedReferenceInfo>(31, "Info", e => e.Info, (e, v) => e.Info = v));
                Add(new Property<int, int>(35, "Length", e => e.Length, (e, v) => e.Length = v));
                Add(new Property<int, int>(77, "LineIndex", e => e.LineIndex, (e, v) => e.LineIndex = v));
                Add(new Property<int, int>(78, "LineNumber", e => e.LineNumber, (e, v) => e.LineNumber = v));
                Add(new Property<int, int>(79, "LineOffset", e => e.LineOffset, (e, v) => e.LineOffset = v));
                Add(new Property<int, int>(80, "LineSpanStart", e => e.LineSpanStart, (e, v) => e.LineSpanStart = v));
                Add(new Property<CharString, CharString>(86, "LineSpanText", e => e.LineSpanText, (e, v) => e.LineSpanText = v));
                Add(new Property<int, int>(36, "Start", e => e.Start, (e, v) => e.Start = v));
            }
        }

        public partial class SourceControlFileInfoDescriptor : SingletonDescriptorBase<SourceControlFileInfo, ISourceControlFileInfo, SourceControlFileInfoDescriptor>, ICreate<SourceControlFileInfoDescriptor>
        {
            static SourceControlFileInfoDescriptor ICreate<SourceControlFileInfoDescriptor>.Create() => new SourceControlFileInfoDescriptor();
            SourceControlFileInfoDescriptor() : base(109, 4)
            {
                Add(new Property<SourceEncodingInfo, SourceEncodingInfo>(106, "EncodingInfo", e => e.EncodingInfo, (e, v) => e.EncodingInfo = v));
                Add(new Property<int, int>(107, "Lines", e => e.Lines, (e, v) => e.Lines = v));
                Add(new Property<int, int>(108, "Size", e => e.Size, (e, v) => e.Size = v));
                Add(new Property<string, string>(109, "SourceControlContentId", e => e.SourceControlContentId, (e, v) => e.SourceControlContentId = v));
            }
        }

        public partial class SourceFileDescriptor : SingletonDescriptorBase<SourceFile, ISourceFile, SourceFileDescriptor>, ICreate<SourceFileDescriptor>
        {
            static SourceFileDescriptor ICreate<SourceFileDescriptor>.Create() => new SourceFileDescriptor();
            SourceFileDescriptor() : base(110, 5)
            {
                Add(new Property<string, string>(23, "Content", e => e.Content, (e, v) => e.Content = v));
                Add(new Property<TextSourceBase, TextSourceBase>(110, "ContentSource", e => e.ContentSource, (e, v) => e.ContentSource = v));
                Add(new Property<bool, bool>(29, "ExcludeFromSearch", e => e.ExcludeFromSearch, (e, v) => e.ExcludeFromSearch = v));
                Add(new Property<BoundSourceFlags, BoundSourceFlags>(30, "Flags", e => e.Flags, (e, v) => e.Flags = v));
                Add(new Property<SourceFileInfo, ISourceFileInfo>(31, "Info", e => e.Info, (e, v) => e.Info = v));
            }
        }

        public partial class SourceFileBaseDescriptor : SingletonDescriptorBase<SourceFileBase, ISourceFileBase, SourceFileBaseDescriptor>, ICreate<SourceFileBaseDescriptor>
        {
            static SourceFileBaseDescriptor ICreate<SourceFileBaseDescriptor>.Create() => new SourceFileBaseDescriptor();
            SourceFileBaseDescriptor() : base(31, 3)
            {
                Add(new Property<bool, bool>(29, "ExcludeFromSearch", e => e.ExcludeFromSearch, (e, v) => e.ExcludeFromSearch = v));
                Add(new Property<BoundSourceFlags, BoundSourceFlags>(30, "Flags", e => e.Flags, (e, v) => e.Flags = v));
                Add(new Property<SourceFileInfo, ISourceFileInfo>(31, "Info", e => e.Info, (e, v) => e.Info = v));
            }
        }

        public partial class SourceFileInfoDescriptor : SingletonDescriptorBase<SourceFileInfo, ISourceFileInfo, SourceFileInfoDescriptor>, ICreate<SourceFileInfoDescriptor>
        {
            static SourceFileInfoDescriptor ICreate<SourceFileInfoDescriptor>.Create() => new SourceFileInfoDescriptor();
            SourceFileInfoDescriptor() : base(112, 14)
            {
                Add(new Property<string, string>(44, "CommitId", e => e.CommitId, (e, v) => e.CommitId = v));
                Add(new Property<string, string>(111, "DownloadAddress", e => e.DownloadAddress, (e, v) => e.DownloadAddress = v));
                Add(new Property<SourceEncodingInfo, SourceEncodingInfo>(106, "EncodingInfo", e => e.EncodingInfo, (e, v) => e.EncodingInfo = v));
                Add(new Property<string, string>(76, "Language", e => e.Language, (e, v) => e.Language = v));
                Add(new Property<int, int>(107, "Lines", e => e.Lines, (e, v) => e.Lines = v));
                Add(new Property<string, string>(1, "ProjectId", e => e.ProjectId, (e, v) => e.ProjectId = v));
                Add(new Property<string, string>(88, "ProjectRelativePath", e => e.ProjectRelativePath, (e, v) => e.ProjectRelativePath = v));
                Add(new Property<PropertyMap, IPropertyMap>(5, "Properties", e => e.Properties, (e, v) => e.Properties = v));
                Add(new Property<string, string>(10, "Qualifier", e => e.Qualifier, (e, v) => e.Qualifier = v));
                Add(new Property<string, string>(87, "RepoRelativePath", e => e.RepoRelativePath, (e, v) => e.RepoRelativePath = v));
                Add(new Property<string, string>(0, "RepositoryName", e => e.RepositoryName, (e, v) => e.RepositoryName = v));
                Add(new Property<int, int>(108, "Size", e => e.Size, (e, v) => e.Size = v));
                Add(new Property<string, string>(109, "SourceControlContentId", e => e.SourceControlContentId, (e, v) => e.SourceControlContentId = v));
                Add(new Property<string, string>(112, "WebAddress", e => e.WebAddress, (e, v) => e.WebAddress = v));
            }
        }

        public partial class SourceSearchModelBaseDescriptor : SingletonDescriptorBase<SourceSearchModelBase, ISourceSearchModelBase, SourceSearchModelBaseDescriptor>, ICreate<SourceSearchModelBaseDescriptor>
        {
            static SourceSearchModelBaseDescriptor ICreate<SourceSearchModelBaseDescriptor>.Create() => new SourceSearchModelBaseDescriptor();
            SourceSearchModelBaseDescriptor() : base(20, 5)
            {
                Add(new Property<MurmurHash, MurmurHash>(16, "EntityContentId", e => e.EntityContentId, (e, v) => e.EntityContentId = v));
                Add(new Property<int, int>(17, "EntityContentSize", e => e.EntityContentSize, (e, v) => e.EntityContentSize = v));
                Add(new Property<bool, bool>(18, "IsAdded", e => e.IsAdded, (e, v) => e.IsAdded = v));
                Add(new Property<int, int>(19, "StableId", e => e.StableId, (e, v) => e.StableId = v));
                Add(new Property<MurmurHash, MurmurHash>(20, "Uid", e => e.Uid, (e, v) => e.Uid = v));
            }
        }

        public partial class SpanDescriptor : SingletonDescriptorBase<Span, ISpan, SpanDescriptor>, ICreate<SpanDescriptor>
        {
            static SpanDescriptor ICreate<SpanDescriptor>.Create() => new SpanDescriptor();
            SpanDescriptor() : base(36, 2)
            {
                Add(new Property<int, int>(35, "Length", e => e.Length, (e, v) => e.Length = v));
                Add(new Property<int, int>(36, "Start", e => e.Start, (e, v) => e.Start = v));
            }
        }

        public partial class StoredBoundSourceFileDescriptor : SingletonDescriptorBase<StoredBoundSourceFile, IStoredBoundSourceFile, StoredBoundSourceFileDescriptor>, ICreate<StoredBoundSourceFileDescriptor>
        {
            static StoredBoundSourceFileDescriptor ICreate<StoredBoundSourceFileDescriptor>.Create() => new StoredBoundSourceFileDescriptor();
            StoredBoundSourceFileDescriptor() : base(116, 6)
            {
                Add(new Property<BoundSourceFile, IBoundSourceFile>(113, "BoundSourceFile", e => e.BoundSourceFile, (e, v) => e.BoundSourceFile = v));
                Add(new Property<ClassificationListModel, IClassificationListModel>(22, "CompressedClassifications", e => e.CompressedClassifications, (e, v) => e.CompressedClassifications = v));
                Add(new Property<ReferenceListModel, IReferenceListModel>(114, "CompressedReferences", e => e.CompressedReferences, (e, v) => e.CompressedReferences = v));
                Add(new ListProperty<CodeSymbol, ICodeSymbol>(13, "References", e => e.References, (e, v) => e.References = v));
                Add(new ListProperty<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>(115, "SemanticData", e => e.SemanticData, (e, v) => e.SemanticData = v));
                Add(new ListProperty<string, string>(116, "SourceFileContentLines", e => e.SourceFileContentLines, (e, v) => e.SourceFileContentLines = v));
            }
        }

        public partial class StoredFilterDescriptor : SingletonDescriptorBase<StoredFilter, IStoredFilter, StoredFilterDescriptor>, ICreate<StoredFilterDescriptor>
        {
            static StoredFilterDescriptor ICreate<StoredFilterDescriptor>.Create() => new StoredFilterDescriptor();
            StoredFilterDescriptor() : base(120, 9)
            {
                Add(new Property<int, int>(117, "Cardinality", e => e.Cardinality, (e, v) => e.Cardinality = v));
                Add(new Property<CommitInfo, ICommitInfo>(118, "CommitInfo", e => e.CommitInfo, (e, v) => e.CommitInfo = v));
                Add(new Property<MurmurHash, MurmurHash>(16, "EntityContentId", e => e.EntityContentId, (e, v) => e.EntityContentId = v));
                Add(new Property<int, int>(17, "EntityContentSize", e => e.EntityContentSize, (e, v) => e.EntityContentSize = v));
                Add(new Property<string, string>(119, "FilterHash", e => e.FilterHash, (e, v) => e.FilterHash = v));
                Add(new Property<bool, bool>(18, "IsAdded", e => e.IsAdded, (e, v) => e.IsAdded = v));
                Add(new Property<int, int>(19, "StableId", e => e.StableId, (e, v) => e.StableId = v));
                Add(new Property<byte[], byte[]>(120, "StableIds", e => e.StableIds, (e, v) => e.StableIds = v));
                Add(new Property<MurmurHash, MurmurHash>(20, "Uid", e => e.Uid, (e, v) => e.Uid = v));
            }
        }

        public partial class StoredRepositoryGroupInfoDescriptor : SingletonDescriptorBase<StoredRepositoryGroupInfo, IStoredRepositoryGroupInfo, StoredRepositoryGroupInfoDescriptor>, ICreate<StoredRepositoryGroupInfoDescriptor>
        {
            static StoredRepositoryGroupInfoDescriptor ICreate<StoredRepositoryGroupInfoDescriptor>.Create() => new StoredRepositoryGroupInfoDescriptor();
            StoredRepositoryGroupInfoDescriptor() : base(121, 1)
            {
                Add(new Property<ImmutableSortedSet<string>, ImmutableSortedSet<string>>(121, "ActiveRepos", e => e.ActiveRepos, (e, v) => e.ActiveRepos = v));
            }
        }

        public partial class StoredRepositoryGroupSettingsDescriptor : SingletonDescriptorBase<StoredRepositoryGroupSettings, IStoredRepositoryGroupSettings, StoredRepositoryGroupSettingsDescriptor>, ICreate<StoredRepositoryGroupSettingsDescriptor>
        {
            static StoredRepositoryGroupSettingsDescriptor ICreate<StoredRepositoryGroupSettingsDescriptor>.Create() => new StoredRepositoryGroupSettingsDescriptor();
            StoredRepositoryGroupSettingsDescriptor() : base(123, 2)
            {
                Add(new Property<string, string>(122, "Base", e => e.Base, (e, v) => e.Base = v));
                Add(new Property<ImmutableHashSet<RepoName>, ImmutableHashSet<RepoName>>(123, "Excludes", e => e.Excludes, (e, v) => e.Excludes = v));
            }
        }

        public partial class StoredRepositoryInfoDescriptor : SingletonDescriptorBase<StoredRepositoryInfo, IStoredRepositoryInfo, StoredRepositoryInfoDescriptor>, ICreate<StoredRepositoryInfoDescriptor>
        {
            static StoredRepositoryInfoDescriptor ICreate<StoredRepositoryInfoDescriptor>.Create() => new StoredRepositoryInfoDescriptor();
            StoredRepositoryInfoDescriptor() : base(73, 1)
            {
                Add(new Property<ImmutableSortedSet<string>, ImmutableSortedSet<string>>(73, "Groups", e => e.Groups, (e, v) => e.Groups = v));
            }
        }

        public partial class StoredRepositorySettingsDescriptor : SingletonDescriptorBase<StoredRepositorySettings, IStoredRepositorySettings, StoredRepositorySettingsDescriptor>, ICreate<StoredRepositorySettingsDescriptor>
        {
            static StoredRepositorySettingsDescriptor ICreate<StoredRepositorySettingsDescriptor>.Create() => new StoredRepositorySettingsDescriptor();
            StoredRepositorySettingsDescriptor() : base(125, 3)
            {
                Add(new Property<RepoAccess, RepoAccess>(124, "Access", e => e.Access, (e, v) => e.Access = v));
                Add(new Property<bool, bool>(125, "ExplicitGroupsOnly", e => e.ExplicitGroupsOnly, (e, v) => e.ExplicitGroupsOnly = v));
                Add(new Property<ImmutableSortedSet<string>, ImmutableSortedSet<string>>(73, "Groups", e => e.Groups, (e, v) => e.Groups = v));
            }
        }

        public partial class SymbolReferenceListDescriptor : SingletonDescriptorBase<SymbolReferenceList, ISymbolReferenceList, SymbolReferenceListDescriptor>, ICreate<SymbolReferenceListDescriptor>
        {
            static SymbolReferenceListDescriptor ICreate<SymbolReferenceListDescriptor>.Create() => new SymbolReferenceListDescriptor();
            SymbolReferenceListDescriptor() : base(126, 3)
            {
                Add(new Property<SharedReferenceInfoSpanModel, ISharedReferenceInfoSpanModel>(126, "CompressedSpans", e => e.CompressedSpans, (e, v) => e.CompressedSpans = v));
                Add(new ListProperty<SharedReferenceInfoSpan, ISharedReferenceInfoSpan>(96, "Spans", e => e.Spans, (e, v) => e.Spans = v));
                Add(new Property<ICodeSymbol, ICodeSymbol>(97, "Symbol", e => e.Symbol, (e, v) => e.Symbol = v));
            }
        }

        public partial class SymbolSpanDescriptor : SingletonDescriptorBase<SymbolSpan, ISymbolSpan, SymbolSpanDescriptor>, ICreate<SymbolSpanDescriptor>
        {
            static SymbolSpanDescriptor ICreate<SymbolSpanDescriptor>.Create() => new SymbolSpanDescriptor();
            SymbolSpanDescriptor() : base(86, 7)
            {
                Add(new Property<int, int>(35, "Length", e => e.Length, (e, v) => e.Length = v));
                Add(new Property<int, int>(77, "LineIndex", e => e.LineIndex, (e, v) => e.LineIndex = v));
                Add(new Property<int, int>(78, "LineNumber", e => e.LineNumber, (e, v) => e.LineNumber = v));
                Add(new Property<int, int>(79, "LineOffset", e => e.LineOffset, (e, v) => e.LineOffset = v));
                Add(new Property<int, int>(80, "LineSpanStart", e => e.LineSpanStart, (e, v) => e.LineSpanStart = v));
                Add(new Property<CharString, CharString>(86, "LineSpanText", e => e.LineSpanText, (e, v) => e.LineSpanText = v));
                Add(new Property<int, int>(36, "Start", e => e.Start, (e, v) => e.Start = v));
            }
        }

        public partial class TextChunkSearchModelDescriptor : SingletonDescriptorBase<TextChunkSearchModel, ITextChunkSearchModel, TextChunkSearchModelDescriptor>, ICreate<TextChunkSearchModelDescriptor>
        {
            static TextChunkSearchModelDescriptor ICreate<TextChunkSearchModelDescriptor>.Create() => new TextChunkSearchModelDescriptor();
            TextChunkSearchModelDescriptor() : base(23, 6)
            {
                Add(new Property<TextSourceBase, TextSourceBase>(23, "Content", e => e.Content, (e, v) => e.Content = v));
                Add(new Property<MurmurHash, MurmurHash>(16, "EntityContentId", e => e.EntityContentId, (e, v) => e.EntityContentId = v));
                Add(new Property<int, int>(17, "EntityContentSize", e => e.EntityContentSize, (e, v) => e.EntityContentSize = v));
                Add(new Property<bool, bool>(18, "IsAdded", e => e.IsAdded, (e, v) => e.IsAdded = v));
                Add(new Property<int, int>(19, "StableId", e => e.StableId, (e, v) => e.StableId = v));
                Add(new Property<MurmurHash, MurmurHash>(20, "Uid", e => e.Uid, (e, v) => e.Uid = v));
            }
        }

        public partial class TextLineSpanDescriptor : SingletonDescriptorBase<TextLineSpan, ITextLineSpan, TextLineSpanDescriptor>, ICreate<TextLineSpanDescriptor>
        {
            static TextLineSpanDescriptor ICreate<TextLineSpanDescriptor>.Create() => new TextLineSpanDescriptor();
            TextLineSpanDescriptor() : base(86, 7)
            {
                Add(new Property<int, int>(35, "Length", e => e.Length, (e, v) => e.Length = v));
                Add(new Property<int, int>(77, "LineIndex", e => e.LineIndex, (e, v) => e.LineIndex = v));
                Add(new Property<int, int>(78, "LineNumber", e => e.LineNumber, (e, v) => e.LineNumber = v));
                Add(new Property<int, int>(79, "LineOffset", e => e.LineOffset, (e, v) => e.LineOffset = v));
                Add(new Property<int, int>(80, "LineSpanStart", e => e.LineSpanStart, (e, v) => e.LineSpanStart = v));
                Add(new Property<CharString, CharString>(86, "LineSpanText", e => e.LineSpanText, (e, v) => e.LineSpanText = v));
                Add(new Property<int, int>(36, "Start", e => e.Start, (e, v) => e.Start = v));
            }
        }

        public partial class TextLineSpanResultDescriptor : SingletonDescriptorBase<TextLineSpanResult, ITextLineSpanResult, TextLineSpanResultDescriptor>, ICreate<TextLineSpanResultDescriptor>
        {
            static TextLineSpanResultDescriptor ICreate<TextLineSpanResultDescriptor>.Create() => new TextLineSpanResultDescriptor();
            TextLineSpanResultDescriptor() : base(127, 2)
            {
                Add(new Property<IProjectFileScopeEntity, IProjectFileScopeEntity>(24, "File", e => e.File, (e, v) => e.File = v));
                Add(new Property<TextLineSpan, ITextLineSpan>(127, "TextSpan", e => e.TextSpan, (e, v) => e.TextSpan = v));
            }
        }

        public partial class TextSourceSearchModelDescriptor : SingletonDescriptorBase<TextSourceSearchModel, ITextSourceSearchModel, TextSourceSearchModelDescriptor>, ICreate<TextSourceSearchModelDescriptor>
        {
            static TextSourceSearchModelDescriptor ICreate<TextSourceSearchModelDescriptor>.Create() => new TextSourceSearchModelDescriptor();
            TextSourceSearchModelDescriptor() : base(128, 7)
            {
                Add(new Property<IChunkReference, IChunkReference>(128, "Chunk", e => e.Chunk, (e, v) => e.Chunk = v));
                Add(new Property<MurmurHash, MurmurHash>(16, "EntityContentId", e => e.EntityContentId, (e, v) => e.EntityContentId = v));
                Add(new Property<int, int>(17, "EntityContentSize", e => e.EntityContentSize, (e, v) => e.EntityContentSize = v));
                Add(new Property<IProjectFileScopeEntity, IProjectFileScopeEntity>(24, "File", e => e.File, (e, v) => e.File = v));
                Add(new Property<bool, bool>(18, "IsAdded", e => e.IsAdded, (e, v) => e.IsAdded = v));
                Add(new Property<int, int>(19, "StableId", e => e.StableId, (e, v) => e.StableId = v));
                Add(new Property<MurmurHash, MurmurHash>(20, "Uid", e => e.Uid, (e, v) => e.Uid = v));
            }
        }

        public partial class UserSettingsDescriptor : SingletonDescriptorBase<UserSettings, IUserSettings, UserSettingsDescriptor>, ICreate<UserSettingsDescriptor>
        {
            static UserSettingsDescriptor ICreate<UserSettingsDescriptor>.Create() => new UserSettingsDescriptor();
            UserSettingsDescriptor() : base(129, 2)
            {
                Add(new Property<Nullable<RepoAccess>, Nullable<RepoAccess>>(124, "Access", e => e.Access, (e, v) => e.Access = v));
                Add(new Property<DateTime, DateTime>(129, "ExpirationUtc", e => e.ExpirationUtc, (e, v) => e.ExpirationUtc = v));
            }
        }
    }
}

namespace Codex.ObjectModel.Internal
{
    using Codex.Sdk.Utilities;

    public static class ObjectStages
    {
        public static IBox<IObjectStage> GetBox(ObjectStage stage)
        {
            switch (stage)
            {
                case Codex.ObjectModel.ObjectStage.None:
                    {
                        return Codex.ObjectModel.Internal.ObjectStages.None.Box;
                    }

                case Codex.ObjectModel.ObjectStage.Analysis:
                    {
                        return Codex.ObjectModel.Internal.ObjectStages.Analysis.Box;
                    }

                case Codex.ObjectModel.ObjectStage.Index:
                    {
                        return Codex.ObjectModel.Internal.ObjectStages.Index.Box;
                    }

                case Codex.ObjectModel.ObjectStage.All:
                    {
                        return Codex.ObjectModel.Internal.ObjectStages.All.Box;
                    }

                case Codex.ObjectModel.ObjectStage.StoreRaw:
                    {
                        return Codex.ObjectModel.Internal.ObjectStages.StoreRaw.Box;
                    }

                case Codex.ObjectModel.ObjectStage.BlockIndex:
                    {
                        return Codex.ObjectModel.Internal.ObjectStages.BlockIndex.Box;
                    }

                case Codex.ObjectModel.ObjectStage.Hash:
                    {
                        return Codex.ObjectModel.Internal.ObjectStages.Hash.Box;
                    }

                case Codex.ObjectModel.ObjectStage.OptimizedStore:
                    {
                        return Codex.ObjectModel.Internal.ObjectStages.OptimizedStore.Box;
                    }
            }

            throw System.Diagnostics.ContractsLight.Contract.AssertFailure($"Invalid object stage: {stage}");
        }

        class None : ObjectStageBase<None>, IObjectStage
        {
            static ObjectStage IObjectStage.GetValue() => Codex.ObjectModel.ObjectStage.None;
        }

        class Analysis : ObjectStageBase<Analysis>, IObjectStage
        {
            static ObjectStage IObjectStage.GetValue() => Codex.ObjectModel.ObjectStage.Analysis;
        }

        class Index : ObjectStageBase<Index>, IObjectStage
        {
            static ObjectStage IObjectStage.GetValue() => Codex.ObjectModel.ObjectStage.Index;
        }

        class All : ObjectStageBase<All>, IObjectStage
        {
            static ObjectStage IObjectStage.GetValue() => Codex.ObjectModel.ObjectStage.All;
        }

        class StoreRaw : ObjectStageBase<StoreRaw>, IObjectStage
        {
            static ObjectStage IObjectStage.GetValue() => Codex.ObjectModel.ObjectStage.StoreRaw;
        }

        class BlockIndex : ObjectStageBase<BlockIndex>, IObjectStage
        {
            static ObjectStage IObjectStage.GetValue() => Codex.ObjectModel.ObjectStage.BlockIndex;
        }

        class Hash : ObjectStageBase<Hash>, IObjectStage
        {
            static ObjectStage IObjectStage.GetValue() => Codex.ObjectModel.ObjectStage.Hash;
        }

        class OptimizedStore : ObjectStageBase<OptimizedStore>, IObjectStage
        {
            static ObjectStage IObjectStage.GetValue() => Codex.ObjectModel.ObjectStage.OptimizedStore;
        }
    }
}