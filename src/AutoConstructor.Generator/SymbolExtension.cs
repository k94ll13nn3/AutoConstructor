using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoConstructor.Generator
{
    public static class SymbolExtension
    {
        public static bool IsInitialized(this IFieldSymbol symbol)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            return (symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax)?.Initializer != null;
        }

        public static bool HasAttribute(this ISymbol symbol, string attributeName, Compilation compilation)
        {
            _ = compilation ?? throw new ArgumentNullException(nameof(compilation));

            return symbol.GetAttribute(attributeName, compilation) is not null;
        }

        public static AttributeData? GetAttribute(this ISymbol symbol, string attributeName, Compilation compilation)
        {
            _ = compilation ?? throw new ArgumentNullException(nameof(compilation));

            INamedTypeSymbol? attributeSymbol = compilation.GetTypeByMetadataName(attributeName);
            return symbol?.GetAttributes().FirstOrDefault(ad => ad.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) == true);
        }
    }
}
