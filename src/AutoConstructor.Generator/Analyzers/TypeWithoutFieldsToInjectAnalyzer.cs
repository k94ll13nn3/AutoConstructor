using System.Collections.Immutable;
using AutoConstructor.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoConstructor.Generator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypeWithoutFieldsToInjectAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.TypeWithoutFieldsToInjectRule);

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
            bool hasFields = SymbolHasFields(symbol) || ParentHasFields(context.Compilation, symbol);
            if (!hasFields)
            {
                SyntaxReference? propertyTypeIdentifier = attr.ApplicationSyntaxReference;
                if (propertyTypeIdentifier is not null)
                {
                    var location = Location.Create(propertyTypeIdentifier.SyntaxTree, propertyTypeIdentifier.Span);
                    var diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeWithoutFieldsToInjectRule, location);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static bool SymbolHasFields(INamedTypeSymbol symbol)
    {
        return symbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Any(x => x.CanBeInjected()
                && !x.IsStatic
                && x.IsReadOnly
                && !x.IsInitialized()
                && !x.HasAttribute(Source.IgnoreAttributeFullName));
    }

    private static bool ParentHasFields(Compilation compilation, INamedTypeSymbol symbol)
    {
        INamedTypeSymbol? baseType = symbol.BaseType;

        if (baseType?.BaseType is not null && baseType.Constructors.Count(d => !d.IsStatic) == 1)
        {
            IMethodSymbol constructor = baseType.Constructors.Single(d => !d.IsStatic);
            return (SymbolEqualityComparer.Default.Equals(baseType.ContainingAssembly, symbol.ContainingAssembly) && baseType.HasAttribute(Source.AttributeFullName))
                ? SymbolHasFields(baseType) || ParentHasFields(compilation, baseType)
                : constructor.Parameters.Length > 0;
        }

        return false;
    }
}
