using AutoConstructor.Generator.Core;
using AutoConstructor.Generator.Models;
using Microsoft.CodeAnalysis;

#pragma warning disable IDE0072

namespace AutoConstructor.Generator.Extensions;

internal static class IndentedTextWriterExtensions
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

    public static void WriteSummary(this IndentedTextWriter writer, string comment)
    {
        writer.WriteLine("/// <summary>");
        writer.WriteLine($"/// {comment}");
        writer.WriteLine("/// </summary>");
    }

    public static void WriteGeneratedCodeAttribute(this IndentedTextWriter writer)
    {
        writer.WriteLine($"""[global::System.CodeDom.Compiler.GeneratedCodeAttribute("{nameof(AutoConstructor)}", "{AutoConstructorGenerator.GeneratorVersion}")]""");
    }

    public static void WriteFileHeader(this IndentedTextWriter writer)
    {
        writer.WriteLine("//------------------------------------------------------------------------------");
        writer.WriteLine("// <auto-generated>");
        writer.WriteLine($"//     This code was generated by the {nameof(AutoConstructor)} source generator.");
        writer.WriteLine("//");
        writer.WriteLine("//     Changes to this file may cause incorrect behavior and will be lost if");
        writer.WriteLine("//     the code is regenerated.");
        writer.WriteLine("// </auto-generated>");
        writer.WriteLine("//------------------------------------------------------------------------------");
    }
}
