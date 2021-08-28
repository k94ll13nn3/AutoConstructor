using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AutoConstructor.Generator;

[Generator]
public class AutoConstructorGenerator : ISourceGenerator
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

    public void Initialize(GeneratorInitializationContext context)
    {
        // Register the attribute source
        context.RegisterForPostInitialization((i) =>
        {
            i.AddSource(Source.AttributeFullName, SourceText.From(Source.AttributeText, Encoding.UTF8));
            i.AddSource(Source.IgnoreAttributeFullName, SourceText.From(Source.IgnoreAttributeText, Encoding.UTF8));
            i.AddSource(Source.InjectAttributeFullName, SourceText.From(Source.InjectAttributeText, Encoding.UTF8));
        });

        // Register a syntax receiver that will be created for each generation pass.
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not SyntaxReceiver receiver)
        {
            return;
        }

        foreach (ClassDeclarationSyntax candidateClass in receiver.CandidateClasses)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                break;
            }

            INamedTypeSymbol? symbol = context
                .Compilation
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
                if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.AutoConstructor_DisableNullChecking", out string? disableNullCheckingSwitch))
                {
                    emitNullChecks = !disableNullCheckingSwitch.Equals("true", StringComparison.OrdinalIgnoreCase);
                }

                var fields = symbol.GetMembers().OfType<IFieldSymbol>()
                    .Where(x => x.CanBeReferencedByName
                        && !x.IsStatic
                        && x.IsReadOnly
                        && !x.IsInitialized()
                        && !x.HasAttribute(Source.IgnoreAttributeFullName, context.Compilation))
                    .Select(x => GetFieldInfo(x, context.Compilation, emitNullChecks))
                    .ToList();

                if (fields.Count == 0)
                {
                    // No need to report diagnostic, taken care by the analyers.
                    continue;
                }

                if (fields.GroupBy(x => x.ParameterName).Any(g => g.Select(c => c.Type).Distinct().Count() > 1))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, candidateClass.GetLocation()));
                    continue;
                }

                context.AddSource(filename, SourceText.From(GenerateAutoConstructor(symbol, fields), Encoding.UTF8));
            }
        }
    }

    private static string GenerateAutoConstructor(INamedTypeSymbol symbol, IEnumerable<FieldInfo> fields)
    {
        var constructorParameters = fields.GroupBy(x => x.ParameterName).Select(x => x.First()).ToList();

        // Split the initialization in two because CodeMaid thinks it is an auto-generated file.
        var source = new StringBuilder("// <auto-");
        source.Append("generated />");

        string tabulation = "    ";
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
{tabulation}{{
{tabulation}    public {symbol.Name}({string.Join(", ", constructorParameters.Select(it => $"{it.Type} {it.ParameterName}"))})
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
        ITypeSymbol type = fieldSymbol!.Type;
        string typeDisplay = type.ToDisplayString();
        string parameterName = fieldSymbol.Name.TrimStart('_');
        string initializer = parameterName;

        AttributeData? attributeData = fieldSymbol.GetAttribute(Source.InjectAttributeFullName, compilation);
        if (attributeData is not null)
        {
            initializer = attributeData.ConstructorArguments[0].Value?.ToString() ?? "";
            parameterName = attributeData.ConstructorArguments[1].Value?.ToString() ?? "";
            typeDisplay = attributeData.ConstructorArguments[2].Value?.ToString() ?? "";
        }

        if ((type.TypeKind == TypeKind.Class || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) && emitNullChecks)
        {
            initializer = $"{initializer} ?? throw new System.ArgumentNullException(nameof({parameterName}))";
        }

        return new(typeDisplay, parameterName, fieldSymbol!.Name, initializer);
    }
}
