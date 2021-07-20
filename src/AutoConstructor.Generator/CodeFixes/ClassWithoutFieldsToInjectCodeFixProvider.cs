using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AutoConstructor.Generator
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ClassWithoutFieldsToInjectCodeFixProvider)), Shared]
    public class ClassWithoutFieldsToInjectCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ClassWithoutFieldsToInjectAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            Diagnostic? diagnostic = context.Diagnostics[0];
            TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

            AttributeListSyntax? declarationList = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<AttributeListSyntax>().First();
            AttributeSyntax? declaration = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<AttributeSyntax>().First();
            if (declarationList is not null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: $"Remove {Source.AttributeFullName}",
                        createChangedDocument: c => RemoveAttributeAsync(context.Document, declarationList, declaration, c),
                        equivalenceKey: $"Remove {Source.AttributeFullName}"),
                    diagnostic);
            }
        }

        private static async Task<Document> RemoveAttributeAsync(Document document, AttributeListSyntax attributeList, AttributeSyntax? attribute, CancellationToken cancellationToken)
        {
            SyntaxNode? oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (oldRoot is not null)
            {
                if (attributeList.Attributes.Count > 1 && attribute is not null)
                {
                    // Multiples attributes on the line.
                    AttributeListSyntax? newSyntax = attributeList.RemoveNode(attribute, SyntaxRemoveOptions.KeepNoTrivia);
                    if (newSyntax is not null)
                    {
                        SyntaxNode? newRoot = oldRoot.ReplaceNode(attributeList, newSyntax);
                        if (newRoot is not null)
                        {
                            return document.WithSyntaxRoot(newRoot);
                        }
                    }
                }
                else
                {
                    // One attribute.
                    SyntaxNode? newRoot = oldRoot.RemoveNode(attributeList, SyntaxRemoveOptions.KeepEndOfLine);
                    if (newRoot is not null)
                    {
                        return document.WithSyntaxRoot(newRoot);
                    }
                }
            }

            throw new InvalidOperationException("Cannot fix code.");
        }
    }
}
