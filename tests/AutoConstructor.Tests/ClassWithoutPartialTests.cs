using AutoConstructor.Generator;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyClassWithoutPartial = AutoConstructor.Tests.Verifiers.CSharpCodeFixVerifier<
    AutoConstructor.Generator.Analyzers.ClassWithoutPartialAnalyzer,
    AutoConstructor.Generator.CodeFixes.ClassWithoutPartialCodeFixProvider>;

namespace AutoConstructor.Tests;

public class ClassWithoutPartialTests
{
    [Fact]
    public async Task Analyzer_ClassWithoutPartial_ShouldReportDiagnostic()
    {
        const string test = @"
namespace Test
{
    [AutoConstructor]
    internal class {|#0:Test|}
    {
        private readonly int _t;
    }
}";

        DiagnosticResult[] expected = new[] {
                VerifyClassWithoutPartial.Diagnostic(DiagnosticDescriptors.ClassWithoutPartialDiagnosticId).WithLocation(0),
            };
        await VerifyClassWithoutPartial.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task CodeFix_ClassWithoutPartial_ShouldFixCode()
    {
        const string test = @"
namespace Test
{
    [AutoConstructor]
    internal class {|#0:Test|}
    {
        private readonly int _t;
    }
}";

        const string fixtest = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly int _t;
    }
}";

        DiagnosticResult[] expected = new[] {
                VerifyClassWithoutPartial.Diagnostic(DiagnosticDescriptors.ClassWithoutPartialDiagnosticId).WithLocation(0),
            };
        await VerifyClassWithoutPartial.VerifyCodeFixAsync(test, expected, fixtest);
    }
}
