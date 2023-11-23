using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AutoConstructor.Generator.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ClassWithoutPartialCodeFixProvider)), Shared]
public sealed class ClassWithoutPartialCodeFixProvider : CodeFixProvider
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

        ClassDeclarationSyntax? declaration = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();
        if (declaration is not null)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Make class partial",
                    createChangedDocument: c => MakeClassPartialAsync(context.Document, declaration, c),
                    equivalenceKey: "Make class partial"),
                diagnostic);
        }
    }

    private static async Task<Document> MakeClassPartialAsync(Document document, ClassDeclarationSyntax classDeclarationSyntax, CancellationToken cancellationToken)
    {
        SyntaxTokenList newModifiers = classDeclarationSyntax.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
        ClassDeclarationSyntax newDeclaration = classDeclarationSyntax.WithModifiers(newModifiers);

        SyntaxNode? oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (oldRoot is not null)
        {
            SyntaxNode newRoot = oldRoot.ReplaceNode(classDeclarationSyntax, newDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }

        throw new InvalidOperationException("Cannot fix code.");
    }
}
