using System.Collections.Immutable;
using AutoConstructor.Generator.Core;
using Microsoft.CodeAnalysis;

namespace AutoConstructor.Generator.Models;

internal record NamedTypeSymbolInfo(TypeKind Kind, string Name, bool IsStatic, EquatableArray<string> TypeParameters)
{
    public NamedTypeSymbolInfo(INamedTypeSymbol namedTypeSymbol)
        : this(namedTypeSymbol.TypeKind, namedTypeSymbol.Name, namedTypeSymbol.IsStatic, namedTypeSymbol.TypeParameters.Select(t => t.Name).ToImmutableArray())
    {
    }
}
