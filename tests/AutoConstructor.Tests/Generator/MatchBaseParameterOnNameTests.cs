using Xunit;
using VerifySourceGenerator = AutoConstructor.Tests.Verifiers.CSharpSourceGeneratorVerifier<AutoConstructor.Generator.AutoConstructorGenerator>;

namespace AutoConstructor.Tests.Generator;

public class MatchBaseParameterOnNameTests
{
    [Fact]
    public async Task Run_WhenMatchBaseParameterOnNameIsTrue_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    public class ParentClass
    {
        private readonly long service;

        public ParentClass(long service)
        {
            this.service = service;
        }
    }

    [AutoConstructor(matchBaseParameterOnName: true)]
    public partial class Test : ParentClass
    {
        private readonly int service;
    }
}";

        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(int service) : base(service)
        {
            this.service = service;
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Run_WhenMatchBaseParameterOnNameIsFalse_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    public class ParentClass
    {
        private readonly long service;

        public ParentClass(long service)
        {
            this.service = service;
        }
    }

    [AutoConstructor(matchBaseParameterOnName: false)]
    public partial class Test : ParentClass
    {
        private readonly int service;
    }
}";

        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(int service, long b0__service) : base(b0__service)
        {
            this.service = service;
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }

    [Fact]
    public async Task Run_WhenMatchBaseParameterOnNameIsFalseWithSameType_ShouldGenerateClass()
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

    [AutoConstructor(matchBaseParameterOnName: false)]
    public partial class Test : ParentClass
    {
        private readonly int service;
    }
}";

        const string generated = @"namespace Test
{
    partial class Test
    {
        public Test(int service) : base(service)
        {
            this.service = service;
        }
    }
}
";
        await VerifySourceGenerator.RunAsync(code, generated);
    }
}
