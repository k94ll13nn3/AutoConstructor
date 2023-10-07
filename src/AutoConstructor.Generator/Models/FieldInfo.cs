namespace AutoConstructor.Generator.Models;

internal sealed record FieldInfo
{
    public FieldInfo(
        string? type,
        string parameterName,
        string fieldName,
        string initializer,
        string fallbackType,
        bool nullable,
        string? comment,
        bool emitArgumentNullException,
        FieldType fieldType)
    {
        Type = type;
        ParameterName = parameterName;
        FieldName = fieldName;
        Initializer = initializer;
        FallbackType = fallbackType;
        Nullable = nullable;
        Comment = comment;
        EmitArgumentNullException = emitArgumentNullException;
        FieldType = fieldType;
    }

    public string? Type { get; }

    public string ParameterName { get; }

    public string FieldName { get; }

    public string Initializer { get; }

    public string FallbackType { get; }

    public bool Nullable { get; }

    public string? Comment { get; }

    public bool EmitArgumentNullException { get; }

    public FieldType FieldType { get; set; }
}
