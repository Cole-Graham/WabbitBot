using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace WabbitBot.SourceGenerators.Generators.Common;

/// <summary>
/// Helper class for writing generated source code
/// </summary>
public static class SourceWriter
{
    /// <summary>
    /// Adds a generated source file to the compilation context
    /// </summary>
    public static void AddSource(GeneratorExecutionContext context, string fileName, string content)
    {
        context.AddSource(fileName, SourceText.From(content, Encoding.UTF8));
    }

    /// <summary>
    /// Creates a standard generated file name
    /// </summary>
    public static string CreateGeneratedFileName(string className, string suffix = "g.cs")
    {
        return $"{className}.{suffix}";
    }

    /// <summary>
    /// Wraps content in a namespace
    /// </summary>
    public static string WrapInNamespace(string content, string namespaceName)
    {
        return $@"namespace {namespaceName}
{{
{content}
}}";
    }

    /// <summary>
    /// Creates a partial class declaration
    /// </summary>
    public static string CreatePartialClass(string className, string content, string? baseClass = null)
    {
        var baseClassDeclaration = string.IsNullOrEmpty(baseClass) ? "" : $" : {baseClass}";
        return $@"public partial class {className}{baseClassDeclaration}
{{
{content}
}}";
    }

    /// <summary>
    /// Creates a static class declaration
    /// </summary>
    public static string CreateStaticClass(string className, string content)
    {
        return $@"public static class {className}
{{
{content}
}}";
    }

    /// <summary>
    /// Creates a method with standard error handling
    /// </summary>
    public static string CreateMethodWithErrorHandling(string methodSignature, string methodBody, string eventType)
    {
        return $@"{methodSignature}
{{
    using var metrics = EventMetrics.Start(""{eventType}"");
    try
    {{
{methodBody}
        metrics.Success();
    }}
    catch (Exception ex)
    {{
        metrics.Failure(ex);
        await ErrorHandler.HandleErrorAsync(ex);
        throw;
    }}
}}";
    }

    /// <summary>
    /// Creates a standard event subscription
    /// </summary>
    public static string CreateEventSubscription(string eventBusInstance, string eventType, string methodName)
    {
        return $@"{eventBusInstance}.Subscribe<{eventType}>(async evt =>
{{
    using var metrics = EventMetrics.Start(""{eventType}"");
    try
    {{
        await {methodName}(evt);
        metrics.Success();
    }}
    catch (Exception ex)
    {{
        metrics.Failure(ex);
        await ErrorHandler.HandleErrorAsync(ex);
        throw;
    }}
}});";
    }

    /// <summary>
    /// Creates a standard request-response subscription
    /// </summary>
    public static string CreateRequestResponseSubscription(string eventBusInstance, string requestType, string responseType, string methodName)
    {
        return $@"{eventBusInstance}.SubscribeRequest<{requestType}, {responseType}>(
    async request =>
    {{
        using var metrics = EventMetrics.Start(""{requestType}"");
        try
        {{
            var response = await {methodName}(request);
            metrics.Success();
            return response;
        }}
        catch (Exception ex)
        {{
            metrics.Failure(ex);
            await ErrorHandler.HandleErrorAsync(ex);
            throw;
        }}
    }});";
    }
}
