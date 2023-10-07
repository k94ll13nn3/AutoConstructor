using AutoConstructor.Generator.Core;
using Microsoft.CodeAnalysis;

namespace AutoConstructor.Generator.Models;

internal sealed record GeneratorExectutionResult(
    MainNamedTypeSymbolInfo? Symbol,
    EquatableArray<FieldInfo> Fields,
    Options Options,
    EquatableArray<Diagnostic> Diagnostics);
