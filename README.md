# AutoConstructor

C# source generator that generates a constructor from readonly fields in a class.

- Visual Studio 16.9+ is needed
- .NET SDK 5.0.200+ is needed

## How to use

For any class where the generator will be used:

- Mark the class as `partial`
- Use `AutoConstructorAttribute` on the class

By default, all `private readonly` without initialization will be used.

Fields marked with `AutoConstructorIgnoreAttribute` will be ignored.

Use `AutoConstructorInjectAttribute` to customize the behavior, usualy when the injected parameter and the fields
do not have the same type. It takes three parameters:

- `Initializer`: a string that will be used to initialize the field (by example `myService.GetData()`)
- `ParameterName`: the name of the parameter to used in the constructor  (by example `myService`)
- `InjectedType`: the type of the parameter to used in the constructor  (by example `IMyService`)

When using `AutoConstructorInjectAttribute`, the parameter name can be shared across multiple fields,
and even use a parameter from another field not annotated with `AutoConstructorInjectAttribute`, but type must match.

Check [src\AutoConstructor.Sample\Program.cs](src\AutoConstructor.Sample\Program.cs) for additional examples.
