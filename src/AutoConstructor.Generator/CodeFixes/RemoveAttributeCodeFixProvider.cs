using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AutoConstructor.Generator
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveAttributeCodeFixProvider)), Shared]
    public class RemoveAttributeCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            ClassWithoutFieldsToInjectAnalyzer.DiagnosticId,
            IgnoreAttributeOnNonProcessedFieldAnalyzer.DiagnosticId,
            InjectAttributeOnIgnoredFieldAnalyzer.DiagnosticId,
            IgnoreOrInjectAttributeOnClassWithoutAttributeAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            Diagnostic? diagnostic = context.Diagnostics[0];
            TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

            string attributeName = root?.FindToken(diagnosticSpan.Start).ToFullString() ?? "unknown";
            MemberDeclarationSyntax? node = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().First();
            if (node is not null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: $"Remove {attributeName} attribute",
                        createChangedDocument: c => RemoveAttributeAsync(context.Document, node, diagnosticSpan, c),
                        equivalenceKey: $"Remove {attributeName} attribute"),
                    diagnostic);
            }
        }

        private static async Task<Document> RemoveAttributeAsync(Document document, MemberDeclarationSyntax declaration, TextSpan location, CancellationToken cancellationToken)
        {
            SyntaxNode? oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (oldRoot is not null)
            {
                MemberDeclarationSyntax newDeclaration = RemoveAttributeFromSyntax(declaration, location);
                SyntaxNode? newRoot = oldRoot.ReplaceNode(declaration, newDeclaration);
                if (newRoot is not null)
                {
                    return document.WithSyntaxRoot(newRoot);
                }
            }

            throw new InvalidOperationException("Cannot fix code.");
        }

        private static MemberDeclarationSyntax RemoveAttributeFromSyntax(MemberDeclarationSyntax node, TextSpan location)
        {
            var newAttributes = new SyntaxList<AttributeListSyntax>();
            foreach (AttributeListSyntax attributeList in node.AttributeLists)
            {
                List<AttributeSyntax> nodesToRemove =
                    attributeList
                    .Attributes
                    .Where(attribute => attribute.Span == location)
                    .ToList();

                if (nodesToRemove.Count != attributeList.Attributes.Count)
                {
                    AttributeListSyntax? newAttributeList = attributeList.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia);
                    if (newAttributeList is not null)
                    {
                        newAttributes = newAttributes.Add(newAttributeList);
                    }
                }
            }

            SyntaxTriviaList leadingTrivia = node.GetLeadingTrivia();
            node = node.WithAttributeLists(newAttributes);
            node = node.WithLeadingTrivia(leadingTrivia);
            return node;
        }
    }
}
