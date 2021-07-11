using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoConstructor.Generator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ClassWithoutFieldsToInjectAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ACONS02";

        private static readonly DiagnosticDescriptor Rule = new(
            DiagnosticId,
            $"Remove {Source.AttributeFullName}",
            $"{Source.AttributeFullName} has no effect on a class without fields to inject",
            "Usage",
            DiagnosticSeverity.Warning,
            true,
            null,
            "https://github.com/k94ll13nn3/AutoConstructor/tree/main/src/AutoConstructor.Generator/Analyzers/ClassWithoutFieldsToInjectAnalyzer.cs",
            WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            _ = context ?? throw new ArgumentNullException(nameof(context));

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;

            if (symbol.GetAttribute(Source.AttributeFullName) is AttributeData attr)
            {
                var fields = symbol.GetMembers().OfType<IFieldSymbol>()
                    .Where(x => x.CanBeReferencedByName && !x.IsStatic && x.IsReadOnly && !x.IsInitialized() && !x.HasAttribute(Source.IgnoreAttributeFullName))
                    .ToList();

                if (fields.Count == 0)
                {
                    SyntaxReference? propertyTypeIdentifier = attr.ApplicationSyntaxReference;
                    if (propertyTypeIdentifier is not null)
                    {
                        var location = Location.Create(propertyTypeIdentifier.SyntaxTree, propertyTypeIdentifier.Span);
                        var diagnostic = Diagnostic.Create(Rule, location);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}
