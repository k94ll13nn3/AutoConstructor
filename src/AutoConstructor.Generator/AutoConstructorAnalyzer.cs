using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoConstructor.Generator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AutoConstructorAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ACONS01";

        private static readonly DiagnosticDescriptor Rule = new(
            DiagnosticId,
            "Couldn't generate consctructo",
            $"Type decorated with {AutoConstructorGenerator.AttributeName} must be also declared partial",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            _ = context ?? throw new ArgumentNullException(nameof(context));

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is ClassDeclarationSyntax classDeclarationSyntax
                && classDeclarationSyntax.AttributeLists.Any(a =>
                    a.Attributes.Any(b =>
                        b.Name.ToString() == AutoConstructorGenerator.AttributeName || b.Name.ToString() == AutoConstructorGenerator.AttributeFullName))
                && !classDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                var diagnostic = Diagnostic.Create(Rule, classDeclarationSyntax.Identifier.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
