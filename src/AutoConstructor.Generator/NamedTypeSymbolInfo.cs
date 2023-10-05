using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AutoConstructor.Generator;

internal record NamedTypeSymbolInfo(string Name, bool IsStatic, EquatableArray<string> TypeParameters)
{
    public NamedTypeSymbolInfo(INamedTypeSymbol namedTypeSymbol)
        : this(namedTypeSymbol.Name, namedTypeSymbol.IsStatic, namedTypeSymbol.TypeParameters.Select(t => t.Name).ToImmutableArray())
    {
    }
}

internal record MainNamedTypeSymbolInfo(
    string Name,
    bool IsStatic,
    EquatableArray<string> TypeParameters,
    bool IsGlobalNamespace,
    string ContainingNamespace,
    EquatableArray<NamedTypeSymbolInfo> ContainingTypes,
    bool HasParameterlessConstructor,
    string Filename)
    : NamedTypeSymbolInfo(Name, IsStatic, TypeParameters)
{
    public MainNamedTypeSymbolInfo(INamedTypeSymbol namedTypeSymbol, bool hasParameterlessConstructor, string filename)
        : this(
            namedTypeSymbol.Name,
            namedTypeSymbol.IsStatic,
            namedTypeSymbol.TypeParameters.Select(t => t.Name).ToImmutableArray(),
            namedTypeSymbol.ContainingNamespace.IsGlobalNamespace,
            namedTypeSymbol.ContainingNamespace.ToDisplayString(),
            namedTypeSymbol.GetContainingTypes().Select(c => new NamedTypeSymbolInfo(c)).ToImmutableArray(),
            hasParameterlessConstructor,
            filename)
    {
    }
}
