using System.Collections.Immutable;
using AutoConstructor.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            bool addParameterless = symbol.DeclaringSyntaxReferences[0].GetSyntax() is TypeDeclarationSyntax
                                    && attr.AttributeConstructor?.Parameters.Length > 0
                                    && attr.GetBoolParameterValue("addParameterless");
            if (!addParameterless)
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
        (IMethodSymbol? constructor, INamedTypeSymbol? baseType) = symbol.GetPreferredBaseConstructorOrBaseType();
        if (constructor is not null)
        {
            return constructor.Parameters.Length > 0;
        }
        else if (baseType is not null)
        {
            return SymbolHasFields(baseType) || ParentHasFields(compilation, baseType);
        }

        return false;
    }
}
