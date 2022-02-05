using Microsoft.CodeAnalysis;

namespace AutoConstructor.Generator;

public class FieldInfo
{
    public FieldInfo(
        ITypeSymbol? type,
        string parameterName,
        string fieldName,
        string initializer,
        ITypeSymbol fallbackType,
        bool nullable,
        string? comment,
        bool emitArgumentNullException)
    {
        Type = type;
        ParameterName = parameterName;
        FieldName = fieldName;
        Initializer = initializer;
        FallbackType = fallbackType;
        Nullable = nullable;
        Comment = comment;
        EmitArgumentNullException = emitArgumentNullException;
    }

    public ITypeSymbol? Type { get; }

    public string ParameterName { get; }

    public string FieldName { get; }

    public string Initializer { get; }

    public ITypeSymbol FallbackType { get; }

    public bool Nullable { get; }

    public string? Comment { get; }

    public bool EmitArgumentNullException { get; }
}
