using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WabbitBot.Generator.Shared;
using WabbitBot.Generator.Shared.Utils;

// # Misc: SnakeCase, pluralize, etc. (from your Common)
// existing code is old, use what still makaes sense and remove the rest
namespace WabbitBot.SourceGenerators.Utils
{
    /// <summary>
    /// Helper utilities for source generators
    /// </summary>
    public static class GeneratorHelpers
    {
        /// <summary>
        /// Converts snake_case to PascalCase.
        /// </summary>
        public static string ToPascalCase(string snakeCase)
        {
            if (string.IsNullOrEmpty(snakeCase))
                return snakeCase;

            return string.Concat(
                snakeCase.Split('_').Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant())
            );
        }

        /// <summary>
        /// Normalizes a service property name from entity class name or override.
        /// Uses singular PascalCase without naive pluralization.
        /// </summary>
        public static string NormalizeServicePropertyName(string name)
        {
            // Respect exact PascalCase provided; ensure first letter is uppercase
            if (string.IsNullOrEmpty(name))
                return name;
            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        /// <summary>
        /// Gets the event bus type from a symbol
        /// </summary>
        public static string GetEventBusType(INamedTypeSymbol classSymbol)
        {
            // Look for EventBoundary attribute
            var eventBoundaryAttr = classSymbol
                .GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Name.Contains("EventBoundary") == true);

            if (eventBoundaryAttr != null)
            {
                // Extract EventBusType from attribute arguments
                var busType = AttributeExtractor.GetAttributeEnumArgument<EventBusType>(
                    eventBoundaryAttr,
                    "EventBusType"
                );
                if (busType.HasValue)
                {
                    return busType.Value.ToString();
                }
            }

            // Default to "Core" if no EventBusType specified
            return "Core";
        }

        /// <summary>
        /// Gets the event bus interface for a given event bus type
        /// </summary>
        public static string GetEventBusInterface(string eventBusType)
        {
            return eventBusType switch
            {
                "Core" => "ICoreEventBus",
                "DiscBot" => "IDiscordEventBus",
                "Discord" => "IGlobalEventBus",
                "Global" => "IGlobalEventBus",
                _ => "ICoreEventBus",
            };
        }

        /// <summary>
        /// Gets the event bus instance for a given event bus type
        /// </summary>
        public static string GetEventBusInstance(string eventBusType)
        {
            return eventBusType switch
            {
                "Core" => "CoreEventBus.Instance",
                "DiscBot" => "EventBus", // Uses the EventBus property from base class
                "Discord" => "EventBus", // Uses the EventBus property from base class
                "Global" => "EventBus", // Uses the EventBus property from base class
                _ => "CoreEventBus.Instance",
            };
        }

        /// <summary>
        /// Gets event handler methods from a class declaration
        /// </summary>
        public static IEnumerable<MethodDeclarationSyntax> GetEventHandlerMethods(
            ClassDeclarationSyntax classDeclaration
        )
        {
            return classDeclaration
                .Members.OfType<MethodDeclarationSyntax>()
                .Where(method =>
                    method
                        .AttributeLists.SelectMany(attrList => attrList.Attributes)
                        .Any(attr => attr.Name.ToString().Contains("EventHandler"))
                );
        }

        /// <summary>
        /// Gets the event type from a method parameter
        /// </summary>
        public static string GetEventTypeFromMethod(MethodDeclarationSyntax method)
        {
            var parameter = method.ParameterList.Parameters.FirstOrDefault();
            return parameter?.Type?.ToString() ?? "object";
        }

        /// <summary>
        /// Checks if a method is a request-response method
        /// </summary>
        public static bool IsRequestResponseMethod(MethodDeclarationSyntax method)
        {
            return method.ReturnType?.ToString() != "Task";
        }

        /// <summary>
        /// Gets the response type from a method return type
        /// </summary>
        public static string GetResponseTypeFromMethod(MethodDeclarationSyntax method)
        {
            var returnType = method.ReturnType.ToString();
            if (returnType.StartsWith("Task<"))
            {
                return returnType.Substring(5, returnType.Length - 6); // Remove Task< and >
            }
            return returnType;
        }

        /// <summary>
        /// Generates event types based on class name
        /// </summary>
        public static IEnumerable<string> GetEventTypesForClass(string className)
        {
            return new[]
            {
                $"{className}Created",
                $"{className}Updated",
                $"{className}Deleted",
                $"{className}StatusChanged",
            };
        }

        /// <summary>
        /// Gets the event property name for a class
        /// </summary>
        public static string GetEventPropertyName(string className)
        {
            return className;
        }

        /// <summary>
        /// Creates a StringBuilder with standard using statements
        /// </summary>
        public static StringBuilder CreateSourceBuilder(
            string className,
            string description,
            params string[] additionalUsings
        )
        {
            var builder = new StringBuilder();

            // File header
            builder.AppendLine($"// <auto-generated />");
            builder.AppendLine($"// This file was generated by the WabbitBot Source Generators");
            builder.AppendLine($"// Description: {description}");
            builder.AppendLine($"// Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            builder.AppendLine();

            // Using statements
            var usings = new List<string>
            {
                "using System;",
                "using System.Threading.Tasks;",
                "using WabbitBot.Common.Events;",
                "using WabbitBot.Common.Events.Interfaces;",
            };

            usings.AddRange(additionalUsings);

            foreach (var usingStatement in usings)
            {
                builder.AppendLine(usingStatement);
            }
            builder.AppendLine();

            return builder;
        }
    }
}
