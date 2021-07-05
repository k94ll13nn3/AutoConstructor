using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoConstructor.Generator
{
    /// <summary>
    /// Created on demand before each generation pass.
    /// </summary>
    internal class SyntaxReceiver : ISyntaxReceiver
    {
        public List<(ClassDeclarationSyntax candidateClass, bool isPartial)> CandidateClasses { get; } = new List<(ClassDeclarationSyntax, bool)>();

        /// <summary>
        /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation.
        /// </summary>
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Only check:
            // - classes
            // - with the "partial" keyword
            // - with the wanted attribute
            if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax
                && classDeclarationSyntax.AttributeLists.Any(a =>
                    a.Attributes.Any(b =>
                        b.Name.ToString() == AutoConstructorGenerator.AttributeName || b.Name.ToString() == AutoConstructorGenerator.AttributeFullName)))
            {
                CandidateClasses.Add((classDeclarationSyntax, classDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword)));
            }
        }
    }
}
