using AutoConstructor.Generator;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyInjectAttributeOnIgnoredField = AutoConstructor.Tests.Verifiers.CSharpCodeFixVerifier<
    AutoConstructor.Generator.Analyzers.InjectAttributeOnIgnoredFieldAnalyzer,
    AutoConstructor.Generator.CodeFixes.RemoveAttributeCodeFixProvider>;

namespace AutoConstructor.Tests.Analyzers;

public class InjectAttributeOnIgnoredFieldTests
{
    [Fact]
    public async Task Analyzer_InjectAttributeOnIgnoredField_ShouldReportDiagnostic()
    {
        const string test = @"
namespace Test
{
    [AutoConstructor]
    internal class Test
    {
        [{|#0:AutoConstructorInject(""a"", ""a"", typeof(int))|}]
        private readonly int _t = 1;
    }
}";

        DiagnosticResult[] expected = new[] {
                VerifyInjectAttributeOnIgnoredField.Diagnostic(DiagnosticDescriptors.InjectAttributeOnIgnoredFieldDiagnosticId).WithLocation(0),
            };
        await VerifyInjectAttributeOnIgnoredField.VerifyAnalyzerAsync(test, expected);
    }

    [Theory]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [{|#0:AutoConstructorInject(""a"", ""a"", typeof(int))|}]
        private readonly int _t = 1;
    }
}", @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly int _t = 1;
    }
}")]
    public async Task Analyzer_InjectAttributeOnIgnoredField_ShouldFixCode(string test, string fixtest)
    {
        DiagnosticResult[] expected = new[] {
                VerifyInjectAttributeOnIgnoredField.Diagnostic(DiagnosticDescriptors.InjectAttributeOnIgnoredFieldDiagnosticId).WithLocation(0),
            };
        await VerifyInjectAttributeOnIgnoredField.VerifyCodeFixAsync(test, expected, fixtest);
    }
}
