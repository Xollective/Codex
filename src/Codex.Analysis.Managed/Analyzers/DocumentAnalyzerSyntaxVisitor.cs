using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codex.Analysis.Managed;

public class DocumentAnalyzerSyntaxVisitor : CSharpSyntaxWalker
{
    internal DocumentAnalyzer Analyzer { get; }
    public SemanticModel Model { get; }
    internal AnalysisState State { get; }

    private int test, test2 = 0;

    internal DocumentAnalyzerSyntaxVisitor(DocumentAnalyzer analyzer, SemanticModel model)
    {
        Analyzer = analyzer;
        Model = model;
        State = analyzer.State;
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        VisitMethodDeclarationCore(node, node.Identifier);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        VisitMethodDeclarationCore(node, node.Identifier);
    }

    public override void VisitDestructorDeclaration(DestructorDeclarationSyntax node)
    {
        VisitMethodDeclarationCore(node, node.Identifier);
    }

    public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
    {
        VisitMethodDeclarationCore(node, node.OperatorToken);
    }

    public override void VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
    {
        VisitMethodDeclarationCore(node, node.ImplicitOrExplicitKeyword);
    }

    private void VisitMethodDeclarationCore(BaseMethodDeclarationSyntax node, SyntaxToken token)
    {
        if (Model.GetDeclaredSymbol(node) is IMethodSymbol methodSymbol)
        {
            Analyzer.AddSymbolSpan(methodSymbol, token, methodSymbol);

            VisitMemberDeclaration(node, token);
            // Skip method bodies
        }
    }

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        var fieldIdentifier = node.Declaration.Variables.FirstOrDefault()?.Identifier;
        if (fieldIdentifier.HasValue)
        {
            VisitMemberDeclaration(node, fieldIdentifier.Value);
        }
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        VisitTypeDeclaration(node, node.ParameterList);

        base.VisitClassDeclaration(node);
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        VisitTypeDeclaration(node, node.ParameterList);

        base.VisitStructDeclaration(node);
    }

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        VisitTypeDeclaration(node, node.ParameterList);

        base.VisitRecordDeclaration(node);
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        VisitTypeDeclaration(node, node.ParameterList);

        base.VisitInterfaceDeclaration(node);
    }

    private void VisitTypeDeclaration(BaseTypeDeclarationSyntax node, ParameterListSyntax parameterList)
    {
        var spanState = State.GetState(node.Identifier);
        spanState.DeclarationNode = node;

        if (parameterList != null)
        {
            var typeSymbol = Model.GetDeclaredSymbol(node);
            foreach (var parameter in parameterList.Parameters)
            {
                var parameterSymbol = Model.GetDeclaredSymbol(parameter);
                foreach (var member in typeSymbol.GetMembers(parameterSymbol.Name))
                {
                    Analyzer.AddSymbolSpan(member, parameter.Identifier, member);
                }
            }
        }
    }

    public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node)
    {
        var symbol = Model.GetDeclaredSymbol(node);

        Analyzer.AddSymbolSpan(symbol, node.ThisKeyword, symbol);

        VisitBasePropertyDeclaration(node, symbol, node.ThisKeyword);
    }

    public void VisitBasePropertyDeclaration(BasePropertyDeclarationSyntax node, IPropertySymbol symbol, SyntaxToken identifier)
    {
        VisitMemberDeclaration(node, identifier);

        if (node.AccessorList != null)
        {
            foreach (var accessor in node.AccessorList.Accessors)
            {
                VisitAccessorDeclaration(accessor, symbol);
            }
        }
    }

    private void VisitMemberDeclaration(SyntaxNode node, SyntaxToken identifier)
    {
        var spanState = State.GetState(identifier);
        spanState.DeclarationNode = node;
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        var symbol = Model.GetDeclaredSymbol(node);
        VisitBasePropertyDeclaration(node, symbol,node.Identifier);
    }

    public void VisitAccessorDeclaration(AccessorDeclarationSyntax node, IPropertySymbol property)
    {
        var spanState = State.GetState(node.Keyword);
        spanState.DeclarationNode = node;

        // Explanation for why is is ok for both DefinitionSymbol AND ReferenceSymbol to have ExcludeFromSearch = true
        // Accessor reference to property is excluded because the reference which is shown as definition
        // for the property on the 'this' keyword. Go to definition will go to this keyword and only this keyword will
        // show in find all references result.
        spanState.ExcludeFromSearch = true;
        spanState.ReferenceKind = node.Keyword.IsKind(SyntaxKind.SetKeyword) ? ReferenceKind.Setter : ReferenceKind.Getter;
        var symbol = Model.GetDeclaredSymbol(node);

        Analyzer.AddSymbolSpan(property, node.Keyword);

        //if (node.Keyword.IsKind(SyntaxKind.GetKeyword))
        //{

        //}
        //Analyzer.AddSymbolSpan(symbol)
    }
}