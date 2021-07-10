using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoConstructor.Generator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ClassWithoutFieldsToInjectAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ACONS02";

        private static readonly DiagnosticDescriptor Rule = new(
            DiagnosticId,
            $"Remove {AutoConstructorGenerator.AttributeFullName}",
            $"{AutoConstructorGenerator.AttributeFullName} is not needed on class without fields to inject",
            "Usage",
            DiagnosticSeverity.Warning,
            true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

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

            if (HasAttribute(symbol) is AttributeData attr)
            {
                var fields = symbol.GetMembers().OfType<IFieldSymbol>()
                    .Where(x => x.CanBeReferencedByName && !x.IsStatic && x.IsReadOnly && !IsInitialized(x) && !HasIgnoreAttribute(x))
                    .ToList();

                if (fields.Count == 0)
                {
                    SyntaxReference? propertyTypeIdentifier = attr.ApplicationSyntaxReference;
                    if (propertyTypeIdentifier is not null)
                    {
                        var location = Location.Create(propertyTypeIdentifier.SyntaxTree, propertyTypeIdentifier.Span);
                        var diagnostic = Diagnostic.Create(Rule, location, symbol.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }

            AttributeData? HasAttribute(ISymbol symbol)
            {
                INamedTypeSymbol? ignoreAttributeSymbol = context.Compilation.GetTypeByMetadataName(AutoConstructorGenerator.AttributeFullName);
                return symbol?.GetAttributes().FirstOrDefault(ad => ad.AttributeClass?.Equals(ignoreAttributeSymbol, SymbolEqualityComparer.Default) == true);
            }

            static bool IsInitialized(IFieldSymbol symbol)
            {
                return (symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax)?.Initializer != null;
            }

            bool HasIgnoreAttribute(IFieldSymbol symbol)
            {
                INamedTypeSymbol? ignoreAttributeSymbol = context.Compilation.GetTypeByMetadataName(AutoConstructorGenerator.IgnoreAttributeFullName);
                AttributeData? attributeData = symbol?.GetAttributes().FirstOrDefault(ad => ad.AttributeClass?.Equals(ignoreAttributeSymbol, SymbolEqualityComparer.Default) == true);
                return attributeData is not null;
            }
        }
    }
}
