using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Codex.Analysis.Managed
{
    public abstract partial class ScopeOperationWalker<TArgument>
    {
        public virtual void VisitLocalSymbol(IOperation operation, ImmutableArray<ILocalSymbol> symbols, TArgument argument)
        {
            if (symbols.IsDefaultOrEmpty) return;

            foreach (var symbol in symbols)
            {
                VisitLocalSymbol(operation, symbol, argument);
            }
        }

        public abstract void VisitLocalSymbol(IOperation operation, ILocalSymbol symbol, TArgument argument);

        public abstract void BeforeVisitScope(IOperation operation, TArgument argument);

        public abstract object AfterVisitScope(IOperation operation, TArgument argument, object? result);
    }
}