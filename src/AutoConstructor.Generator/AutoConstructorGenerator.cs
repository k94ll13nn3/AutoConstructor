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

        foreach (IGrouping<ISymbol?, ClassDeclarationSyntax> groupedClasses in classes.GroupBy(c => compilation.GetSemanticModel(c.SyntaxTree).GetDeclaredSymbol(c), SymbolEqualityComparer.Default))
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                break;
            }

            INamedTypeSymbol? symbol = groupedClasses.Key as INamedTypeSymbol;
            if (symbol is not null)
            {
                string filename = string.Empty;

                if (symbol.ContainingType is not null)
                {
                    filename = $"{string.Join(".", symbol.GetContainingTypes().Select(c => c.Name))}.";
                }

                filename += $"{symbol.Name}.g.cs";
                if (!symbol.ContainingNamespace.IsGlobalNamespace)
                {
                    filename = $"{symbol.ContainingNamespace.ToDisplayString()}.{filename}";
                }

                bool emitNullChecks = false;
                if (options.TryGetValue("build_property.AutoConstructor_DisableNullChecking", out string? disableNullCheckingSwitch))
                {
                    emitNullChecks = !disableNullCheckingSwitch.Equals("true", StringComparison.OrdinalIgnoreCase);
                }

                FieldInfo[] fields = symbol.GetMembers().OfType<IFieldSymbol>()
                    .Where(x => x.CanBeInjected(compilation)
                        && !x.IsStatic
                        && x.IsReadOnly
                        && !x.IsInitialized()
                        && !x.HasAttribute(Source.IgnoreAttributeFullName, compilation))
                    .Select(x => GetFieldInfo(x, compilation, emitNullChecks))
                    .ToArray();

                if (fields.Length == 0)
                {
                    // No need to report diagnostic, taken care by the analyzers.
                    continue;
                }

                if (fields.GroupBy(x => x.ParameterName).Any(g =>
                    g.Where(c => c.Type is not null).Select(c => c.Type).Distinct(SymbolEqualityComparer.Default).Count() > 1
                    || (g.All(c => c.Type is null) && g.Select(c => c.FallbackType).Distinct(SymbolEqualityComparer.Default).Count() > 1)
                    ))
                {
                    foreach (ClassDeclarationSyntax classDeclaration in groupedClasses)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MistmatchTypesRule, classDeclaration.GetLocation()));
                    }

                    continue;
                }

                context.AddSource(filename, SourceText.From(GenerateAutoConstructor(symbol, fields, options), Encoding.UTF8));
            }
        }
    }

    private static string GenerateAutoConstructor(INamedTypeSymbol symbol, FieldInfo[] fields, AnalyzerConfigOptions options)
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

        var codeGenerator = new CodeGenerator();

        if (fields.Any(f => f.Nullable))
        {
            codeGenerator.AddNullableAnnotation();
        }

        if (generateConstructorDocumentation)
        {
            codeGenerator.AddDocumentation(string.Format(CultureInfo.InvariantCulture, constructorDocumentationComment, symbol.Name));
        }

        if (!symbol.ContainingNamespace.IsGlobalNamespace)
        {
            codeGenerator.AddNamespace(symbol.ContainingNamespace.ToDisplayString());
        }

        foreach (INamedTypeSymbol containingType in symbol.GetContainingTypes())
        {
            codeGenerator.AddClass(containingType.Name, containingType.IsStatic, containingType.TypeParameters.ToArray());
        }

        codeGenerator
            .AddClass(symbol.Name, typeParameterList: symbol.TypeParameters.ToArray())
            .AddConstructor(fields);

        return codeGenerator.ToString();
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
            injectedType,
            parameterName,
            fieldSymbol.AssociatedSymbol?.Name ?? fieldSymbol.Name,
            initializer,
            type,
            type.IsReferenceType && type.NullableAnnotation == NullableAnnotation.Annotated,
            summaryText,
            type.IsReferenceType && type.NullableAnnotation == NullableAnnotation.None && emitNullChecks);
    }

    private static T? GetParameterValue<T>(string parameterName, ImmutableArray<IParameterSymbol> parameters, ImmutableArray<TypedConstant> arguments)
        where T : class
    {
        return parameters.ToList().FindIndex(c => c.Name == parameterName) is int index and not -1
            ? (arguments[index].Value as T)
            : null;
    }
}
