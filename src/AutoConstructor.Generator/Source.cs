namespace AutoConstructor.Generator;

public static class Source
{
    internal const string AttributeFullName = "AutoConstructorAttribute";

    internal const string AttributeText = $@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the {nameof(AutoConstructor)} source generator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class {AttributeFullName} : System.Attribute
{{
}}
";

    internal const string IgnoreAttributeFullName = "AutoConstructorIgnoreAttribute";

    internal const string IgnoreAttributeText = $@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the {nameof(AutoConstructor)} source generator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

[System.AttributeUsage(System.AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
internal sealed class {IgnoreAttributeFullName} : System.Attribute
{{
}}
";

    internal const string InjectAttributeFullName = "AutoConstructorInjectAttribute";

    internal const string InjectAttributeText = $@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the {nameof(AutoConstructor)} source generator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

[System.AttributeUsage(System.AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
internal sealed class {InjectAttributeFullName} : System.Attribute
{{
    public {InjectAttributeFullName}(string initializer = null, string parameterName = null, System.Type injectedType = null)
    {{
        Initializer = initializer;
        ParameterName = parameterName;
        InjectedType = injectedType;
    }}

    public string Initializer {{ get; }}

    public string ParameterName {{ get; }}

    public System.Type InjectedType {{ get; }}
}}
";
}
