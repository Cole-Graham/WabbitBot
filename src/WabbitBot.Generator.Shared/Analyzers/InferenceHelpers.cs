using Microsoft.CodeAnalysis;

namespace WabbitBot.Generator.Shared.Analyzers;

/// <summary>
/// Helper methods for inferring types and names during generation.
/// </summary>
public static class InferenceHelpers
{
    /// <summary>
    /// Infers the event bus type from a symbol's namespace or attributes.
    /// </summary>
    public static EventBusType InferBusType(ISymbol symbol)
    {
        // Check for EventType attribute
        var eventTypeAttr = symbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == AttributeNames.EventType);

        if (eventTypeAttr != null)
        {
            return (EventBusType)eventTypeAttr.ConstructorArguments.FirstOrDefault().Value!;
        }

        // Infer from namespace
        var ns = symbol.ContainingNamespace?.ToDisplayString();
        if (ns?.Contains("DiscBot") == true)
            return EventBusType.DiscBot;
        if (ns?.Contains("Core") == true)
            return EventBusType.Core;

        return EventBusType.Global; // Default
    }

    /// <summary>
    /// Infers an event name from a method name.
    /// </summary>
    public static string InferEventName(string methodName)
    {
        // Convert PascalCase to EventName
        if (methodName.StartsWith("On"))
            return methodName + "Event";

        // Handle common patterns
        if (methodName.StartsWith("Create"))
            return methodName.Replace("Create", "") + "CreatedEvent";
        if (methodName.StartsWith("Update"))
            return methodName.Replace("Update", "") + "UpdatedEvent";
        if (methodName.StartsWith("Delete"))
            return methodName.Replace("Delete", "") + "DeletedEvent";

        return methodName + "Event";
    }

    /// <summary>
    /// Extracts parameter names and types for event payload.
    /// </summary>
    public static IEnumerable<(string Type, string Name)> ExtractEventParameters(IMethodSymbol method)
    {
        return method.Parameters
            .Where(p => p.Type.ToDisplayString() != "DSharpPlus.Commands.InteractionContext") // Skip context
            .Select(p => (p.Type.ToDisplayString(), p.Name));
    }
}
