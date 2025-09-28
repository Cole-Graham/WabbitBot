using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WabbitBot.SourceGenerators.Templates;
using WabbitBot.SourceGenerators.Utils;

namespace WabbitBot.SourceGenerators.Generators.Event;

/// <summary>
/// Generates event subscription code for classes marked with [GenerateEventSubscriptions].
/// Creates handlers that subscribe to events and call appropriate methods.
/// </summary>
[Generator]
public class EventSubscriptionGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var isCoreProject = context.CompilationProvider.IsCore();
        // Pipeline for [GenerateEventSubscriptions] handlers
        var handlers = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (node, _) => node.HasAttribute("GenerateEventSubscriptions"),
            transform: (ctx, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) as INamedTypeSymbol;
                if (classSymbol == null)
                    return null;

                // Extract handler info - for now just the class symbol
                return new HandlerInfo(classSymbol.Name, classSymbol);
            })
            .Where(info => info != null)
            .Collect();

        var subSource = handlers.Select((handlerInfos, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            var validHandlers = handlerInfos.Where(h => h != null).Select(h => h!).ToList();
            return GenerateSubscriptionCode(validHandlers);
        });

        context.RegisterSourceOutput(subSource.Combine(isCoreProject), (spc, tuple) =>
        {
            if (!tuple.Right)
                return;
            spc.AddSource("HandlerSubscriptions.g.cs", tuple.Left);
        });
    }

    private SourceText GenerateSubscriptionCode(List<HandlerInfo> handlers)
    {
        var subscriptions = new StringBuilder();

        foreach (var handler in handlers)
        {
            // Analyze handler methods to determine what events they handle
            var handlerMethods = handler.ClassSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.DeclaredAccessibility == Accessibility.Public &&
                           !m.IsStatic &&
                           m.Name.StartsWith("Handle") ||
                           m.GetAttributes().Any(attr => attr.AttributeClass?.Name.Contains("EventHandler") == true));

            foreach (var method in handlerMethods)
            {
                var eventType = InferEventTypeFromMethod(method);
                if (eventType != null)
                {
                    subscriptions.AppendLine($"            globalBus.Subscribe<{eventType}>(async evt =>");
                    subscriptions.AppendLine("            {");
                    subscriptions.AppendLine($"                var handler = new {handler.ClassName}();");
                    subscriptions.AppendLine($"                await handler.{method.Name}(evt);");
                    subscriptions.AppendLine("            });");
                    subscriptions.AppendLine();
                }
            }
        }

        var content = $$"""
            {{CommonTemplates.CreateFileHeader("EventSubscriptionGenerator")}}

            namespace WabbitBot.Core.Common.Events
            {
                /// <summary>
                /// Auto-generated event subscriptions.
                /// This file contains subscription setup for event handlers.
                /// </summary>
                public static class EventSubscriptions
                {
                    /// <summary>
                    /// Registers all auto-generated event subscriptions.
                    /// </summary>
                        public static void RegisterSubscriptions(WabbitBot.Common.Events.IGlobalEventBus globalBus)
                    {
            {{subscriptions}}
                    }
                }
            }
            """;

        return SourceText.From(content, Encoding.UTF8);
    }

    private string? InferEventTypeFromMethod(IMethodSymbol method)
    {
        // Look for event parameter in method
        var eventParam = method.Parameters.FirstOrDefault(p =>
            p.Type.AllInterfaces.Any(i => i.Name == "IEvent"));

        if (eventParam != null)
        {
            return eventParam.Type.ToDisplayString();
        }

        // Fallback: infer from method name
        if (method.Name.StartsWith("Handle"))
        {
            var eventName = method.Name.Substring(6); // Remove "Handle"
            return $"WabbitBot.Core.Common.Events.{eventName}";
        }

        return null;
    }

    private record HandlerInfo(string ClassName, INamedTypeSymbol ClassSymbol);
}
