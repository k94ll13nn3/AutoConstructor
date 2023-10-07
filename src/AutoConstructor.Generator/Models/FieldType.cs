namespace AutoConstructor.Generator.Models;

[Flags]
internal enum FieldType
{
    None = 0,
    Initialized = 1,
    PassedToBase = 1 << 1,
}
