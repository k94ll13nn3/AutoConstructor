using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace AutoConstructor.Tests.Verifiers;

public static class CSharpSourceGeneratorVerifier<TSourceGenerator>
    where TSourceGenerator : ISourceGenerator, new()
{
    internal class Test : CSharpSourceGeneratorTest<TSourceGenerator, XUnitVerifier>
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
