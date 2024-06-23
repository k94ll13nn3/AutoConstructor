using Xunit;
using VerifySourceGenerator = AutoConstructor.Tests.Verifiers.CSharpSourceGeneratorVerifier<AutoConstructor.Generator.AutoConstructorGenerator>;

namespace AutoConstructor.Tests;

public class ConflictingParameterNameTests
{
    [Fact]
    public async Task Run_WhenBaseAndChildHaveSameParameterNameWithDifferentType_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    public class ParentClass
    {
        private readonly int service;

        public ParentClass(int service)
        {
            this.service = service;
        }
    }

    [AutoConstructor]
    public partial class Test : ParentClass
    {
        private readonly string service;
    }
}";

        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(string service, int b0__service) : base(b0__service)
        {
            this.service = service ?? throw new global::System.ArgumentNullException(nameof(service));
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Run_WhenBaseAndChildHaveSameParameterNameWithDifferentTypeWhenBaseIsGenerated_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    public partial class ParentClass
    {
        private readonly int service;
    }

    [AutoConstructor]
    public partial class Test : ParentClass
    {
        private readonly string service;
    }
}";

        const string generatedTest = @"namespace Test
{
    partial class Test
    {
        public Test(string service, int b0__service) : base(b0__service)
        {
            this.service = service ?? throw new global::System.ArgumentNullException(nameof(service));
        }
    }
}
";

        const string generatedBase = @"namespace Test
{
    partial class ParentClass
    {
        public ParentClass(int service)
        {
            this.service = service;
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, [(generatedBase, "Test.ParentClass.g.cs"), (generatedTest, "Test.Test.g.cs")]);
    }

    [Fact]
    public async Task Run_WhenBaseAndChildHaveSameParameterNameWithDifferentTypeWith3Levels_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    public partial class ParentClass
    {
        private readonly int service;
    }

    [AutoConstructor]
    public partial class MiddleClass : ParentClass
    {
        private readonly global::System.DateTime service;
    }

    [AutoConstructor]
    public partial class Test : MiddleClass
    {
        private readonly string service;
    }
}";

        const string generatedTest = @"namespace Test
{
    partial class Test
    {
        public Test(string service, global::System.DateTime b0__service, int b1__service) : base(b0__service, b1__service)
        {
            this.service = service ?? throw new global::System.ArgumentNullException(nameof(service));
        }
    }
}
";

        const string generatedMiddle = @"namespace Test
{
    partial class MiddleClass
    {
        public MiddleClass(global::System.DateTime service, int b0__service) : base(b0__service)
        {
            this.service = service;
        }
    }
}
";

        const string generatedBase = @"namespace Test
{
    partial class ParentClass
    {
        public ParentClass(int service)
        {
            this.service = service;
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, [(generatedBase, "Test.ParentClass.g.cs"), (generatedMiddle, "Test.MiddleClass.g.cs"), (generatedTest, "Test.Test.g.cs")]);
    }

    [Fact]
    public async Task Run_WhenBaseAndChildHaveSameParameterNameWithDifferentTypeWith3LevelsButOneInCommon_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    public partial class ParentClass
    {
        private readonly string service;
    }

    [AutoConstructor]
    public partial class MiddleClass : ParentClass
    {
        private readonly System.DateTime service;
    }

    [AutoConstructor]
    public partial class Test : MiddleClass
    {
        private readonly string service;
    }
}";

        const string generatedTest = @"namespace Test
{
    partial class Test
    {
        public Test(string service, global::System.DateTime b0__service) : base(b0__service, service)
        {
            this.service = service ?? throw new global::System.ArgumentNullException(nameof(service));
        }
    }
}
";

        const string generatedMiddle = @"namespace Test
{
    partial class MiddleClass
    {
        public MiddleClass(global::System.DateTime service, string b0__service) : base(b0__service)
        {
            this.service = service;
        }
    }
}
";

        const string generatedBase = @"namespace Test
{
    partial class ParentClass
    {
        public ParentClass(string service)
        {
            this.service = service ?? throw new global::System.ArgumentNullException(nameof(service));
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, [(generatedBase, "Test.ParentClass.g.cs"), (generatedMiddle, "Test.MiddleClass.g.cs"), (generatedTest, "Test.Test.g.cs")]);
    }
}
