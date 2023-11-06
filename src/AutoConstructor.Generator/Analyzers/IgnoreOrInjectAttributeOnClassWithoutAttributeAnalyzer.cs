using System.Collections.Immutable;
using AutoConstructor.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoConstructor.Generator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IgnoreOrInjectAttributeOnClassWithoutAttributeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.IgnoreOrInjectAttributeOnClassWithoutAttributeRule);

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

        var fields = symbol.GetMembers().OfType<IFieldSymbol>()
            .Where(x => x.HasAttribute(Source.IgnoreAttributeFullName) || x.HasAttribute(Source.InjectAttributeFullName))
            .ToList();

        if (symbol.GetAttribute(Source.AttributeFullName) is null && fields.Count > 0)
        {
            foreach (IFieldSymbol field in fields)
            {
                foreach (string attributeName in new[] { Source.IgnoreAttributeFullName, Source.InjectAttributeFullName })
                {
                    if (field.GetAttribute(attributeName) is AttributeData attr)
                    {
                        SyntaxReference? propertyTypeIdentifier = attr.ApplicationSyntaxReference;
                        if (propertyTypeIdentifier is not null)
                        {
                            var location = Location.Create(propertyTypeIdentifier.SyntaxTree, propertyTypeIdentifier.Span);
                            var diagnostic = Diagnostic.Create(DiagnosticDescriptors.IgnoreOrInjectAttributeOnClassWithoutAttributeRule, location);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }
}
