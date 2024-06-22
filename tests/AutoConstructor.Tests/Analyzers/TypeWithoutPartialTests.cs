using AutoConstructor.Generator;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyClassWithoutPartial = AutoConstructor.Tests.Verifiers.CSharpCodeFixVerifier<
    AutoConstructor.Generator.Analyzers.TypeWithoutPartialAnalyzer,
    AutoConstructor.CodeFixes.TypeWithoutPartialCodeFixProvider>;

namespace AutoConstructor.Tests.Analyzers;

public class TypeWithoutPartialTests
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

        DiagnosticResult[] expected = [
                VerifyClassWithoutPartial.Diagnostic(DiagnosticDescriptors.TypeWithoutPartialDiagnosticId).WithLocation(0),
        ];
        await VerifyClassWithoutPartial.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task Analyzer_StructWithoutPartial_ShouldReportDiagnostic()
    {
        const string test = @"
namespace Test
{
    [AutoConstructor]
    internal struct {|#0:Test|}
    {
        private readonly int _t;
    }
}";

        DiagnosticResult[] expected = [
                VerifyClassWithoutPartial.Diagnostic(DiagnosticDescriptors.TypeWithoutPartialDiagnosticId).WithLocation(0),
        ];
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

        DiagnosticResult[] expected = [
                VerifyClassWithoutPartial.Diagnostic(DiagnosticDescriptors.TypeWithoutPartialDiagnosticId).WithLocation(0),
        ];
        await VerifyClassWithoutPartial.VerifyCodeFixAsync(test, expected, fixtest);
    }

    [Fact]
    public async Task CodeFix_StructWithoutPartial_ShouldFixCode()
    {
        const string test = @"
namespace Test
{
    [AutoConstructor]
    internal struct {|#0:Test|}
    {
        private readonly int _t;
    }
}";

        const string fixtest = @"
namespace Test
{
    [AutoConstructor]
    internal partial struct Test
    {
        private readonly int _t;
    }
}";

        DiagnosticResult[] expected = [
                VerifyClassWithoutPartial.Diagnostic(DiagnosticDescriptors.TypeWithoutPartialDiagnosticId).WithLocation(0),
        ];
        await VerifyClassWithoutPartial.VerifyCodeFixAsync(test, expected, fixtest);
    }
}
