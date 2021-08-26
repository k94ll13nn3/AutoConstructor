using AutoConstructor.Generator;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyIgnoreAttributeOnNonProcessedField = AutoConstructor.Tests.Verifiers.CSharpCodeFixVerifier<
    AutoConstructor.Generator.IgnoreAttributeOnNonProcessedFieldAnalyzer,
    AutoConstructor.Generator.RemoveAttributeCodeFixProvider>;

namespace AutoConstructor.Tests;

public class IgnoreAttributeOnNonProcessedFieldTests
{
    [Fact]
    public async Task Analyzer_IgnoreAttributeOnNonProcessedField_ShouldReportDiagnostic()
    {
        const string test = @"
namespace Test
{
    [AutoConstructor]
    internal class Test
    {
        [{|#0:AutoConstructorIgnore|}]
        private readonly int _t = 1;
    }
}";

        DiagnosticResult[] expected = new[] {
                VerifyIgnoreAttributeOnNonProcessedField.Diagnostic(IgnoreAttributeOnNonProcessedFieldAnalyzer.DiagnosticId).WithLocation(0),
            };
        await VerifyIgnoreAttributeOnNonProcessedField.VerifyAnalyzerAsync(test, expected);
    }

    [Theory]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [{|#0:AutoConstructorIgnore|}]
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
    public async Task Analyzer_IgnoreAttributeOnNonProcessedField_ShouldFixCode(string test, string fixtest)
    {
        DiagnosticResult[] expected = new[] {
                VerifyIgnoreAttributeOnNonProcessedField.Diagnostic(IgnoreAttributeOnNonProcessedFieldAnalyzer.DiagnosticId).WithLocation(0),
            };
        await VerifyIgnoreAttributeOnNonProcessedField.VerifyCodeFixAsync(test, expected, fixtest);
    }
}
