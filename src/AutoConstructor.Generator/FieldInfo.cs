namespace AutoConstructor.Generator;

public class FieldInfo
{
    public FieldInfo(string? type, string parameterName, string fieldName, string initializer, string fallbackType, bool nullable, string? comment)
    {
        Type = type;
        ParameterName = parameterName;
        FieldName = fieldName;
        Initializer = initializer;
        FallbackType = fallbackType;
        Nullable = nullable;
        Comment = comment;
    }

    public string? Type { get; }

    public string ParameterName { get; }

    public string FieldName { get; }

    public string Initializer { get; }

    public string FallbackType { get; }

    public bool Nullable { get; }

    public string? Comment { get; }
}
