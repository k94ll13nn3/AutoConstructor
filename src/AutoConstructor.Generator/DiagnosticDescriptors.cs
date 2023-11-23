using Microsoft.CodeAnalysis;

namespace AutoConstructor.Generator;

public static class DiagnosticDescriptors
{
    public const string TypeWithoutPartialDiagnosticId = "ACONS01";

    public const string TypeWithoutFieldsToInjectDiagnosticId = "ACONS02";

    public const string IgnoreAttributeOnNonProcessedFieldDiagnosticId = "ACONS03";

    public const string InjectAttributeOnIgnoredFieldDiagnosticId = "ACONS04";

    public const string IgnoreOrInjectAttributeOnTypeWithoutAttributeDiagnosticId = "ACONS05";

    public const string MistmatchTypesDiagnosticId = "ACONS06";

    public const string TypeWithWrongConstructorAccessibilityDiagnosticId = "ACONS07";

    public const string MultipleInitializerMethodsDiagnosticId = "ACONS08";

    public const string InitializerMethodMustReturnVoidDiagnosticId = "ACONS09";

    public const string InitializerMethodMustBeParameterlessDiagnosticId = "ACONS10";

    public static readonly DiagnosticDescriptor TypeWithoutPartialRule = new(
        TypeWithoutPartialDiagnosticId,
        "Couldn't generate constructor",
        $"Type decorated with {Source.AttributeFullName} must be also declared partial",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        null,
        $"https://github.com/k94ll13nn3/AutoConstructor#{TypeWithoutPartialDiagnosticId}",
        WellKnownDiagnosticTags.Build);

    public static readonly DiagnosticDescriptor TypeWithoutFieldsToInjectRule = new(
        TypeWithoutFieldsToInjectDiagnosticId,
        $"Remove {Source.AttributeFullName}",
        $"{Source.AttributeFullName} has no effect on a type without fields to inject",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        null,
        $"https://github.com/k94ll13nn3/AutoConstructor#{TypeWithoutFieldsToInjectDiagnosticId}",
        WellKnownDiagnosticTags.Unnecessary);

    public static readonly DiagnosticDescriptor IgnoreAttributeOnNonProcessedFieldRule = new(
        IgnoreAttributeOnNonProcessedFieldDiagnosticId,
        $"Remove {Source.IgnoreAttributeFullName}",
        $"{Source.IgnoreAttributeFullName} has no effect on a field that cannot be injected",
        "Usage",
        DiagnosticSeverity.Info,
        true,
        null,
        $"https://github.com/k94ll13nn3/AutoConstructor#{IgnoreAttributeOnNonProcessedFieldDiagnosticId}",
        WellKnownDiagnosticTags.Unnecessary);

    public static readonly DiagnosticDescriptor InjectAttributeOnIgnoredFieldRule = new(
        InjectAttributeOnIgnoredFieldDiagnosticId,
        $"Remove {Source.InjectAttributeFullName}",
        $"{Source.InjectAttributeFullName} has no effect on an ignored field",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        null,
        $"https://github.com/k94ll13nn3/AutoConstructor#{InjectAttributeOnIgnoredFieldDiagnosticId}",
        WellKnownDiagnosticTags.Unnecessary);

    public static readonly DiagnosticDescriptor IgnoreOrInjectAttributeOnTypeWithoutAttributeRule = new(
        IgnoreOrInjectAttributeOnTypeWithoutAttributeDiagnosticId,
        $"Remove {Source.InjectAttributeFullName} and {Source.IgnoreAttributeFullName}",
        $"{Source.InjectAttributeFullName} and {Source.IgnoreAttributeFullName} have no effect if the type is not annotated with {Source.AttributeFullName}",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        null,
        $"https://github.com/k94ll13nn3/AutoConstructor#{IgnoreOrInjectAttributeOnTypeWithoutAttributeDiagnosticId}",
        WellKnownDiagnosticTags.Unnecessary);

    public static readonly DiagnosticDescriptor MistmatchTypesRule = new(
        MistmatchTypesDiagnosticId,
        "Couldn't generate constructor",
        "One or more parameter have mismatching types",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        null,
        $"https://github.com/k94ll13nn3/AutoConstructor#{MistmatchTypesDiagnosticId}",
        WellKnownDiagnosticTags.Build);

    public static readonly DiagnosticDescriptor TypeWithWrongConstructorAccessibilityRule = new(
        TypeWithWrongConstructorAccessibilityDiagnosticId,
        "Wrong constuctor accessibility",
        "Unknown constuctor accessibility, allowed values are public, private, protected, internal, protected internal or private protected",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        null,
        $"https://github.com/k94ll13nn3/AutoConstructor#{TypeWithWrongConstructorAccessibilityDiagnosticId}",
        WellKnownDiagnosticTags.Build);

    public static readonly DiagnosticDescriptor MultipleInitializerMethodsRule = new(
        MultipleInitializerMethodsDiagnosticId,
        $"Multiple {Source.InitializerAttributeFullName}",
        $"Multiple {Source.InitializerAttributeFullName} methods are defined, only the first one is called, remove the attributes on the others",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        null,
        $"https://github.com/k94ll13nn3/AutoConstructor#{MultipleInitializerMethodsDiagnosticId}",
        WellKnownDiagnosticTags.Unnecessary);

    public static readonly DiagnosticDescriptor InitializerMethodMustReturnVoidRule = new(
        InitializerMethodMustReturnVoidDiagnosticId,
        $"{Source.InitializerAttributeFullName} on method not returning void",
        $"Method marked with {Source.InitializerAttributeFullName} must return void",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        null,
        $"https://github.com/k94ll13nn3/AutoConstructor#{InitializerMethodMustReturnVoidDiagnosticId}",
        WellKnownDiagnosticTags.Build);

    public static readonly DiagnosticDescriptor InitializerMethodMustBeParameterlessRule = new(
        InitializerMethodMustBeParameterlessDiagnosticId,
        $"{Source.InitializerAttributeFullName} on method with parameter",
        $"Method marked with {Source.InitializerAttributeFullName} must not have parameters",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        null,
        $"https://github.com/k94ll13nn3/AutoConstructor#{InitializerMethodMustBeParameterlessDiagnosticId}",
        WellKnownDiagnosticTags.Build);
}
