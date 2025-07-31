using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Codex.Analysis.Managed
{
    partial class ScopeOperationWalker<TArgument> : OperationWalker<TArgument>
    {
        public override object VisitBlock(IBlockOperation operation, TArgument argument)
        {
            BeforeVisitScope(operation, argument);
            VisitLocalSymbol(operation, operation.Locals, argument);
            return AfterVisitScope(operation, argument, base.VisitBlock(operation, argument));
        }

        public override object VisitSwitch(ISwitchOperation operation, TArgument argument)
        {
            BeforeVisitScope(operation, argument);
            VisitLocalSymbol(operation, operation.Locals, argument);
            return AfterVisitScope(operation, argument, base.VisitSwitch(operation, argument));
        }

        public override object VisitForEachLoop(IForEachLoopOperation operation, TArgument argument)
        {
            BeforeVisitScope(operation, argument);
            VisitLocalSymbol(operation, operation.Locals, argument);
            return AfterVisitScope(operation, argument, base.VisitForEachLoop(operation, argument));
        }

        public override object VisitForLoop(IForLoopOperation operation, TArgument argument)
        {
            BeforeVisitScope(operation, argument);
            VisitLocalSymbol(operation, operation.ConditionLocals, argument);
            VisitLocalSymbol(operation, operation.Locals, argument);
            return AfterVisitScope(operation, argument, base.VisitForLoop(operation, argument));
        }

        public override object VisitForToLoop(IForToLoopOperation operation, TArgument argument)
        {
            BeforeVisitScope(operation, argument);
            VisitLocalSymbol(operation, operation.Locals, argument);
            return AfterVisitScope(operation, argument, base.VisitForToLoop(operation, argument));
        }

        public override object VisitWhileLoop(IWhileLoopOperation operation, TArgument argument)
        {
            BeforeVisitScope(operation, argument);
            VisitLocalSymbol(operation, operation.Locals, argument);
            return AfterVisitScope(operation, argument, base.VisitWhileLoop(operation, argument));
        }

        public override object VisitUsing(IUsingOperation operation, TArgument argument)
        {
            BeforeVisitScope(operation, argument);
            VisitLocalSymbol(operation, operation.Locals, argument);
            return AfterVisitScope(operation, argument, base.VisitUsing(operation, argument));
        }

        public override object VisitLocalReference(ILocalReferenceOperation operation, TArgument argument)
        {
            BeforeVisitScope(operation, argument);
            VisitLocalSymbol(operation, operation.Local, argument);
            return AfterVisitScope(operation, argument, base.VisitLocalReference(operation, argument));
        }

        public override object VisitFieldInitializer(IFieldInitializerOperation operation, TArgument argument)
        {
            BeforeVisitScope(operation, argument);
            VisitLocalSymbol(operation, operation.Locals, argument);
            return AfterVisitScope(operation, argument, base.VisitFieldInitializer(operation, argument));
        }

        public override object VisitVariableInitializer(IVariableInitializerOperation operation, TArgument argument)
        {
            BeforeVisitScope(operation, argument);
            VisitLocalSymbol(operation, operation.Locals, argument);
            return AfterVisitScope(operation, argument, base.VisitVariableInitializer(operation, argument));
        }

        public override object VisitPropertyInitializer(IPropertyInitializerOperation operation, TArgument argument)
        {
            BeforeVisitScope(operation, argument);
            VisitLocalSymbol(operation, operation.Locals, argument);
            return AfterVisitScope(operation, argument, base.VisitPropertyInitializer(operation, argument));
        }

        public override object VisitParameterInitializer(IParameterInitializerOperation operation, TArgument argument)
        {
            BeforeVisitScope(operation, argument);
            VisitLocalSymbol(operation, operation.Locals, argument);
            return AfterVisitScope(operation, argument, base.VisitParameterInitializer(operation, argument));
        }

        public override object VisitVariableDeclarator(IVariableDeclaratorOperation operation, TArgument argument)
        {
            BeforeVisitScope(operation, argument);
            VisitLocalSymbol(operation, operation.Symbol, argument);
            return AfterVisitScope(operation, argument, base.VisitVariableDeclarator(operation, argument));
        }

        public override object VisitCatchClause(ICatchClauseOperation operation, TArgument argument)
        {
            BeforeVisitScope(operation, argument);
            VisitLocalSymbol(operation, operation.Locals, argument);
            return AfterVisitScope(operation, argument, base.VisitCatchClause(operation, argument));
        }

        public override object VisitSwitchCase(ISwitchCaseOperation operation, TArgument argument)
        {
            BeforeVisitScope(operation, argument);
            VisitLocalSymbol(operation, operation.Locals, argument);
            return AfterVisitScope(operation, argument, base.VisitSwitchCase(operation, argument));
        }

        public override object VisitConstructorBodyOperation(IConstructorBodyOperation operation, TArgument argument)
        {
            BeforeVisitScope(operation, argument);
            VisitLocalSymbol(operation, operation.Locals, argument);
            return AfterVisitScope(operation, argument, base.VisitConstructorBodyOperation(operation, argument));
        }

        public override object VisitStaticLocalInitializationSemaphore(IStaticLocalInitializationSemaphoreOperation operation, TArgument argument)
        {
            BeforeVisitScope(operation, argument);
            VisitLocalSymbol(operation, operation.Local, argument);
            return AfterVisitScope(operation, argument, base.VisitStaticLocalInitializationSemaphore(operation, argument));
        }

        public override object VisitSwitchExpressionArm(ISwitchExpressionArmOperation operation, TArgument argument)
        {
            BeforeVisitScope(operation, argument);
            VisitLocalSymbol(operation, operation.Locals, argument);
            return AfterVisitScope(operation, argument, base.VisitSwitchExpressionArm(operation, argument));
        }
    }
}