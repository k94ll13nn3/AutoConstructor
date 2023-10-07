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
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register the attribute source
        context.RegisterPostInitializationOutput((i) =>
        {
            i.AddSource(Source.AttributeFullName, SourceText.From(Source.AttributeText, Encoding.UTF8));
            i.AddSource(Source.IgnoreAttributeFullName, SourceText.From(Source.IgnoreAttributeText, Encoding.UTF8));
            i.AddSource(Source.InjectAttributeFullName, SourceText.From(Source.InjectAttributeText, Encoding.UTF8));
        });

        IncrementalValuesProvider<GeneratorExectutionResult?> valueProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                Source.AttributeFullName,
                static (node, _) => IsSyntaxTargetForGeneration(node),
                static (context, _) => (ClassDeclarationSyntax)context.TargetNode)
            .Where(static m => m is not null)
            .Combine(context.AnalyzerConfigOptionsProvider.Select((c, _) => c.GlobalOptions))
            .Combine(context.CompilationProvider)
            .Select((c, _) => Execute(c.Right, c.Left.Left, c.Left.Right));

        context.RegisterSourceOutput(valueProvider, static (context, item) =>
        {
            if (item is not null)
            {
                if (!item.Diagnostics.IsEmpty)
                {
                    foreach (Diagnostic diagnostic in item.Diagnostics)
                    {
                        context.ReportDiagnostic(diagnostic);
                    }
                }
                else
                {
                    CodeGenerator codeGenerator = GenerateAutoConstructor(item.Symbol!, item.Fields, item.Options);
                    context.AddSource($"{item.Symbol!.Filename}.g.cs", codeGenerator.ToString());
                }
            }
        });
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclarationSyntax && classDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword);
    }

    private static GeneratorExectutionResult? Execute(Compilation compilation, ClassDeclarationSyntax classSyntax, AnalyzerConfigOptions analyzerOptions)
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

        Options options = new(generateConstructorDocumentation, constructorDocumentationComment, emitNullChecks);

        INamedTypeSymbol? symbol = compilation.GetSemanticModel(classSyntax.SyntaxTree).GetDeclaredSymbol(classSyntax);
        if (symbol is null)
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

        List<FieldInfo> concatenatedFields = GetFieldsFromSymbol(compilation, symbol, emitNullChecks);

        ExtractFieldsFromParent(compilation, symbol, emitNullChecks, concatenatedFields);

        EquatableArray<FieldInfo> fields = concatenatedFields.ToImmutableArray();

        if (fields.IsEmpty)
        {
            // No need to report diagnostic, taken care by the analyzers.
            return null;
        }

        var diagnotics = new List<Diagnostic>();

        if (fields.GroupBy(x => x.ParameterName).Any(g =>
            g.Where(c => c.Type is not null).Select(c => c.Type).Count() > 1
            || (g.All(c => c.Type is null) && g.Select(c => c.FallbackType).Count() > 1)
            ))
        {
            diagnotics.Add(Diagnostic.Create(DiagnosticDescriptors.MistmatchTypesRule, classSyntax.GetLocation()));

            return new(null, fields, options, diagnotics.ToImmutableArray());
        }

        bool hasParameterlessConstructor =
            classSyntax
            .ChildNodes()
            .Count(n => n is ConstructorDeclarationSyntax constructor && !constructor.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword))) == 1
            && symbol.Constructors.Any(d => !d.IsStatic && d.Parameters.Length == 0);

        return new(new MainNamedTypeSymbolInfo(symbol, hasParameterlessConstructor, filename), fields, options, Array.Empty<Diagnostic>().ToImmutableArray());
    }

    private static CodeGenerator GenerateAutoConstructor(MainNamedTypeSymbolInfo symbol, EquatableArray<FieldInfo> fields, Options options)
    {
        bool generateConstructorDocumentation = options.GenerateConstructorDocumentation;
        string? constructorDocumentationComment = options.ConstructorDocumentationComment;
        if (string.IsNullOrWhiteSpace(constructorDocumentationComment))
        {
            constructorDocumentationComment = "Initializes a new instance of the {0} class.";
        }

        var codeGenerator = new CodeGenerator();

        if (fields.Any(f => f.Nullable))
        {
            codeGenerator.AddNullableAnnotation();
        }

        if (generateConstructorDocumentation)
        {
            codeGenerator.AddDocumentation(string.Format(CultureInfo.InvariantCulture, constructorDocumentationComment, symbol.Name));
        }

        if (!symbol.IsGlobalNamespace)
        {
            codeGenerator.AddNamespace(symbol.ContainingNamespace);
        }

        foreach (NamedTypeSymbolInfo containingType in symbol.ContainingTypes)
        {
            codeGenerator.AddClass(containingType);
        }

        codeGenerator
            .AddClass(symbol)
            .AddConstructor(fields, symbol.HasParameterlessConstructor);

        return codeGenerator;
    }

    private static List<FieldInfo> GetFieldsFromSymbol(Compilation compilation, INamedTypeSymbol symbol, bool emitNullChecks)
    {
        return symbol.GetMembers().OfType<IFieldSymbol>()
            .Where(x => x.CanBeInjected(compilation)
                && !x.IsStatic
                && (x.IsReadOnly || IsPropertyWithExplicitInjection(x))
                && !x.IsInitialized()
                && !x.HasAttribute(Source.IgnoreAttributeFullName, compilation))
            .Select(x => GetFieldInfo(x, compilation, emitNullChecks))
            .ToList();

        bool IsPropertyWithExplicitInjection(IFieldSymbol x)
        {
            return x.AssociatedSymbol is not null && x.HasAttribute(Source.InjectAttributeFullName, compilation);
        }
    }

    private static FieldInfo GetFieldInfo(IFieldSymbol fieldSymbol, Compilation compilation, bool emitNullChecks)
    {
        ITypeSymbol type = fieldSymbol.Type;
        ITypeSymbol? injectedType = type;
        string parameterName = fieldSymbol.Name.TrimStart('_');
        if (fieldSymbol.AssociatedSymbol is not null)
        {
            parameterName = char.ToLowerInvariant(fieldSymbol.AssociatedSymbol.Name[0]) + fieldSymbol.AssociatedSymbol.Name.Substring(1);
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

        AttributeData? attributeData = fieldSymbol.GetAttribute(Source.InjectAttributeFullName, compilation);
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
            type.IsReferenceType && type.NullableAnnotation != NullableAnnotation.Annotated && emitNullChecks,
            FieldType.Initialized);
    }

    private static T? GetParameterValue<T>(string parameterName, ImmutableArray<IParameterSymbol> parameters, ImmutableArray<TypedConstant> arguments)
        where T : class
    {
        return parameters.ToList().FindIndex(c => c.Name == parameterName) is int index and not -1
            ? (arguments[index].Value as T)
            : null;
    }

    private static void ExtractFieldsFromParent(Compilation compilation, INamedTypeSymbol symbol, bool emitNullChecks, List<FieldInfo> concatenatedFields)
    {
        INamedTypeSymbol? baseType = symbol.BaseType;

        // Check if base type is not object (ie. its base type is null) and that there is only one constructor.
        if (baseType?.BaseType is not null && baseType.Constructors.Count(d => !d.IsStatic) == 1)
        {
            IMethodSymbol constructor = baseType.Constructors.Single(d => !d.IsStatic);
            if (baseType.HasAttribute(Source.AttributeFullName, compilation))
            {
                ExtractFieldsFromGeneratedParent(compilation, emitNullChecks, concatenatedFields, baseType);
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
                concatenatedFields[index].FieldType |= FieldType.PassedToBase;
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

    private static void ExtractFieldsFromGeneratedParent(Compilation compilation, bool emitNullChecks, List<FieldInfo> concatenatedFields, INamedTypeSymbol symbol)
    {
        foreach (FieldInfo parameter in GetFieldsFromSymbol(compilation, symbol, emitNullChecks))
        {
            int index = concatenatedFields.FindIndex(p => p.ParameterName == parameter.ParameterName);
            if (index != -1)
            {
                concatenatedFields[index].FieldType |= FieldType.PassedToBase;
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

        ExtractFieldsFromParent(compilation, symbol, emitNullChecks, concatenatedFields);
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
