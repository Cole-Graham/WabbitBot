using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WabbitBot.SourceGenerators.Utils;

namespace WabbitBot.SourceGenerators.Generators.Event
{
    /// <summary>
    /// Source generator that creates publishers and subscribers from [EventGenerator] attributes on event classes.
    /// Groups generated code by namespace (e.g., Scrimmages_Publishers.g.cs, Scrimmages_Events.g.cs).
    /// </summary>
    [Generator]
    public class EventGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Register a post-initialization output for diagnostics
            context.RegisterPostInitializationOutput(ctx =>
            {
                ctx.AddSource(
                    "__WG_Init_EventGenerator.g.cs",
                    "// EventGenerator: Looking for event classes with [EventGenerator] attribute\n"
                );
            });

            // Determine which project we're compiling
            var isCoreProject = context.CompilationProvider.IsCore();
            var isDiscBotProject = context.CompilationProvider.IsDiscBot();

            // Find all classes/records with [EventGenerator] attribute in CURRENT project source
            var eventClasses = context
                .SyntaxProvider.CreateSyntaxProvider(
                    predicate: static (node, _) =>
                    {
                        // Look for class or record declarations with attributes
                        return (node is ClassDeclarationSyntax or RecordDeclarationSyntax)
                            && ((TypeDeclarationSyntax)node).AttributeLists.Count > 0;
                    },
                    transform: static (ctx, _) =>
                    {
                        var typeDecl = (TypeDeclarationSyntax)ctx.Node;
                        var typeSymbol = ctx.SemanticModel.GetDeclaredSymbol(typeDecl) as INamedTypeSymbol;
                        if (typeSymbol is null)
                            return null;

                        // Check for EventGenerator attribute
                        var attr = typeSymbol
                            .GetAttributes()
                            .FirstOrDefault(a =>
                                a.AttributeClass?.ToDisplayString()
                                    == "WabbitBot.SourceGenerators.Attributes.EventGeneratorAttribute"
                                || a.AttributeClass?.ToDisplayString()
                                    == "WabbitBot.Common.Attributes.EventGeneratorAttribute"
                            );

                        if (attr is null)
                            return null;

                        return ExtractEventInfo(typeSymbol, attr);
                    }
                )
                .Where(static info => info is not null);

            // Collect events from source files
            var sourceEvents = eventClasses.Collect();

            // Also scan referenced assemblies for events (for cross-project generation)
            var referencedEvents = context.CompilationProvider.Select(
                (compilation, _) =>
                {
                    var events = new List<EventInfo>();

                    // Scan all referenced assemblies
                    foreach (var reference in compilation.References)
                    {
                        var assembly = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                        if (assembly is null)
                            continue;

                        // Look for types with EventGenerator attribute
                        foreach (var typeSymbol in GetAllTypes(assembly.GlobalNamespace))
                        {
                            var attr = typeSymbol
                                .GetAttributes()
                                .FirstOrDefault(a =>
                                    a.AttributeClass?.ToDisplayString()
                                        == "WabbitBot.SourceGenerators.Attributes.EventGeneratorAttribute"
                                    || a.AttributeClass?.ToDisplayString()
                                        == "WabbitBot.Common.Attributes.EventGeneratorAttribute"
                                );

                            if (attr is not null)
                            {
                                var eventInfo = ExtractEventInfo(typeSymbol, attr);
                                if (eventInfo is not null)
                                    events.Add(eventInfo);
                            }
                        }
                    }

                    return events;

                    static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol namespaceSymbol)
                    {
                        foreach (var type in namespaceSymbol.GetTypeMembers())
                        {
                            yield return type;
                            foreach (var nestedType in GetNestedTypes(type))
                                yield return nestedType;
                        }

                        foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
                        {
                            foreach (var type in GetAllTypes(childNamespace))
                                yield return type;
                        }
                    }

                    static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
                    {
                        foreach (var nestedType in type.GetTypeMembers())
                        {
                            yield return nestedType;
                            foreach (var deeplyNested in GetNestedTypes(nestedType))
                                yield return deeplyNested;
                        }
                    }
                }
            );

            // Combine source events and referenced events for all generation
            var allEvents = sourceEvents
                .Combine(referencedEvents)
                .Select(
                    (data, _) =>
                    {
                        var (srcEvents, refEvents) = data;
                        return srcEvents.Concat(refEvents).ToArray();
                    }
                );

            // Diagnostic output
            context.RegisterSourceOutput(
                allEvents,
                (spc, events) =>
                {
                    var names = string.Join(", ", events.Select(e => e!.EventClassName));
                    spc.AddSource(
                        "__WG_Diagnostic_EventGenerator.g.cs",
                        $"// Found {events.Length} event generators (source + referenced): {names}\n"
                    );
                }
            );

            // Generate publishers grouped by namespace
            var publisherOutput = allEvents.Combine(context.CompilationProvider);
            context.RegisterSourceOutput(
                publisherOutput,
                (spc, data) =>
                {
                    var (events, compilation) = data;

                    // Filter to events whose pubTargetClass is defined in THIS compilation's source (not references)
                    var eventsForThisCompilation = events
                        .Where(e => e?.PubTargetClass is not null)
                        .Where(e =>
                        {
                            var targetSymbol = compilation.GetTypeByMetadataName(e!.PubTargetClass!);
                            // Check that the symbol exists AND is defined in this compilation (not a reference)
                            return targetSymbol is not null
                                && SymbolEqualityComparer.Default.Equals(
                                    targetSymbol.ContainingAssembly,
                                    compilation.Assembly
                                );
                        })
                        .ToList();

                    if (!eventsForThisCompilation.Any())
                        return;

                    var grouped = eventsForThisCompilation
                        .Where(e => e is not null && e.ShouldGeneratePublisher)
                        .GroupBy(e => GetPublisherFileName(e!.PubTargetClass!));

                    foreach (var group in grouped)
                    {
                        try
                        {
                            var source = GeneratePublishers(group.ToList()!);
                            spc.AddSource($"{group.Key}_Publishers.g.cs", source);
                        }
                        catch (Exception ex)
                        {
                            spc.AddSource(
                                $"{group.Key}_Publishers_Error.g.cs",
                                $"// Error generating publishers for {group.Key}: {ex.Message}"
                            );
                        }
                    }
                }
            );

            // Generate subscribers grouped by target subscriber classes
            var subscriberOutput = allEvents.Combine(context.CompilationProvider);
            context.RegisterSourceOutput(
                subscriberOutput,
                (spc, data) =>
                {
                    var (events, compilation) = data;

                    // Filter to events whose subscriber target classes are defined in THIS compilation's source
                    var eventsForThisCompilation = events
                        .Where(e => e is not null && e.ShouldGenerateSubscribers)
                        .Where(e =>
                        {
                            // Check if ANY of the subscriber target classes are defined in this compilation (not references)
                            return e!.SubTargetClasses.Any(targetClass =>
                            {
                                var targetSymbol = compilation.GetTypeByMetadataName(targetClass);
                                return targetSymbol is not null
                                    && SymbolEqualityComparer.Default.Equals(
                                        targetSymbol.ContainingAssembly,
                                        compilation.Assembly
                                    );
                            });
                        })
                        .ToList();

                    if (!eventsForThisCompilation.Any())
                        return;

                    // Build per-target-class groups to generate one partial per target class
                    var byTargetClass = new Dictionary<string, List<EventInfo>>();
                    foreach (var evt in eventsForThisCompilation)
                    {
                        foreach (var targetClass in evt!.SubTargetClasses)
                        {
                            // Only emit for classes that exist in this compilation (safety)
                            var targetSymbol = compilation.GetTypeByMetadataName(targetClass);
                            if (targetSymbol is null)
                                continue;

                            if (!byTargetClass.TryGetValue(targetClass, out var list))
                            {
                                list = new List<EventInfo>();
                                byTargetClass[targetClass] = list;
                            }
                            list.Add(evt);
                        }
                    }

                    foreach (var kvp in byTargetClass)
                    {
                        var targetClass = kvp.Key;
                        var targetEvents = kvp.Value;
                        try
                        {
                            var source = GenerateSubscribersForTarget(targetClass, targetEvents, compilation);
                            // Use a stable filename per target class
                            var safeName = targetClass.Replace('.', '_');
                            spc.AddSource($"{safeName}_Subscribers.g.cs", source);
                        }
                        catch (Exception ex)
                        {
                            var safeName = targetClass.Replace('.', '_');
                            spc.AddSource(
                                $"{safeName}_Subscribers_Error.g.cs",
                                $"// Error generating subscribers for {targetClass}: {ex.Message}"
                            );
                        }
                    }
                }
            );
        }

        private static string GetLastNamespacePart(string fullNamespace)
        {
            var parts = fullNamespace.Split('.');
            return parts[parts.Length - 1];
        }

        /// <summary>
        /// Extracts the appropriate file name for publishers based on the target class.
        /// Examples:
        /// - "WabbitBot.Core.Common.Models.Common.MatchCore" -> "Common_Match"
        /// - "WabbitBot.Core.Scrimmages.ScrimmageCore" -> "Scrimmage"
        /// - "WabbitBot.Core.Leaderboards.LeaderboardCore" -> "Leaderboard"
        /// </summary>
        private static string GetPublisherFileName(string pubTargetClass)
        {
            // Extract the class name (last part after the last dot)
            var lastDot = pubTargetClass.LastIndexOf('.');
            var className = lastDot > 0 ? pubTargetClass.Substring(lastDot + 1) : pubTargetClass;

            // Check if it's in the Common domain
            if (pubTargetClass.Contains("WabbitBot.Core.Common.Models.Common"))
            {
                // Extract entity type from class name (e.g., "MatchCore" -> "Match")
                var entityType = className.Replace("Core", "");
                return $"Common_{entityType}";
            }

            // Check for dedicated domains
            if (pubTargetClass.Contains("WabbitBot.Core.Scrimmages"))
            {
                return "Scrimmage";
            }

            if (pubTargetClass.Contains("WabbitBot.Core.Leaderboards"))
            {
                return "Leaderboard";
            }

            if (pubTargetClass.Contains("WabbitBot.Core.Tournaments"))
            {
                return "Tournament";
            }

            // Fallback to class name if no pattern matches
            return className.Replace("Core", "");
        }

        private static EventInfo? ExtractEventInfo(INamedTypeSymbol typeSymbol, AttributeData attribute)
        {
            try
            {
                // Extract attribute parameters - they're all passed as named arguments
                string? pubTargetClass = null;
                var subTargetClasses = new List<string>();

                // Check named arguments first (this is how the attribute is typically used)
                foreach (var namedArg in attribute.NamedArguments)
                {
                    switch (namedArg.Key)
                    {
                        case "PubTargetClass":
                            pubTargetClass = namedArg.Value.Value as string;
                            break;
                        case "SubTargetClasses":
                            if (namedArg.Value.Values.Length > 0)
                            {
                                subTargetClasses = namedArg
                                    .Value.Values.Select(v => v.Value as string)
                                    .Where(s => !string.IsNullOrEmpty(s))
                                    .Select(s => s!)
                                    .ToList();
                            }
                            break;
                    }
                }

                // Fallback to constructor arguments if named args are empty
                if (pubTargetClass is null && attribute.ConstructorArguments.Length > 0)
                {
                    pubTargetClass = attribute.ConstructorArguments[0].Value as string;
                }
                if (subTargetClasses.Count == 0 && attribute.ConstructorArguments.Length > 1)
                {
                    var arrayArg = attribute.ConstructorArguments[1];
                    if (!arrayArg.IsNull && arrayArg.Values.Length > 0)
                    {
                        subTargetClasses = arrayArg
                            .Values.Select(v => v.Value as string)
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Select(s => s!)
                            .ToList();
                    }
                }

                // Extract event parameters from record/class properties or constructor parameters
                var parameters = new List<EventParameter>();

                // For records, get primary constructor parameters
                if (typeSymbol.IsRecord)
                {
                    var primaryCtor = typeSymbol.InstanceConstructors.FirstOrDefault(c => c.Parameters.Length > 0);

                    if (primaryCtor is not null)
                    {
                        foreach (var param in primaryCtor.Parameters)
                        {
                            // Skip IEvent interface properties
                            if (param.Name == "EventId" || param.Name == "Timestamp" || param.Name == "EventBusType")
                                continue;

                            parameters.Add(
                                new EventParameter(
                                    param.Name,
                                    param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                                )
                            );
                        }
                    }
                }
                else
                {
                    // For classes, get public properties
                    foreach (
                        var prop in typeSymbol
                            .GetMembers()
                            .OfType<IPropertySymbol>()
                            .Where(p => p.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Public)
                    )
                    {
                        // Skip IEvent interface properties
                        if (prop.Name == "EventId" || prop.Name == "Timestamp" || prop.Name == "EventBusType")
                            continue;

                        parameters.Add(
                            new EventParameter(
                                prop.Name,
                                prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                            )
                        );
                    }
                }

                return new EventInfo(
                    typeSymbol.Name,
                    typeSymbol.ContainingNamespace.ToDisplayString(),
                    pubTargetClass,
                    subTargetClasses,
                    parameters,
                    typeSymbol
                );
            }
            catch
            {
                // Return null if extraction fails
                return null;
            }
        }

        private static string GeneratePublishers(List<EventInfo> events)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();

            // Add using statements
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using WabbitBot.Common.Models;");
            sb.AppendLine("using WabbitBot.Core.Common.Services;");

            // Collect all event namespaces for using statements
            var eventNamespaces = events.Select(e => e.EventNamespace).Distinct().ToList();
            foreach (var ns in eventNamespaces)
            {
                sb.AppendLine($"using {ns};");
            }
            sb.AppendLine();

            // Group by target class
            var byClass = events.GroupBy(e => e.PubTargetClass);

            foreach (var classGroup in byClass)
            {
                var key = classGroup.Key;
                if (string.IsNullOrEmpty(key))
                    continue;

                // Extract namespace and class name from fully qualified class name
                var lastDot = key!.LastIndexOf('.');
                var targetNamespace = key.Substring(0, lastDot);
                var targetClassName = key.Substring(lastDot + 1);

                sb.AppendLine($"namespace {targetNamespace}");
                sb.AppendLine("{");
                sb.AppendLine($"    public partial class {targetClassName}");
                sb.AppendLine("    {");

                foreach (var evt in classGroup)
                {
                    // Generate publisher method with simplified type names
                    var paramList = string.Join(
                        ", ",
                        evt.Parameters.Select(p =>
                        {
                            var simplifiedType = SimplifyTypeName(p.Type);
                            return $"{simplifiedType} {ToCamelCase(p.Name)}";
                        })
                    );
                    var methodName = $"Publish{evt.EventClassName}Async";

                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// Publishes a {evt.EventClassName} event.");
                    sb.AppendLine($"        /// </summary>");
                    sb.AppendLine($"        public static async Task<Result> {methodName}({paramList})");
                    sb.AppendLine("        {");
                    sb.AppendLine("            try");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                var @event = new {evt.EventClassName}(");

                    for (int i = 0; i < evt.Parameters.Count; i++)
                    {
                        var param = evt.Parameters[i];
                        sb.Append($"                    {ToCamelCase(param.Name)}");
                        if (i < evt.Parameters.Count - 1)
                            sb.AppendLine(",");
                        else
                            sb.AppendLine();
                    }

                    sb.AppendLine("                );");
                    sb.AppendLine();
                    sb.AppendLine("                await CoreService.PublishAsync(@event);");
                    sb.AppendLine("                return Result.CreateSuccess();");
                    sb.AppendLine("            }");
                    sb.AppendLine("            catch (Exception ex)");
                    sb.AppendLine("            {");
                    sb.AppendLine(
                        $"                await CoreService.ErrorHandler.CaptureAsync(ex, \"Failed to publish {evt.EventClassName}\", \"{methodName}\");"
                    );
                    sb.AppendLine(
                        $"                return Result.Failure(\"Failed to publish {evt.EventClassName}: \" + ex.Message);"
                    );
                    sb.AppendLine("            }");
                    sb.AppendLine("        }");
                    sb.AppendLine();
                }

                sb.AppendLine("    }");
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private static string GenerateSubscribersForTarget(
            string targetClass,
            List<EventInfo> events,
            Compilation compilation
        )
        {
            var targetSymbol = compilation.GetTypeByMetadataName(targetClass);
            if (targetSymbol is null)
            {
                return $"// Skipping generation for missing type: {targetClass}";
            }

            // Split target class full name
            var lastDot = targetClass.LastIndexOf('.');
            var targetNamespace = lastDot > 0 ? targetClass.Substring(0, lastDot) : "";
            var targetClassName = targetClass.Substring(lastDot + 1);

            // Determine static modifier to match original declaration
            var isStatic = targetSymbol.IsStatic;
            var staticKeyword = isStatic ? " static" : string.Empty;

            // Determine which bus accessor to use based on boundary
            string busAccessor;
            string busNamespace;
            if (targetNamespace.StartsWith("WabbitBot.DiscBot", StringComparison.Ordinal))
            {
                busAccessor = "DiscBotService.EventBus";
                busNamespace = "WabbitBot.DiscBot.App.Services.DiscBot";
            }
            else if (targetNamespace.StartsWith("WabbitBot.Core", StringComparison.Ordinal))
            {
                busAccessor = "CoreService.EventBus";
                busNamespace = "WabbitBot.Core.Common.Services";
            }
            else
            {
                busAccessor = "GlobalEventBusProvider.GetGlobalEventBus()";
                busNamespace = "WabbitBot.Common.Events";
            }

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();

            // Add using statements
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine($"using {busNamespace};");

            // Collect all event namespaces for using statements
            var eventNamespaces = events.Select(e => e.EventNamespace).Distinct().ToList();
            foreach (var ns in eventNamespaces)
            {
                sb.AppendLine($"using {ns};");
            }
            sb.AppendLine();

            if (!string.IsNullOrEmpty(targetNamespace))
            {
                sb.AppendLine($"namespace {targetNamespace}");
                sb.AppendLine("{");
            }

            sb.AppendLine($"    public{staticKeyword} partial class {targetClassName}");
            sb.AppendLine("    {");
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Auto-generated subscription wiring for events.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static void Initialize()");
            sb.AppendLine("        {");

            foreach (var evt in events)
            {
                var evtFullName = $"{evt.EventNamespace}.{evt.EventClassName}";
                var handlerName = $"Handle{evt.EventClassName}Async";

                // Try to detect an existing static handler with the expected signature
                var hasExistingHandler = targetSymbol
                    .GetMembers(handlerName)
                    .OfType<IMethodSymbol>()
                    .Any(m =>
                        m.IsStatic
                        && m.Parameters.Length == 1
                        && string.Equals(m.Parameters[0].Type.ToDisplayString(), evtFullName, StringComparison.Ordinal)
                    );

                if (hasExistingHandler)
                {
                    sb.AppendLine(
                        $"            {busAccessor}.Subscribe<{evt.EventClassName}>(async evt => await {targetClassName}.{handlerName}(evt));"
                    );
                }
                else
                {
                    // Wire to a generated local handler stub to keep compile green
                    sb.AppendLine(
                        $"            {busAccessor}.Subscribe<{evt.EventClassName}>(async evt => await {handlerName}(evt));"
                    );
                }
            }

            sb.AppendLine("        }");
            sb.AppendLine();

            // Emit handler stubs for any events without an existing handler
            foreach (var evt in events)
            {
                var evtFullName = $"{evt.EventNamespace}.{evt.EventClassName}";
                var handlerName = $"Handle{evt.EventClassName}Async";

                var hasExistingHandler = targetSymbol
                    .GetMembers(handlerName)
                    .OfType<IMethodSymbol>()
                    .Any(m =>
                        m.IsStatic
                        && m.Parameters.Length == 1
                        && string.Equals(m.Parameters[0].Type.ToDisplayString(), evtFullName, StringComparison.Ordinal)
                    );

                if (!hasExistingHandler)
                {
                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine($"        /// Auto-generated placeholder handler for {evt.EventClassName}.");
                    sb.AppendLine("        /// Replace body with actual handling logic.");
                    sb.AppendLine("        /// </summary>");
                    sb.AppendLine($"        private static async Task {handlerName}({evt.EventClassName} evt)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            await Task.CompletedTask;");
                    sb.AppendLine("        }");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("    }");
            if (!string.IsNullOrEmpty(targetNamespace))
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private static string SimplifyTypeName(string fullTypeName)
        {
            // Remove global:: prefix
            var typeName = fullTypeName.Replace("global::", "");

            // Handle common System types
            if (typeName.StartsWith("System."))
            {
                var simpleName = typeName.Substring("System.".Length);
                // Keep common types simple
                if (
                    simpleName == "Guid"
                    || simpleName == "String"
                    || simpleName == "Int32"
                    || simpleName == "Boolean"
                    || simpleName == "DateTime"
                    || simpleName == "Double"
                )
                {
                    return simpleName;
                }
            }

            // For other types, return just the simple name (last part after the last dot)
            var lastDot = typeName.LastIndexOf('.');
            if (lastDot > 0)
            {
                return typeName.Substring(lastDot + 1);
            }

            return typeName;
        }

        private static string ToCamelCase(string str)
        {
            if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
                return str;
            return char.ToLowerInvariant(str[0]) + str.Substring(1);
        }
    }

    /// <summary>
    /// Local copy of EventInfo to avoid runtime dependency on WabbitBot.Generator.Shared in analyzer load context.
    /// </summary>
    internal record EventInfo(
        string EventClassName,
        string EventNamespace,
        string? PubTargetClass,
        List<string> SubTargetClasses,
        List<EventParameter> Parameters,
        INamedTypeSymbol EventClassSymbol
    )
    {
        public bool ShouldGeneratePublisher => !string.IsNullOrEmpty(PubTargetClass);
        public bool ShouldGenerateSubscribers => SubTargetClasses?.Any() ?? false;
    }

    /// <summary>
    /// Local copy of EventParameter metadata.
    /// </summary>
    internal record EventParameter(string Name, string Type);
}
