using System.Threading.Tasks;
using AutoConstructor.Generator;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyIgnoreOrInjectAttributeOnClassWithoutAttribute = AutoConstructor.Tests.Verifiers.CSharpCodeFixVerifier<
    AutoConstructor.Generator.IgnoreOrInjectAttributeOnClassWithoutAttributeAnalyzer,
    AutoConstructor.Generator.RemoveAttributeCodeFixProvider>;

namespace AutoConstructor.Tests
{
    public class IgnoreOrInjectAttributeOnClassWithoutAttributeTests
    {
        [Theory]
        [InlineData(@"
namespace Test
{
    internal class Test
    {
        [{|#0:AutoConstructorInject(""a"", ""a"", typeof(int))|}]
        private readonly int _t = 1;
    }
}")]
        [InlineData(@"
namespace Test
{
    internal class Test
    {
        [{|#0:AutoConstructorIgnore|}]
        private readonly int _t = 1;
    }
}")]
        public async Task Analyzer_IgnoreOrInjectAttributeOnClassWithoutAttribute_ShouldReportDiagnostic(string test)
        {
            DiagnosticResult[] expected = new[] {
                VerifyIgnoreOrInjectAttributeOnClassWithoutAttribute.Diagnostic(IgnoreOrInjectAttributeOnClassWithoutAttributeAnalyzer.DiagnosticId).WithLocation(0),
            };
            await VerifyIgnoreOrInjectAttributeOnClassWithoutAttribute.VerifyAnalyzerAsync(test, expected);
        }

        [Theory]
        [InlineData(@"
namespace Test
{
    internal class Test
    {
        [{|#0:AutoConstructorInject(""a"", ""a"", typeof(int))|}]
        private readonly int _t = 1;
    }
}", @"
namespace Test
{
    internal class Test
    {
        private readonly int _t = 1;
    }
}")]
        [InlineData(@"
namespace Test
{
    internal class Test
    {
        [{|#0:AutoConstructorIgnore|}]
        private readonly int _t = 1;
    }
}", @"
namespace Test
{
    internal class Test
    {
        private readonly int _t = 1;
    }
}")]
        public async Task Analyzer_IgnoreOrInjectAttributeOnClassWithoutAttribute_ShouldFixCode(string test, string fixtest)
        {
            DiagnosticResult[] expected = new[] {
                VerifyIgnoreOrInjectAttributeOnClassWithoutAttribute.Diagnostic(IgnoreOrInjectAttributeOnClassWithoutAttributeAnalyzer.DiagnosticId).WithLocation(0),
            };
            await VerifyIgnoreOrInjectAttributeOnClassWithoutAttribute.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
