using Microsoft.CodeAnalysis.CSharp;

namespace AutoConstructor.Generator.Extensions;

internal static class StringExtensions
{
    public static string SanitizeReservedKeyword(this string parameterName)
    {
        return SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(parameterName)) ? "@" + parameterName : parameterName;
    }
}
