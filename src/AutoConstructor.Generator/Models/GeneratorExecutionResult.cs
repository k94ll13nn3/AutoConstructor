using AutoConstructor.Generator.Core;

namespace AutoConstructor.Generator.Models;

internal sealed record GeneratorExectutionResult(MainNamedTypeSymbolInfo? Symbol, EquatableArray<FieldInfo> Fields, ReportedDiagnostic? ReportedDiagnostic);
