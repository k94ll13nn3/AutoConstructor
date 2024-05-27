using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoConstructor.Generator.Extensions;

internal static class AnalyzerConfigOptionsExtensions
{
    public static bool GetBoolValue(this AnalyzerConfigOptions analyzerOptions, string optionName, bool defaultValue)
    {
        bool option = defaultValue;
        if (analyzerOptions.TryGetValue($"build_property.{optionName}", out string? markParameterlessConstructorAsObsoleteSwitch))
        {
            option = defaultValue ?
                !markParameterlessConstructorAsObsoleteSwitch.Equals("false", StringComparison.OrdinalIgnoreCase) :
                markParameterlessConstructorAsObsoleteSwitch.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        return option;
    }

    public static string? GetStringValue(this AnalyzerConfigOptions analyzerOptions, string optionName)
    {
        analyzerOptions.TryGetValue($"build_property.{optionName}", out string? option);
        return option;
    }
}
