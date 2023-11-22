using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoConstructor.Generator.Extensions;

internal static class SymbolExtension
{
    public static bool IsInitialized(this IFieldSymbol symbol)
    {
        _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

        bool isInitialized = false;
        if (symbol.AssociatedSymbol is not null)
        {
            isInitialized = symbol.AssociatedSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is PropertyDeclarationSyntax { Initializer: not null };
        }

        return isInitialized ||
            symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is VariableDeclaratorSyntax { Initializer: not null };
    }

    public static bool HasAttribute(this ISymbol symbol, string attributeName)
    {
        _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

        return symbol.GetAttribute(attributeName) is not null;
    }

    public static AttributeData? GetAttribute(this ISymbol symbol, string attributeName)
    {
        _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

        return symbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass?.Name == attributeName);
    }

    public static bool CanBeInjected(this ISymbol symbol)
    {
        _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

        return symbol.CanBeReferencedByName
            || (!symbol.CanBeReferencedByName && symbol is IFieldSymbol { AssociatedSymbol: not null });
    }

    public static IEnumerable<INamedTypeSymbol> GetContainingTypes(this INamedTypeSymbol symbol)
    {
        _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

        if (symbol.ContainingType is not null)
        {
            foreach (INamedTypeSymbol item in symbol.ContainingType.GetContainingTypes())
            {
                yield return item;
            }

            yield return symbol.ContainingType;
        }
    }

    public static string GenerateFilename(this INamedTypeSymbol symbol)
    {
        string filename = string.Empty;
        if (symbol.ContainingType is not null)
        {
            filename = $"{string.Join(".", symbol.GetContainingTypes().Select(c => c.Name))}.";
        }

        filename += $"{symbol.Name}";

        if (symbol.TypeArguments.Length > 0)
        {
            filename += string.Concat(symbol.TypeArguments.Select(tp => $".{tp.Name}"));
        }

        if (!symbol.ContainingNamespace.IsGlobalNamespace)
        {
            filename = $"{symbol.ContainingNamespace.ToDisplayString()}.{filename}";
        }

        return filename;
    }
}
