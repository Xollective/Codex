using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Codex.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Codex.Analysis.Managed;
internal record AnalysisState(DocumentAnalyzer Analyzer) : LocalSymbolState
{
    public ConcurrentDictionary<int, SpanAnalysisState> SpansByStart { get; } = new();

    public static readonly SymbolAnalysisState NamespaceSymbolState = new SymbolAnalysisState()
    {
        IsScope = true,
        SymbolDepth = 0
    };

    public ConcurrentDictionary<SymbolKey, SymbolAnalysisState> SymbolAnalysisState { get; } = new();

    public ConcurrentDictionary<(SymbolAnalysisState ContainerState, string Name), int> LocalNameMap { get; } = new();

    public SpanAnalysisState GetState(int spanStart)
    {
        return SpansByStart.GetOrAdd(spanStart, i => new SpanAnalysisState());
    }

    public SpanAnalysisState GetState(SyntaxToken token) => GetState(token.SpanStart);

    public SymbolAnalysisState GetState(ISymbol symbol, SymbolSpec spec = default)
    {
        symbol = symbol.OriginalDefinition ?? symbol;
        return SymbolAnalysisState.GetOrAdd(new(symbol, spec), static i => new SymbolAnalysisState());
    }

    public bool TryGetState(ISymbol symbol, out SymbolAnalysisState state, SymbolSpec spec = default)
    {
        if (symbol.IsNamespace())
        {
            state = NamespaceSymbolState;
            return true;
        }
        else return SymbolAnalysisState.TryGetValue(new (symbol, spec), out state);
    }

    public bool TryGetStateAt(SyntaxToken token, out SpanAnalysisState state) => SpansByStart.TryGetValue(token.SpanStart, out state);
    public bool TryGetStateAt(int spanStart, out SpanAnalysisState state) => SpansByStart.TryGetValue(spanStart, out state);
}

public record struct InterfaceMemberMapping(
    ILookup<ISymbol, ISymbol> memberByImplementedLookup,
    IDictionary<ISymbol, ISymbol> interfaceMemberToImplementationMap);

public enum SymbolSpec
{
    None,
    This,
    Base,
}

public record struct SymbolKey(ISymbol Value, SymbolSpec Specialization) : IEquatable<SymbolKey>
{
    public bool Equals(SymbolKey other)
    {
        return SymbolEqualityComparer.Default.Equals(Value, other.Value)
            && Specialization == other.Specialization;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SymbolEqualityComparer.Default.GetHashCode(Value), Specialization.GetHashCode());
    }
}

public class SymbolicClassificationSpan : ClassificationSpan
{
    public SymbolicClassificationSpan(IClassificationSpan span) 
        : base(span)
    {
    }

    public SymbolicClassificationSpan()
    {
    }

    public SymbolAnalysisState State { get; private set; }

    public int TrackerLocalId { get; set; }

    public void Associate(SymbolAnalysisState state)
    {
        state.Associate(this);
        State = state;
    }
}

public class SymbolAnalysisState
{
    public InterfaceMemberMapping? InterfaceMemberMapping;

    public bool IsScope { get; set; }
    public int? SymbolDepth { get; set; }

    public int ScopedLocalId { get; set; }

    public int StartLocalId { get; set; }
    public int AfterStartLocalId { get; set; }

    public long? ConstantValue { get; set; }


    public string LocalName { get; set; }
    public int LocalSymbolStateIndex { get; set; } = -1;
    public int MaxSymbolStart;
    public int MinSymbolStart = int.MaxValue;

    public void Associate(ISpan span)
    {
        MinSymbolStart = Math.Min(MinSymbolStart, span.Start);
        MaxSymbolStart = Math.Max(MaxSymbolStart, span.Start);
    }
}

public class SpanAnalysisState
{
    public ReferenceKind? ReferenceKind;
    public OperationKind? OperationKind;
    public TextSpan? Span;
    public bool Skip;

    public Utilities.Optional<SyntaxNode> DeclarationNode;

    public bool ExcludeFromSearch;
}