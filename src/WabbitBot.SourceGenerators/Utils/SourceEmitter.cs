// # Static: IndentedStringBuilder wrappers, templates (replaces SourceWriter)

using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace WabbitBot.SourceGenerators.Utils;

/// <summary>
/// Modern utility for emitting generated source code with improved performance and readability.
/// Replaces the deprecated SourceWriter.
/// </summary>
public static class SourceEmitter
{
    /// <summary>
    /// Creates a SourceText from a string with UTF-8 encoding.
    /// </summary>
    public static SourceText ToSourceText(this string content)
    {
        return SourceText.From(content, Encoding.UTF8);
    }

    /// <summary>
    /// Creates a standard generated file name.
    /// </summary>
    public static string CreateGeneratedFileName(string className, string suffix = "g.cs")
    {
        return $"{className}.{suffix}";
    }

    /// <summary>
    /// Wraps content in a namespace block.
    /// </summary>
    public static string WrapInNamespace(string content, string namespaceName)
    {
        return $$"""
            namespace {{namespaceName}}
            {
            {{content}}
            }
            """;
    }

    /// <summary>
    /// Creates a partial class declaration with optional base class.
    /// </summary>
    public static string CreatePartialClass(string className, string content, string? baseClass = null)
    {
        var baseClassDeclaration = string.IsNullOrEmpty(baseClass) ? "" : $" : {baseClass}";
        return $$"""
            public partial class {{className}}{{baseClassDeclaration}}
            {
            {{content}}
            }
            """;
    }

    /// <summary>
    /// Creates a partial record declaration.
    /// </summary>
    public static string CreatePartialRecord(string recordName, string content, string? baseClass = null)
    {
        var baseClassDeclaration = string.IsNullOrEmpty(baseClass) ? "" : $" : {baseClass}";
        return $$"""
            public partial record {{recordName}}{{baseClassDeclaration}}
            {
            {{content}}
            }
            """;
    }

    /// <summary>
    /// Creates a using directive block.
    /// </summary>
    public static string CreateUsingDirectives(params string[] namespaces)
    {
        var builder = new StringBuilder();
        foreach (var ns in namespaces)
        {
            builder.AppendLine($"using {ns};");
        }
        return builder.ToString();
    }

    /// <summary>
    /// Creates a method declaration with optional modifiers.
    /// </summary>
    public static string CreateMethod(string returnType, string methodName, string parameters, string body, string modifiers = "public")
    {
        return $$"""
            {{modifiers}} {{returnType}} {{methodName}}({{parameters}})
            {
            {{body}}
            }
            """;
    }

    /// <summary>
    /// Creates a property declaration.
    /// </summary>
    public static string CreateProperty(string type, string name, string? getter = null, string? setter = null, string modifiers = "public")
    {
        var getterBody = getter != null ? $"get => {getter};" : "";
        var setterBody = setter != null ? $"set => {setter};" : "";
        return $$"""
            {{modifiers}} {{type}} {{name}} { {{getterBody}} {{setterBody}} }
            """;
    }

    /// <summary>
    /// Indents each line in the content by the specified number of spaces.
    /// </summary>
    public static string Indent(string content, int spaces = 4)
    {
        var indent = new string(' ', spaces);
        var newline = "\n"; // Hardcoded newline instead of Environment.NewLine
        return string.Join(newline,
            content.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                   .Select(line => string.IsNullOrWhiteSpace(line) ? line : indent + line));
    }

    /// <summary>
    /// Creates XML documentation comment.
    /// </summary>
    public static string CreateXmlDoc(string summary, params (string param, string description)[] parameters)
    {
        var builder = new StringBuilder();
        builder.AppendLine("/// <summary>");
        builder.AppendLine($"/// {summary}");
        builder.Append("/// </summary>");

        foreach (var (param, desc) in parameters)
        {
            builder.AppendLine();
            builder.Append($"/// <param name=\"{param}\">{desc}</param>");
        }

        return builder.ToString();
    }
}