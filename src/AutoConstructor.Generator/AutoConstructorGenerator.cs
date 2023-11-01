using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Xml;
using AutoConstructor.Generator.Core;
using AutoConstructor.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace AutoConstructor.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class AutoConstructorGenerator : IIncrementalGenerator
{
    internal static readonly string GeneratorVersion = typeof(CodeGenerator).Assembly.GetName().Version!.ToString();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register the attribute source
        context.RegisterPostInitializationOutput((i) =>
        {
            i.AddSource(Source.AttributeFullName, SourceText.From(Source.AttributeText, Encoding.UTF8));
            i.AddSource(Source.IgnoreAttributeFullName, SourceText.From(Source.IgnoreAttributeText, Encoding.UTF8));
            i.AddSource(Source.InjectAttributeFullName, SourceText.From(Source.InjectAttributeText, Encoding.UTF8));
        });

        IncrementalValuesProvider<(GeneratorExectutionResult? result, Options options)> valueProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                Source.AttributeFullName,
                static (node, _) => IsSyntaxTargetForGeneration(node),
                static (context, _) => Execute(context, (ClassDeclarationSyntax)context.TargetNode))
            .WithTrackingName("Execute")
            .Where(static m => m is not null)
            .Combine(context.AnalyzerConfigOptionsProvider.Select((c, _) => ParseOptions(c.GlobalOptions)))
            .WithTrackingName("Combine");

        context.RegisterSourceOutput(valueProvider, static (context, item) =>
        {
            if (item.result is not null)
            {
                if (item.result.ReportedDiagnostic is ReportedDiagnostic diagnostic)
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MistmatchTypesRule, Location.Create(diagnostic.FilePath, diagnostic.TextSpan, diagnostic.LineSpan)));
                }
                else if (item.result.Symbol is not null)
                {
                    string generatedCode = GenerateAutoConstructor(item.result.Symbol!, item.result.Fields, item.options);
                    context.AddSource($"{item.result.Symbol!.Filename}.g.cs", generatedCode);
                }
            }
        });
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclarationSyntax && classDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword);
    }

    private static Options ParseOptions(AnalyzerConfigOptions analyzerOptions)
    {
        bool generateConstructorDocumentation = false;
        if (analyzerOptions.TryGetValue("build_property.AutoConstructor_GenerateConstructorDocumentation", out string? generateConstructorDocumentationSwitch))
        {
            generateConstructorDocumentation = generateConstructorDocumentationSwitch.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        analyzerOptions.TryGetValue("build_property.AutoConstructor_ConstructorDocumentationComment", out string? constructorDocumentationComment);

        bool emitNullChecks = false;
        if (analyzerOptions.TryGetValue("build_property.AutoConstructor_DisableNullChecking", out string? disableNullCheckingSwitch))
        {
            emitNullChecks = disableNullCheckingSwitch.Equals("false", StringComparison.OrdinalIgnoreCase);
        }

        return new(generateConstructorDocumentation, constructorDocumentationComment, emitNullChecks);
    }

    private static GeneratorExectutionResult? Execute(GeneratorAttributeSyntaxContext context, ClassDeclarationSyntax classSyntax)
    {
        if (context.TargetSymbol is not INamedTypeSymbol symbol)
        {
            return null;
        }

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

        List<FieldInfo> concatenatedFields = GetFieldsFromSymbol(symbol);

        ExtractFieldsFromParent(symbol, concatenatedFields);

        EquatableArray<FieldInfo> fields = concatenatedFields.ToImmutableArray();

        if (fields.IsEmpty)
        {
            // No need to report diagnostic, taken care by the analyzers.
            return null;
        }

        if (fields.GroupBy(x => x.ParameterName).Any(g =>
            g.Where(c => c.Type is not null).Select(c => c.Type).Count() > 1
            || (g.All(c => c.Type is null) && g.Select(c => c.FallbackType).Count() > 1)

            ))
        {
            Location location = classSyntax.GetLocation();
            return new(null, fields, new(location.SourceTree!.FilePath, location.SourceSpan, location.GetLineSpan().Span));
        }

        bool hasParameterlessConstructor =
            classSyntax
            .ChildNodes()
            .Count(n => n is ConstructorDeclarationSyntax constructor && !constructor.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword))) == 1
            && symbol.Constructors.Any(d => !d.IsStatic && d.Parameters.Length == 0);

        return new(new MainNamedTypeSymbolInfo(symbol, hasParameterlessConstructor, filename), fields, null);
    }

    private static string GenerateAutoConstructor(MainNamedTypeSymbolInfo symbol, EquatableArray<FieldInfo> fields, Options options)
    {
        bool generateConstructorDocumentation = options.GenerateConstructorDocumentation;
        string? constructorDocumentationComment = options.ConstructorDocumentationComment;
        if (string.IsNullOrWhiteSpace(constructorDocumentationComment))
        {
            constructorDocumentationComment = "Initializes a new instance of the {0} class.";
        }

        var codeGenerator = new CodeGenerator();
        var writer = new IndentedTextWriter();
        writer.WriteLine("//------------------------------------------------------------------------------");
        writer.WriteLine("// <auto-generated>");
        writer.WriteLine($"//     This code was generated by the {nameof(AutoConstructor)} source generator.");
        writer.WriteLine("//");
        writer.WriteLine("//     Changes to this file may cause incorrect behavior and will be lost if");
        writer.WriteLine("//     the code is regenerated.");
        writer.WriteLine("// </auto-generated>");
        writer.WriteLine("//------------------------------------------------------------------------------");

        if (fields.Any(f => f.Nullable))
        {
            writer.WriteLine("#nullable enable");
        }

        if (generateConstructorDocumentation)
        {
            codeGenerator.AddDocumentation(string.Format(CultureInfo.InvariantCulture, constructorDocumentationComment, symbol.Name));
        }

        if (!symbol.IsGlobalNamespace)
        {
            writer.WriteLine($"namespace {symbol.ContainingNamespace}");
            writer.WriteLine("{");
            writer.IncreaseIndent();
        }

        foreach (NamedTypeSymbolInfo containingType in symbol.ContainingTypes)
        {
            codeGenerator.AddClass(containingType);
        }

        codeGenerator
            .AddClass(symbol)
            .AddConstructor(fields, symbol.HasParameterlessConstructor, options.EmitNullChecks);

        writer.WriteLine($"partial class {symbol.Name}");
        using (writer.WriteBlock())
        {
            writer.WriteLine($"""[global::System.CodeDom.Compiler.GeneratedCodeAttribute("{nameof(AutoConstructor)}", "{GeneratorVersion}")]""");
            writer.Write($"public {symbol.Name}(");
            writer.Write(string.Join(", ", fields.Select(field => $"{field.Type} {field.ParameterName}")));
            writer.WriteLine(")");

            using (writer.WriteBlock())
            {
                foreach (FieldInfo field in fields)
                {
                    writer.WriteLine($"this.{field.FieldName} = {field.ParameterName};");
                }
            }
        }

        if (!symbol.IsGlobalNamespace)
        {
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }

        return writer.ToString();
    }

    private static List<FieldInfo> GetFieldsFromSymbol(INamedTypeSymbol symbol)
    {
        return symbol.GetMembers().OfType<IFieldSymbol>()
            .Where(x => x.CanBeInjected()
                && !x.IsStatic
                && (x.IsReadOnly || IsPropertyWithExplicitInjection(x))
                && !x.IsInitialized()
                && !x.HasAttribute(Source.IgnoreAttributeFullName))
            .Select(GetFieldInfo)
            .ToList();

        static bool IsPropertyWithExplicitInjection(IFieldSymbol x)
        {
            return x.AssociatedSymbol is not null && x.HasAttribute(Source.InjectAttributeFullName);
        }
    }

    private static FieldInfo GetFieldInfo(IFieldSymbol fieldSymbol)
    {
        ITypeSymbol type = fieldSymbol.Type;
        ITypeSymbol? injectedType = type;
        string parameterName = fieldSymbol.Name.TrimStart('_');
        if (fieldSymbol.AssociatedSymbol is not null)
        {
            parameterName = char.ToLowerInvariant(fieldSymbol.AssociatedSymbol.Name[0]) + fieldSymbol.AssociatedSymbol.Name[1..];
        }

        string initializer = parameterName;
        string? documentationComment = (fieldSymbol.AssociatedSymbol ?? fieldSymbol).GetDocumentationCommentXml();
        string? summaryText = null;

        if (!string.IsNullOrWhiteSpace(documentationComment))
        {
            using var reader = new StringReader(documentationComment);
            var document = new XmlDocument();
            document.Load(reader);
            summaryText = document.SelectSingleNode("member/summary")?.InnerText.Trim();
        }

        AttributeData? attributeData = fieldSymbol.GetAttribute(Source.InjectAttributeFullName);
        if (attributeData is not null)
        {
            ImmutableArray<IParameterSymbol> parameters = attributeData.AttributeConstructor?.Parameters ?? ImmutableArray.Create<IParameterSymbol>();
            if (GetParameterValue<string>("parameterName", parameters, attributeData.ConstructorArguments) is string { Length: > 0 } parameterNameValue)
            {
                parameterName = parameterNameValue;
                initializer = parameterNameValue;
            }

            if (GetParameterValue<string>("initializer", parameters, attributeData.ConstructorArguments) is string { Length: > 0 } initializerValue)
            {
                initializer = initializerValue;
            }

            injectedType = GetParameterValue<INamedTypeSymbol>("injectedType", parameters, attributeData.ConstructorArguments);
        }

        return new FieldInfo(
            injectedType?.ToDisplayString(),
            parameterName,
            fieldSymbol.AssociatedSymbol?.Name ?? fieldSymbol.Name,
            initializer,
            type.ToDisplayString(),
            IsNullable(type),
            summaryText,
            type.IsReferenceType && type.NullableAnnotation != NullableAnnotation.Annotated,
            FieldType.Initialized);
    }

    private static T? GetParameterValue<T>(string parameterName, ImmutableArray<IParameterSymbol> parameters, ImmutableArray<TypedConstant> arguments)
        where T : class
    {
        return parameters.ToList().FindIndex(c => c.Name == parameterName) is int index and not -1
            ? (arguments[index].Value as T)
            : null;
    }

    private static void ExtractFieldsFromParent(INamedTypeSymbol symbol, List<FieldInfo> concatenatedFields)
    {
        INamedTypeSymbol? baseType = symbol.BaseType;

        // Check if base type is not object (ie. its base type is null) and that there is only one constructor.
        if (baseType?.BaseType is not null && baseType.Constructors.Count(d => !d.IsStatic) == 1)
        {
            IMethodSymbol constructor = baseType.Constructors.Single(d => !d.IsStatic);
            if (baseType.HasAttribute(Source.AttributeFullName))
            {
                ExtractFieldsFromGeneratedParent(concatenatedFields, baseType);
            }
            else
            {
                ExtractFieldsFromConstructedParent(concatenatedFields, constructor);
            }
        }
    }

    private static void ExtractFieldsFromConstructedParent(List<FieldInfo> concatenatedFields, IMethodSymbol constructor)
    {
        foreach (IParameterSymbol parameter in constructor.Parameters)
        {
            int index = concatenatedFields.FindIndex(p => p.ParameterName == parameter.Name);
            if (index != -1)
            {
                concatenatedFields[index] = concatenatedFields[index] with { FieldType = concatenatedFields[index].FieldType | FieldType.PassedToBase };
            }
            else
            {
                concatenatedFields.Add(new FieldInfo(
                    parameter.Type.ToDisplayString(),
                    parameter.Name,
                    string.Empty,
                    string.Empty,
                    parameter.Type.ToDisplayString(),
                    IsNullable(parameter.Type),
                    null,
                    false,
                    FieldType.PassedToBase));
            }
        }
    }

    private static void ExtractFieldsFromGeneratedParent(List<FieldInfo> concatenatedFields, INamedTypeSymbol symbol)
    {
        foreach (FieldInfo parameter in GetFieldsFromSymbol(symbol))
        {
            int index = concatenatedFields.FindIndex(p => p.ParameterName == parameter.ParameterName);
            if (index != -1)
            {
                concatenatedFields[index] = concatenatedFields[index] with { FieldType = concatenatedFields[index].FieldType | FieldType.PassedToBase };
            }
            else
            {
                concatenatedFields.Add(new FieldInfo(
                    parameter.Type,
                    parameter.ParameterName,
                    string.Empty,
                    string.Empty,
                    parameter.FallbackType,
                    parameter.Nullable,
                    null,
                    false,
                    FieldType.PassedToBase));
            }
        }

        ExtractFieldsFromParent(symbol, concatenatedFields);
    }

    private static bool IsNullable(ITypeSymbol typeSymbol)
    {
        bool isNullable = typeSymbol.IsReferenceType && typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
        if (typeSymbol is INamedTypeSymbol namedSymbol)
        {
            isNullable |= namedSymbol.TypeArguments.Any(IsNullable);
        }

        return isNullable;
    }
}
