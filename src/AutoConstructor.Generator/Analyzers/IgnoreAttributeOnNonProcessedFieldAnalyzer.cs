using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoConstructor.Generator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class IgnoreAttributeOnNonProcessedFieldAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ACONS03";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        $"Remove {Source.IgnoreAttributeFullName}",
        $"{Source.IgnoreAttributeFullName} has no effect on a field that cannot be injected",
        "Usage",
        DiagnosticSeverity.Info,
        true,
        null,
        $"https://github.com/k94ll13nn3/AutoConstructor#{DiagnosticId}",
        WellKnownDiagnosticTags.Unnecessary);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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

        if (symbol.GetAttribute(Source.IgnoreAttributeFullName, context.Compilation) is AttributeData attr
            && (!symbol.CanBeReferencedByName || symbol.IsStatic || !symbol.IsReadOnly || symbol.IsInitialized()))
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
