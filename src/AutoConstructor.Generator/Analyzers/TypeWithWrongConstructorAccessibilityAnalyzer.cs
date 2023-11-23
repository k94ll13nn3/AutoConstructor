using System.Collections.Immutable;
using AutoConstructor.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoConstructor.Generator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypeWithWrongConstructorAccessibilityAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.TypeWithWrongConstructorAccessibilityRule);

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

        if (symbol.DeclaringSyntaxReferences[0].GetSyntax() is TypeDeclarationSyntax typeDeclarationSyntax
            && symbol.GetAttribute(Source.AttributeFullName) is AttributeData attribute
            && attribute.AttributeConstructor?.Parameters.Length > 0
            && attribute.GetParameterValue<string>("accessibility") is string { Length: > 0 } accessibilityValue
            && !AutoConstructorGenerator.ConstuctorAccessibilities.Contains(accessibilityValue))
        {
            var diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeWithWrongConstructorAccessibilityRule, typeDeclarationSyntax.Identifier.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
