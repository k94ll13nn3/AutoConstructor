using AutoConstructor.Generator.Core;
using AutoConstructor.Generator.Models;
using Microsoft.CodeAnalysis;

#pragma warning disable IDE0072

namespace AutoConstructor.Generator.Extensions;

internal static class IndentedTextWriterExtension
{
    public static void WriteNamedTypeSymbolInfoLine(this IndentedTextWriter writer, NamedTypeSymbolInfo symbol)
    {
        if (symbol.IsStatic)
        {
            writer.Write("static ");
        }

        writer.Write("partial ");
        writer.Write(symbol.Kind switch
        {
            TypeKind.Struct => "struct",
            TypeKind.Interface => "interface",
            _ => "class"
        });
        writer.Write(" ");
        writer.Write(symbol.Name);

        if (!symbol.TypeParameters.IsEmpty)
        {
            writer.Write("<");
            writer.Write(string.Join(", ", symbol.TypeParameters));
            writer.Write(">");
        }

        writer.WriteLine();
    }

    public static void StartBlock(this IndentedTextWriter writer)
    {
        writer.WriteLine("{");
        writer.IncreaseIndent();
    }

    public static void EndBlock(this IndentedTextWriter writer)
    {
        writer.DecreaseIndent();
        writer.WriteLine("}");
    }
}
