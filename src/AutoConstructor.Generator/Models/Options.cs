namespace AutoConstructor.Generator.Models;

internal sealed record Options(
    bool GenerateConstructorDocumentation,
    string? ConstructorDocumentationComment,
    bool EmitNullChecks,
    bool EmitThisCalls,
    bool MarkParameterlessConstructorAsObsolete,
    string? ParameterlessConstructorObsoleteMessage);
