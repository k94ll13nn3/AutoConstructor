using System.Collections.Immutable;
using AutoConstructor.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoConstructor.Generator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InitializerMethodAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DiagnosticDescriptors.MultipleInitializerMethodsRule,
        DiagnosticDescriptors.InitializerMethodMustBeParameterlessRule,
        DiagnosticDescriptors.InitializerMethodMustReturnVoidRule);

    public override void Initialize(AnalysisContext context)
    {
        _ = context ?? throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzeInitializerMethod, SymbolKind.Method);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;

        if (symbol.DeclaringSyntaxReferences[0].GetSyntax() is ClassDeclarationSyntax && symbol.HasAttribute(Source.AttributeFullName))
        {
            var initializerMethods = symbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(x => x.HasAttribute(Source.InitializerAttributeFullName))
                .ToList();

            if (initializerMethods.Count >= 2)
            {
                foreach (IMethodSymbol initializerMethod in initializerMethods.Skip(1))
                {
                    if (initializerMethod.GetAttribute(Source.InitializerAttributeFullName) is AttributeData attr)
                    {
                        SyntaxReference? propertyTypeIdentifier = attr.ApplicationSyntaxReference;
                        if (propertyTypeIdentifier is not null)
                        {
                            var location = Location.Create(propertyTypeIdentifier.SyntaxTree, propertyTypeIdentifier.Span);
                            var diagnostic = Diagnostic.Create(DiagnosticDescriptors.MultipleInitializerMethodsRule, location);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }

    private void AnalyzeInitializerMethod(SymbolAnalysisContext context)
    {
        var symbol = (IMethodSymbol)context.Symbol;

        if (symbol.HasAttribute(Source.InitializerAttributeFullName))
        {
            if (!symbol.ReturnsVoid)
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.InitializerMethodMustReturnVoidRule, symbol.Locations[0]);
                context.ReportDiagnostic(diagnostic);
            }

            if (!symbol.Parameters.IsEmpty)
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.InitializerMethodMustBeParameterlessRule, symbol.Locations[0]);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
