using Microsoft.CodeAnalysis;

namespace AutoConstructor.Generator.Extensions;

internal static class AttributeDataExtension
{
    public static T? GetParameterValue<T>(this AttributeData attributeData, string parameterName)
        where T : class
    {
        if (attributeData.AttributeConstructor is null)
        {
            throw new InvalidOperationException("Missing constructor");
        }

        return attributeData.AttributeConstructor.Parameters.ToList().FindIndex(c => c.Name == parameterName) is int index and not -1
            ? attributeData.ConstructorArguments[index].Value as T
            : null;
    }
}
