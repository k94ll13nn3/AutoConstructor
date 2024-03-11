using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Xml;
using AutoConstructor.Generator.Core;
using AutoConstructor.Generator.Extensions;
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
    internal static readonly string[] ConstuctorAccessibilities = ["public", "private", "internal", "protected", "protected internal", "private protected"];
    internal static readonly string GeneratorVersion = typeof(AutoConstructorGenerator).Assembly.GetName().Version!.ToString();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register the attribute source
        context.RegisterPostInitializationOutput((i) =>
        {
            i.AddSource(Source.AttributeFullName, SourceText.From(Source.AttributeText, Encoding.UTF8));
            i.AddSource(Source.IgnoreAttributeFullName, SourceText.From(Source.IgnoreAttributeText, Encoding.UTF8));
            i.AddSource(Source.InjectAttributeFullName, SourceText.From(Source.InjectAttributeText, Encoding.UTF8));
            i.AddSource(Source.InitializerAttributeFullName, SourceText.From(Source.InitializerAttributeText, Encoding.UTF8));
        });

        IncrementalValuesProvider<(GeneratorExectutionResult? result, Options options)> valueProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                Source.AttributeFullName,
                static (node, _) => IsSyntaxTargetForGeneration(node),
                static (context, _) => Execute(context, (TypeDeclarationSyntax)context.TargetNode))
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
        // We don't need to filter for class/struct here, as FAWMN is already doing that extra work for us
        return node is TypeDeclarationSyntax { AttributeLists.Count: > 0 } typeDeclarationSyntax && typeDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword);
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

        bool emitThisCalls = true;
        if (analyzerOptions.TryGetValue("build_property.AutoConstructor_GenerateThisCalls", out string? enableThisCallsSwitch))
        {
            emitThisCalls = enableThisCallsSwitch.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        return new(generateConstructorDocumentation, constructorDocumentationComment, emitNullChecks, emitThisCalls);
    }

    private static GeneratorExectutionResult? Execute(GeneratorAttributeSyntaxContext context, TypeDeclarationSyntax typeSyntax)
    {
        if (context.TargetSymbol is not INamedTypeSymbol symbol)
        {
            return null;
        }

        List<FieldInfo> concatenatedFields = GetFieldsFromSymbol(symbol);

        ExtractFieldsFromParent(symbol, concatenatedFields);

        EquatableArray<FieldInfo> fields = concatenatedFields.ToImmutableArray();

        if (fields.IsEmpty)
        {
            // No need to report diagnostic, taken care by the analyzers.
            return null;
        }

        foreach (IGrouping<string, FieldInfo> fieldGroup in fields.GroupBy(x => x.ParameterName))
        {
            string?[] types = fieldGroup.Where(c => c.Type is not null).Select(c => c.Type).Distinct().ToArray();

            // Get all fields defined with AutoConstructorInject, without a type specified and without an initializer (or just the parameter name as Initializer).
            // It's because thoses fields have a type depending on the initializer, and it cannot be computed.
            string[] fallbackTypesOfFieldsWithoutInitializer = fieldGroup.Where(c => c.Type is null && c.Initializer == c.ParameterName).Select(c => c.FallbackType).Distinct().ToArray();

            if (types.Length > 1
                || fallbackTypesOfFieldsWithoutInitializer.Length > 1
                || (fallbackTypesOfFieldsWithoutInitializer.Length > 0 && types.Length > 0 && fallbackTypesOfFieldsWithoutInitializer[0] != types[0]))
            {
                Location location = typeSyntax.GetLocation();
                return new(null, fields, new(location.SourceTree!.FilePath, location.SourceSpan, location.GetLineSpan().Span));
            }
        }

        bool hasParameterlessConstructor =
            typeSyntax
            .ChildNodes()
            .Count(n => n is ConstructorDeclarationSyntax constructor && !constructor.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword))) == 1
            && symbol.Constructors.Any(d => !d.IsStatic && d.Parameters.Length == 0);

        string? accessibility = null;
        AttributeData? attributeData = symbol.GetAttribute(Source.AttributeFullName);
        if (attributeData?.AttributeConstructor?.Parameters.Length > 0
            && attributeData.GetParameterValue<string>("accessibility") is string { Length: > 0 } accessibilityValue
            && ConstuctorAccessibilities.Contains(accessibilityValue))
        {
            accessibility = accessibilityValue;
        }

        IMethodSymbol? initializerMethod = symbol.GetMembers()
            .OfType<IMethodSymbol>()
            .FirstOrDefault(x => x.HasAttribute(Source.InitializerAttributeFullName));

        MainNamedTypeSymbolInfo mainNamedTypeSymbolInfo = new(
            symbol,
            hasParameterlessConstructor,
            symbol.GenerateFilename(),
            accessibility ?? "public",
            initializerMethod is null ? null : new(initializerMethod.IsStatic, initializerMethod.Name));
        return new(mainNamedTypeSymbolInfo, fields, null);
    }

    private static string GenerateAutoConstructor(MainNamedTypeSymbolInfo symbol, EquatableArray<FieldInfo> fields, Options options)
    {
        bool generateConstructorDocumentation = options.GenerateConstructorDocumentation;
        string? constructorDocumentationComment = options.ConstructorDocumentationComment;
        if (string.IsNullOrWhiteSpace(constructorDocumentationComment))
        {
            constructorDocumentationComment = $"Initializes a new instance of the {{0}} {(symbol.Kind is TypeKind.Struct ? "struct" : "class")}.";
        }

        var writer = new IndentedTextWriter();

        // Add file header.
        writer.WriteLine("//------------------------------------------------------------------------------");
        writer.WriteLine("// <auto-generated>");
        writer.WriteLine($"//     This code was generated by the {nameof(AutoConstructor)} source generator.");
        writer.WriteLine("//");
        writer.WriteLine("//     Changes to this file may cause incorrect behavior and will be lost if");
        writer.WriteLine("//     the code is regenerated.");
        writer.WriteLine("// </auto-generated>");
        writer.WriteLine("//------------------------------------------------------------------------------");

        // Add nullable annotation.
        if (fields.Any(f => f.Nullable))
        {
            writer.WriteLine("#nullable enable");
        }

        // Add namespace
        if (!symbol.Namespace.IsGlobal)
        {
            writer.WriteLine($"namespace {symbol.Namespace.Name}");
            writer.StartBlock();
        }

        // Add nested types.
        foreach (NamedTypeSymbolInfo containingType in symbol.ContainingTypes)
        {
            writer.WriteNamedTypeSymbolInfoLine(containingType);
            writer.StartBlock();
        }

        // Write type name line.
        writer.WriteNamedTypeSymbolInfoLine(symbol);

        // Write type content.
        using (writer.WriteBlock())
        {
            // Get all constuctor parameters from fields. 
            FieldInfo[] constructorParameters = fields
                .GroupBy(x => x.ParameterName)
                .Select(x => x.Any(c => c.Type is not null) ? x.First(c => c.Type is not null) : x.First())
                .ToArray();

            // Write constructor documentation if enable.
            if (generateConstructorDocumentation)
            {
                writer.WriteLine("/// <summary>");
                writer.WriteLine($"/// {string.Format(CultureInfo.InvariantCulture, constructorDocumentationComment, symbol.Name)}");
                writer.WriteLine("/// </summary>");
                foreach (FieldInfo parameter in constructorParameters)
                {
                    writer.WriteLine($"/// <param name=\"{parameter.ParameterName}\">{parameter.Comment ?? parameter.ParameterName}</param>");
                }
            }

            // Write constructor signature.
            writer.WriteLine($"""[global::System.CodeDom.Compiler.GeneratedCodeAttribute("{nameof(AutoConstructor)}", "{GeneratorVersion}")]""");
            writer.Write($"{symbol.Accessibility} {symbol.Name}(");
            writer.Write(string.Join(", ", constructorParameters.Select(p => $"{p.Type ?? p.FallbackType} {p.SanitizedParameterName}")));
            writer.Write(")");

            // Write base call if any of the parameters is of type PassedToBase
            if (Array.Exists(constructorParameters, p => p.FieldType.HasFlag(FieldType.PassedToBase)))
            {
                writer.Write(" : base(");
                writer.Write(string.Join(", ", constructorParameters.Where(p => p.FieldType.HasFlag(FieldType.PassedToBase)).Select(p => p.SanitizedParameterName)));
                writer.Write(")");
            }
            // Write this call if the symbol has a parameterless constructor
            else if (options.EmitThisCalls && symbol.HasParameterlessConstructor)
            {
                writer.Write(" : this()");
            }

            // End constructor line.
            writer.WriteLine();

            // Write constructor body.
            using (writer.WriteBlock())
            {
                foreach (FieldInfo field in fields.Where(f => f.FieldType.HasFlag(FieldType.Initialized)))
                {
                    writer.Write($"this.{field.FieldName.SanitizeReservedKeyword()} = {field.Initializer.SanitizeReservedKeyword()}");
                    if (options.EmitNullChecks && field.EmitArgumentNullException)
                    {
                        writer.Write($" ?? throw new System.ArgumentNullException(nameof({field.SanitizedParameterName}))");
                    }
                    writer.WriteLine(";");
                }

                if (symbol.InitializerMethod is not null)
                {
                    writer.WriteLine();
                    writer.WriteLine($"{(!symbol.InitializerMethod.IsStatic ? "this." : "")}{symbol.InitializerMethod.Name}();");
                }
            }
        }

        // Close nested types.
        foreach (NamedTypeSymbolInfo containingType in symbol.ContainingTypes)
        {
            writer.EndBlock();
        }

        // Close namespace.
        if (!symbol.Namespace.IsGlobal)
        {
            writer.EndBlock();
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
            if (attributeData.GetParameterValue<string>("parameterName") is string { Length: > 0 } parameterNameValue)
            {
                parameterName = parameterNameValue;
                initializer = parameterNameValue;
            }

            if (attributeData.GetParameterValue<string>("initializer") is string { Length: > 0 } initializerValue)
            {
                initializer = initializerValue;
            }

            injectedType = attributeData.GetParameterValue<INamedTypeSymbol>("injectedType");
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

    private static void ExtractFieldsFromParent(INamedTypeSymbol symbol, List<FieldInfo> concatenatedFields)
    {
        INamedTypeSymbol? baseType = symbol.BaseType;

        // Check if base type is not object (ie. its base type is null) and that there is only one constructor.
        if (baseType?.BaseType is not null && baseType.Constructors.Count(d => !d.IsStatic) == 1)
        {
            IMethodSymbol constructor = baseType.Constructors.Single(d => !d.IsStatic);

            // If symbol is in same assembly, the generated constructor is not visible as it might not be yet generated.
            // If not is the same assembly, is does not matter if the constructor was generated or not.
            if (SymbolEqualityComparer.Default.Equals(baseType.ContainingAssembly, symbol.ContainingAssembly) && baseType.HasAttribute(Source.AttributeFullName))
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
