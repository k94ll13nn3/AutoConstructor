using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoConstructor.Generator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ClassWithoutFieldsToInjectAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.ClassWithoutFieldsToInjectRule);

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

        if (symbol.GetAttribute(Source.AttributeFullName, context.Compilation) is AttributeData attr)
        {
            var fields = symbol.GetMembers().OfType<IFieldSymbol>()
                .Where(x => x.CanBeInjected(context.Compilation) && !x.IsStatic && x.IsReadOnly && !x.IsInitialized() && !x.HasAttribute(Source.IgnoreAttributeFullName, context.Compilation))
                .ToList();

            if (fields.Count == 0)
            {
                SyntaxReference? propertyTypeIdentifier = attr.ApplicationSyntaxReference;
                if (propertyTypeIdentifier is not null)
                {
                    var location = Location.Create(propertyTypeIdentifier.SyntaxTree, propertyTypeIdentifier.Span);
                    var diagnostic = Diagnostic.Create(DiagnosticDescriptors.ClassWithoutFieldsToInjectRule, location);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
