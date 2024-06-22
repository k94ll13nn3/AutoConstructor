using System.Collections.Immutable;
using System.Composition;
using AutoConstructor.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AutoConstructor.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TypeWithoutPartialCodeFixProvider)), Shared]
public sealed class TypeWithoutPartialCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticDescriptors.TypeWithoutPartialDiagnosticId);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        Diagnostic? diagnostic = context.Diagnostics[0];
        TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

        TypeDeclarationSyntax? declaration = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();
        if (declaration is not null)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Make type partial",
                    createChangedDocument: c => MakeTypePartialAsync(context.Document, declaration, c),
                    equivalenceKey: "Make type partial"),
                diagnostic);
        }
    }

    private static async Task<Document> MakeTypePartialAsync(Document document, TypeDeclarationSyntax typeDeclarationSyntax, CancellationToken cancellationToken)
    {
        SyntaxTokenList newModifiers = typeDeclarationSyntax.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
        TypeDeclarationSyntax newDeclaration = typeDeclarationSyntax.WithModifiers(newModifiers);

        SyntaxNode? oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (oldRoot is not null)
        {
            SyntaxNode newRoot = oldRoot.ReplaceNode(typeDeclarationSyntax, newDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }

        throw new InvalidOperationException("Cannot fix code.");
    }
}
