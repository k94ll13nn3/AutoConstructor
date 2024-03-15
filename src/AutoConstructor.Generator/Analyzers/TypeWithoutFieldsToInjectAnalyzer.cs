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

        // Check if base type is not object (ie. its base type is null)
        if (baseType?.BaseType is not null)
        {
            // Check if there is a defined preferedBaseConstructor
            IMethodSymbol? preferedBaseConstructor = baseType.Constructors.FirstOrDefault(d => d.HasAttribute(Source.DefaultBaseAttributeFullName));
            if (preferedBaseConstructor is not null)
            {
                return preferedBaseConstructor.Parameters.Length > 0;
            }
            // If symbol is in same assembly, the generated constructor is not visible as it might not be yet generated.
            // If not is the same assembly, is does not matter if the constructor was generated or not.
            else if (SymbolEqualityComparer.Default.Equals(baseType.ContainingAssembly, symbol.ContainingAssembly) && baseType.HasAttribute(Source.AttributeFullName))
            {
                AttributeData? attributeData = baseType.GetAttribute(Source.AttributeFullName);
                if (attributeData?.GetBoolParameterValue("addDefaultBaseAttribute") is true)
                {
                    return SymbolHasFields(baseType) || ParentHasFields(compilation, baseType);
                }
                else if (baseType.Constructors.Count(d => !d.IsStatic) == 1)
                {
                    return SymbolHasFields(baseType) || ParentHasFields(compilation, baseType);
                }
            }
            // Check if there is only one constructor.
            else if (baseType.Constructors.Count(d => !d.IsStatic) == 1)
            {
                IMethodSymbol constructor = baseType.Constructors.Single(d => !d.IsStatic);
                return constructor.Parameters.Length > 0;
            }
        }

        return false;
    }
}
