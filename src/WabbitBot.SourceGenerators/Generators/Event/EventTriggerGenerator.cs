using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WabbitBot.Generator.Shared;
using WabbitBot.SourceGenerators.Utils;

namespace WabbitBot.SourceGenerators.Generators.Event;

/// <summary>
/// Generates event publisher implementations for methods marked with [EventTrigger].
/// Only processes classes with [EventGenerator(TriggerMode = "OptIn")].
/// </summary>
[Generator]
public class EventTriggerGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Pipeline: Find classes with [EventGenerator] where TriggerMode = "OptIn"
        var eventGeneratorClasses = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: (node, _) => node.HasAttribute("EventGenerator"),
                transform: (ctx, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) as INamedTypeSymbol;
                    if (classSymbol == null)
                        return null;

                    // Check if TriggerMode = "OptIn"
                    var attr = classSymbol.GetAttributes()
                        .FirstOrDefault(a => a.AttributeClass?.Name == "EventGeneratorAttribute");

                    if (attr == null)
                        return null;

                    var triggerMode = attr.NamedArguments
                        .FirstOrDefault(kvp => kvp.Key == "TriggerMode")
                        .Value.Value as string;

                    if (triggerMode != "OptIn")
                        return null;

                    // Get default bus type
                    var defaultBusType = EventBusType.Core;
                    var defaultBusArg = attr.NamedArguments
                        .FirstOrDefault(kvp => kvp.Key == "DefaultBus");
                    if (defaultBusArg.Value.Value is int busValue)
                    {
                        defaultBusType = (EventBusType)busValue;
                    }

                    // Get GeneratePublishers flag
                    var generatePublishers = attr.NamedArguments
                        .FirstOrDefault(kvp => kvp.Key == "GeneratePublishers")
                        .Value.Value as bool? ?? false;

                    if (!generatePublishers)
                        return null;

                    // Find methods with [EventTrigger]
                    var triggerMethods = classSymbol.GetMembers()
                        .OfType<IMethodSymbol>()
                        .Where(m => m.GetAttributes().Any(a => a.AttributeClass?.Name == "EventTriggerAttribute"))
                        .Select(m => ExtractTriggerMethodInfo(m))
                        .Where(m => m != null)
                        .ToList();

                    if (!triggerMethods.Any())
                        return null;

                    return new EventTriggerClassInfo(
                        classSymbol.Name,
                        classSymbol.ContainingNamespace.ToDisplayString(),
                        defaultBusType,
                        triggerMethods!);
                })
            .Where(info => info != null)
            .Collect();

        // Generate partial class implementations
        context.RegisterSourceOutput(eventGeneratorClasses, (spc, classes) =>
        {
            foreach (var classInfo in classes.Where(c => c != null))
            {
                var source = GeneratePartialClass(classInfo!);
                spc.AddSource($"{classInfo!.ClassName}.EventTriggers.g.cs", source);
            }
        });
    }

    private TriggerMethodInfo? ExtractTriggerMethodInfo(IMethodSymbol method)
    {
        var attr = method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "EventTriggerAttribute");

        if (attr == null)
            return null;

        // Extract BusType
        var busType = EventBusType.Global;
        var busTypeArg = attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "BusType");
        if (busTypeArg.Value.Value is int busValue)
        {
            busType = (EventBusType)busValue;
        }

        // Extract Targets
        var targets = EventTargets.Global;
        var targetsArg = attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "Targets");
        if (targetsArg.Value.Value is int targetsValue)
        {
            targets = (EventTargets)targetsValue;
        }

        // Extract parameters
        var parameters = method.Parameters
            .Select(p => (p.Type.ToDisplayString(), p.Name))
            .ToList();

        return new TriggerMethodInfo(
            method.Name,
            method.ReturnType.ToDisplayString(),
            busType,
            targets,
            parameters);
    }

    private SourceText GeneratePartialClass(EventTriggerClassInfo classInfo)
    {
        var methods = new StringBuilder();

        foreach (var method in classInfo.Methods)
        {
            methods.AppendLine(GenerateTriggerMethod(classInfo, method));
            methods.AppendLine();
        }

        var namespaceDecl = classInfo.Namespace;
        var className = classInfo.ClassName;

        // Determine which usings are needed based on namespace
        var isDiscBot = namespaceDecl.Contains("DiscBot");
        var isCore = namespaceDecl.Contains("Core");

        var usings = new StringBuilder();
        usings.AppendLine("using System;");
        usings.AppendLine("using System.Threading.Tasks;");
        usings.AppendLine("using WabbitBot.Common.Events.EventInterfaces;");

        if (isCore)
        {
            usings.AppendLine("using WabbitBot.Common.Events;");
            usings.AppendLine("using WabbitBot.Core.Common.Events;");
            usings.AppendLine("using WabbitBot.Core.Common.Services;");
        }

        if (isDiscBot)
        {
            usings.AppendLine("using WabbitBot.Common.Events;");
            usings.AppendLine("using WabbitBot.DiscBot.App.Events;");
            usings.AppendLine("using WabbitBot.DiscBot.App.Services.DiscBot;");
        }

        var content = $$"""
            // <auto-generated />
            // This file was generated by EventTriggerGenerator
            // DO NOT EDIT - Changes will be overwritten

            {{usings}}
            namespace {{namespaceDecl}}
            {
                public partial class {{className}}
                {
            {{SourceEmitter.Indent(methods.ToString(), 2)}}
                }
            }
            """;

        return SourceText.From(content, Encoding.UTF8);
    }

    private string GenerateTriggerMethod(EventTriggerClassInfo classInfo, TriggerMethodInfo method)
    {
        var eventName = InferEventName(method.MethodName);
        var paramList = string.Join(", ", method.Parameters.Select(p => $"{p.type} {p.name}"));
        var paramNames = string.Join(", ", method.Parameters.Select(p => p.name));

        var body = new StringBuilder();

        // Determine which bus(es) to publish to
        var publishLocal = (method.Targets & EventTargets.Local) == EventTargets.Local;
        var publishGlobal = (method.Targets & EventTargets.Global) == EventTargets.Global;

        // Special case: Targets.Both means publish to local bus AND global bus
        if (method.Targets == EventTargets.Both)
        {
            var localBus = GetBusAccessor(classInfo.DefaultBusType, classInfo.Namespace);
            var globalBus = GetBusAccessor(EventBusType.Global, classInfo.Namespace);
            body.AppendLine($"await {localBus}.PublishAsync(new {eventName}({paramNames}));");
            body.AppendLine($"await {globalBus}.PublishAsync(new {eventName}({paramNames}));");
        }
        else if (publishLocal)
        {
            var localBus = GetBusAccessor(classInfo.DefaultBusType, classInfo.Namespace);
            body.AppendLine($"await {localBus}.PublishAsync(new {eventName}({paramNames}));");
        }
        else if (publishGlobal)
        {
            var globalBus = GetBusAccessor(EventBusType.Global, classInfo.Namespace);
            body.AppendLine($"await {globalBus}.PublishAsync(new {eventName}({paramNames}));");
        }
        else
        {
            // Default to the BusType specified
            var bus = GetBusAccessor(method.BusType, classInfo.Namespace);
            body.AppendLine($"await {bus}.PublishAsync(new {eventName}({paramNames}));");
        }

        // No return statement needed for async ValueTask methods

        var indentedBody = SourceEmitter.Indent(body.ToString(), 3);

        var sb = new StringBuilder();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Auto-generated event publisher for {eventName}.");
        sb.AppendLine($"        /// Publishes to: {method.Targets}");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async partial ValueTask {method.MethodName}({paramList})");
        sb.AppendLine("        {");
        sb.AppendLine(indentedBody);
        sb.Append("        }");

        return sb.ToString();
    }

    private string InferEventName(string methodName)
    {
        // Method names like "PublishGameStartedAsync" -> "GameStarted"
        var name = methodName;
        if (name.StartsWith("Publish"))
            name = name.Substring(7);
        if (name.EndsWith("Async"))
            name = name.Substring(0, name.Length - 5);
        return name;
    }

    private string GetBusAccessor(EventBusType busType, string namespaceName)
    {
        var isCore = namespaceName.Contains("Core");
        var isDiscBot = namespaceName.Contains("DiscBot");

        return busType switch
        {
            EventBusType.Core when isCore => "CoreService.EventBus",
            EventBusType.DiscBot when isDiscBot => "DiscBotService.EventBus",
            EventBusType.Global when isCore => "GlobalEventBusProvider.GetGlobalEventBus()",
            EventBusType.Global when isDiscBot => "GlobalEventBusProvider.GetGlobalEventBus()",
            EventBusType.Global => "GlobalEventBusProvider.GetGlobalEventBus()",
            // Fallback to local bus
            _ when isCore => "CoreService.EventBus",
            _ when isDiscBot => "DiscBotService.EventBus",
            _ => "GlobalEventBusProvider.GetGlobalEventBus()"
        };
    }
}

/// <summary>
/// Information about a class with [EventGenerator(TriggerMode = "OptIn")]
/// </summary>
internal record EventTriggerClassInfo(
    string ClassName,
    string Namespace,
    EventBusType DefaultBusType,
    List<TriggerMethodInfo> Methods);

/// <summary>
/// Information about a method with [EventTrigger]
/// </summary>
internal record TriggerMethodInfo(
    string MethodName,
    string ReturnType,
    EventBusType BusType,
    EventTargets Targets,
    List<(string type, string name)> Parameters);

/// <summary>
/// Event targets enum (matches WabbitBot.Common.Attributes)
/// </summary>
[Flags]
internal enum EventTargets
{
    Local = 1,
    Global = 2,
    Both = 3
}

