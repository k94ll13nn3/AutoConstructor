using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyClassWithoutFieldsToInject = AutoConstructor.Tests.Verifiers.CSharpCodeFixVerifier<
    AutoConstructor.Generator.ClassWithoutFieldsToInjectAnalyzer,
    AutoConstructor.Generator.ClassWithoutFieldsToInjectCodeFixProvider>;
using VerifyClassWithoutPartial = AutoConstructor.Tests.Verifiers.CSharpCodeFixVerifier<
    AutoConstructor.Generator.ClassWithoutPartialAnalyzer,
    AutoConstructor.Generator.ClassWithoutPartialCodeFixProvider>;

namespace AutoConstructor.Tests
{
    public class AnalyzerAndCodeFixTests
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
                VerifyClassWithoutPartial.Diagnostic("ACONS01").WithLocation(0),
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
                VerifyClassWithoutPartial.Diagnostic("ACONS01").WithLocation(0),
            };
            await VerifyClassWithoutPartial.VerifyCodeFixAsync(test, expected, fixtest);
        }

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
                VerifyClassWithoutFieldsToInject.Diagnostic("ACONS02").WithLocation(0),
            };
            await VerifyClassWithoutFieldsToInject.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task Analyzer_ClassWithoutFieldsToInject_ShouldFixCode()
        {
            const string test = @"
namespace Test
{
    class TT
    {
    }

    [{|#0:AutoConstructor|}]
    internal partial class Test
    {
    }
}";
            const string fixtest = @"
namespace Test
{
    class TT
    {
    }

    internal partial class Test
    {
    }
}";

            DiagnosticResult[] expected = new[] {
                VerifyClassWithoutFieldsToInject.Diagnostic("ACONS02").WithLocation(0),
            };
            await VerifyClassWithoutFieldsToInject.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
