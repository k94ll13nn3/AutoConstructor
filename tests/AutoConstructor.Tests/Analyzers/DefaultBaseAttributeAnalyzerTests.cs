using AutoConstructor.Generator;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyClassWithDefaultBase = AutoConstructor.Tests.Verifiers.CSharpAnalyzerVerifier<
    AutoConstructor.Generator.Analyzers.DefaultBaseAttributeAnalyzer>;

namespace AutoConstructor.Tests.Analyzers;

public class DefaultBaseAttributeAnalyzerTests
{
    [Fact]
    public async Task Analyzer_MultipleDefaultBase_ShouldReportDiagnostics()
    {
        const string test = @"
namespace Test
{
    internal class BaseClass
    {
        private readonly int _t;

        public BaseClass(int t)
        {
            this._t = t;
        }

        [{|#0:AutoConstructorDefaultBase|}]
        public BaseClass()
        {
        }

        [{|#1:AutoConstructorDefaultBase|}]
        public BaseClass(int a, int b)
        {
        }
    }
}";

        DiagnosticResult[] expected = [
                VerifyClassWithDefaultBase.Diagnostic(DiagnosticDescriptors.MultipleDefaultBaseDiagnosticId).WithLocation(0),
            VerifyClassWithDefaultBase.Diagnostic(DiagnosticDescriptors.MultipleDefaultBaseDiagnosticId).WithLocation(1),
        ];
        await VerifyClassWithDefaultBase.VerifyAnalyzerAsync(test, expected);
    }
}
