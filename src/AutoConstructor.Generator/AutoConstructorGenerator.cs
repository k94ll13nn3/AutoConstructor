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
    internal static readonly string[] ConstructorAccessibilities = ["public", "private", "internal", "protected", "protected internal", "private protected"];
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
            i.AddSource(Source.DefaultBaseAttributeFullName, SourceText.From(Source.DefaultBaseAttributeText, Encoding.UTF8));
        });

        // TODO: remove in v6
        IncrementalValueProvider<bool> obsoleteOptionDiagnosticProvider = context.AnalyzerConfigOptionsProvider
            .Select((c, _) =>
            {
                return
                    c.GlobalOptions.TryGetValue($"build_property.{BuildProperties.AutoConstructor_DisableNullChecking}", out string? disableNullCheckingSwitch) &&
                    !string.IsNullOrWhiteSpace(disableNullCheckingSwitch);
            })
            .WithTrackingName("obsoleteOptionDiagnosticProvider");

        IncrementalValuesProvider<(GeneratorExecutionResult? result, Options options)> valuesProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                Source.AttributeFullName,
                static (node, _) => IsSyntaxTargetForGeneration(node),
                static (context, _) => Execute(context, (TypeDeclarationSyntax)context.TargetNode))
            .WithTrackingName("Execute")
            .Where(static m => m is not null)
            .Combine(context.AnalyzerConfigOptionsProvider.Select((c, _) => ParseOptions(c.GlobalOptions)))
            .WithTrackingName("Combine");

        context.RegisterSourceOutput(obsoleteOptionDiagnosticProvider, static (context, item) =>
        {
            if (item)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.DisableNullCheckingIsObsoleteRule, null));
            }
        });

        context.RegisterSourceOutput(valuesProvider, static (context, item) =>
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
        if (analyzerOptions.TryGetValue($"build_property.{BuildProperties.AutoConstructor_GenerateConstructorDocumentation}", out string? generateConstructorDocumentationSwitch))
        {
            generateConstructorDocumentation = generateConstructorDocumentationSwitch.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        analyzerOptions.TryGetValue($"build_property.{BuildProperties.AutoConstructor_ConstructorDocumentationComment}", out string? constructorDocumentationComment);

        // TODO: remove in v6
        bool emitNullChecks = false;
        if (analyzerOptions.TryGetValue($"build_property.{BuildProperties.AutoConstructor_DisableNullChecking}", out string? disableNullCheckingSwitch))
        {
            emitNullChecks = disableNullCheckingSwitch.Equals("false", StringComparison.OrdinalIgnoreCase);
        }

        if (analyzerOptions.TryGetValue($"build_property.{BuildProperties.AutoConstructor_GenerateArgumentNullExceptionChecks}", out string? generateArgumentNullExceptionChecksSwitch))
        {
            emitNullChecks = generateArgumentNullExceptionChecksSwitch.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        bool emitThisCalls = true;
        if (analyzerOptions.TryGetValue($"build_property.{BuildProperties.AutoConstructor_GenerateThisCalls}", out string? enableThisCallsSwitch))
        {
            emitThisCalls = !enableThisCallsSwitch.Equals("false", StringComparison.OrdinalIgnoreCase);
        }

        bool markParameterlessConstructorAsObsolete = true;
        if (analyzerOptions.TryGetValue($"build_property.{BuildProperties.AutoConstructor_MarkParameterlessConstructorAsObsolete}", out string? markParameterlessConstructorAsObsoleteSwitch))
        {
            markParameterlessConstructorAsObsolete = !markParameterlessConstructorAsObsoleteSwitch.Equals("false", StringComparison.OrdinalIgnoreCase);
        }

        analyzerOptions.TryGetValue($"build_property.{BuildProperties.AutoConstructor_ParameterlessConstructorObsoleteMessage}", out string? parameterlessConstructorObsoleteMessage);

        return new(
            generateConstructorDocumentation,
            constructorDocumentationComment,
            emitNullChecks,
            emitThisCalls,
            markParameterlessConstructorAsObsolete,
            parameterlessConstructorObsoleteMessage);
    }

    private static GeneratorExecutionResult? Execute(GeneratorAttributeSyntaxContext context, TypeDeclarationSyntax typeSyntax)
    {
        if (context.TargetSymbol is not INamedTypeSymbol symbol)
        {
            return null;
        }

        List<FieldInfo> concatenatedFields = GetFieldsFromSymbol(symbol);

        ExtractFieldsFromParent(symbol, concatenatedFields);

        EquatableArray<FieldInfo> fields = concatenatedFields.ToImmutableArray();

        foreach (IGrouping<string, FieldInfo> fieldGroup in fields.GroupBy(x => x.ParameterName))
        {
            string?[] types = fieldGroup.Where(c => c.Type is not null).Select(c => c.Type).Distinct().ToArray();

            // Get all fields defined with AutoConstructorInject, without a type specified and without an initializer (or just the parameter name as Initializer).
            // It's because those fields have a type depending on the initializer, and it cannot be computed.
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
            && ConstructorAccessibilities.Contains(accessibilityValue))
        {
            accessibility = accessibilityValue;
        }

        bool generatedDefaultBaseAttribute = attributeData.GetOptionalBoolParameterValue("addDefaultBaseAttribute");
        bool disableThisCall = attributeData.GetOptionalBoolParameterValue("disableThisCall");
        bool addParameterless = attributeData.GetOptionalBoolParameterValue("addParameterless");

        if (fields.IsEmpty && !addParameterless)
        {
            // No need to report diagnostic, taken care by the analyzers.
            return null;
        }

        IMethodSymbol? initializerMethod = symbol.GetMembers()
            .OfType<IMethodSymbol>()
            .FirstOrDefault(x => x.HasAttribute(Source.InitializerAttributeFullName));

        MainNamedTypeSymbolInfo mainNamedTypeSymbolInfo = new(
            symbol,
            hasParameterlessConstructor,
            symbol.GenerateFilename(),
            accessibility ?? "public",
            initializerMethod is null ? null : new(initializerMethod.IsStatic, initializerMethod.Name),
            generatedDefaultBaseAttribute,
            disableThisCall,
            addParameterless);
        return new(mainNamedTypeSymbolInfo, fields, null);
    }

    private static string GenerateAutoConstructor(MainNamedTypeSymbolInfo symbol, EquatableArray<FieldInfo> fields, Options options)
    {
        string? constructorDocumentationComment = options.ConstructorDocumentationComment;
        if (string.IsNullOrWhiteSpace(constructorDocumentationComment))
        {
            constructorDocumentationComment = $"Initializes a new instance of the {{0}} {(symbol.Kind is TypeKind.Struct ? "struct" : "class")}.";
        }

        string? parameterlessConstructorObsoleteMessage = options.ParameterlessConstructorObsoleteMessage;
        if (string.IsNullOrWhiteSpace(parameterlessConstructorObsoleteMessage))
        {
            parameterlessConstructorObsoleteMessage = "Not intended for direct usage.";
        }

        var writer = new IndentedTextWriter();

        // Add file header.
        writer.WriteFileHeader();

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
            if (!fields.IsEmpty)
            {
                WriteMainConstructor(symbol, fields, options, constructorDocumentationComment, writer);
            }

            if (symbol.AddParameterless)
            {
                WriteParameterlessConstructor(symbol, fields.IsEmpty, options, parameterlessConstructorObsoleteMessage, writer);
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

    private static void WriteMainConstructor(
        MainNamedTypeSymbolInfo symbol,
        EquatableArray<FieldInfo> fields,
        Options options,
        string? constructorDocumentationComment,
        IndentedTextWriter writer)
    {
        // Get all constructor parameters from fields. 
        FieldInfo[] constructorParameters = fields
            .GroupBy(x => x.ParameterName)
            .Select(x => x.Any(c => c.Type is not null) ? x.First(c => c.Type is not null) : x.First())
            .ToArray();

        // Write constructor documentation if enabled.
        if (options.GenerateConstructorDocumentation)
        {
            writer.WriteSummary(string.Format(CultureInfo.InvariantCulture, constructorDocumentationComment, symbol.Name));
            foreach (FieldInfo parameter in constructorParameters)
            {
                writer.WriteLine($"/// <param name=\"{parameter.ParameterName}\">{parameter.Comment ?? parameter.ParameterName}</param>");
            }
        }

        // Write attributes.
        if (symbol.GenerateDefaultBaseAttribute)
        {
            writer.WriteLine("[AutoConstructorDefaultBase]");
        }

        // Write generated code attribute.
        writer.WriteGeneratedCodeAttribute();

        // Write constructor signature.
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
        else if (options.EmitThisCalls && !symbol.DisableThisCall && symbol.HasParameterlessConstructor)
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

    private static void WriteParameterlessConstructor(
        MainNamedTypeSymbolInfo symbol,
        bool fieldsEmpty,
        Options options,
        string? parameterlessConstructorObsoleteMessage,
        IndentedTextWriter writer)
    {
        // Insert new line if there was a previous constructor.
        if (!fieldsEmpty)
        {
            writer.WriteLine();
        }

        // Write constructor documentation if enabled.
        if (options.GenerateConstructorDocumentation && parameterlessConstructorObsoleteMessage is string { Length: > 0 })
        {
            writer.WriteSummary(parameterlessConstructorObsoleteMessage);
        }

        writer.WriteLine("#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.");
        if (options.MarkParameterlessConstructorAsObsolete)
        {
            writer.WriteLine($"""[global::System.ObsoleteAttribute("{parameterlessConstructorObsoleteMessage}", true)]""");
        }

        // Write generated code attribute.
        writer.WriteGeneratedCodeAttribute();

        // Write constructor signature.
        writer.WriteLine($"{symbol.Accessibility} {symbol.Name}()");

        // Write constructor body.
        writer.StartBlock();
        writer.EndBlock();
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
        (IMethodSymbol? constructor, INamedTypeSymbol? baseType) = symbol.GetPreferedBaseConstructorOrBaseType();
        if (constructor is not null)
        {
            ExtractFieldsFromConstructedParent(concatenatedFields, constructor);
        }
        else if (baseType is not null)
        {
            ExtractFieldsFromGeneratedParent(concatenatedFields, baseType);
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
