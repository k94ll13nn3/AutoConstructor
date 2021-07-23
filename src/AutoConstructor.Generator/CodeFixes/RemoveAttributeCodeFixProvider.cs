using System;
using System.Collections.Generic;
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveAttributeCodeFixProvider)), Shared]
    public class RemoveAttributeCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            ClassWithoutFieldsToInjectAnalyzer.DiagnosticId,
            IgnoreAttributeOnNonProcessedFieldAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            Diagnostic? diagnostic = context.Diagnostics[0];
            TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

            string attributeName = diagnostic.Id switch
            {
                ClassWithoutFieldsToInjectAnalyzer.DiagnosticId => Source.AttributeName,
                IgnoreAttributeOnNonProcessedFieldAnalyzer.DiagnosticId => Source.IgnoreAttributeName,
                _ => throw new InvalidOperationException("Invalid diagnostic."),
            };

            MemberDeclarationSyntax? node = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().First();
            if (node is not null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: $"Remove {attributeName}Attribute",
                        createChangedDocument: c => RemoveAttributeAsync(context.Document, node, attributeName, c),
                        equivalenceKey: $"Remove {attributeName}Attribute"),
                    diagnostic);
            }
        }

        private static async Task<Document> RemoveAttributeAsync(Document document, MemberDeclarationSyntax declaration, string attributeName, CancellationToken cancellationToken)
        {
            SyntaxNode? oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (oldRoot is not null)
            {
                SyntaxNode newDeclaration = RemoveAttributeFromSyntax(declaration, attributeName);
                SyntaxNode? newRoot = oldRoot.ReplaceNode(declaration, newDeclaration);
                if (newRoot is not null)
                {
                    return document.WithSyntaxRoot(newRoot);
                }
            }

            throw new InvalidOperationException("Cannot fix code.");
        }

        private static SyntaxNode RemoveAttributeFromSyntax(MemberDeclarationSyntax node, string attributeName)
        {
            var newAttributes = new SyntaxList<AttributeListSyntax>();
            foreach (AttributeListSyntax attributeList in node.AttributeLists)
            {
                List<AttributeSyntax> nodesToRemove =
                    attributeList
                    .Attributes
                    .Where(
                        attribute => attribute.Name.ToString() == attributeName || attribute.Name.ToString() == $"{attributeName}Attribute")
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
