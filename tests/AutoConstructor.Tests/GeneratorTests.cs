using System.Globalization;
using AutoConstructor.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifySourceGenerator = AutoConstructor.Tests.Verifiers.CSharpSourceGeneratorVerifier<AutoConstructor.Generator.AutoConstructorGenerator>;

namespace AutoConstructor.Tests;

public class GeneratorTests
{
    [Fact]
    public async Task Run_WithAttributeAndPartial_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly int _t;
    }
}";
        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(int t)
        {
            this._t = t;
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Run_WithAttributeAndPartial_ShouldGenerateStruct()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial struct Test
    {
        private readonly int _t;
    }
}";
        const string generated = @"namespace Test
{
    partial struct Test
    {
        public Test(int t)
        {
            this._t = t;
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Theory]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorInject(""guid.ToString()"", ""guid"", typeof(System.Guid))]
        private readonly string _guidString;
    }
}", @"namespace Test
{
    partial class Test
    {
        public Test(System.Guid guid)
        {
            this._guidString = guid.ToString() ?? throw new System.ArgumentNullException(nameof(guid));
        }
    }
}
")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorInject(injectedType: typeof(System.Guid), parameterName: ""guid"", initializer: ""guid.ToString()"")]
        private readonly string _guidString;
    }
}", @"namespace Test
{
    partial class Test
    {
        public Test(System.Guid guid)
        {
            this._guidString = guid.ToString() ?? throw new System.ArgumentNullException(nameof(guid));
        }
    }
}
")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorInject(null, ""guid"", typeof(string))]
        private readonly string _guidString;
    }
}", @"namespace Test
{
    partial class Test
    {
        public Test(string guid)
        {
            this._guidString = guid ?? throw new System.ArgumentNullException(nameof(guid));
        }
    }
}
")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly System.Guid _guid;
        [AutoConstructorInject(""guid.ToString()"", ""guid"", null)]
        private readonly string _guidString;
    }
}", @"namespace Test
{
    partial class Test
    {
        public Test(System.Guid guid)
        {
            this._guid = guid;
            this._guidString = guid.ToString() ?? throw new System.ArgumentNullException(nameof(guid));
        }
    }
}
")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorInject(""guid.ToString()"", ""guid"", null)]
        private readonly string _guidString;
    }
}", @"namespace Test
{
    partial class Test
    {
        public Test(string guid)
        {
            this._guidString = guid.ToString() ?? throw new System.ArgumentNullException(nameof(guid));
        }
    }
}
")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorInject(initializer: ""guid.ToString()"", injectedType: typeof(System.Guid))]
        private readonly string _guid;
    }
}", @"namespace Test
{
    partial class Test
    {
        public Test(System.Guid guid)
        {
            this._guid = guid.ToString() ?? throw new System.ArgumentNullException(nameof(guid));
        }
    }
}
")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorInject(parameterName: ""guid"")]
        private readonly string _guidString;
    }
}", @"namespace Test
{
    partial class Test
    {
        public Test(string guid)
        {
            this._guidString = guid ?? throw new System.ArgumentNullException(nameof(guid));
        }
    }
}
")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial struct Test
    {
        [AutoConstructorInject(parameterName: ""guid"")]
        private readonly string _guidString;
    }
}", @"namespace Test
{
    partial struct Test
    {
        public Test(string guid)
        {
            this._guidString = guid ?? throw new System.ArgumentNullException(nameof(guid));
        }
    }
}
")]
    public async Task Run_WithInjectAttribute_ShouldGenerateType(string code, string generated)
    {
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Theory]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
    }
}")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorIgnore]
        private readonly int _ignore;
    }
}")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly int _ignore = 0;
    }
}")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private int _ignore;
    }
}")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial struct Test
    {
        private int _ignore;
    }
}")]
    public async Task Run_NoFieldsToInject_ShouldNotGenerateType(string code)
    {
        await VerifySourceGenerator.RunAsync(code);
    }

    [Fact]
    public async Task Run_WithAttributeAndWithoutPartial_ShouldNotGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal class Test
    {
        private readonly int _t;
    }
}";

        await VerifySourceGenerator.RunAsync(code);
    }

    [Fact]
    public async Task Run_WithAttributeAndWithoutPartial_ShouldNotGenerateStruct()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal struct Test
    {
        private readonly int _t;
    }
}";

        await VerifySourceGenerator.RunAsync(code);
    }

    [Fact]
    public async Task Run_ClassWithoutNamespace_ShouldGenerateClass()
    {
        const string code = @"
[AutoConstructor]
internal partial class Test
{
    private readonly int _t;
}";
        const string generated = @"partial class Test
{
    public Test(int t)
    {
        this._t = t;
    }
}
";

        await VerifySourceGenerator.RunAsync(code, generated, generatedName: "Test.g.cs");
    }

    [Theory]
    [InlineData("t")]
    [InlineData("_t")]
    [InlineData("__t")]
    public async Task Run_IdentifierWithOrWithoutUnderscore_ShouldGenerateSameClass(string identifier)
    {
        string code = $@"
namespace Test
{{
    [AutoConstructor]
    internal partial class Test
    {{
        private readonly int {identifier};
    }}
}}";
        string generated = $@"namespace Test
{{
    partial class Test
    {{
        public Test(int t)
        {{
            this.{identifier} = t;
        }}
    }}
}}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Run_WithMsbuildConfigNullChecks_ShouldGenerateClass(bool disableNullChecks)
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly string _t;
    }
}";
        string generated = $@"namespace Test
{{
    partial class Test
    {{
        public Test(string t)
        {{
            this._t = t{(!disableNullChecks ? " ?? throw new System.ArgumentNullException(nameof(t))" : "")};
        }}
    }}
}}
";

        await VerifySourceGenerator.RunAsync(code, generated, configFileContent: $"build_property.AutoConstructor_DisableNullChecking = {disableNullChecks}");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Run_WithMsbuildConfigGenerateDocumentation_ShouldGenerateClass(bool generateDocumentation)
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        /// <summary>
        /// Some field.
        /// </summary>
        private readonly string _t1;
        private readonly string _t2;
    }
}";

        string generated = @"namespace Test
{
    partial class Test
    {
        public Test(string t1, string t2)
        {
            this._t1 = t1 ?? throw new System.ArgumentNullException(nameof(t1));
            this._t2 = t2 ?? throw new System.ArgumentNullException(nameof(t2));
        }
    }
}
";

        if (generateDocumentation)
        {
            generated = @"namespace Test
{
    partial class Test
    {
        /// <summary>
        /// Initializes a new instance of the Test class.
        /// </summary>
        /// <param name=""t1"">Some field.</param>
        /// <param name=""t2"">t2</param>
        public Test(string t1, string t2)
        {
            this._t1 = t1 ?? throw new System.ArgumentNullException(nameof(t1));
            this._t2 = t2 ?? throw new System.ArgumentNullException(nameof(t2));
        }
    }
}
";
        }

        await VerifySourceGenerator.RunAsync(
            code,
            generated,
            configFileContent: $"build_property.AutoConstructor_GenerateConstructorDocumentation = {generateDocumentation}");
    }

    [Theory]
    [InlineData(false, "", true)]
    [InlineData(true, "Class {0} comment", true)]
    [InlineData(true, "", true)]
    [InlineData(false, "", false)]
    public async Task Run_WithMsbuildConfigGenerateDocumentationWithCustomComment_ShouldGenerateType(bool hasCustomComment, string commentConfig, bool isClass)
    {
        string code = $$"""
namespace Test
{
    [AutoConstructor]
    internal partial {{(isClass ? "class" : "struct")}} Test
    {
        /// <summary>
        /// Some field.
        /// </summary>
        private readonly string _t1;
        private readonly string _t2;
    }
}
""";

        string comment = string.Format(CultureInfo.InvariantCulture, commentConfig, "Test");
        if (string.IsNullOrWhiteSpace(comment))
        {
            comment = $"Initializes a new instance of the Test {(isClass ? "class" : "struct")}.";
        }
        string generated = $@"namespace Test
{{
    partial {(isClass ? "class" : "struct")} Test
    {{
        /// <summary>
        /// {comment}
        /// </summary>
        /// <param name=""t1"">Some field.</param>
        /// <param name=""t2"">t2</param>
        public Test(string t1, string t2)
        {{
            this._t1 = t1 ?? throw new System.ArgumentNullException(nameof(t1));
            this._t2 = t2 ?? throw new System.ArgumentNullException(nameof(t2));
        }}
    }}
}}
";

        string configFileContent = @"
build_property.AutoConstructor_GenerateConstructorDocumentation = true
";

        if (hasCustomComment)
        {
            configFileContent = $@"
build_property.AutoConstructor_GenerateConstructorDocumentation = true
build_property.AutoConstructor_ConstructorDocumentationComment = {commentConfig}
";
        }

        await VerifySourceGenerator.RunAsync(code, generated, configFileContent: configFileContent);
    }

    [Fact]
    public async Task Run_WithMismatchingTypes_ShouldNotGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorInject(""guid.ToString()"", ""guid"", typeof(System.Guid))]
        private readonly string _i;
        private readonly string _guid;
    }
}";

        DiagnosticResult diagnosticResult = new DiagnosticResult(DiagnosticDescriptors.MistmatchTypesDiagnosticId, DiagnosticSeverity.Error).WithSpan(4, 5, 10, 6);
        await VerifySourceGenerator.RunAsync(code, diagnostics: new[] { diagnosticResult });
    }

    [Fact]
    public async Task Run_WithMismatchingTypesAndFallbackTypes_ShouldNotGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorInject(null, ""t"", null)]
        private readonly int _a;
        [AutoConstructorInject(null, ""t"", typeof(string))]
        private readonly int _b;
        [AutoConstructorInject(null, ""t"", null)]
        private readonly int _c;
        [AutoConstructorInject(null, ""t"", typeof(string))]
        private readonly int _d;
    }
}";

        DiagnosticResult diagnosticResult = new DiagnosticResult(DiagnosticDescriptors.MistmatchTypesDiagnosticId, DiagnosticSeverity.Error).WithSpan(4, 5, 15, 6);
        await VerifySourceGenerator.RunAsync(code, diagnostics: new[] { diagnosticResult });
    }

    [Fact]
    public async Task Run_WithMismatchingFallbackTypes_ShouldNotGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorInject(null, ""guid"", null)]
        private readonly string _i;

        [AutoConstructorInject(null, ""guid"", null)]
        private readonly System.Guid _guid;
    }
}";

        DiagnosticResult diagnosticResult = new DiagnosticResult(DiagnosticDescriptors.MistmatchTypesDiagnosticId, DiagnosticSeverity.Error).WithSpan(4, 5, 12, 6);
        await VerifySourceGenerator.RunAsync(code, diagnostics: new[] { diagnosticResult });
    }

    [Fact]
    public async Task Run_WithAliasForAttribute_ShouldGenerateClass()
    {
        const string code = @"using Alias = AutoConstructorAttribute;
namespace Test
{
    [Alias]
    internal partial class Test
    {
        private readonly int _t;
    }
}";
        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(int t)
        {
            this._t = t;
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Theory]
    [InlineData(@"namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly string? _t1;
        private readonly string _t2;
        private readonly int _d1;
        private readonly int? _d2;
    }
}", @"#nullable enable
namespace Test
{
    partial class Test
    {
        public Test(string? t1, string t2, int d1, int? d2)
        {
            this._t1 = t1;
            this._t2 = t2 ?? throw new System.ArgumentNullException(nameof(t2));
            this._d1 = d1;
            this._d2 = d2;
        }
    }
}
")]
    [InlineData(@"using System.Threading.Tasks;namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly Task<object?> _t1;
    }
}", @"#nullable enable
namespace Test
{
    partial class Test
    {
        public Test(System.Threading.Tasks.Task<object?> t1)
        {
            this._t1 = t1 ?? throw new System.ArgumentNullException(nameof(t1));
        }
    }
}
")]
    [InlineData(@"using System.Threading.Tasks;namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly Task<Task<object?>> _t1;
    }
}", @"#nullable enable
namespace Test
{
    partial class Test
    {
        public Test(System.Threading.Tasks.Task<System.Threading.Tasks.Task<object?>> t1)
        {
            this._t1 = t1 ?? throw new System.ArgumentNullException(nameof(t1));
        }
    }
}
")]
    [InlineData(@"using System.Threading.Tasks;namespace Test
{
    [AutoConstructor]
    internal partial class Test : TestBase
    {

    }

    internal partial class TestBase
    {
        public readonly Task<Task<object?>> _t1;

        public TestBase(System.Threading.Tasks.Task<System.Threading.Tasks.Task<object?>> t1)
        {
            this._t1 = t1 ?? throw new System.ArgumentNullException(nameof(t1));
        }
    }
}", @"#nullable enable
namespace Test
{
    partial class Test
    {
        public Test(System.Threading.Tasks.Task<System.Threading.Tasks.Task<object?>> t1) : base(t1)
        {
        }
    }
}
")]
    public async Task Run_WithNullableReferenceType_ShouldGenerateClass(string code, string generated)
    {
        await VerifySourceGenerator.RunAsync(code, generated, nullable: true);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Run_WithOrWithoutNullableReferenceType_ShouldGenerateClassWithNullCheck(bool enableBoolean)
    {
        const string code = @"namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly string _t;
    }
}";
        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(string t)
        {
            this._t = t ?? throw new System.ArgumentNullException(nameof(t));
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated, nullable: enableBoolean);
    }

    [Theory]
    [InlineData(@"
namespace Nested
{
    internal partial class Outer
    {
        [AutoConstructor]
        internal partial class Inner
        {
            private readonly int _t;
        }
    }
}", @"namespace Nested
{
    partial class Outer
    {
        partial class Inner
        {
            public Inner(int t)
            {
                this._t = t;
            }
        }
    }
}
", "Nested.Outer.Inner.g.cs")]
    [InlineData(@"
namespace Nested
{
    internal static partial class Outer
    {
        [AutoConstructor]
        internal partial class Inner
        {
            private readonly int _t;
        }
    }
}", @"namespace Nested
{
    static partial class Outer
    {
        partial class Inner
        {
            public Inner(int t)
            {
                this._t = t;
            }
        }
    }
}
", "Nested.Outer.Inner.g.cs")]
    [InlineData(@"
internal static partial class Outer
{
    [AutoConstructor]
    internal partial class Inner
    {
        private readonly int _t;
    }
}", @"static partial class Outer
{
    partial class Inner
    {
        public Inner(int t)
        {
            this._t = t;
        }
    }
}
", "Outer.Inner.g.cs")]
    [InlineData(@"
internal partial class Outer
{
    [AutoConstructor]
    internal partial class Inner
    {
        private readonly int _t;
    }
}", @"partial class Outer
{
    partial class Inner
    {
        public Inner(int t)
        {
            this._t = t;
        }
    }
}
", "Outer.Inner.g.cs")]
    [InlineData(@"
internal partial class Outer1
{
    internal partial class Outer2
    {
        [AutoConstructor]
        internal partial class Inner
        {
            private readonly int _t;
        }
    }
}", @"partial class Outer1
{
    partial class Outer2
    {
        partial class Inner
        {
            public Inner(int t)
            {
                this._t = t;
            }
        }
    }
}
", "Outer1.Outer2.Inner.g.cs")]
    [InlineData(@"
namespace Nested
{
    internal partial class Outer0
    {
        static internal partial class Outer1
        {
            internal partial class Outer2
            {
                [AutoConstructor]
                internal partial class Inner
                {
                    private readonly int _t;
                }
            }
        }
    }
}", @"namespace Nested
{
    partial class Outer0
    {
        static partial class Outer1
        {
            partial class Outer2
            {
                partial class Inner
                {
                    public Inner(int t)
                    {
                        this._t = t;
                    }
                }
            }
        }
    }
}
", "Nested.Outer0.Outer1.Outer2.Inner.g.cs")]
    public async Task Run_WithNestedClass_ShouldGenerateClass(string code, string generated, string generatedName)
    {
        await VerifySourceGenerator.RunAsync(code, generated, generatedName: generatedName);
    }

    [Fact]
    public async Task Run_WithInjectAttributeOnProperties_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [field: AutoConstructorInject]
        public int Injected { get; }

        /// <summary>
        /// Some property.
        /// </summary>
        [field: AutoConstructorInject]
        public int InjectedWithDocumentation { get; }

        [field: AutoConstructorInject]
        public int InjectedBecauseExplicitInjection { get; set; }

        [field: AutoConstructorInject]
        public static int NotInjectedBecauseStatic { get; }

        [field: AutoConstructorInject]
        public int NotInjectedBecauseInitialized { get; } = 2;

        public int AlsoInjectedEvenWhenMissingAttribute { get; }

        [field: AutoConstructorIgnore]
        public int NotInjectedBecauseHasIgnoreAttribute { get; }

        [field: AutoConstructorInject(initializer: ""injected.ToString()"", injectedType: typeof(int), parameterName: ""injected"")]
        public string InjectedWithoutCreatingAParam { get; }
    }
}";
        const string generated = @"namespace Test
{
    partial class Test
    {
        /// <summary>
        /// Initializes a new instance of the Test class.
        /// </summary>
        /// <param name=""injected"">injected</param>
        /// <param name=""injectedWithDocumentation"">Some property.</param>
        /// <param name=""injectedBecauseExplicitInjection"">injectedBecauseExplicitInjection</param>
        /// <param name=""alsoInjectedEvenWhenMissingAttribute"">alsoInjectedEvenWhenMissingAttribute</param>
        public Test(int injected, int injectedWithDocumentation, int injectedBecauseExplicitInjection, int alsoInjectedEvenWhenMissingAttribute)
        {
            this.Injected = injected;
            this.InjectedWithDocumentation = injectedWithDocumentation;
            this.InjectedBecauseExplicitInjection = injectedBecauseExplicitInjection;
            this.AlsoInjectedEvenWhenMissingAttribute = alsoInjectedEvenWhenMissingAttribute;
            this.InjectedWithoutCreatingAParam = injected.ToString() ?? throw new System.ArgumentNullException(nameof(injected));
        }
    }
}
";

        await VerifySourceGenerator.RunAsync(code, generated, configFileContent: "build_property.AutoConstructor_GenerateConstructorDocumentation = true");
    }

    [Fact]
    public async Task Run_WithRecordLikeClass_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    public partial class Test
    {
        public string Name { get; }
        public string LastName { get; }
    }
}";
        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(string name, string lastName)
        {
            this.Name = name ?? throw new System.ArgumentNullException(nameof(name));
            this.LastName = lastName ?? throw new System.ArgumentNullException(nameof(lastName));
        }
    }
}
";

        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Run_AllKindsOfFields_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly int _t1;
        public readonly int _t2;
        protected readonly int _t3;
        private static readonly int _t4;
        [AutoConstructorInject]
        private int _t5;
    }
}";
        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(int t1, int t2, int t3)
        {
            this._t1 = t1;
            this._t2 = t2;
            this._t3 = t3;
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Run_OnInheritedWithStaticImplicitConstructor_ShouldGenerateBaseCall()
    {
        const string code = @"
namespace Test
{
    internal class TestBase
    {
        private static readonly int _s = 1;

        private readonly int _t1;

        public TestBase(int t1)
        {
            this._t1 = t1;
        }
    }

    [AutoConstructor]
    internal partial class Test : TestBase
    {
    }
}";
        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(int t1) : base(t1)
        {
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Run_MultiplePartialParts_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly int _i1;
    }

    internal partial class Test
    {
        private readonly int _i2;
    }
}";
        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(int i1, int i2)
        {
            this._i1 = i1;
            this._i2 = i2;
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Run_MultiplePartialPartsAndAttributesInMultipleParts_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly int _i1;
    }

    internal partial class Test
    {
        [AutoConstructorInject(parameterName: ""i1"")]
        private readonly int _i2;
    }
}";
        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(int i1)
        {
            this._i1 = i1;
            this._i2 = i1;
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Run_WithMismatchingTypesWithTwoPartialParts_ShouldReportDiagnosticOnFirstPart()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorInject(""guid.ToString()"", ""guid"", typeof(System.Guid))]
        private readonly string _i;
    }

    internal partial class Test
    {
        private readonly string _guid;
    }
}";

        DiagnosticResult diagnosticResultFirstPart = new DiagnosticResult(DiagnosticDescriptors.MistmatchTypesDiagnosticId, DiagnosticSeverity.Error).WithSpan(4, 5, 9, 6);
        await VerifySourceGenerator.RunAsync(code, diagnostics: new[] { diagnosticResultFirstPart });
    }

    [Theory]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test<T>
    {
        private readonly T _generic;
    }
}", @"namespace Test
{
    partial class Test<T>
    {
        public Test(T generic)
        {
            this._generic = generic;
        }
    }
}
")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test<T1, T2>
    {
        private readonly T1 _generic1;
        private readonly T2 _generic2;
    }
}", @"namespace Test
{
    partial class Test<T1, T2>
    {
        public Test(T1 generic1, T2 generic2)
        {
            this._generic1 = generic1;
            this._generic2 = generic2;
        }
    }
}
", "Test.Test.T1.T2.g.cs")]
    [InlineData(@"
namespace Test
{
    [AutoConstructor]
    internal partial class Test<T> where T : class
    {
        private readonly T _generic;
    }
}", @"namespace Test
{
    partial class Test<T>
    {
        public Test(T generic)
        {
            this._generic = generic ?? throw new System.ArgumentNullException(nameof(generic));
        }
    }
}
")]
    [InlineData(@"
namespace Nested
{
    internal partial class Outer<T>
    {
        [AutoConstructor]
        internal partial class Inner
        {
            private readonly T _t;
        }
    }
}", @"namespace Nested
{
    partial class Outer<T>
    {
        partial class Inner
        {
            public Inner(T t)
            {
                this._t = t;
            }
        }
    }
}
", "Nested.Outer.Inner.g.cs")]
    [InlineData(@"
namespace Nested
{
    internal partial class Outer<T1>
    {
        [AutoConstructor]
        internal partial class Inner<T2>
        {
            private readonly T1 _t1;
            private readonly T2 _t2;
        }
    }
}", @"namespace Nested
{
    partial class Outer<T1>
    {
        partial class Inner<T2>
        {
            public Inner(T1 t1, T2 t2)
            {
                this._t1 = t1;
                this._t2 = t2;
            }
        }
    }
}
", "Nested.Outer.Inner.T2.g.cs")]
    [InlineData(@"
namespace Test
{
    interface IThing<T> {}

    [AutoConstructor]
    internal partial class Test<T>
    {
        private readonly IThing<T> _generic;
    }
}", @"namespace Test
{
    partial class Test<T>
    {
        public Test(Test.IThing<T> generic)
        {
            this._generic = generic ?? throw new System.ArgumentNullException(nameof(generic));
        }
    }
}
")]
    public async Task Run_WithGenericClass_ShouldGenerateClass(string code, string generated, string generatedName = "Test.Test.T.g.cs")
    {
        await VerifySourceGenerator.RunAsync(code, generated, generatedName: generatedName);
    }

    [Fact]
    public async Task Run_WithBasicInheritance_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    internal class BaseClass
    {
        private readonly int _t;
        public BaseClass(int t)
        {
            this._t = t;
        }
    }
    [AutoConstructor]
    internal partial class Test : BaseClass
    {
    }
}";
        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(int t) : base(t)
        {
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Theory]
    [InlineData(@"
namespace Test
{
    internal class BaseClass
    {
        private readonly int _t;
        private readonly System.Guid _guid;
        public BaseClass(System.Guid guid, int t)
        {
            this._t = t;
            this._guid = guid;
        }
    }
    [AutoConstructor]
    internal partial class Test : BaseClass
    {
        [AutoConstructorInject(parameterName: ""guid"")]
        private readonly System.Guid _guid2;
    }
}", @"namespace Test
{
    partial class Test
    {
        public Test(System.Guid guid, int t) : base(guid, t)
        {
            this._guid2 = guid;
        }
    }
}
")]
    [InlineData(@"
namespace Test
{
    internal class BaseClass
    {
        private readonly int _t;
        public BaseClass(int t)
        {
            this._t = t;
        }
    }
    [AutoConstructor]
    internal partial class Test : BaseClass
    {
        private readonly System.Guid _guid;
    }
}", @"namespace Test
{
    partial class Test
    {
        public Test(System.Guid guid, int t) : base(t)
        {
            this._guid = guid;
        }
    }
}
")]
    [InlineData(@"
namespace Test
{
    internal class UpperClass
    {
        private readonly System.DateTime _date;
        public UpperClass(System.DateTime date)
        {
            this._date = date;
        }
    }
    internal class BaseClass : UpperClass
    {
        private readonly int _t;
        public BaseClass(System.DateTime date, int t) : base(date)
        {
            this._t = t;
        }
    }
    [AutoConstructor]
    internal partial class Test : BaseClass
    {
        private readonly System.Guid _guid;
    }
}", @"namespace Test
{
    partial class Test
    {
        public Test(System.Guid guid, System.DateTime date, int t) : base(date, t)
        {
            this._guid = guid;
        }
    }
}
")]
    public async Task Run_WithInheritanceAndFieldsInBothClasses_ShouldGenerateClass(string code, string generated)
    {
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Run_WithInheritanceAlsoGenerated_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class BaseClass
    {
        private readonly int _t;
    }
    [AutoConstructor]
    internal partial class Test : BaseClass
    {
    }
}";
        const string generatedTest = @"namespace Test
{
    partial class Test
    {
        public Test(int t) : base(t)
        {
        }
    }
}
";

        const string generatedBase = @"namespace Test
{
    partial class BaseClass
    {
        public BaseClass(int t)
        {
            this._t = t;
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, new[] { (generatedBase, "Test.BaseClass.g.cs"), (generatedTest, "Test.Test.g.cs") });
    }

    [Fact]
    public async Task Run_WithInheritanceAlsoGeneratedWithNullables_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class BaseClass
    {
        private readonly string? _t;
    }
    [AutoConstructor]
    internal partial class Test : BaseClass
    {
    }
}";
        const string generatedTest = @"#nullable enable
namespace Test
{
    partial class Test
    {
        public Test(string? t) : base(t)
        {
        }
    }
}
";

        const string generatedBase = @"#nullable enable
namespace Test
{
    partial class BaseClass
    {
        public BaseClass(string? t)
        {
            this._t = t;
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, new[] { (generatedBase, "Test.BaseClass.g.cs"), (generatedTest, "Test.Test.g.cs") }, nullable: true);
    }

    [Fact]
    public async Task Run_WithMultipleInheritanceAlsoGenerated_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class MotherClass
    {
        private readonly string _s;
    }
    [AutoConstructor]
    internal partial class BaseClass : MotherClass
    {
        private readonly int _t;
    }
    [AutoConstructor]
    internal partial class Test : BaseClass
    {
    }
}";
        const string generatedTest = @"namespace Test
{
    partial class Test
    {
        public Test(int t, string s) : base(t, s)
        {
        }
    }
}
";

        const string generatedBase = @"namespace Test
{
    partial class BaseClass
    {
        public BaseClass(int t, string s) : base(s)
        {
            this._t = t;
        }
    }
}
";

        const string generatedMother = @"namespace Test
{
    partial class MotherClass
    {
        public MotherClass(string s)
        {
            this._s = s ?? throw new System.ArgumentNullException(nameof(s));
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, new[]
        {
            (generatedMother, "Test.MotherClass.g.cs"),
            (generatedBase, "Test.BaseClass.g.cs"),
            (generatedTest, "Test.Test.g.cs")
        });
    }

    [Fact]
    public async Task Run_WithMultipleInheritanceGeneratedAndNotGenerated_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    internal class Class1
    {
        private readonly string _s;
        public Class1(string s)
        {
            this._s = s ?? throw new System.ArgumentNullException(nameof(s));
        }
    }
    [AutoConstructor]
    internal partial class Class2 : Class1
    {
        private readonly int _t;
    }
    [AutoConstructor]
    internal partial class Class3 : Class2
    {
    }
    internal class Class4 : Class3
    {
        public Class4(int t, string s) : base(t, s)
        {
        }
    }
    [AutoConstructor]
    internal partial class Class5 : Class4
    {
    }
}";
        const string generatedClass3 = @"namespace Test
{
    partial class Class3
    {
        public Class3(int t, string s) : base(t, s)
        {
        }
    }
}
";

        const string generatedClass2 = @"namespace Test
{
    partial class Class2
    {
        public Class2(int t, string s) : base(s)
        {
            this._t = t;
        }
    }
}
";

        const string generatedClass5 = @"namespace Test
{
    partial class Class5
    {
        public Class5(int t, string s) : base(t, s)
        {
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, new[]
        {
            (generatedClass2, "Test.Class2.g.cs"),
            (generatedClass3, "Test.Class3.g.cs"),
            (generatedClass5, "Test.Class5.g.cs")
        });
    }

    [Fact]
    public async Task Run_ClassWithParameterlessConstructor_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly int _t;
        public Test(){
            System.Console.WriteLine(""Hello world!"");
        }
    }
}";
        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(int t) : this()
        {
            this._t = t;
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Run_ClassWithParameterlessConstructorAndBaseClass_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    internal class BaseClass
    {
        private readonly int _t;
        public BaseClass(int t)
        {
            this._t = t;
        }
    }
    [AutoConstructor]
    internal partial class Test : BaseClass
    {
        public Test() : base(0)
        {
        }
    }
}";
        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(int t) : base(t)
        {
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Run_ClassWithStaticConstructor_ShouldGenerateClassWithoutCallToStaticConstructor()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        static Test()
        {
        }

        private readonly object _someDependency;
    }
}";
        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(object someDependency)
        {
            this._someDependency = someDependency ?? throw new System.ArgumentNullException(nameof(someDependency));
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Theory]
    [InlineData(null, "public")]
    [InlineData("", "public")]
    [InlineData("public", "public")]
    [InlineData("private", "private")]
    [InlineData("internal", "internal")]
    [InlineData("protected", "protected")]
    [InlineData("protected internal", "protected internal")]
    [InlineData("private protected", "private protected")]
    [InlineData("wrong value", "public")]
    public async Task Run_WithConstructorAccessibility_ShouldGenerateClass(string? attributeAccessibility, string constructorAccessibility)
    {
        string code = $@"
namespace Test
{{
    [AutoConstructor(""{attributeAccessibility}"")]
    internal partial class Test
    {{
        private readonly int _t;
    }}
}}";
        string generated = $@"namespace Test
{{
    partial class Test
    {{
        {constructorAccessibility} Test(int t)
        {{
            this._t = t;
        }}
    }}
}}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Run_WithInitializerMethod_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly int _t;

        [AutoConstructorInitializer]
        public void Initializer()
        {
        }
    }
}";
        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(int t)
        {
            this._t = t;

            this.Initializer();
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Run_WithStaticInitializerMethod_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly int _t;

        [AutoConstructorInitializer]
        public static void Initializer()
        {
        }
    }
}";
        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(int t)
        {
            this._t = t;

            Initializer();
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Run_WithReservedNamesAsParameters_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test : BaseClass
    {
        private readonly int _base;
        private readonly int _if;
        private readonly int _public;
        private readonly int _true;
        [AutoConstructorInject(parameterName: ""struct"")]
        private readonly string _myStruct;
        private readonly int @class;
    }

    internal class BaseClass
    {
        private readonly int _private;
        public BaseClass(int @private)
        {
            this._private = @private;
        }
    }
}";
        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(int @base, int @if, int @public, int @true, string @struct, int @class, int @private) : base(@private)
        {
            this._base = @base;
            this._if = @if;
            this._public = @public;
            this._true = @true;
            this._myStruct = @struct ?? throw new System.ArgumentNullException(nameof(@struct));
            this.@class = @class;
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Run_WithMultipleInjectWithSameParameterName_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorInject(""t.Length"", ""t"", null)]
        private readonly int _a;
        [AutoConstructorInject(null, ""t"", typeof(string))]
        private readonly string _b;
        [AutoConstructorInject(null, ""t"", null)]
        private readonly string _c;
        [AutoConstructorInject(null, ""t"", typeof(string))]
        private readonly string _d;
    }
}";
        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(string t)
        {
            this._a = t.Length;
            this._b = t ?? throw new System.ArgumentNullException(nameof(t));
            this._c = t ?? throw new System.ArgumentNullException(nameof(t));
            this._d = t ?? throw new System.ArgumentNullException(nameof(t));
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Run_NestedTypeWithinDifferentTypeKinds_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    internal partial class A
    {
        public partial interface B
        {
            [AutoConstructor]
            public partial struct C
            {
                private readonly int _t;
            }
        }
    }
}";
        const string generated = @"namespace Test
{
    partial class A
    {
        partial interface B
        {
            partial struct C
            {
                public C(int t)
                {
                    this._t = t;
                }
            }
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated, generatedName: "Test.A.B.C.g.cs");
    }

    [Fact]
    public async Task Run_WithInitializerMethod_ShouldGenerateStruct()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial struct Test
    {
        private readonly int _t;

        [AutoConstructorInitializer]
        public void Initializer()
        {
        }
    }
}";
        const string generated = @"namespace Test
{
    partial struct Test
    {
        public Test(int t)
        {
            this._t = t;

            this.Initializer();
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Fix_Issue106_WorkingCase()
    {
        const string additionnalSource = @"
namespace Test2
{
    public partial class ParentClass
    {
        private int Dependency { get; }
public ParentClass(int dependency)
{
    Dependency = dependency;
}
    }
}";

        const string code = @"
using Test2;
namespace Test
{
    [AutoConstructor]
    public partial class Test : ParentClass
    {
    }
}";
        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(int dependency) : base(dependency)
        {
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated, additionalProjectsSource: additionnalSource);
    }

    [Fact]
    public async Task Fix_Issue106_NotWorkingCase()
    {
        const string additionnalSource = $@"
namespace Test2
{{
    {Source.AttributeText}

    [AutoConstructor]
    public partial class ParentClass
    {{
        public ParentClass(int dependency)
        {{
        }}
    }}
}}";

        const string code = @"
using Test2;
namespace Test
{
    [AutoConstructor]
    public partial class Test : ParentClass
    {
    }
}";
        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(int dependency) : base(dependency)
        {
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated, additionalProjectsSource: additionnalSource);
    }
}
