using System.Text;
using AutoConstructor.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Text;

namespace AutoConstructor.Tests.Verifiers;

public static class CSharpSourceGeneratorVerifier<TSourceGenerator>
    where TSourceGenerator : IIncrementalGenerator, new()
{
    public static async Task RunAsync(
        string code,
        string? generated = null,
        string? generatedName = "Test.Test.g.cs",
        bool nullable = false,
        IEnumerable<DiagnosticResult>? diagnostics = null,
        IEnumerable<(string filename, SourceText content)>? configFiles = null)
    {
        var test = new CSharpSourceGeneratorVerifier<TSourceGenerator>.Test()
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
                },
            EnableNullable = nullable,
            LanguageVersion = LanguageVersion.Default,
        };

        if (generated is not null)
        {
            test.TestState.GeneratedSources.Add((typeof(AutoConstructorGenerator), generatedName ?? "class.g.cs", SourceText.From(generated, Encoding.UTF8)));
        }

        if (diagnostics is not null)
        {
            test.TestState.ExpectedDiagnostics.AddRange(diagnostics);
        }

        if (configFiles is not null)
        {
            test.TestState.AnalyzerConfigFiles.AddRange(configFiles);
        }

        await test.RunAsync();
    }

    private class Test : CSharpSourceGeneratorTest<TSourceGenerator, XUnitVerifier>
    {
        public bool EnableNullable { get; set; }

        public LanguageVersion LanguageVersion { get; set; }

        protected override CompilationOptions CreateCompilationOptions()
        {
            if (base.CreateCompilationOptions() is not CSharpCompilationOptions compilationOptions)
            {
                throw new InvalidOperationException("Invalid compilation options");
            }

            if (EnableNullable)
            {
                compilationOptions = compilationOptions.WithNullableContextOptions(NullableContextOptions.Enable);
            }

            return compilationOptions
                .WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
        }

        protected override ParseOptions CreateParseOptions()
        {
            return ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
        }
    }
}
