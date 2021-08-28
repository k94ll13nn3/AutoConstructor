﻿using System.Text;
using AutoConstructor.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
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
        const string generated = @"// <auto-generated />
namespace Test
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
        await new VerifySourceGenerator.Test
        {
            TestState =
                {
                    Sources = { code },
                    GeneratedSources =
                    {
                        (typeof(AutoConstructorGenerator), "AutoConstructorAttribute.cs", SourceText.From(Source.AttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "AutoConstructorIgnoreAttribute.cs", SourceText.From(Source.IgnoreAttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "AutoConstructorInjectAttribute.cs", SourceText.From(Source.InjectAttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "Test.Test.g.cs", SourceText.From(generated, Encoding.UTF8)),
                    }
                }
        }.RunAsync();
    }

    [Fact]
    public async Task Run_WithInjectAttribute_ShouldGenerateClass()
    {
        const string code = @"
namespace Test
{
    [AutoConstructor]
    internal partial class Test
    {
        [AutoConstructorInject(""guid.ToString()"", ""guid"", typeof(System.Guid))]
        private readonly string _guidString;
    }
}";
        const string generated = @"// <auto-generated />
namespace Test
{
    partial class Test
    {
        public Test(System.Guid guid)
        {
            this._guidString = guid.ToString() ?? throw new System.ArgumentNullException(nameof(guid));
        }
    }
}
";
        await new VerifySourceGenerator.Test
        {
            TestState =
                {
                    Sources = { code },
                    GeneratedSources =
                    {
                        (typeof(AutoConstructorGenerator), "AutoConstructorAttribute.cs", SourceText.From(Source.AttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "AutoConstructorIgnoreAttribute.cs", SourceText.From(Source.IgnoreAttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "AutoConstructorInjectAttribute.cs", SourceText.From(Source.InjectAttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "Test.Test.g.cs", SourceText.From(generated, Encoding.UTF8)),
                    }
                }
        }.RunAsync();
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
        await new VerifySourceGenerator.Test
        {
            TestState =
                {
                    Sources = { code },
                    GeneratedSources =
                    {
                        (typeof(AutoConstructorGenerator), "AutoConstructorAttribute.cs", SourceText.From(Source.AttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "AutoConstructorIgnoreAttribute.cs", SourceText.From(Source.IgnoreAttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "AutoConstructorInjectAttribute.cs", SourceText.From(Source.InjectAttributeText, Encoding.UTF8)),
                    }
                }
        }.RunAsync();
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

        await new VerifySourceGenerator.Test
        {
            TestState =
                {
                    Sources = { code },
                    GeneratedSources =
                    {
                        (typeof(AutoConstructorGenerator), "AutoConstructorAttribute.cs", SourceText.From(Source.AttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "AutoConstructorIgnoreAttribute.cs", SourceText.From(Source.IgnoreAttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "AutoConstructorInjectAttribute.cs", SourceText.From(Source.InjectAttributeText, Encoding.UTF8)),
                    }
                }
        }.RunAsync();
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
        const string generated = @"// <auto-generated />
partial class Test
{
    public Test(int t)
    {
        this._t = t;
    }
}
";

        await new VerifySourceGenerator.Test
        {
            TestState =
                {
                    Sources = { code },
                    GeneratedSources =
                    {
                        (typeof(AutoConstructorGenerator), "AutoConstructorAttribute.cs", SourceText.From(Source.AttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "AutoConstructorIgnoreAttribute.cs", SourceText.From(Source.IgnoreAttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "AutoConstructorInjectAttribute.cs", SourceText.From(Source.InjectAttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "Test.g.cs", SourceText.From(generated, Encoding.UTF8)),
                    }
                }
        }.RunAsync();
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
        string generated = $@"// <auto-generated />
namespace Test
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
        await new VerifySourceGenerator.Test
        {
            TestState =
                {
                    Sources = { code },
                    GeneratedSources =
                    {
                        (typeof(AutoConstructorGenerator), "AutoConstructorAttribute.cs", SourceText.From(Source.AttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "AutoConstructorIgnoreAttribute.cs", SourceText.From(Source.IgnoreAttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "AutoConstructorInjectAttribute.cs", SourceText.From(Source.InjectAttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "Test.Test.g.cs", SourceText.From(generated, Encoding.UTF8)),
                    }
                }
        }.RunAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Run_WithMsbuildConfig_ShouldGenerateClass(bool disableNullChecks)
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
        string generated = $@"// <auto-generated />
namespace Test
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
        await new VerifySourceGenerator.Test
        {
            TestState =
                {
                    Sources = { code },
                    GeneratedSources =
                    {
                        (typeof(AutoConstructorGenerator), "AutoConstructorAttribute.cs", SourceText.From(Source.AttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "AutoConstructorIgnoreAttribute.cs", SourceText.From(Source.IgnoreAttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "AutoConstructorInjectAttribute.cs", SourceText.From(Source.InjectAttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "Test.Test.g.cs", SourceText.From(generated, Encoding.UTF8)),
                    },
                AnalyzerConfigFiles = { ("/.editorconfig", $@"
is_global=true
build_property.AutoConstructor_DisableNullChecking = {disableNullChecks}
") }
                }
        }.RunAsync();
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

        await new VerifySourceGenerator.Test
        {
            TestState =
                {
                    Sources = { code },
                    GeneratedSources =
                    {
                        (typeof(AutoConstructorGenerator), "AutoConstructorAttribute.cs", SourceText.From(Source.AttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "AutoConstructorIgnoreAttribute.cs", SourceText.From(Source.IgnoreAttributeText, Encoding.UTF8)),
                        (typeof(AutoConstructorGenerator), "AutoConstructorInjectAttribute.cs", SourceText.From(Source.InjectAttributeText, Encoding.UTF8)),
                    },
                    ExpectedDiagnostics = { new DiagnosticResult(AutoConstructorGenerator.MistmatchTypesDiagnosticId, DiagnosticSeverity.Error).WithSpan(4, 5, 10, 6) },
                }
        }.RunAsync();
    }
}
