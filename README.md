# AutoConstructor

[![NuGet](https://img.shields.io/nuget/vpre/AutoConstructor?logo=nuget
)](https://www.nuget.org/packages/AutoConstructor/)
[![GitHub release](https://img.shields.io/github/release/k94ll13nn3/AutoConstructor.svg?logo=github)](https://github.com/k94ll13nn3/AutoConstructor/releases/latest)
[![GitHub license](https://img.shields.io/github/license/k94ll13nn3/AutoConstructor
)](https://raw.githubusercontent.com/k94ll13nn3/AutoConstructor/main/LICENSE)
![ci.yml](https://github.com/k94ll13nn3/AutoConstructor/workflows/.github/workflows/ci.yml/badge.svg)

C# source generator that generates a constructor from readonly fields/properties in a class or struct.

## Installation

- Grab the latest package on [NuGet](https://www.nuget.org/packages/AutoConstructor/).

## Requirements

| Version | Visual Studio | .NET SDK |
|---------|---------------|----------|
| <=1.3.0 | 16.10+        | 5.0.300+ |
| >=2.0.0 | 17.0+         | 6.0.100+ |
| >=5.0.0 | 17.6+         | 7.0.302+ |

## Basic usage

The following code:

```csharp
[AutoConstructor]
public partial class MyClass
{
    private readonly MyDbContext _context;
    private readonly IHttpClientFactory _clientFactory;
    private readonly IService _service;

    [AutoConstructorInject("options?.Value", "options", typeof(IOptions<ApplicationOptions>))]
    private readonly ApplicationOptions _options;
}
```

will generate:

```csharp
partial class MyClass
{
    public MyClass(
        MyApp.MyDbContext context,
        System.Net.Http.IHttpClientFactory clientFactory,
        MyApp.IService service,
        Microsoft.Extensions.Options.IOptions<MyApp.ApplicationOptions> options)
    {
        this._context = context;
        this._clientFactory = clientFactory;
        this._service = service;
        this._options = options?.Value;
    }
}
```

A sample containing more cases is available at the end of this README.

## How to use

For any class where the generator will be used:

- Mark the class or struct as `partial`
- Use `AutoConstructorAttribute` on the class or struct

By default, all `readonly` non-`static` fields without initialization will be used. They will be injected with the same name without any leading `_`.

Fields marked with `AutoConstructorIgnoreAttribute` will be ignored.

Use `AutoConstructorInjectAttribute` to customize the behavior, usually when the injected parameter and the fields
do not have the same type. It takes three optional parameters:

- `initializer`: a string that will be used to initialize the field (by example `myService.GetData()`), default to the `parameterName` if null or empty.
- `parameterName`: the name of the parameter to used in the constructor  (by example `myService`), default to the field name trimmed if null or empty.
- `injectedType`: the type of the parameter to used in the constructor  (by example `IMyService`), default to the field type if null.

If no parameters are provided, the behavior will be the same as without the attribute. Using the attribute on a field that would not be injected otherwise
won't make the field injectable.

When using `AutoConstructorInjectAttribute`, the parameter name can be shared across multiple fields,
and even use a parameter from another field not annotated with `AutoConstructorInjectAttribute`, but type must match.

### Constructor accessibility

Constructor accessibility can be changed using the optional parameter `accessibility` on `AutoConstructorAttribute` (like `[AutoConstructor("internal")]`).
The default is `public` and it can be set to one of the following values:
- `public`
- `private`
- `protected`
- `internal`
- `protected internal`
- `private protected`

### Initializer method

It is possible to add a method call at the end of the constructor. To do this, the attribute `AutoConstructorInitializer` can be added to
a parameterless method that returns void. This will generate a call to the method at the end.

```csharp
[AutoConstructor]
internal partial class Test
{
    private readonly int _t;

    [AutoConstructorInitializer]
    public void Initializer()
    {
    }
}
```

will generate

```csharp
public Test(int t)
{
    this._t = t;

    this.Initializer();
}
```

### Configuring `base` call

It is possible to configure which base constructor is called when a type has a non-object base type and has its constructor generated. By default, a call to `base` is emitted only when the is only one constructor on the base type.
This behavior can be changed by adding a `[AutoConstructorDefaultBase]` on a constructor in the base type to indicate that it must be chosen as the base call.

If the base type is also generated, the `addDefaultBaseAttribute` parameter on `AutoConstructorAttribute` can be used to generate the attribute with the generated constructor.

```csharp
internal class BaseClass
{
    private readonly int _t;

    [AutoConstructorDefaultBase]
    public BaseClass(int t1, int t3)
    {
        this._t = t1 + t3;
    }

    public BaseClass(int t)
    {
        this._t = t;
    }

    public BaseClass()
    {
    }
}

[AutoConstructor]
internal partial class Test : BaseClass
{
    private readonly int _t2;
}
```

will generate

```csharp
partial class Test
{
    public Test(int t2, int t1, int t3) : base(t1, t3)
    {
        this._t2 = t2;
    }
}
```

### Properties injection

Get-only properties (`public int Property { get; }`) are injected by the generator by default.
Non get-only properties (`public int Property { get; set;}`) are injected only if marked with (`[field: AutoConstructorInject]`) attribute.
The behavior of the injection can be modified using auto-implemented property field-targeted attributes on its backing field. The following code show an injected get-only property with a custom injecter:

```csharp
[field: AutoConstructorInject(initializer: "injected.ToString()", injectedType: typeof(int), parameterName: "injected")]
public int Property { get; }
```

⚠️ The compiler support for auto-implemented property field-targeted attributes is not perfect, and Roslyn analyzers are not running on backings fields so some warnings may not be reported.

## Configuration

### Generating `ArgumentNullException`

By default, null checks with `ArgumentNullException` are not generated when needed.

To enable this behavior, set `AutoConstructor_GenerateArgumentNullExceptionChecks` to `true` in the project file:

``` xml
<AutoConstructor_GenerateArgumentNullExceptionChecks>true</AutoConstructor_GenerateArgumentNullExceptionChecks>
```

<details>
  <summary>5.2.X and previous versions</summary>
  
  To enable this behavior, set `AutoConstructor_DisableNullChecking` to `false` in the project file.
</details>

### Generating `this()` calls

By default, if a non-generated parameterless constructor is available on the class (other than the implicit one), a call
to `this()` is generated with the generated constructor.
To disable this behavior, set `AutoConstructor_GenerateThisCalls` to `false` in the project file:

``` xml
<AutoConstructor_GenerateThisCalls>false</AutoConstructor_GenerateThisCalls>
```

This is also configurable at the attribute level with the `DisableThisCall` parameter on `AutoConstructorAttribute` (⚠ it is not possible force the generation at the attribute level if the generation is globally disabled).

### Generating XML documentation comment

By default, no XML documentation comment will be generated for the constructor.
To enable this behavior, set `AutoConstructor_GenerateConstructorDocumentation` to `true` in the project file:

``` xml
<AutoConstructor_GenerateConstructorDocumentation>true</AutoConstructor_GenerateConstructorDocumentation>
```

This will generate a default comment like this one, with each parameter reusing the corresponding field summary if available, and the parameter name otherwise:

``` c#
/// <summary>
/// Initializes a new instance of the Test class.
/// </summary>
/// <param name=""t1"">Some field.</param>
/// <param name=""t2"">t2</param>
```

By using the `AutoConstructor_ConstructorDocumentationComment` property, you can configure the comment message:

``` xml
<AutoConstructor_ConstructorDocumentationComment>Some comment for the {0} class.</AutoConstructor_ConstructorDocumentationComment>
```

This will generate the following code:

``` c#
/// <summary>
/// Some comment for the Test class.
/// </summary>
/// <param name=""t1"">Some field.</param>
/// <param name=""t2"">t2</param>
```

### Generating a parameterless constructor

If needed, a parameterless constructor can also be generated alongside the generated constructor using the `addParameterless` option on `AutoConstructor`.

``` csharp
[AutoConstructor(addParameterless: true)]
public partial class Test
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

will generate

```csharp
partial class Test
{
    #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
    [global::System.ObsoleteAttribute("For serialization only", true)]
    public Test()
    {
    }
}
```

This option can also be used without any fields or properties to inject, this will disable the warning that will normally be reported and generate a parameterless constructor.

:warning: With inheritance, to be able to generate a parameterless constructor, the base type of the target type must have parameterless constructor itself.

By default, this constructor is marked as `[Obsolete]` with a default message. This is configurable with :

``` xml
<AutoConstructor_MarkParameterlessConstructorAsObsolete>false</AutoConstructor_MarkParameterlessConstructorAsObsolete>
<AutoConstructor_ParameterlessConstructorObsoleteMessage>Custom obsolete message</AutoConstructor_ParameterlessConstructorObsoleteMessage>
```

## Samples describing some cases

### Sample for fields

The following code

``` csharp
[AutoConstructor]
partial class Test
{
    private readonly string _name;

    // Won't be injected
    private readonly Uri _uri = new Uri("/non-modified", UriKind.Relative);

    // Won't be injected
    [AutoConstructorIgnore]
    private readonly DateTime _dateNotTaken;

    // Won't be injected because not readonly. Attribute would be taken into account if this were a property, not a field.
    [AutoConstructorInject]
    private int  _stuff;

    // Won't be injected
    private int? _toto;

    // Support for nullables
    private readonly DateTime? _date;

    // Support for generics
    private readonly List<DateTime> _items;

    // Inject with custom initializer
    [AutoConstructorInject("guid.ToString()", "guid", typeof(Guid))]
    private readonly string _guidString;

    // Use existing parameter defined with AutoConstructorInject
    [AutoConstructorInject("guid.ToString().Length", "guid", typeof(Guid))]
    private readonly int _guidLength;

    // Use existing parameter from a basic injection
    [AutoConstructorInject("name.ToUpper()", "name", typeof(string))]
    private readonly string _nameShared;
}
```

will generate

```csharp
public Test(string name, System.DateTime? date, System.Collections.Generic.List<System.DateTime> items, System.Guid guid)
{
    this._name = name ?? throw new System.ArgumentNullException(nameof(name));
    this._date = date ?? throw new System.ArgumentNullException(nameof(date));
    this._items = items ?? throw new System.ArgumentNullException(nameof(items));
    this._guidString = guid.ToString() ?? throw new System.ArgumentNullException(nameof(guid));
    this._guidLength = guid.ToString().Length;
    this._nameShared = name.ToUpper() ?? throw new System.ArgumentNullException(nameof(name));
}
```

### Sample for get-only properties

The following code

``` csharp
[AutoConstructor]
public partial class Test
{
    [field: AutoConstructorInject]
    public int Injected { get; }

    public int AlsoInjectedEvenWhenMissingAttribute { get; }

    /// <summary>
    /// Some property.
    /// </summary>
    [field: AutoConstructorInject]
    public int InjectedWithDocumentation { get; }

    [field: AutoConstructorInject]
    public int InjectedBecauseExplicitInjection { get; set; }

    [field: AutoConstructorInject]
    public static int NotInjectedBecauseStatic { get; }

    [field: AutoConstructorInject]
    public int NotInjectedBecauseInitialized { get; } = 2;

    [field: AutoConstructorIgnore]
    public int NotInjectedBecauseHasIgnoreAttribute { get; }

    [field: AutoConstructorInject(initializer: ""injected.ToString()"", injectedType: typeof(int), parameterName: ""injected"")]
    public string InjectedWithoutCreatingAParam { get; }
}
```

will generate

```csharp
 partial class Test
    {
        /// <summary>
        /// Initializes a new instance of the Test class.
        /// </summary>
        /// <param name=""injected"">injected</param>
        /// <param name=""injectedWithDocumentation"">Some property.</param>
        /// <param name=""injectedBecauseExplicitInjection"">injectedBecauseExplicitInjection</param>
        /// <param name=""alsoInjectedEvenWhenMissingAttribute"">alsoInjectedEvenWhenMissingAttribute</param>
        public Test(int injected, int injectedWithDocumentation, int injectedBecauseExplicitInjection, int alsoInjectedEvenWhenMissingAttribute)
        {
            this.Injected = injected;
            this.InjectedWithDocumentation = injectedWithDocumentation;
            this.InjectedBecauseExplicitInjection = injectedBecauseExplicitInjection;
            this.AlsoInjectedEvenWhenMissingAttribute = alsoInjectedEvenWhenMissingAttribute;
            this.InjectedWithoutCreatingAParam = injected.ToString() ?? throw new System.ArgumentNullException(nameof(injected));
        }
    }
```


## Diagnostics

### ACONS01

The `AutoConstructor` attribute is used on a class that is not partial.

### ACONS02

The `AutoConstructor` attribute is used on a class without fields to inject (without specifying `addParameterless` as true).

### ACONS03

The `AutoConstructorIgnore` attribute is used on a field that won't already be processed.

### ACONS04

The `AutoConstructorInject` attribute is used on a field that won't already be processed.

### ACONS05

The `AutoConstructorIgnore` or `AutoConstructorInject` are used on a class without the `AutoConstructor` attribute.

### ACONS06

A type specified in `AutoConstructorInject` attribute does not match the type of another parameter with the same name.

In the following sample, both fields will be injected with `guid` as parameter name, but one of type `string` and the other of type `Guid`,
preventing the generator from running.

``` csharp
public partial class Test
{
    [AutoConstructorInject("guid.ToString()", "guid", typeof(Guid))]
    private readonly string _guid2;
    private readonly string _guid;
}
```

### ACONS07

The accessibility defined in the `AutoConstructor` attribute is not an allowed value.

### ACONS08

`AutoConstructorInitializer` attribute used on multiple methods inside type.

### ACONS09

`AutoConstructorInitializer` attribute used on a method not returning void.

### ACONS10

`AutoConstructorInitializer` attribute used on a method with parameters.

### ACONS11

`AutoConstructorDefaultBase` attribute used on multiple constructors inside type.

### ACONS12

The `addParameterless` option is used on a type whose base type does not have a parameterless constructor.

### ACONS99

`AutoConstructor_DisableNullChecking` is obsolete.
