using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.ContractsLight;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading;
using ICSharpCode.Decompiler.IL;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Symbols;

namespace Codex.Analysis.Managed;

/// <summary>
/// This is a workaround for the fact that the compiler analysis loop creates a new compilation
/// for every single file analysis call.
/// When updating Roslyn we need to verify that only the methods below are called when constructing
/// the <see cref="CompilationWithAnalyzers"/>:
/// <see cref="Compilation.WithEventQueue"/>
/// <see cref="Compilation.WithOptions"/>
/// <see cref="Compilation.WithSemanticModelProvider"/>
/// Also the expectation is that only the methods below are called before using the compilation:
/// <see cref="Compilation.WithEventQueue"/>
/// <see cref="Compilation.WithSemanticModelProvider"/>
/// </summary>
public class AnalysisReuseCompilationWrapper : Compilation
{
    public Compilation Inner { get; }

    /// <summary>
    /// Gets whether the current instance is final which mean the _inner compilation will
    /// not be modified by With* calls. 
    /// The <see cref="WithEventQueue"/> method will return the _inner compilation when this is
    /// true since the compiler analysis flow calls this before actually using the compilation.
    /// </summary>
    public bool IsFinal { get; set; }

    public AnalysisReuseCompilationWrapper(Compilation inner) 
        : base(
            inner.AssemblyName, 
            inner.ExternalReferences, 
            ImmutableDictionary<string, string>.Empty, 
            inner.IsSubmission, 
            inner.SemanticModelProvider, 
            inner.EventQueue)
    {
        Inner = inner;
    }

    private Compilation WithCore(Func<Compilation> transformInner)
    {
        if (IsFinal)
        {
            return this;
        }
        else
        {
            return new AnalysisReuseCompilationWrapper(transformInner());
        }
    }

    protected override bool CommonContainsSyntaxTree(SyntaxTree? syntaxTree)
    {
        return Inner.ContainsSyntaxTree(syntaxTree);
    }

    public override Compilation WithEventQueue(AsyncQueue<CompilationEvent>? eventQueue)
    {
        if (IsFinal)
        {
            Contract.Assert(eventQueue != null);
            Contract.Assert(Inner.EventQueue != null);
            return Inner;
        }

        return WithCore(() => Inner.WithEventQueue(eventQueue));
    }

    public override Compilation WithSemanticModelProvider(SemanticModelProvider semanticModelProvider)
    {
        if (IsFinal)
        {
            Contract.Check(semanticModelProvider?.GetType() == Inner.SemanticModelProvider?.GetType())
                ?.Assert($"Types should match: '{semanticModelProvider?.GetType()}' != '{Inner.SemanticModelProvider?.GetType()}'");
        }

        return WithCore(() => Inner.WithSemanticModelProvider(semanticModelProvider));
    }

    protected override Compilation CommonWithOptions(CompilationOptions options)
    {
        Contract.Assert(!IsFinal);

        return WithCore(() => Inner.WithOptions(options));
    }

    protected override CompilationOptions CommonOptions => Inner.Options;

    private Exception NotImplemented([CallerMemberName]string caller = null)
    {
        throw Contract.AssertFailure($"Compilation.{caller} is not expected to be called");
    }

    protected override Compilation CommonClone()
    {
        throw NotImplemented();
    }

    public override bool IsCaseSensitive => Inner.IsCaseSensitive;

    public override ScriptCompilationInfo? CommonScriptCompilationInfo => Inner.CommonScriptCompilationInfo;

    public override string Language => Inner.Language;

    public override ImmutableArray<SyntaxTree> CommonSyntaxTrees => Inner.CommonSyntaxTrees;

    public override ImmutableArray<MetadataReference> DirectiveReferences => Inner.DirectiveReferences;

    public override IEnumerable<ReferenceDirective> ReferenceDirectives => Inner.ReferenceDirectives;

    public override IDictionary<(string path, string content), MetadataReference> ReferenceDirectiveMap => Inner.ReferenceDirectiveMap;

    public override IEnumerable<AssemblyIdentity> ReferencedAssemblyNames => Inner.ReferencedAssemblyNames;

    public override CommonAnonymousTypeManager CommonAnonymousTypeManager => Inner.CommonAnonymousTypeManager;

    public override CommonMessageProvider MessageProvider => Inner.MessageProvider;

    public override byte LinkerMajorVersion => Inner.LinkerMajorVersion;

    public override bool IsDelaySigned => Inner.IsDelaySigned;

    public override StrongNameKeys StrongNameKeys => Inner.StrongNameKeys;

    public override Guid DebugSourceDocumentLanguageId => Inner.DebugSourceDocumentLanguageId;

    protected override IAssemblySymbol CommonAssembly => throw NotImplemented();

    protected override IModuleSymbol CommonSourceModule => throw NotImplemented();

    protected override INamespaceSymbol CommonGlobalNamespace => throw NotImplemented();

    protected override INamedTypeSymbol CommonObjectType => throw NotImplemented();

    protected override ITypeSymbol CommonDynamicType => throw NotImplemented();

    protected override ITypeSymbol? CommonScriptGlobalsType => throw NotImplemented();

    protected override INamedTypeSymbol? CommonScriptClass => throw NotImplemented();

    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1014:Do not ignore values returned by methods on immutable objects", Justification = "<Pending>")]
    public override void AddDebugSourceDocumentsForChecksumDirectives(DebugDocumentsBuilder documentsBuilder, SyntaxTree tree, DiagnosticBag diagnostics)
    {
        Inner.AddDebugSourceDocumentsForChecksumDirectives(documentsBuilder, tree, diagnostics);
    }

    public override CommonConversion ClassifyCommonConversion(ITypeSymbol source, ITypeSymbol destination)
    {
        return Inner.ClassifyCommonConversion(source, destination);
    }

    public override IConvertibleConversion ClassifyConvertibleConversion(IOperation source, ITypeSymbol destination, out ConstantValue? constantValue)
    {
        return Inner.ClassifyConvertibleConversion(source, destination, out constantValue);
    }

    public override CommonReferenceManager CommonGetBoundReferenceManager()
    {
        return Inner.CommonGetBoundReferenceManager();
    }

    public override MetadataReference? CommonGetMetadataReference(IAssemblySymbol assemblySymbol)
    {
        return Inner.CommonGetMetadataReference(assemblySymbol);
    }

    public override INamedTypeSymbolInternal CommonGetSpecialType(SpecialType specialType)
    {
        return Inner.CommonGetSpecialType(specialType);
    }

    public override ISymbolInternal CommonGetSpecialTypeMember(SpecialMember specialMember)
    {
        return Inner.CommonGetSpecialTypeMember(specialMember);
    }

    public override ITypeSymbolInternal CommonGetWellKnownType(WellKnownType wellknownType)
    {
        return Inner.CommonGetWellKnownType(wellknownType);
    }

    public override ISymbolInternal? CommonGetWellKnownTypeMember(WellKnownMember member)
    {
        return Inner.CommonGetWellKnownTypeMember(member);
    }

    public override int CompareSourceLocations(Location loc1, Location loc2)
    {
        return Inner.CompareSourceLocations(loc1, loc2);
    }

    public override int CompareSourceLocations(SyntaxReference loc1, SyntaxReference loc2)
    {
        return Inner.CompareSourceLocations(loc1, loc2);
    }

    public override int CompareSourceLocations(SyntaxNode loc1, SyntaxNode loc2)
    {
        return Inner.CompareSourceLocations(loc1, loc2);
    }

    public override bool CompileMethods(CommonPEModuleBuilder moduleBuilder, bool emittingPdb, DiagnosticBag diagnostics, Predicate<ISymbolInternal>? filterOpt, CancellationToken cancellationToken)
    {
        return Inner.CompileMethods(moduleBuilder, emittingPdb, diagnostics, filterOpt, cancellationToken);
    }

    public override void CompleteTrees(SyntaxTree? filterTree)
    {
        Inner.CompleteTrees(filterTree);
    }

    public override bool ContainsSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default)
    {
        return Inner.ContainsSymbolsWithName(predicate, filter, cancellationToken);
    }

    public override bool ContainsSymbolsWithName(string name, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default)
    {
        return Inner.ContainsSymbolsWithName(name, filter, cancellationToken);
    }

    public override AnalyzerDriver CreateAnalyzerDriver(ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerManager analyzerManager, SeverityFilter severityFilter)
    {
        return Inner.CreateAnalyzerDriver(analyzers, analyzerManager, severityFilter);
    }

    public override CommonPEModuleBuilder? CreateModuleBuilder(EmitOptions emitOptions, IMethodSymbol? debugEntryPoint, Stream? sourceLinkStream, IEnumerable<EmbeddedText>? embeddedTexts, IEnumerable<ResourceDescription>? manifestResources, CompilationTestData? testData, DiagnosticBag diagnostics, CancellationToken cancellationToken)
    {
        return Inner.CreateModuleBuilder(emitOptions, debugEntryPoint, sourceLinkStream, embeddedTexts, manifestResources, testData, diagnostics, cancellationToken);
    }

    public override EmitDifferenceResult EmitDifference(EmitBaseline baseline, IEnumerable<SemanticEdit> edits, Func<ISymbol, bool> isAddedSymbol, Stream metadataStream, Stream ilStream, Stream pdbStream, CompilationTestData? testData, CancellationToken cancellationToken)
    {
        return Inner.EmitDifference(baseline, edits, isAddedSymbol, metadataStream, ilStream, pdbStream, testData, cancellationToken);
    }

    public override bool GenerateDocumentationComments(Stream? xmlDocStream, string? outputNameOverride, DiagnosticBag diagnostics, CancellationToken cancellationToken)
    {
        return Inner.GenerateDocumentationComments(xmlDocStream, outputNameOverride, diagnostics, cancellationToken);
    }

    public override bool GenerateResources(CommonPEModuleBuilder moduleBuilder, Stream? win32Resources, bool useRawWin32Resources, DiagnosticBag diagnostics, CancellationToken cancellationToken)
    {
        return Inner.GenerateResources(moduleBuilder, win32Resources, useRawWin32Resources, diagnostics, cancellationToken);
    }

    public override ImmutableArray<Diagnostic> GetDeclarationDiagnostics(CancellationToken cancellationToken = default)
    {
        return Inner.GetDeclarationDiagnostics(cancellationToken);
    }

    public override ImmutableArray<Diagnostic> GetDiagnostics(CancellationToken cancellationToken = default)
    {
        return Inner.GetDiagnostics(cancellationToken);
    }

    public override void GetDiagnostics(CompilationStage stage, bool includeEarlierStages, DiagnosticBag diagnostics, CancellationToken cancellationToken = default)
    {
        Inner.GetDiagnostics(stage, includeEarlierStages, diagnostics, cancellationToken);
    }

    public override ImmutableArray<Diagnostic> GetMethodBodyDiagnostics(CancellationToken cancellationToken = default)
    {
        return Inner.GetMethodBodyDiagnostics(cancellationToken);
    }

    public override ImmutableArray<Diagnostic> GetParseDiagnostics(CancellationToken cancellationToken = default)
    {
        return Inner.GetParseDiagnostics(cancellationToken);
    }

    public override IEnumerable<ISymbol> GetSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default)
    {
        return Inner.GetSymbolsWithName(predicate, filter, cancellationToken);
    }

    public override IEnumerable<ISymbol> GetSymbolsWithName(string name, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default)
    {
        return Inner.GetSymbolsWithName(name, filter, cancellationToken);
    }

    public override int GetSyntaxTreeOrdinal(SyntaxTree tree)
    {
        return Inner.GetSyntaxTreeOrdinal(tree);
    }

    public override ImmutableArray<MetadataReference> GetUsedAssemblyReferences(CancellationToken cancellationToken = default)
    {
        return Inner.GetUsedAssemblyReferences(cancellationToken);
    }

    public override bool HasCodeToEmit()
    {
        return Inner.HasCodeToEmit();
    }

    public override bool HasSubmissionResult()
    {
        return Inner.HasSubmissionResult();
    }

    public override bool IsAttributeType(ITypeSymbol type)
    {
        return Inner.IsAttributeType(type);
    }

    public override bool IsSymbolAccessibleWithinCore(ISymbol symbol, ISymbol within, ITypeSymbol? throughType)
    {
        return Inner.IsSymbolAccessibleWithinCore(symbol, within, throughType);
    }

    public override bool IsSystemTypeReference(ITypeSymbolInternal type)
    {
        return Inner.IsSystemTypeReference(type);
    }

    public override bool IsUnreferencedAssemblyIdentityDiagnosticCode(int code)
    {
        return Inner.IsUnreferencedAssemblyIdentityDiagnosticCode(code);
    }

    public override void ReportUnusedImports(DiagnosticBag diagnostics, CancellationToken cancellationToken)
    {
        Inner.ReportUnusedImports(diagnostics, cancellationToken);
    }

    public override void SerializePdbEmbeddedCompilationOptions(BlobBuilder builder)
    {
        Inner.SerializePdbEmbeddedCompilationOptions(builder);
    }

    public override bool SupportsRuntimeCapabilityCore(RuntimeCapability capability)
    {
        return Inner.SupportsRuntimeCapabilityCore(capability);
    }

    public override CompilationReference ToMetadataReference(ImmutableArray<string> aliases = default, bool embedInteropTypes = false)
    {
        return Inner.ToMetadataReference(aliases, embedInteropTypes);
    }

    public override void ValidateDebugEntryPoint(IMethodSymbol debugEntryPoint, DiagnosticBag diagnostics)
    {
        Inner.ValidateDebugEntryPoint(debugEntryPoint, diagnostics);
    }

#pragma warning disable RSEXPERIMENTAL001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    protected override SemanticModel CommonGetSemanticModel(SyntaxTree syntaxTree, SemanticModelOptions options)
    {
        throw NotImplemented();
    }

    public override SemanticModel CreateSemanticModel(SyntaxTree syntaxTree, SemanticModelOptions options)
    {
        return Inner.CreateSemanticModel(syntaxTree, options);
    }
#pragma warning restore RSEXPERIMENTAL001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    protected override INamedTypeSymbol CommonCreateErrorTypeSymbol(INamespaceOrTypeSymbol? container, string name, int arity)
    {
        throw NotImplemented();
    }

    protected override INamespaceSymbol CommonCreateErrorNamespaceSymbol(INamespaceSymbol container, string name)
    {
        throw NotImplemented();
    }

    protected override Compilation CommonWithAssemblyName(string? outputName)
    {
        throw NotImplemented();
    }

    protected override Compilation CommonWithScriptCompilationInfo(ScriptCompilationInfo? info)
    {
        throw NotImplemented();
    }

    protected override Compilation CommonAddSyntaxTrees(IEnumerable<SyntaxTree> trees)
    {
        throw NotImplemented();
    }

    protected override Compilation CommonRemoveSyntaxTrees(IEnumerable<SyntaxTree> trees)
    {
        throw NotImplemented();
    }

    protected override Compilation CommonRemoveAllSyntaxTrees()
    {
        throw NotImplemented();
    }

    protected override Compilation CommonReplaceSyntaxTree(SyntaxTree oldTree, SyntaxTree newTree)
    {
        throw NotImplemented();
    }

    protected override Compilation CommonWithReferences(IEnumerable<MetadataReference> newReferences)
    {
        throw NotImplemented();
    }

    protected override ISymbol? CommonGetAssemblyOrModuleSymbol(MetadataReference reference)
    {
        throw NotImplemented();
    }

    protected override INamespaceSymbol? CommonGetCompilationNamespace(INamespaceSymbol namespaceSymbol)
    {
        throw NotImplemented();
    }

    protected override IMethodSymbol? CommonGetEntryPoint(CancellationToken cancellationToken)
    {
        throw NotImplemented();
    }

    protected override IArrayTypeSymbol CommonCreateArrayTypeSymbol(ITypeSymbol elementType, int rank, NullableAnnotation elementNullableAnnotation)
    {
        throw NotImplemented();
    }

    protected override IPointerTypeSymbol CommonCreatePointerTypeSymbol(ITypeSymbol elementType)
    {
        throw NotImplemented();
    }

    protected override IFunctionPointerTypeSymbol CommonCreateFunctionPointerTypeSymbol(ITypeSymbol returnType, RefKind returnRefKind, ImmutableArray<ITypeSymbol> parameterTypes, ImmutableArray<RefKind> parameterRefKinds, SignatureCallingConvention callingConvention, ImmutableArray<INamedTypeSymbol> callingConventionTypes)
    {
        throw NotImplemented();
    }

    protected override INamedTypeSymbol CommonCreateNativeIntegerTypeSymbol(bool signed)
    {
        throw NotImplemented();
    }

    protected override INamedTypeSymbol? CommonGetTypeByMetadataName(string metadataName)
    {
        throw NotImplemented();
    }

    protected override INamedTypeSymbol CommonCreateTupleTypeSymbol(ImmutableArray<ITypeSymbol> elementTypes, ImmutableArray<string?> elementNames, ImmutableArray<Location?> elementLocations, ImmutableArray<NullableAnnotation> elementNullableAnnotations)
    {
        throw NotImplemented();
    }

    protected override INamedTypeSymbol CommonCreateTupleTypeSymbol(INamedTypeSymbol underlyingType, ImmutableArray<string?> elementNames, ImmutableArray<Location?> elementLocations, ImmutableArray<NullableAnnotation> elementNullableAnnotations)
    {
        throw NotImplemented();
    }

    protected override INamedTypeSymbol CommonCreateAnonymousTypeSymbol(ImmutableArray<ITypeSymbol> memberTypes, ImmutableArray<string> memberNames, ImmutableArray<Location> memberLocations, ImmutableArray<bool> memberIsReadOnly, ImmutableArray<NullableAnnotation> memberNullableAnnotations)
    {
        throw NotImplemented();
    }

    protected override IMethodSymbol CommonCreateBuiltinOperator(string name, ITypeSymbol returnType, ITypeSymbol leftType, ITypeSymbol rightType)
    {
        throw NotImplemented();
    }

    protected override IMethodSymbol CommonCreateBuiltinOperator(string name, ITypeSymbol returnType, ITypeSymbol operandType)
    {
        throw NotImplemented();
    }

    protected override void AppendDefaultVersionResource(Stream resourceStream)
    {
        throw NotImplemented();
    }

    [return: NotNullIfNotNull("symbol")]
    public override TSymbol? GetSymbolInternal<TSymbol>(ISymbol? symbol) where TSymbol : class
    {
        throw NotImplemented();
    }
}
