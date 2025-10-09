using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using WabbitBot.Generator.Shared;
using WabbitBot.Generator.Shared.Analyzers;
using WabbitBot.SourceGenerators.Templates; // For CommonTemplates
using WabbitBot.SourceGenerators.Utils; // For SourceEmitter, InferenceHelpers

namespace WabbitBot.SourceGenerators.Templates;

/// <summary>
/// Represents a method that triggers event generation.
/// </summary>
/// <param name="Name">The method name</param>
/// <param name="Parameters">The method parameters as (type, name) tuples</param>
public record MethodInfo(string Name, List<(string type, string name)> Parameters);

/// <summary>
/// Templates for generating event-related code.
/// </summary>
public static class EventTemplates
{
    /// <summary>
    /// Template for event subscription (used when generating handler registrations).
    /// </summary>
    public const string EventSubscription =
        @"
            {0}.Subscribe<{1}>(async evt =>
            {{
                using var metrics = EventMetrics.Start(""{1}"");
                try
                {{
                    await {2}(evt);
                    metrics.Success();
                }}
                catch (Exception ex)
                {{
                    metrics.Failure(ex);
                    await ErrorHandler.HandleErrorAsync(ex);
                    throw;
                }}
            }});";

    /// <summary>
    /// Template for request-response subscription (used when generating handler registrations).
    /// </summary>
    public const string RequestResponseSubscription =
        @"
            {0}.SubscribeRequest<{1}, {2}>(
                async request =>
                {{
                    using var metrics = EventMetrics.Start(""{1}"");
                    try
                    {{
                        var response = await {3}(request);
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

    /// <summary>
    /// Generates event subscriber code for handler classes.
    /// TODO: Expand to analyze handler methods and emit using EventSubscription/RequestResponseSubscription templates.
    /// </summary>
    public static SourceText GenerateSubscriber(List<object> handlerInfos)
    {
        var subscriptions = new StringBuilder();

        // Placeholder: Replace with real logic, e.g.:
        // foreach (var handler in handlerInfos.Cast<HandlerInfo>())
        // {
        //     foreach (var method in GetEventHandlerMethods(handler.ClassSymbol))
        //     {
        //         var eventType = InferEventType(method);
        //         var sub = string.Format(EventSubscription, "globalBus", eventType, $"{handler.ClassName}.{method.Name}");
        //         subscriptions.AppendLine(sub);
        //     }
        // }

        subscriptions.AppendLine("// TODO: Auto-generate subscriptions based on handler methods");

        var content = $$"""
            {{CommonTemplates.CreateFileHeader("EventSubscriptionGenerator")}}
            using WabbitBot.Common.Events.Interfaces;

            namespace WabbitBot.Core.Common.Events;

            /// <summary>
            /// Auto-generated event subscriptions.
            /// This file contains subscription setup for event handlers.
            /// </summary>
            public static class EventSubscriptions
            {
                /// <summary>
                /// Registers all auto-generated event subscriptions.
                /// </summary>
                public static void RegisterSubscriptions(IGlobalEventBus globalBus)
                {
            {{SourceEmitter.Indent(subscriptions.ToString())}}
                }
            }
            """;

        return SourceText.From(content, Encoding.UTF8);
    }

    /// <summary>
    /// Generates event publisher code for boundary classes.
    /// </summary>
    public static SourceText GeneratePublisher(string className, List<MethodInfo> methods, EventBusType busType)
    {
        var publishers = new StringBuilder();
        var dependencies = new[] { ("WabbitBot.Common.Events.EventInterfaces.ICoreEventBus", "_eventBus") };

        foreach (var method in methods)
        {
            var eventName = InferenceHelpers.InferEventName(method.Name);
            var paramList = string.Join(", ", method.Parameters.Select(p => $"{p.type} {p.name}"));
            var paramAssignments = string.Join(", ", method.Parameters.Select(p => $"{p.name}: {p.name}"));

            var publisherMethod = SourceEmitter.CreateMethod(
                returnType: "ValueTask",
                methodName: $"Raise{method.Name}Async",
                parameters: paramList,
                body: $$"""
                {{CommonTemplates.CreateNullCheck("_eventBus")}}
                var @event = new {{eventName}}({{paramAssignments}}, EventBusType = {{busType}});
                {{CommonTemplates.CreateBusPublish("_eventBus", "@event")}}
                """,
                modifiers: "partial"
            );

            publishers.AppendLine(CommonTemplates.CreateGeneratedDoc($"Raises {eventName} event for {method.Name}."));
            publishers.AppendLine(publisherMethod);
            publishers.AppendLine();
        }

        var classContent = CommonTemplates.CreatePartialClassWithDependencies(
            className,
            publishers.ToString(),
            dependencies
        );

        var content = $$"""
            {{CommonTemplates.CreateFileHeader("EventBoundaryGenerator")}}
            {{CommonTemplates.CreateNamespace("WabbitBot.Core", classContent)}}
            """;

        return SourceText.From(content, Encoding.UTF8);
    }
}
