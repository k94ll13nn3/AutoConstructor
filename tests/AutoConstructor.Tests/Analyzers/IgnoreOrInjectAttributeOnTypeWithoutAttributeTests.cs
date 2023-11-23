using AutoConstructor.Generator;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyIgnoreOrInjectAttributeOnTestWithoutAttribute = AutoConstructor.Tests.Verifiers.CSharpCodeFixVerifier<
    AutoConstructor.Generator.Analyzers.IgnoreOrInjectAttributeOnTypeWithoutAttributeAnalyzer,
    AutoConstructor.Generator.CodeFixes.RemoveAttributeCodeFixProvider>;

namespace AutoConstructor.Tests.Analyzers;

public class IgnoreOrInjectAttributeOnTypeWithoutAttributeTests
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
    [InlineData(@"
namespace Test
{
    internal struct Test
    {
        [{|#0:AutoConstructorInject(""a"", ""a"", typeof(int))|}]
        private readonly int _t = 1;

        public Test()
        {
        }
    }
}")]
    public async Task Analyzer_IgnoreOrInjectAttributeOnTypeWithoutAttribute_ShouldReportDiagnostic(string test)
    {
        DiagnosticResult[] expected = [
                VerifyIgnoreOrInjectAttributeOnTestWithoutAttribute.Diagnostic(DiagnosticDescriptors.IgnoreOrInjectAttributeOnTypeWithoutAttributeDiagnosticId).WithLocation(0),
        ];
        await VerifyIgnoreOrInjectAttributeOnTestWithoutAttribute.VerifyAnalyzerAsync(test, expected);
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
    [InlineData(@"
namespace Test
{
    internal struct Test
    {
        [{|#0:AutoConstructorIgnore|}]
        private readonly int _t = 1;

        public Test()
        {
        }
    }
}", @"
namespace Test
{
    internal struct Test
    {
        private readonly int _t = 1;

        public Test()
        {
        }
    }
}")]
    public async Task Analyzer_IgnoreOrInjectAttributeOnTypeWithoutAttribute_ShouldFixCode(string test, string fixtest)
    {
        DiagnosticResult[] expected = [
                VerifyIgnoreOrInjectAttributeOnTestWithoutAttribute.Diagnostic(DiagnosticDescriptors.IgnoreOrInjectAttributeOnTypeWithoutAttributeDiagnosticId).WithLocation(0),
        ];
        await VerifyIgnoreOrInjectAttributeOnTestWithoutAttribute.VerifyCodeFixAsync(test, expected, fixtest);
    }

    [Fact]
    public async Task Analyzer_BothAttributesOnClassWithoutAttribute_ShouldReportDiagnosticAndFixCode()
    {
        const string test = @"
namespace Test
{
    internal class Test
    {
        [{|#0:AutoConstructorIgnore|}]
        [{|#1:AutoConstructorInject(""a"", ""a"", typeof(int))|}]
        private readonly int _t = 1;
    }
}";

        const string fixtest = @"
namespace Test
{
    internal class Test
    {
        private readonly int _t = 1;
    }
}";

        DiagnosticResult[] expected = [
                VerifyIgnoreOrInjectAttributeOnTestWithoutAttribute.Diagnostic(DiagnosticDescriptors.IgnoreOrInjectAttributeOnTypeWithoutAttributeDiagnosticId).WithLocation(0),
            VerifyIgnoreOrInjectAttributeOnTestWithoutAttribute.Diagnostic(DiagnosticDescriptors.IgnoreOrInjectAttributeOnTypeWithoutAttributeDiagnosticId).WithLocation(1),
        ];
        await VerifyIgnoreOrInjectAttributeOnTestWithoutAttribute.VerifyCodeFixAsync(test, expected, fixtest);
    }
}
