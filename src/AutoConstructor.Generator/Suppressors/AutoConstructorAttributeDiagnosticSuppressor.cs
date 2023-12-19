using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoConstructor.Generator.Suppressors;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AutoConstructorAttributeDiagnosticSuppressor : DiagnosticSuppressor
{
    public static readonly SuppressionDescriptor AutoConstructorAttributeAssemblyConflict = new(
        id: "ACONSSPR0001",
        suppressedDiagnosticId: "CS0436",
        justification: "It is expected to have multiple [AutoConstructor] attributes, and the default choice from Roslyn is fine to use.");

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(AutoConstructorAttributeAssemblyConflict);

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
        {
            SyntaxNode? syntaxNode = diagnostic.Location.SourceTree?.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan);
            if (syntaxNode is null)
            {
                continue;
            }

            SemanticModel semanticModel = context.GetSemanticModel(syntaxNode.SyntaxTree);
            TypeInfo typeInfo = semanticModel.GetTypeInfo(syntaxNode, context.CancellationToken);
            if (typeInfo.Type is INamedTypeSymbol
                {
                    Name: Source.AttributeFullName or Source.IgnoreAttributeFullName or Source.InitializerAttributeFullName or Source.InjectAttributeFullName
                })
            {
                context.ReportSuppression(Suppression.Create(AutoConstructorAttributeAssemblyConflict, diagnostic));
            }
        }
    }
}
