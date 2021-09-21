namespace AutoConstructor.Generator;

public class FieldInfo
{
    public FieldInfo(string? type, string parameterName, string fieldName, string initializer, string fallbackType)
    {
        Type = type;
        ParameterName = parameterName;
        FieldName = fieldName;
        Initializer = initializer;
        FallbackType = fallbackType;
    }

    public string? Type { get; set; }

    public string ParameterName { get; set; }

    public string FieldName { get; set; }

    public string Initializer { get; set; }

    public string FallbackType { get; set; }
}
