using AutoConstructor.Generator.Core;
using AutoConstructor.Generator.Models;

namespace AutoConstructor.Generator;

internal static class IndentedTextWriterExtension
{
    public static void WriteNamedTypeSymbolInfoLine(this IndentedTextWriter writer, NamedTypeSymbolInfo symbol)
    {
        if (symbol.IsStatic)
        {
            writer.Write("static ");
        }

        writer.Write($"partial class {symbol.Name}");

        if (!symbol.TypeParameters.IsEmpty)
        {
            writer.Write("<");
            writer.Write(string.Join(", ", symbol.TypeParameters));
            writer.Write(">");
        }

        writer.WriteLine();
    }
}
