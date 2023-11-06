using AutoConstructor.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace AutoConstructor.Tests.Verifiers;

internal static class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public static DiagnosticResult Diagnostic(string diagnosticId)
    {
        return CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(diagnosticId);
    }

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor diagnostic)
    {
        return CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(diagnostic);
    }

    public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new Test
        {
            TestCode = AppendBaseCode(source),
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }

    public static async Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource, int? numberOfFixAllIterations = null)
    {
        var test = new Test
        {
            TestCode = AppendBaseCode(source),
            FixedCode = AppendBaseCode(fixedSource),
            NumberOfFixAllIterations = numberOfFixAllIterations ?? expected.Length,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }

    private static string AppendBaseCode(string value)
    {
        // Appends the attributes from the generator to the code to be compiled.
        string valueWithCode = value;
        valueWithCode += $"{Source.AttributeText}\n";
        valueWithCode += $"{Source.IgnoreAttributeText}\n";
        valueWithCode += $"{Source.InjectAttributeText}\n";
        valueWithCode += $"{Source.InitializerAttributeText}\n";
        return valueWithCode;
    }

    internal sealed class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
    {
        public Test()
        {
            SolutionTransforms.Add((solution, projectId) =>
            {
                CompilationOptions? compilationOptions = solution.GetProject(projectId)?.CompilationOptions;
                compilationOptions = compilationOptions?.WithSpecificDiagnosticOptions(
                    compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));

                if (compilationOptions is not null)
                {
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);
                }

                return solution;
            });
        }
    }
}
