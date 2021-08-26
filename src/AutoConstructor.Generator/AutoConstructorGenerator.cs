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

            SemanticModel model = context.Compilation.GetSemanticModel(candidateClass.SyntaxTree);
            INamedTypeSymbol? symbol = model.GetDeclaredSymbol(candidateClass);

            if (symbol is not null)
            {
                string filename = $"{symbol.Name}.g.cs";
                if (!symbol.ContainingNamespace.IsGlobalNamespace)
                {
                    filename = $"{symbol.ContainingNamespace.ToDisplayString()}.{filename}";
                }
                string source = GenerateAutoConstructor(symbol, context.Compilation);
                if (!string.IsNullOrWhiteSpace(source))
                {
                    context.AddSource(filename, SourceText.From(source, Encoding.UTF8));
                }
            }
        }
    }

    private static string GenerateAutoConstructor(INamedTypeSymbol symbol, Compilation compilation)
    {
        var fields = symbol.GetMembers().OfType<IFieldSymbol>()
            .Where(x => x.CanBeReferencedByName && !x.IsStatic && x.IsReadOnly && !x.IsInitialized() && !x.HasAttribute(Source.IgnoreAttributeFullName, compilation))
            .Select(GetFieldInfo)
            .ToList();

        if (fields.Count == 0)
        {
            return string.Empty;
        }

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

        foreach ((string type, string parameterName, string fieldName, string initializer) in fields)
        {
            source.Append($@"
{tabulation}        this.{fieldName} = {initializer};");
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

        (string Type, string ParameterName, string FieldName, string Initializer) GetFieldInfo(IFieldSymbol fieldSymbol)
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

            if (type.TypeKind == TypeKind.Class || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                initializer = $"{initializer} ?? throw new System.ArgumentNullException(nameof({parameterName}))";
            }

            return new(typeDisplay, parameterName, fieldSymbol!.Name, initializer);
        }
    }
}
