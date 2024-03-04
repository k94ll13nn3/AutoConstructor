using AutoConstructor.Generator;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyClassWithoutFieldsToInject = AutoConstructor.Tests.Verifiers.CSharpCodeFixVerifier<
    AutoConstructor.Generator.Analyzers.TypeWithoutFieldsToInjectAnalyzer,
    AutoConstructor.Generator.CodeFixes.RemoveAttributeCodeFixProvider>;

namespace AutoConstructor.Tests.Analyzers;

public class TypeWithoutFieldsToInjectTests
{
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

        DiagnosticResult[] expected = [
                VerifyClassWithoutFieldsToInject.Diagnostic(DiagnosticDescriptors.TypeWithoutFieldsToInjectDiagnosticId).WithLocation(0),
        ];
        await VerifyClassWithoutFieldsToInject.VerifyAnalyzerAsync(test, expected);
    }

    [Theory]
    [InlineData(@"
namespace Test
{
    internal partial class Test<T>
    {
        public T MyType { get; }

        public Test(T myType)
        {
            MyType = myType;
        }
    }

    [AutoConstructor]
    internal partial class Test2 : Test<int>
    {
        public Test2(int myType) : base(myType)
        {
        }
    }
}")]
    [InlineData(@"
namespace Test
{
    internal partial class Test<T>
    {
        public T MyType { get; }

        private static readonly int _t1 = 3;

        public Test(T myType)
        {
            MyType = myType;
        }
    }

    [AutoConstructor]
    internal partial class Test2 : Test<int>
    {
        public Test2(int myType) : base(myType)
        {
        }
    }
}")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly int _t;
    }

    [AutoConstructor]
    internal partial class Test2 : Test
    {
    }
}")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly int _t;
    }

    [AutoConstructor]
    internal partial class Test2 : Test
    {
    }

    [AutoConstructor]
    internal partial class Test3 : Test2
    {
    }
}")]
    [InlineData(@"
namespace Test
{
    internal partial class Test<T>
    {
        public T MyType { get; }

        public Test(T myType)
        {
            MyType = myType;
        }
    }

    [AutoConstructor]
    internal partial class Test2 : Test<int>
    {
        public Test2(int myType) : base(myType)
        {
        }
    }

    [AutoConstructor]
    internal partial class Test3 : Test2
    {
        public Test3(int myType) : base(myType)
        {
        }
    }
}")]
    public async Task Analyzer_ClassWithoutFieldsToInjectButFieldsOnParent_ShouldNotReportDiagnostic(string test)
    {
        await VerifyClassWithoutFieldsToInject.VerifyAnalyzerAsync(test, []);
    }

    [Theory]
    [InlineData(@"
namespace Test
{
    [{|#0:AutoConstructor|}]
    internal partial class Test
    {
    }
}", @"
namespace Test
{
    internal partial class Test
    {
    }
}")]
    public async Task Analyzer_ClassWithoutFieldsToInject_ShouldFixCode(string test, string fixtest)
    {
        DiagnosticResult[] expected = [
                VerifyClassWithoutFieldsToInject.Diagnostic(DiagnosticDescriptors.TypeWithoutFieldsToInjectDiagnosticId).WithLocation(0),
        ];
        await VerifyClassWithoutFieldsToInject.VerifyCodeFixAsync(test, expected, fixtest);
    }

    [Fact]
    public async Task Fix_Issue106_ShouldNotReportDiagnostic()
    {
        // ACONS02 is expected here but is not the tested case.
        const string additionnalSource = $@"
namespace Test2
{{
    {Source.AttributeText}

    [{{|#0:AutoConstructor|}}]
    public partial class ParentClass
    {{
        public ParentClass(int dependency)
        {{
        }}
    }}
}}";

        const string test = @"
using Test2;
namespace Test
{
    [AutoConstructor]
    public partial class Test : ParentClass
    {
        public Test(int dependency) : base(dependency)
        {
        }
    }
}";
        DiagnosticResult[] expected = [
            VerifyClassWithoutFieldsToInject.Diagnostic(DiagnosticDescriptors.TypeWithoutFieldsToInjectDiagnosticId).WithLocation(0),
        ];

        await VerifyClassWithoutFieldsToInject.VerifyAnalyzerAsync(test, expected, additionnalSource);
    }
}
