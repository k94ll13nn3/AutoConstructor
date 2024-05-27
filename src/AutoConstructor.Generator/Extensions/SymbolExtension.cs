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

    public static (IMethodSymbol? constructor, INamedTypeSymbol? baseType) GetPreferredBaseConstructorOrBaseType(this INamedTypeSymbol symbol)
    {
        INamedTypeSymbol? baseType = symbol.BaseType;

        // Check if base type is not object (ie. its base type is null)
        if (baseType?.BaseType is not null)
        {
            // Check if there is a defined preferredBaseConstructor
            IMethodSymbol? preferredBaseConstructor = baseType.Constructors.FirstOrDefault(d => d.HasAttribute(Source.DefaultBaseAttributeFullName));
            if (preferredBaseConstructor is not null)
            {
                return (preferredBaseConstructor, null);
            }
            // If symbol is in same assembly, the generated constructor is not visible as it might not be yet generated.
            // If not is the same assembly, is does not matter if the constructor was generated or not.
            else if (SymbolEqualityComparer.Default.Equals(baseType.ContainingAssembly, symbol.ContainingAssembly) && baseType.HasAttribute(Source.AttributeFullName))
            {
                AttributeData? attributeData = baseType.GetAttribute(Source.AttributeFullName);
                if (attributeData?.GetBoolParameterValue("addDefaultBaseAttribute") is true)
                {
                    return (null, baseType);
                }
                else if (baseType.Constructors.Count(d => !d.IsStatic) == 1)
                {
                    return (null, baseType);
                }
            }
            // Check if there is only one constructor.
            else if (baseType.Constructors.Count(d => !d.IsStatic) == 1)
            {
                IMethodSymbol constructor = baseType.Constructors.Single(d => !d.IsStatic);
                return (constructor, null);
            }
        }

        return (null, null);
    }
}
