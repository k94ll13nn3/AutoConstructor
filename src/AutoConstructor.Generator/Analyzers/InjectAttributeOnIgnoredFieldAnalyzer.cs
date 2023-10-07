using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoConstructor.Generator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InjectAttributeOnIgnoredFieldAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.InjectAttributeOnIgnoredFieldRule);

    public override void Initialize(AnalysisContext context)
    {
        _ = context ?? throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Field);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = (IFieldSymbol)context.Symbol;

        if (symbol.GetAttribute(Source.InjectAttributeFullName, context.Compilation) is AttributeData attr
            && (!symbol.CanBeInjected(context.Compilation) || symbol.IsStatic || !symbol.IsReadOnly || symbol.IsInitialized() || symbol.HasAttribute(Source.IgnoreAttributeFullName, context.Compilation)))
        {
            SyntaxReference? propertyTypeIdentifier = attr.ApplicationSyntaxReference;
            if (propertyTypeIdentifier is not null)
            {
                var location = Location.Create(propertyTypeIdentifier.SyntaxTree, propertyTypeIdentifier.Span);
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.InjectAttributeOnIgnoredFieldRule, location);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
