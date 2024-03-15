using System.Collections.Immutable;
using AutoConstructor.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoConstructor.Generator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DefaultBaseAttributeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.MultipleDefaultBaseRule);

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

        if (symbol.DeclaringSyntaxReferences[0].GetSyntax() is TypeDeclarationSyntax)
        {
            var baseConstructors = symbol.Constructors
                .Where(x => x.HasAttribute(Source.DefaultBaseAttributeFullName))
                .ToList();

            if (baseConstructors.Count >= 2)
            {
                foreach (IMethodSymbol baseConstructor in baseConstructors)
                {
                    if (baseConstructor.GetAttribute(Source.DefaultBaseAttributeFullName) is AttributeData attr)
                    {
                        SyntaxReference? propertyTypeIdentifier = attr.ApplicationSyntaxReference;
                        if (propertyTypeIdentifier is not null)
                        {
                            var location = Location.Create(propertyTypeIdentifier.SyntaxTree, propertyTypeIdentifier.Span);
                            var diagnostic = Diagnostic.Create(DiagnosticDescriptors.MultipleDefaultBaseRule, location);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }
}
