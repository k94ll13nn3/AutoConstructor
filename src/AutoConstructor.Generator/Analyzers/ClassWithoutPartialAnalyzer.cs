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
    public class ClassWithoutPartialAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ACONS01";

        private static readonly DiagnosticDescriptor Rule = new(
            DiagnosticId,
            "Couldn't generate consctructor",
            $"Type decorated with {Source.AttributeFullName} must be also declared partial",
            "Usage",
            DiagnosticSeverity.Warning,
            true,
            null,
            $"https://github.com/k94ll13nn3/AutoConstructor#{DiagnosticId}",
            WellKnownDiagnosticTags.Build);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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
                        b.Name.ToString() == Source.AttributeName || b.Name.ToString() == Source.AttributeFullName))
                && !classDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                var diagnostic = Diagnostic.Create(Rule, classDeclarationSyntax.Identifier.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
