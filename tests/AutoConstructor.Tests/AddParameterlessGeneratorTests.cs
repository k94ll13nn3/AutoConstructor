using System.Globalization;
using Xunit;
using VerifySourceGenerator = AutoConstructor.Tests.Verifiers.CSharpSourceGeneratorVerifier<AutoConstructor.Generator.AutoConstructorGenerator>;

namespace AutoConstructor.Tests;

public class AddParameterlessGeneratorTests
{
    [Theory]
    [InlineData("""

namespace Test
{
    [AutoConstructor (accessibility:"internal",addParameterless:true)]
    internal partial class Test
    {
        public int X {get;set;}
    }
}
"""),
        //no base ctor call generated
        InlineData("""

namespace Test
{
    internal class TestBase {
        private readonly int _t;

        public TestBase (int t){
            t= _t;
        }

        public TestBase(){
        }

    }

    [AutoConstructor (accessibility:"internal",addParameterless:true)]
    internal partial class Test: TestBase
    {
        public int X {get;set;}
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
        internal Test()
        {
        }
    }
}
""";
        await VerifySourceGenerator.RunAsync(code, generated, expectedConstructors: Verifiers.ConstructorType.ParameterlessOnly);
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
        /// For serialization only.
        /// </summary>
        public Test()
        {
        }
    }
}
""";
        await VerifySourceGenerator.RunAsync(code, generated, configFileContent: "build_property.AutoConstructor_GenerateConstructorDocumentation = true",
        expectedConstructors: Verifiers.ConstructorType.ParameterlessOnly);
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
        public Test()
        {
        }
    }
}
""";
        await VerifySourceGenerator.RunAsync(code, generated, expectedConstructors: Verifiers.ConstructorType.Both);
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
        public Test()
        {
        }
    }
}
""";
        await VerifySourceGenerator.RunAsync(code, generated, expectedConstructors: Verifiers.ConstructorType.Both);
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
        const string generatedTest = @"namespace Test
{
    partial class Test
    {
        public Test(int t, string s) : base(t, s)
        {
        }
        public Test()
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
        public BaseClass()
        {
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
        public MotherClass()
        {
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code,
        [
            (generatedMother, "Test.MotherClass.g.cs"),
            (generatedBase, "Test.BaseClass.g.cs"),
            (generatedTest, "Test.Test.g.cs")
        ], expectedConstructors: Verifiers.ConstructorType.Both);
    }

    [Theory]
    [InlineData(false, "", "", true)]
    [InlineData(true, "Class {0} comment", "Class Test comment", true)]
    [InlineData(true, "Don't use", "Don't use", true)]
    [InlineData(true, "", "", true)]
    [InlineData(false, "", "", false)]
    public async Task Run_WithConfigForObsoleteMessage_ShouldGenerateType(bool hasCustomComment, string commentConfig, string expectedComment, bool isClass)
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

        string comment = string.Format(CultureInfo.InvariantCulture, commentConfig, "Test");
        if (string.IsNullOrWhiteSpace(comment))
        {
            comment = "For serialization only.";
        }
        string generated = $$"""
namespace Test
{
    partial {{(isClass ? "class" : "struct")}} Test
    {
        /// <summary>
        /// {{comment}}
        /// </summary>
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
build_property.AutoConstructor_ParameterlessConstructorObsoleteMessage = {commentConfig}
";
        }

        await VerifySourceGenerator.RunAsync(code, generated, configFileContent: configFileContent, expectedConstructors: Verifiers.ConstructorType.ParameterlessOnly, expectedObsoleteMessage: expectedComment);
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
        public Test()
        {
        }
    }
}

""";

        const string configFileContent = @"
build_property.AutoConstructor_MarkParameterlessConstructorAsObsolete=false";
        await VerifySourceGenerator.RunAsync(code, generated, configFileContent: configFileContent, expectedConstructors: Verifiers.ConstructorType.ParameterlessOnly, expectedObsoleteMessage: null);
    }
}
