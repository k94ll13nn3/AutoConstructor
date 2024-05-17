using System.Collections.Immutable;
using AutoConstructor.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoConstructor.Generator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypeWithoutBaseParameterlessConstructorAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.TypeWithoutBaseParameterlessConstructorRule);

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
            bool addParameterLess = symbol.DeclaringSyntaxReferences[0].GetSyntax() is TypeDeclarationSyntax
                                    && symbol.GetAttribute(Source.AttributeFullName) is AttributeData attribute
                                    && attribute.AttributeConstructor?.Parameters.Length > 0
                                    && attribute.GetBoolParameterValue("addParameterless");
            if (addParameterLess)
            {
                bool baseHasAccessibleParameterlessConstructor = BaseHasAccessibleParameterlessConstructor(symbol);
                if (!baseHasAccessibleParameterlessConstructor)
                {
                    SyntaxReference? propertyTypeIdentifier = attr.ApplicationSyntaxReference;
                    if (propertyTypeIdentifier is not null)
                    {
                        var location = Location.Create(propertyTypeIdentifier.SyntaxTree, propertyTypeIdentifier.Span);
                        var diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeWithoutBaseParameterlessConstructorRule, location);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }

    private static bool BaseHasAccessibleParameterlessConstructor(INamedTypeSymbol symbol)
    {
        INamedTypeSymbol? baseType = symbol.BaseType;
        if (baseType?.BaseType is null)
        {
            return true;
        }
        IMethodSymbol? acceptableConstructor = baseType.Constructors.FirstOrDefault(d =>
            !d.IsStatic && d.DeclaredAccessibility != Accessibility.Private && d.Parameters.Length == 0);
        return acceptableConstructor != null;
    }
}
