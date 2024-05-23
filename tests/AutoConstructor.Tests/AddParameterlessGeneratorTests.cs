using Xunit;
using VerifySourceGenerator = AutoConstructor.Tests.Verifiers.CSharpSourceGeneratorVerifier<AutoConstructor.Generator.AutoConstructorGenerator>;

namespace AutoConstructor.Tests;

public class AddParameterlessGeneratorTests
{
    [Theory]
    [InlineData("""

namespace Test
{
    [AutoConstructor (accessibility: "internal", addParameterless: true)]
    internal partial class Test
    {
        public int X { get; set; }
    }
}
""")]
    [InlineData("""

namespace Test
{
    internal class TestBase
    {
        private readonly int _t;

        public TestBase (int t)
        {
            t = _t;
        }

        public TestBase()
        {
        }

    }

    [AutoConstructor (accessibility: "internal", addParameterless: true)]
    internal partial class Test: TestBase
    {
        public int X { get; set; }
    }
}
""")]
    public async Task Run_WithOptionNothingToInject_ShouldGenerateOneConstructor(string code)
    {
        const string generated = """
namespace Test
{
    partial class Test
    {
        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
        [global::System.ObsoleteAttribute("Not intended for direct usage.", true)]
        internal Test()
        {
        }
    }
}
""";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Run_WithOptionNothingToInjectAndDocs_ShouldGenerateOneConstructor()
    {
        const string code = """

namespace Test
{
    [AutoConstructor (addParameterless:true)]
    internal partial class Test
    {
        public int X {get;set;}
    }
}
""";
        const string generated = """
namespace Test
{
    partial class Test
    {
        /// <summary>
        /// Not intended for direct usage.
        /// </summary>
        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
        [global::System.ObsoleteAttribute("Not intended for direct usage.", true)]
        public Test()
        {
        }
    }
}
""";
        await VerifySourceGenerator.RunAsync(code, generated, configFileContent: "build_property.AutoConstructor_GenerateConstructorDocumentation = true");
    }

    [Fact]
    public async Task Run_WithOptionAndToInject_ShouldGenerateTwoConstructors()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor (addParameterless:true)]
    internal partial class Test
    {
        public int X {get;set;}
        private readonly int _t;
    }
}";
        const string generated = """
namespace Test
{
    partial class Test
    {
        public Test(int t)
        {
            this._t = t;
        }

        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
        [global::System.ObsoleteAttribute("Not intended for direct usage.", true)]
        public Test()
        {
        }
    }
}
""";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Run_WithOptionAndToInject_ShouldGenerateTwoConstructorsSingleDefaultBase()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor (addParameterless:true, addDefaultBaseAttribute: true)]
    internal partial class Test
    {
        public int X {get;set;}
        private readonly int _t;
    }
}";
        const string generated = """
namespace Test
{
    partial class Test
    {
        [AutoConstructorDefaultBase]
        public Test(int t)
        {
            this._t = t;
        }

        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
        [global::System.ObsoleteAttribute("Not intended for direct usage.", true)]
        public Test()
        {
        }
    }
}
""";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    /// <summary>
    /// Note  that : this() won't be generated, because that would call generated constructor marked with [Obsolete].
    /// </summary>
    [Fact]
    public async Task Run_WithMultipleInheritanceAlsoGenerated_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor(addParameterless: true)]
    internal partial class MotherClass
    {
        private readonly string _s;
    }
    [AutoConstructor(addParameterless: true)]
    internal partial class BaseClass : MotherClass
    {
        private readonly int _t;
    }
    [AutoConstructor(addParameterless: true)]
    internal partial class Test : BaseClass
    {
    }
}";
        const string generatedTest = """
namespace Test
{
    partial class Test
    {
        public Test(int t, string s) : base(t, s)
        {
        }

        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
        [global::System.ObsoleteAttribute("Not intended for direct usage.", true)]
        public Test()
        {
        }
    }
}

""";

        const string generatedBase = """
namespace Test
{
    partial class BaseClass
    {
        public BaseClass(int t, string s) : base(s)
        {
            this._t = t;
        }

        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
        [global::System.ObsoleteAttribute("Not intended for direct usage.", true)]
        public BaseClass()
        {
        }
    }
}

""";

        const string generatedMother = """
namespace Test
{
    partial class MotherClass
    {
        public MotherClass(string s)
        {
            this._s = s ?? throw new System.ArgumentNullException(nameof(s));
        }

        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
        [global::System.ObsoleteAttribute("Not intended for direct usage.", true)]
        public MotherClass()
        {
        }
    }
}

""";
        await VerifySourceGenerator.RunAsync(code,
        [
            (generatedMother, "Test.MotherClass.g.cs"),
            (generatedBase, "Test.BaseClass.g.cs"),
            (generatedTest, "Test.Test.g.cs")
        ]);
    }

    [Theory]
    [InlineData(false, "", true)]
    [InlineData(true, "Don't use", true)]
    [InlineData(true, "", true)]
    [InlineData(false, "", false)]
    public async Task Run_WithConfigForObsoleteMessage_ShouldGenerateType(bool hasCustomComment, string comment, bool isClass)
    {
        string code = $$"""
namespace Test
{
    [AutoConstructor(addParameterless: true)]
    internal partial {{(isClass ? "class" : "struct")}} Test
    {
        public int X { get; set; }
    }
}
""";

        if (string.IsNullOrWhiteSpace(comment))
        {
            comment = "Not intended for direct usage.";
        }

        string generated = $$"""
namespace Test
{
    partial {{(isClass ? "class" : "struct")}} Test
    {
        /// <summary>
        /// {{comment}}
        /// </summary>
        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
        [global::System.ObsoleteAttribute("{{comment}}", true)]
        public Test()
        {
        }
    }
}

""";

        string configFileContent = @"
build_property.AutoConstructor_GenerateConstructorDocumentation=true";
        if (hasCustomComment)
        {
            configFileContent += $@"
build_property.AutoConstructor_ParameterlessConstructorObsoleteMessage = {comment}
";
        }

        await VerifySourceGenerator.RunAsync(code, generated, configFileContent: configFileContent);
    }

    [Fact]
    public async Task Run_WithConfigNoObsoleteAttribute_SkipsObsoleteAttribute()
    {
        const string code = """
namespace Test
{
    [AutoConstructor(addParameterless: true)]
    internal partial class Test
    {
        public int X { get; set; }
    }
}
""";

        const string generated = """
namespace Test
{
    partial class Test
    {
        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
        public Test()
        {
        }
    }
}

""";

        const string configFileContent = @"
build_property.AutoConstructor_MarkParameterlessConstructorAsObsolete=false";
        await VerifySourceGenerator.RunAsync(code, generated, configFileContent: configFileContent);
    }
}
