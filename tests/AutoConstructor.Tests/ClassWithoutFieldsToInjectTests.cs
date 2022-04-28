using AutoConstructor.Generator;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyClassWithoutFieldsToInject = AutoConstructor.Tests.Verifiers.CSharpCodeFixVerifier<
    AutoConstructor.Generator.Analyzers.ClassWithoutFieldsToInjectAnalyzer,
    AutoConstructor.Generator.CodeFixes.RemoveAttributeCodeFixProvider>;

namespace AutoConstructor.Tests;

public class ClassWithoutFieldsToInjectTests
{
    [Fact]
    public async Task Analyzer_ClassWithoutFieldsToInject_ShouldReportDiagnostic()
    {
        const string test = @"
namespace Test
{
    [{|#0:AutoConstructor|}]
    internal partial class Test
    {
    }
}";

        DiagnosticResult[] expected = new[] {
                VerifyClassWithoutFieldsToInject.Diagnostic(DiagnosticDescriptors.ClassWithoutFieldsToInjectDiagnosticId).WithLocation(0),
            };
        await VerifyClassWithoutFieldsToInject.VerifyAnalyzerAsync(test, expected);
    }

    [Theory]
    [InlineData(@"
namespace Test
{
    [{|#0:AutoConstructor|}]
    internal partial class Test
    {
    }
}", @"
namespace Test
{
    internal partial class Test
    {
    }
}")]
    public async Task Analyzer_ClassWithoutFieldsToInject_ShouldFixCode(string test, string fixtest)
    {
        DiagnosticResult[] expected = new[] {
                VerifyClassWithoutFieldsToInject.Diagnostic(DiagnosticDescriptors.ClassWithoutFieldsToInjectDiagnosticId).WithLocation(0),
            };
        await VerifyClassWithoutFieldsToInject.VerifyCodeFixAsync(test, expected, fixtest);
    }
}
