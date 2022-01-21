using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace AutoConstructor.Generator;

[Generator]
public class AutoConstructorGenerator : IIncrementalGenerator
{
    public const string MistmatchTypesDiagnosticId = "ACONS06";

    private static readonly DiagnosticDescriptor Rule = new(
        MistmatchTypesDiagnosticId,
        "Couldn't generate constructor",
        "One or more parameter have mismatching types",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        null,
        $"https://github.com/k94ll13nn3/AutoConstructor#{MistmatchTypesDiagnosticId}",
        WellKnownDiagnosticTags.Build);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register the attribute source
        context.RegisterPostInitializationOutput((i) =>
        {
            i.AddSource(Source.AttributeFullName, SourceText.From(Source.AttributeText, Encoding.UTF8));
            i.AddSource(Source.IgnoreAttributeFullName, SourceText.From(Source.IgnoreAttributeText, Encoding.UTF8));
            i.AddSource(Source.InjectAttributeFullName, SourceText.From(Source.InjectAttributeText, Encoding.UTF8));
        });

        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(static (s, _) => IsSyntaxTargetForGeneration(s), static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null)!;

        IncrementalValueProvider<(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, AnalyzerConfigOptions options)> valueProvider =
            context.CompilationProvider
            .Combine(classDeclarations.Collect())
            .Combine(context.AnalyzerConfigOptionsProvider.Select((c, _) => c.GlobalOptions))
            .Select((c, _) => (compilation: c.Left.Left, classes: c.Left.Right, options: c.Right));

        context.RegisterSourceOutput(valueProvider, static (spc, source) => Execute(source.compilation, source.classes, spc, source.options));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDeclarationSyntax && classDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword);
    }

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        INamedTypeSymbol? symbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

        return symbol?.HasAttribute(Source.AttributeFullName, context.SemanticModel.Compilation) is true ? classDeclarationSyntax : null;
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context, AnalyzerConfigOptions options)
    {
        if (classes.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (ClassDeclarationSyntax candidateClass in classes)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                break;
            }

            INamedTypeSymbol? symbol = compilation
                .GetSemanticModel(candidateClass.SyntaxTree)
                .GetDeclaredSymbol(candidateClass);
            if (symbol is not null)
            {
                string filename = $"{symbol.Name}.g.cs";
                if (!symbol.ContainingNamespace.IsGlobalNamespace)
                {
                    filename = $"{symbol.ContainingNamespace.ToDisplayString()}.{filename}";
                }

                bool emitNullChecks = true;
                if (options.TryGetValue("build_property.AutoConstructor_DisableNullChecking", out string? disableNullCheckingSwitch))
                {
                    emitNullChecks = !disableNullCheckingSwitch.Equals("true", StringComparison.OrdinalIgnoreCase);
                }

                var fields = symbol.GetMembers().OfType<IFieldSymbol>()
                    .Where(x => x.CanBeReferencedByName
                        && !x.IsStatic
                        && x.IsReadOnly
                        && !x.IsInitialized()
                        && !x.HasAttribute(Source.IgnoreAttributeFullName, compilation))
                    .Select(x => GetFieldInfo(x, compilation, emitNullChecks))
                    .ToList();

                if (fields.Count == 0)
                {
                    // No need to report diagnostic, taken care by the analyers.
                    continue;
                }

                if (fields.GroupBy(x => x.ParameterName).Any(g =>
                    g.Where(c => c.Type is not null).Select(c => c.Type).Distinct().Count() > 1
                    || (g.All(c => c.Type is null) && g.Select(c => c.FallbackType).Distinct().Count() > 1)
                    ))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, candidateClass.GetLocation()));
                    continue;
                }

                context.AddSource(filename, SourceText.From(GenerateAutoConstructor(symbol, fields, options), Encoding.UTF8));
            }
        }
    }

    private static string GenerateAutoConstructor(INamedTypeSymbol symbol, IEnumerable<FieldInfo> fields, AnalyzerConfigOptions options)
    {
        bool generateConstructorDocumentation = false;
        if (options.TryGetValue("build_property.AutoConstructor_GenerateConstructorDocumentation", out string? generateConstructorDocumentationSwitch))
        {
            generateConstructorDocumentation = generateConstructorDocumentationSwitch.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        options.TryGetValue("build_property.AutoConstructor_ConstructorDocumentationComment", out string? constructorDocumentationComment);
        if (string.IsNullOrWhiteSpace(constructorDocumentationComment))
        {
            constructorDocumentationComment = "Initializes a new instance of the {0} class.";
        }

        var constructorParameters = fields
            .GroupBy(x => x.ParameterName)
            .Select(x => x.Any(c => c.Type is not null) ? x.First(c => c.Type is not null) : x.First())
            .ToList();

        var source = new StringBuilder("// <auto-generated />");
        string tabulation = "    ";
        if (fields.Any(f => f.Nullable))
        {
            source.Append(@"
#nullable enable");
        }

        if (symbol.ContainingNamespace.IsGlobalNamespace)
        {
            tabulation = string.Empty;
        }
        else
        {
            source.Append($@"
namespace {symbol.ContainingNamespace.ToDisplayString()}
{{");
        }

        source.Append($@"
{tabulation}partial class {symbol.Name}
{tabulation}{{");

        if (generateConstructorDocumentation)
        {
            source.Append($@"
{tabulation}    /// <summary>
{tabulation}    /// {string.Format(CultureInfo.InvariantCulture, constructorDocumentationComment, symbol.Name)}
{tabulation}    /// </summary>");

            foreach (FieldInfo parameter in constructorParameters)
            {
                source.Append($@"
{tabulation}    /// <param name=""{parameter.ParameterName}"">{parameter.Comment ?? parameter.ParameterName}</param>");
            }
        }

        source.Append($@"
{tabulation}    public {symbol.Name}({string.Join(", ", constructorParameters.Select(it => $"{it.Type ?? it.FallbackType} {it.ParameterName}"))})
{tabulation}    {{");

        foreach (FieldInfo field in fields)
        {
            source.Append($@"
{tabulation}        this.{field.FieldName} = {field.Initializer};");
        }
        source.Append($@"
{tabulation}    }}
{tabulation}}}
");
        if (!symbol.ContainingNamespace.IsGlobalNamespace)
        {
            source.Append(@"}
");
        }

        return source.ToString();
    }

    private static FieldInfo GetFieldInfo(IFieldSymbol fieldSymbol, Compilation compilation, bool emitNullChecks)
    {
        ITypeSymbol type = fieldSymbol.Type;
        string? typeDisplay = type.ToDisplayString();
        string parameterName = fieldSymbol.Name.TrimStart('_');
        string initializer = parameterName;

        string? documentationComment = fieldSymbol.GetDocumentationCommentXml();
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
            if (GetParameterValue("parameterName", parameters, attributeData.ConstructorArguments) is string { Length: > 0 } parameterNameValue)
            {
                parameterName = parameterNameValue;
                initializer = parameterNameValue;
            }

            if (GetParameterValue("initializer", parameters, attributeData.ConstructorArguments) is string { Length: > 0 } initializerValue)
            {
                initializer = initializerValue;
            }

            string? injectedTypeValue = GetParameterValue("injectedType", parameters, attributeData.ConstructorArguments);
            typeDisplay = string.IsNullOrWhiteSpace(injectedTypeValue) ? null : injectedTypeValue;
        }

        if (type.NullableAnnotation != NullableAnnotation.NotAnnotated && emitNullChecks)
        {
            initializer = $"{initializer} ?? throw new System.ArgumentNullException(nameof({parameterName}))";
        }

        return new FieldInfo(
            typeDisplay,
            parameterName,
            fieldSymbol.Name,
            initializer,
            type.ToDisplayString(),
            type.IsReferenceType && type.NullableAnnotation == NullableAnnotation.Annotated,
            summaryText);
    }

    private static string? GetParameterValue(string parameterName, ImmutableArray<IParameterSymbol> parameters, ImmutableArray<TypedConstant> arguments)
    {
        return parameters.ToList().FindIndex(c => c.Name == parameterName) is int index and not -1
            ? (arguments[index].Value?.ToString())
            : null;
    }
}
