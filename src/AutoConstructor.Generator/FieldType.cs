namespace AutoConstructor.Generator;

[Flags]
public enum FieldType
{
    None = 0,
    Initialized = 1,
    PassedToBase = 1 << 1,
}
