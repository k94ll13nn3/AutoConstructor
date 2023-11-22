using AutoConstructor.Generator.Extensions;

namespace AutoConstructor.Generator.Models;

internal sealed record FieldInfo(
    string? Type,
    string ParameterName,
    string FieldName,
    string Initializer,
    string FallbackType,
    bool Nullable,
    string? Comment,
    bool EmitArgumentNullException,
    FieldType FieldType)
{
    public string SanitizedParameterName => ParameterName.SanitizeReservedKeyword();
}
