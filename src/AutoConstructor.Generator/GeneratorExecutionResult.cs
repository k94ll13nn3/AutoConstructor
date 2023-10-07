using Microsoft.CodeAnalysis;

namespace AutoConstructor.Generator;

internal record GeneratorExectutionResult(
    MainNamedTypeSymbolInfo? Symbol,
    EquatableArray<FieldInfo> Fields,
    Options Options,
    EquatableArray<Diagnostic> Diagnostics);
