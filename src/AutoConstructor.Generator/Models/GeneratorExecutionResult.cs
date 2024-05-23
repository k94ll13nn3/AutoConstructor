using AutoConstructor.Generator.Core;

namespace AutoConstructor.Generator.Models;

internal sealed record GeneratorExecutionResult(MainNamedTypeSymbolInfo? Symbol, EquatableArray<FieldInfo> Fields, ReportedDiagnostic? ReportedDiagnostic);
