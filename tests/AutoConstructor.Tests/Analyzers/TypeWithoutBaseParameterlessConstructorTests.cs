using AutoConstructor.Generator;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyClassWithoutFieldsToInject = AutoConstructor.Tests.Verifiers.CSharpAnalyzerVerifier<
    AutoConstructor.Generator.Analyzers.TypeWithoutBaseParameterlessConstructorAnalyzer>;

namespace AutoConstructor.Tests.Analyzers;

public class TypeWithoutBaseParameterlessConstructorTests
{
    [Fact]
    public async Task Analyzer_ClassWithoutInheritance_ShouldNotReportDiagnostic()
    {
        const string test = @"
namespace Test
{
    [{|#0:AutoConstructor(addParameterless: true)|}]
    internal partial class Test
    {
    }
}";

        await VerifyClassWithoutFieldsToInject.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task Analyzer_ClassWithCorrectImplicitBaseConstructor_ShouldNotReportDiagnostic()
    {
        const string test = @"
namespace Test
{
    [{|#0:AutoConstructor(addParameterless: true)|}]
    internal partial class Test : TestBase { }

    internal partial class TestBase { }
}";

        await VerifyClassWithoutFieldsToInject.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task Analyzer_ClassWithCorrectExplicitBaseConstructor_ShouldNotReportDiagnostic()
    {
        const string test = @"
namespace Test
{
    [{|#0:AutoConstructor(addParameterless: true)|}]
    internal partial class Test : TestBase { }

    internal partial class TestBase {
        internal TestBase() {}
    }
}";

        await VerifyClassWithoutFieldsToInject.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task Analyzer_ClassWithoutParameterlessOption_ShouldNotReportDiagnostic()
    {
        const string test = @"
namespace Test
{
    [{|#0:AutoConstructor(addParameterless:false)|}]
    internal partial class Test : TestBase
    {
        public Test(int id) : base(id){}
    }

    internal partial class TestBase{
        public TestBase(int id)
        {
            Id = id;
        }

        public int Id { get; }
    }

}";
        await VerifyClassWithoutFieldsToInject.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task Analyzer_ClassWithoutParameterlessBaseConstructor_ShouldReportDiagnostics()
    {
        const string test = @"
namespace Test
{
    [{|#0:AutoConstructor(addParameterless: true)|}]
    internal partial class Test : TestBase
    {
        public Test(int id): base(id){}
    }

    internal partial class TestBase{
        public TestBase(int id)
        {
            Id = id;
        }

        public int Id { get; }
    }

}";
        DiagnosticResult[] expected = [
               VerifyClassWithoutFieldsToInject.Diagnostic(DiagnosticDescriptors.TypeWithoutBaseParameterlessConstructorDiagnosticId).WithLocation(0),
        ];
        await VerifyClassWithoutFieldsToInject.VerifyAnalyzerAsync(test, expected);
    }
}
