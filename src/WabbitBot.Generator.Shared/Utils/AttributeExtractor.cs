using Microsoft.CodeAnalysis;

namespace WabbitBot.Generator.Shared.Utils;

/// <summary>
/// Static utilities for extracting attribute arguments from symbols.
/// </summary>
public static class AttributeExtractor
{
    /// <summary>
    /// Extracts a string argument from an attribute by parameter name.
    /// </summary>
    public static string? GetAttributeArgument(AttributeData attribute, string parameterName)
    {
        // Check named arguments first
        var namedArg = attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == parameterName);
        if (!namedArg.Equals(default(KeyValuePair<string, TypedConstant>)))
        {
            return namedArg.Value.Value?.ToString();
        }

        // Check positional arguments by finding the parameter index
        var attrClass = attribute.AttributeClass;
        if (attrClass != null)
        {
            var parameters = attrClass.GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.MethodKind == MethodKind.Constructor)?
                .Parameters;

            if (parameters != null)
            {
                var paramIndex = parameters.Value.ToList().FindIndex(p => p.Name == parameterName);
                if (paramIndex >= 0 && paramIndex < attribute.ConstructorArguments.Length)
                {
                    return attribute.ConstructorArguments[paramIndex].Value?.ToString();
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts an enum value from an attribute by parameter name.
    /// </summary>
    public static TEnum? GetAttributeEnumArgument<TEnum>(AttributeData attribute, string parameterName) where TEnum : struct, Enum
    {
        var value = GetAttributeArgument(attribute, parameterName);
        if (value != null && Enum.TryParse(value, out TEnum result))
        {
            return result;
        }
        return null;
    }

    /// <summary>
    /// Extracts an integer argument from an attribute by parameter name.
    /// </summary>
    public static int? GetAttributeIntArgument(AttributeData attribute, string parameterName)
    {
        var value = GetAttributeArgument(attribute, parameterName);
        if (value != null && int.TryParse(value, out int result))
        {
            return result;
        }
        return null;
    }

    /// <summary>
    /// Extracts a boolean argument from an attribute by parameter name.
    /// </summary>
    public static bool? GetAttributeBoolArgument(AttributeData attribute, string parameterName)
    {
        var value = GetAttributeArgument(attribute, parameterName);
        if (value != null && bool.TryParse(value, out bool result))
        {
            return result;
        }
        return null;
    }
}