using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoConstructor.Generator;

internal class SyntaxReceiver : ISyntaxContextReceiver
{
    public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        // Only check:
        // - classes
        // - with the "partial" keyword
        // - with the wanted attribute
        if (context.Node is ClassDeclarationSyntax classDeclarationSyntax
            && classDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            INamedTypeSymbol? symbol = context
                .SemanticModel
                .GetDeclaredSymbol(classDeclarationSyntax);

            if (symbol?.HasAttribute(Source.AttributeFullName, context.SemanticModel.Compilation) is true)
            {
                CandidateClasses.Add(classDeclarationSyntax);
            }
        }
    }
}
