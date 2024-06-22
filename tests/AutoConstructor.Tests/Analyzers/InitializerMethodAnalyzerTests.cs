using AutoConstructor.Generator;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyClassWithInitializerMethod = AutoConstructor.Tests.Verifiers.CSharpCodeFixVerifier<
    AutoConstructor.Generator.Analyzers.InitializerMethodAnalyzer,
    AutoConstructor.CodeFixes.RemoveAttributeCodeFixProvider>;

namespace AutoConstructor.Tests.Analyzers;

public class InitializerMethodAnalyzerTests
{
    [Fact]
    public async Task Analyzer_MultipleInitialierMethods_ShouldReportDiagnostics()
    {
        const string test = @"
namespace Test
{
    [AutoConstructor]
    internal class Test
    {
        private readonly int _t;

        [AutoConstructorInitializer]
        public void Method0()
        {
        }

        [{|#0:AutoConstructorInitializer|}]
        public void Method1()
        {
        }

        [{|#1:AutoConstructorInitializer|}]
        public void Method2()
        {
        }
    }
}";

        DiagnosticResult[] expected = [
                VerifyClassWithInitializerMethod.Diagnostic(DiagnosticDescriptors.MultipleInitializerMethodsRule).WithLocation(0),
            VerifyClassWithInitializerMethod.Diagnostic(DiagnosticDescriptors.MultipleInitializerMethodsRule).WithLocation(1),
        ];
        await VerifyClassWithInitializerMethod.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task Analyzer_InitializerMethodNotReturningVoid_ShouldReportDiagnostic()
    {
        const string test = @"
namespace Test
{
    [AutoConstructor]
    internal class Test
    {
        private readonly int _t;

        [AutoConstructorInitializer]
        public int {|#0:Method1|}()
        {
            return 1;
        }
    }
}";

        DiagnosticResult[] expected = [
                VerifyClassWithInitializerMethod.Diagnostic(DiagnosticDescriptors.InitializerMethodMustReturnVoidRule).WithLocation(0),
        ];
        await VerifyClassWithInitializerMethod.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task Analyzer_InitializerMethodWithParameters_ShouldReportDiagnostic()
    {
        const string test = @"
namespace Test
{
    [AutoConstructor]
    internal class Test
    {
        private readonly int _t;

        [AutoConstructorInitializer]
        public void {|#0:Method1|}(int i)
        {
        }
    }
}";

        DiagnosticResult[] expected = [
                VerifyClassWithInitializerMethod.Diagnostic(DiagnosticDescriptors.InitializerMethodMustBeParameterlessRule).WithLocation(0),
        ];
        await VerifyClassWithInitializerMethod.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task Analyzer_MultipleInitialierMethods_ShouldFixCode()
    {
        const string test = @"
namespace Test
{
    [AutoConstructor]
    internal class Test
    {
        private readonly int _t;

        [AutoConstructorInitializer]
        public void Method0()
        {
        }

        [{|#0:AutoConstructorInitializer|}]
        public void Method1()
        {
        }

        [{|#1:AutoConstructorInitializer|}]
        public void Method2()
        {
        }
    }
}";

        const string fixtest = @"
namespace Test
{
    [AutoConstructor]
    internal class Test
    {
        private readonly int _t;

        [AutoConstructorInitializer]
        public void Method0()
        {
        }

        public void Method1()
        {
        }

        public void Method2()
        {
        }
    }
}";
        DiagnosticResult[] expected = [
                VerifyClassWithInitializerMethod.Diagnostic(DiagnosticDescriptors.MultipleInitializerMethodsRule).WithLocation(0),
            VerifyClassWithInitializerMethod.Diagnostic(DiagnosticDescriptors.MultipleInitializerMethodsRule).WithLocation(1),
        ];
        await VerifyClassWithInitializerMethod.VerifyCodeFixAsync(test, expected, fixtest, 1);
    }
}
