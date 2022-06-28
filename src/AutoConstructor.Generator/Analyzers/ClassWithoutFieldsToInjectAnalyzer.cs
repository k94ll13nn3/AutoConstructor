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
            bool hasFields = SymbolHasFields(context.Compilation, symbol) || ParentHasFields(context.Compilation, symbol);
            if (!hasFields)
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

    private static bool SymbolHasFields(Compilation compilation, INamedTypeSymbol symbol)
    {
        return symbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Any(x => x.CanBeInjected(compilation)
                && !x.IsStatic
                && x.IsReadOnly
                && !x.IsInitialized()
                && !x.HasAttribute(Source.IgnoreAttributeFullName, compilation));
    }

    private static bool ParentHasFields(Compilation compilation, INamedTypeSymbol symbol)
    {
        INamedTypeSymbol? baseType = symbol.BaseType;

        if (baseType?.BaseType is not null && baseType?.Constructors.Length == 1)
        {
            IMethodSymbol constructor = baseType.Constructors[0];
            return baseType?.HasAttribute(Source.AttributeFullName, compilation) is true
                ? SymbolHasFields(compilation, baseType) || ParentHasFields(compilation, baseType)
                : constructor.Parameters.Length > 0;
        }

        return false;
    }
}
