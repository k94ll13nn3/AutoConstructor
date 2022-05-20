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
    public async Task Run_WithInjectAttribute_ShouldGenerateClass(string code, string generated)
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
    public async Task Run_NoFieldsToInject_ShouldNotGenerateClass(string code)
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
    [InlineData(false, "")]
    [InlineData(true, "Class {0} comment")]
    [InlineData(true, "")]
    public async Task Run_WithMsbuildConfigGenerateDocumentationWithCustomComment_ShouldGenerateClass(bool hasCustomComment, string commentConfig)
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

        string comment = string.Format(CultureInfo.InvariantCulture, commentConfig, "Test");
        if (string.IsNullOrWhiteSpace(comment))
        {
            comment = "Initializes a new instance of the Test class.";
        }
        string generated = $@"namespace Test
{{
    partial class Test
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

    [Fact]
    public async Task Run_WithNullableReferenceType_ShouldGenerateClass()
    {
        const string code = @"namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        private readonly string? _t1;
        private readonly string _t2;
        private readonly int _d1;
        private readonly int? _d2;
    }
}";
        const string generated = @"#nullable enable
namespace Test
{
    partial class Test
    {
        public Test(string? t1, string t2, int d1, int? d2)
        {
            this._t1 = t1;
            this._t2 = t2;
            this._d1 = d1;
            this._d2 = d2;
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated, nullable: true);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Run_WithOrWithoutNullableReferenceType_ShouldGenerateClassWithOrWithoutNullCheck(bool enableBoolean)
    {
        const string code = @"namespace Test
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
            this._t = t{(!enableBoolean ? " ?? throw new System.ArgumentNullException(nameof(t))" : "")};
        }}
    }}
}}
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
        public int NotInjectedBecauseNotReadonly { get; set; }

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
        /// <param name=""alsoInjectedEvenWhenMissingAttribute"">alsoInjectedEvenWhenMissingAttribute</param>
        public Test(int injected, int injectedWithDocumentation, int alsoInjectedEvenWhenMissingAttribute)
        {
            this.Injected = injected;
            this.InjectedWithDocumentation = injectedWithDocumentation;
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
    public async Task Run_WithMismatchingTypesWithTwoPartialParts_ShouldReportDiagnosticOnEachPart()
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
        DiagnosticResult diagnosticResultSecondPart = new DiagnosticResult(DiagnosticDescriptors.MistmatchTypesDiagnosticId, DiagnosticSeverity.Error).WithSpan(11, 5, 14, 6);
        await VerifySourceGenerator.RunAsync(code, diagnostics: new[] { diagnosticResultFirstPart, diagnosticResultSecondPart });
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
")]
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
", "Nested.Outer.Inner.g.cs")]
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
    public async Task Run_WithGenericClass_ShouldGenerateClass(string code, string generated, string generatedName = "Test.Test.g.cs")
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
}
