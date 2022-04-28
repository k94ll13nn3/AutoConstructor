using Microsoft.CodeAnalysis;

// Fix for https://github.com/dotnet/roslyn-analyzers/issues/5828.
#pragma warning disable IDE0090 // Use 'new DiagnosticDescriptor(...)'

namespace AutoConstructor.Generator;

public static class DiagnosticDescriptors
{
    public const string ClassWithoutPartialDiagnosticId = "ACONS01";

    public const string ClassWithoutFieldsToInjectDiagnosticId = "ACONS02";

    public const string IgnoreAttributeOnNonProcessedFieldDiagnosticId = "ACONS03";

    public const string InjectAttributeOnIgnoredFieldDiagnosticId = "ACONS04";

    public const string IgnoreOrInjectAttributeOnClassWithoutAttributeDiagnosticId = "ACONS05";

    public const string MistmatchTypesDiagnosticId = "ACONS06";

    public static readonly DiagnosticDescriptor ClassWithoutPartialRule = new DiagnosticDescriptor(
        ClassWithoutPartialDiagnosticId,
        "Couldn't generate constructor",
        $"Type decorated with {Source.AttributeFullName} must be also declared partial",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        null,
        $"https://github.com/k94ll13nn3/AutoConstructor#{ClassWithoutPartialDiagnosticId}",
        WellKnownDiagnosticTags.Build);

    public static readonly DiagnosticDescriptor ClassWithoutFieldsToInjectRule = new DiagnosticDescriptor(
        ClassWithoutFieldsToInjectDiagnosticId,
        $"Remove {Source.AttributeFullName}",
        $"{Source.AttributeFullName} has no effect on a class without fields to inject",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        null,
        $"https://github.com/k94ll13nn3/AutoConstructor#{ClassWithoutFieldsToInjectDiagnosticId}",
        WellKnownDiagnosticTags.Unnecessary);

    public static readonly DiagnosticDescriptor IgnoreAttributeOnNonProcessedFieldRule = new DiagnosticDescriptor(
        IgnoreAttributeOnNonProcessedFieldDiagnosticId,
        $"Remove {Source.IgnoreAttributeFullName}",
        $"{Source.IgnoreAttributeFullName} has no effect on a field that cannot be injected",
        "Usage",
        DiagnosticSeverity.Info,
        true,
        null,
        $"https://github.com/k94ll13nn3/AutoConstructor#{IgnoreAttributeOnNonProcessedFieldDiagnosticId}",
        WellKnownDiagnosticTags.Unnecessary);

    public static readonly DiagnosticDescriptor InjectAttributeOnIgnoredFieldRule = new DiagnosticDescriptor(
        InjectAttributeOnIgnoredFieldDiagnosticId,
        $"Remove {Source.InjectAttributeFullName}",
        $"{Source.InjectAttributeFullName} has no effect on an ignored field",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        null,
        $"https://github.com/k94ll13nn3/AutoConstructor#{InjectAttributeOnIgnoredFieldDiagnosticId}",
        WellKnownDiagnosticTags.Unnecessary);

    public static readonly DiagnosticDescriptor IgnoreOrInjectAttributeOnClassWithoutAttributeRule = new DiagnosticDescriptor(
        IgnoreOrInjectAttributeOnClassWithoutAttributeDiagnosticId,
        $"Remove {Source.InjectAttributeFullName} and {Source.IgnoreAttributeFullName}",
        $"{Source.InjectAttributeFullName} and {Source.IgnoreAttributeFullName} have no effect if the class is not annotated with {Source.AttributeFullName}",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        null,
        $"https://github.com/k94ll13nn3/AutoConstructor#{IgnoreOrInjectAttributeOnClassWithoutAttributeDiagnosticId}",
        WellKnownDiagnosticTags.Unnecessary);

    public static readonly DiagnosticDescriptor MistmatchTypesRule = new DiagnosticDescriptor(
        MistmatchTypesDiagnosticId,
        "Couldn't generate constructor",
        "One or more parameter have mismatching types",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        null,
        $"https://github.com/k94ll13nn3/AutoConstructor#{MistmatchTypesDiagnosticId}",
        WellKnownDiagnosticTags.Build);
}
