using Codex.ObjectModel;
using Codex.Utilities.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codex
{
    public record struct ClassifiedExtent(ClassificationName Classification, int Length)
    {
        public int AsIntegral() => (Length << 8) | (byte)Classification;

        public static ClassifiedExtent FromIntegral(int value) => new((ClassificationName)(byte)value, value >> 8);
    }

    public interface IDefinitionSymbol : IReferenceSymbol, IDisplayCodeSymbol, IJsonRangeTracking<IDefinitionSymbol>
    {
        /// <summary>
        /// The unique identifier for the symbol
        /// NOTE: This is not applicable to most symbols. Only set for symbols
        /// which are added in a shared context and need this for deduplication)
        /// </summary>
        string Uid { get; }

        /// <summary>
        /// The abbreviated name of the symbol (i.e. ElasticSearchCodex => esc).
        /// This is used to find the symbol when search term does not contain '.'
        /// </summary>
        // TODO: In theory this doesn't need to be serialized since it is derived from short name
        [SearchBehavior(SearchBehavior.PrefixTerm)]
        [CoerceGet]
        string AbbreviatedName { get; }

        /// <summary>
        /// Additional search terms for the symbol.
        /// (i.e. integral value for enum field)
        /// </summary>
        [SearchBehavior(SearchBehavior.NormalizedKeyword)]
        IReadOnlyList<string> Keywords { get; }

        /// <summary>
        /// The short name of the symbol (i.e. Task).
        /// This is used to find the symbol when search term does not contain '.'
        /// </summary>
        [SearchBehavior(SearchBehavior.PrefixShortName)]
        [CoerceGet]
        string ShortName { get; }

        /// <summary>
        /// The qualified name of the symbol (i.e. System.Threading.Tasks.Task).
        /// This is used to find the symbol when the search term contains '.'
        /// </summary>
        [SearchBehavior(SearchBehavior.PrefixFullName)]
        string ContainerQualifiedName { get; }

        /// <summary>
        /// Gets the symbol id of the container type. 
        /// NOTE: This is only set for type members.
        /// </summary>
        SymbolId ContainerTypeSymbolId { get; }

        /// <summary>
        /// Modifiers for the symbol such as public
        /// </summary>
        // TODO: Consider using single CopyTo field for keywords
        [SearchBehavior(SearchBehavior.NormalizedKeyword)]
        IReadOnlyList<string> Modifiers { get; }

        /// <summary>
        /// The glyph
        /// </summary>
        Glyph Glyph { get; }

        /// <summary>
        /// The depth of the symbol in its symbolic tree
        /// </summary>
        int SymbolDepth { get; }

        /// <summary>
        /// Indicates if the symbol should be excluded from the definition/find all references search (by default).
        /// Symbol will only be included if kind is explicitly specified
        /// </summary>
        [Include(ObjectStage.Analysis)]
        [SearchBehavior(SearchBehavior.Term)]
        bool ExcludeFromDefaultSearch { get; }

        // TODO: None of the properties below are intrinsic to the definition. They should be stored separately
        // That said, it is fine to store these on the definition to provide the association. They just need to be
        // removed before computing the content id
        // TODO: Transition to using DocumentationInfo

        ///// <summary>
        ///// Documentation for the symbol (if any)
        ///// </summary>
        //[Include(ObjectStage.Analysis)]
        //[SearchBehavior(SearchBehavior.None)]
        //IDocumentationInfo DocumentationInfo { get; }

        /// <summary>
        /// The name of the type for the symbol
        /// (i.e. return type of method)
        /// </summary>
        [Include(ObjectStage.Analysis)]
        string TypeName { get; }

        /// <summary>
        /// The declaration name for the symbol
        /// </summary>
        [Include(ObjectStage.Analysis)]
        string DeclarationName { get; }

        /// <summary>
        /// The comment applied to the definition
        /// </summary>
        [Include(ObjectStage.Analysis)]
        string Comment { get; }

        /// <summary>
        /// The usage count of the definition if known
        /// </summary>
        [Include(ObjectStage.Analysis)]
        [CoerceGet]
        int ReferenceCount { get; }

        [Include(ObjectStage.Analysis)]
        IReadOnlyList<IDefinitionSymbolExtendedSearchInfo> ExtendedSearchInfo { get; }

        /// <summary>
        /// Additional information to search for extension member search
        /// </summary>
        IDefinitionSymbolExtensionInfo ExtensionInfo { get; }
    }

    public interface IDefinitionSymbolExtendedSearchInfo
    {
        // TODO: Add extension method info here instead of IDefinitionSymbol.

        /// <summary>
        /// The constant value
        /// </summary>
        long? ConstantValue { get; set; }
    }

    /// <summary>
    /// Additional information to search for extension member search
    /// </summary>
    public interface IDefinitionSymbolExtensionInfo
    {
        /// <summary>
        /// The qualified name of the symbol (i.e. System.Threading.Tasks.Task).
        /// This is used to find the symbol when the search term contains '.'
        /// </summary>
        [SearchBehavior(SearchBehavior.PrefixFullName)]
        string ContainerQualifiedName { get; }

        /// <summary>
        /// The identifier of the project in which the symbol appears
        /// </summary>
        [SearchBehavior(SearchBehavior.NormalizedKeyword)]
        string ProjectId { get; }
    }

    public interface IReferenceSymbol : ICodeSymbol
    {
        /// <summary>
        /// The kind of reference. This is used to group references.
        /// (i.e. for C#(aka .NET) MethodOverride, InterfaceMemberImplementation, InterfaceImplementation, etc.)
        /// </summary>
        [SearchBehavior(SearchBehavior.Sortword)]
        [CoerceGet]
        ReferenceKind ReferenceKind { get; }

        /// <summary>
        /// Indicates if the symbol should NEVER be included in the definition/find all references search.
        /// </summary>
        [Include(ObjectStage.Analysis)]
        bool ExcludeFromSearch { get; }
    }

    public interface IDisplayCodeSymbol : ICodeSymbol
    {
        /// <summary>
        /// The display name of the symbol
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Classifications for tokens in DisplayName
        /// </summary>
        [ReadOnlyList]
        IReadOnlyList<ClassifiedExtent> Classifications { get; }
    }

    public interface ICodeSymbol
    {
        /// <summary>
        /// The identifier of the project in which the symbol appears
        /// </summary>
        [SearchBehavior(SearchBehavior.NormalizedKeyword)]
        string ProjectId { get; }

        /// <summary>
        /// The identifier for the symbol
        /// </summary>
        [SearchBehavior(SearchBehavior.NormalizedKeyword)]
        SymbolId Id { get; }

        /// <summary>
        /// The symbol kind. (i.e. interface, method, field)
        /// </summary>
        [SearchBehavior(SearchBehavior.NormalizedKeyword)]
        StringEnum<SymbolKinds> Kind { get; }
    }
}
