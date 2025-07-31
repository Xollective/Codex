using Codex.ObjectModel.Implementation;
using Codex.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Codex.Analysis.Managed;

public class DeclarationSyntaxVisitor : CSharpSyntaxWalker
{
    private Dictionary<int, DefinitionSpan> _defSpanMap;

    public DeclarationSyntaxVisitor(IEnumerable<DefinitionSpan> spans)
        : base(SyntaxWalkerDepth.Token)
    {
        _defSpanMap = spans.ToDictionarySafe(s => s.Start);
    }

    public override void VisitToken(SyntaxToken token)
    {
        var start = token.SpanStart;

        if (_defSpanMap.TryGetValue(start, out var defSpan))
        {
            var parent = token.Parent;
            defSpan.FullSpan = parent.Span.ToExtent();
        }
    }
}