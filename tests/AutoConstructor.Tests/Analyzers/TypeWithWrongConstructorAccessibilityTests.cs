using AutoConstructor.Generator;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyClassWithWrongConstructorAccessibility = AutoConstructor.Tests.Verifiers.CSharpAnalyzerVerifier<
    AutoConstructor.Generator.Analyzers.TypeWithWrongConstructorAccessibilityAnalyzer>;

namespace AutoConstructor.Tests.Analyzers;

public class TypeWithWrongConstructorAccessibilityTests
{
    [Fact]
    public async Task Analyzer_ClassWithWrongConstructorAccessibility_ShouldReportDiagnostic()
    {
        const string test = @"
namespace Test
{
    [AutoConstructor(""wring value"")]
    internal class {|#0:Test|}
    {
        private readonly int _t;
    }
}";

        DiagnosticResult[] expected = [
                VerifyClassWithWrongConstructorAccessibility.Diagnostic(DiagnosticDescriptors.TypeWithWrongConstructorAccessibilityDiagnosticId).WithLocation(0),
        ];
        await VerifyClassWithWrongConstructorAccessibility.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task Analyzer_ClassWithGoodConstructorAccessibility_ShouldNotReportDiagnostic()
    {
        const string test = @"
namespace Test
{
    [AutoConstructor(""public"")]
    internal class {|#0:Test|}
    {
        private readonly int _t;
    }
}";

        await VerifyClassWithWrongConstructorAccessibility.VerifyAnalyzerAsync(test, []);
    }
}
