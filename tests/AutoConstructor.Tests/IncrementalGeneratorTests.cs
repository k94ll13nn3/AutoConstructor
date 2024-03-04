using System.Reflection;
using AutoConstructor.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace AutoConstructor.Tests;

public class IncrementalGeneratorTests
{
    [Theory]
    [InlineData("""

        namespace Test
        {
            [AutoConstructor]
            internal partial class Test
            {
                private readonly int _t;
            }
        }


        """, """

        namespace Test
        {
            [AutoConstructor]
            internal partial class Test
            {
                // Test
                private readonly int _t;
            }
        }


        """, IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged, IncrementalStepRunReason.Cached)]
    [InlineData("""

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
        }
        """, """

        namespace Nested
        {
            internal partial class Outer<T1>
            {
                [AutoConstructor]
                internal partial class Inner<T2>
                {
                    // Some comment
                    private readonly T1 _t1;
                    private readonly T2 _t2;
                }
            }
        }
        """, IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged, IncrementalStepRunReason.Cached)]
    [InlineData("""

        namespace Test
        {
            [AutoConstructor]
            internal partial class Test
            {
                private readonly int _t;
            }
        }


        """, """

        namespace Test
        {
            [AutoConstructor]
            internal partial class Test
            {
                private readonly int _toto;
            }
        }


        """, IncrementalStepRunReason.New, IncrementalStepRunReason.Modified, IncrementalStepRunReason.Modified)]
    [InlineData("""

        namespace Test
        {
            [AutoConstructor]
            internal partial class Test
            {
                private readonly int _t;
            }
        }


        """, """

        namespace Test
        {
            [AutoConstructor]
            internal partial class Test
            {
                private readonly int _t;
                private readonly int _toto = 2;
            }
        }


        """, IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged, IncrementalStepRunReason.Cached)]
    public void CheckGeneratorIsIncremental(
        string source,
        string sourceUpdated,
        IncrementalStepRunReason sourceStepReason,
        IncrementalStepRunReason executeStepReason,
        IncrementalStepRunReason combineStepReason)
    {
        SyntaxTree baseSyntaxTree = CSharpSyntaxTree.ParseText(AppendBaseCode(source));

        Compilation compilation = CreateCompilation(baseSyntaxTree);

        ISourceGenerator sourceGenerator = new AutoConstructorGenerator().AsSourceGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [sourceGenerator],
            driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: true));

        // Run the generator
        driver = driver.RunGenerators(compilation);

        // Update the compilation and rerun the generator
        compilation = compilation.ReplaceSyntaxTree(baseSyntaxTree, CSharpSyntaxTree.ParseText(AppendBaseCode(sourceUpdated)));
        driver = driver.RunGenerators(compilation);

        GeneratorRunResult result = driver.GetRunResult().Results.Single();
        IEnumerable<(object Value, IncrementalStepRunReason Reason)> sourceOuputs =
            result.TrackedOutputSteps.SelectMany(outputStep => outputStep.Value).SelectMany(output => output.Outputs);

        (_, IncrementalStepRunReason Reason) = Assert.Single(sourceOuputs);
        Assert.Equal(sourceStepReason, Reason);
        Assert.Equal(executeStepReason, result.TrackedSteps["Execute"].Single().Outputs[0].Reason);
        Assert.Equal(combineStepReason, result.TrackedSteps["Combine"].Single().Outputs[0].Reason);
    }

    private static CSharpCompilation CreateCompilation(SyntaxTree syntaxTree)
    {
        return CSharpCompilation.Create("compilation",
                [syntaxTree],
                new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.ConsoleApplication));
    }

    private static string AppendBaseCode(string value)
    {
        // Appends the attributes from the generator to the code to be compiled.
        string valueWithCode = value;
        valueWithCode += $"""
            {Source.AttributeText}

            """;
        valueWithCode += $"""
            {Source.IgnoreAttributeText}

            """;
        valueWithCode += $"""
            {Source.InjectAttributeText}

            """;
        return valueWithCode;
    }
}
