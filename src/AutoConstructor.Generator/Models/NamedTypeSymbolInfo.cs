using System.Collections.Immutable;
using AutoConstructor.Generator.Core;
using Microsoft.CodeAnalysis;

namespace AutoConstructor.Generator.Models;

internal record NamedTypeSymbolInfo(string Name, bool IsStatic, EquatableArray<string> TypeParameters)
{
    public NamedTypeSymbolInfo(INamedTypeSymbol namedTypeSymbol)
        : this(namedTypeSymbol.Name, namedTypeSymbol.IsStatic, namedTypeSymbol.TypeParameters.Select(t => t.Name).ToImmutableArray())
    {
    }
}
