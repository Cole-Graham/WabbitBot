using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace WabbitBot.SourceGenerators.Generators.Event
{
    [Generator]
    public class EventBusSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // Register for syntax notifications
            context.RegisterForSyntaxNotifications(() => new EventHandlerSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not EventHandlerSyntaxReceiver receiver)
                return;

            // Generate event handler registration code
            foreach (var handlerClass in receiver.EventHandlerClasses)
            {
                var sourceBuilder = GenerateEventHandlerRegistration(handlerClass, context);
                var fileName = $"{handlerClass.Identifier.Text}.g.cs";
                var sourceText = sourceBuilder.ToString();

                // Check if this source has already been added to avoid duplicates
                if (!context.Compilation.SyntaxTrees.Any(tree => tree.FilePath.EndsWith(fileName)))
                {
                    context.AddSource(fileName, sourceText);
                }
            }

            // Generate event publisher code
            foreach (var publisherClass in receiver.EventPublisherClasses)
            {
                var sourceBuilder = GenerateEventPublisherMethods(publisherClass, context);
                var fileName = $"{publisherClass.Identifier.Text}.g.cs";
                var sourceText = sourceBuilder.ToString();

                // Check if this source has already been added to avoid duplicates
                if (!context.Compilation.SyntaxTrees.Any(tree => tree.FilePath.EndsWith(fileName)))
                {
                    context.AddSource(fileName, sourceText);
                }
            }
        }

        private bool HasInitializeAsyncMethod(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.Members
                .OfType<MethodDeclarationSyntax>()
                .Any(method => method.Identifier.Text == "InitializeAsync");
        }

        private bool HasBaseClassWithInitializeAsync(ClassDeclarationSyntax classDeclaration)
        {
            // Check if the class has a base class (inherits from something)
            return classDeclaration.BaseList != null && classDeclaration.BaseList.Types.Any();
        }

        private string GetBaseClassDeclaration(ClassDeclarationSyntax classDeclaration)
        {
            if (classDeclaration.BaseList != null && classDeclaration.BaseList.Types.Any())
            {
                // Get the first base type (primary inheritance)
                var baseType = classDeclaration.BaseList.Types.First();
                return $" : {baseType.Type}";
            }

            // No base class
            return "";
        }

        private StringBuilder GenerateEventHandlerRegistration(ClassDeclarationSyntax handlerClass, GeneratorExecutionContext context)
        {
            var className = handlerClass.Identifier.Text;
            var eventBusType = GetEventBusType(handlerClass);
            var eventBusInterface = GetEventBusInterface(eventBusType);
            var eventBusInstance = GetEventBusInstance(eventBusType);
            var sourceBuilder = new StringBuilder();

            // Get base class inheritance if it exists
            var baseClassDeclaration = GetBaseClassDeclaration(handlerClass);

            // Always generate RegisterEventSubscriptions method for consistency
            // All handlers should call this from their own InitializeAsync methods
            var usingStatements = GetUsingStatementsForNamespace(GetNamespace(handlerClass));
            sourceBuilder.AppendLine($@"
{usingStatements}

namespace {GetNamespace(handlerClass)}
{{
    public partial class {className}{baseClassDeclaration}
    {{
        /// <summary>
        /// Auto-generated method to register event subscriptions.
        /// Call this method from your InitializeAsync method.
        /// </summary>
        private void RegisterEventSubscriptions()
        {{
");

            // Generate subscriptions for each event handler method
            var eventHandlerMethods = GetEventHandlerMethods(handlerClass);
            foreach (var method in eventHandlerMethods)
            {
                var eventType = GetEventTypeFromMethod(method);
                var isRequestResponse = IsRequestResponseMethod(method);

                if (isRequestResponse)
                {
                    var responseType = GetResponseTypeFromMethod(method);
                    var eventBusFieldName = GetEventBusFieldName(handlerClass);
                    sourceBuilder.AppendLine($@"
            // Subscribe to {eventType} -> {responseType}
            {eventBusFieldName}.Subscribe<{eventType}>({method.Identifier.Text});");
                }
                else
                {
                    var eventBusFieldName = GetEventBusFieldName(handlerClass);
                    sourceBuilder.AppendLine($@"
            // Subscribe to {eventType}
            {eventBusFieldName}.Subscribe<{eventType}>({method.Identifier.Text});");
                }
            }

            // Always close with RegisterEventSubscriptions method
            sourceBuilder.AppendLine(@"
        }
    }
}");

            return sourceBuilder;
        }

        private StringBuilder GenerateEventPublisherMethods(ClassDeclarationSyntax publisherClass, GeneratorExecutionContext context)
        {
            var className = publisherClass.Identifier.Text;
            var eventBusType = GetEventBusType(publisherClass);
            var eventBusInterface = GetEventBusInterface(eventBusType);
            var eventBusInstance = GetEventBusInstance(eventBusType);
            var sourceBuilder = new StringBuilder();

            // Check if this is a Service class
            var isServiceClass = className.EndsWith("Service");

            if (isServiceClass)
            {
                // Generate Service class publisher methods
                GenerateServicePublisherMethods(sourceBuilder, className, eventBusInterface, eventBusInstance, publisherClass, context);
            }
            else
            {
                // Generate entity class publisher methods (original behavior)
                GenerateEntityPublisherMethods(sourceBuilder, className, eventBusInterface, eventBusInstance, publisherClass);
            }

            return sourceBuilder;
        }

        private void GenerateServicePublisherMethods(StringBuilder sourceBuilder, string className, string eventBusInterface, string eventBusInstance, ClassDeclarationSyntax publisherClass, GeneratorExecutionContext context)
        {
            // Check if the service already inherits from CoreService
            if (InheritsFromCoreService(publisherClass))
            {
                // Services inheriting from CoreService already have EventBus access
                // No additional code generation needed
                return;
            }

            var entityName = GetEntityNameFromServiceClass(className);
            var usingStatements = GetUsingStatementsForNamespace(GetNamespace(publisherClass));

            sourceBuilder.AppendLine($@"
{usingStatements}

namespace {GetNamespace(publisherClass)}
{{
    public partial class {className}
    {{
        private static readonly {eventBusInterface} _eventBus = {eventBusInstance};

        // Auto-generated event publisher methods for Service class
");

            // Only generate ID-based publisher methods
            GenerateSimpleIdBasedPublisherMethods(sourceBuilder, context, entityName);

            sourceBuilder.AppendLine(@"
    }
}");
        }

        private void GenerateEntityPublisherMethods(StringBuilder sourceBuilder, string className, string eventBusInterface, string eventBusInstance, ClassDeclarationSyntax publisherClass)
        {
            var usingStatements = GetUsingStatementsForNamespace(GetNamespace(publisherClass));

            // Check if the class already has _eventBus field
            var hasEventBusField = HasMember(publisherClass, "_eventBus");
            var hasPublishEventAsyncMethod = HasMember(publisherClass, "PublishEventAsync");

            sourceBuilder.AppendLine($@"
{usingStatements}

namespace {GetNamespace(publisherClass)}
{{
    public partial class {className}
    {{");

            // Only generate _eventBus field if it doesn't exist
            if (!hasEventBusField)
            {
                sourceBuilder.AppendLine($@"
        private static readonly {eventBusInterface} _eventBus = {eventBusInstance};");
            }

            sourceBuilder.AppendLine($@"
        // Auto-generated event publisher methods for Entity class
");

            // Generate publisher methods based on class name and common patterns (original behavior)
            var eventTypes = GetEventTypesForClass(className);
            foreach (var eventType in eventTypes)
            {
                var methodName = $"Publish{eventType}";
                var eventClassName = $"{eventType}Event";

                // Only generate the method if it doesn't already exist
                if (!HasMember(publisherClass, methodName))
                {
                    sourceBuilder.AppendLine($@"
        public async Task {methodName}(string userId)
        {{
            var evt = new {eventClassName}
            {{
                {GetEventPropertyName(className)}Id = Id,
                {GetEventPropertyName(className)} = this,
                Timestamp = DateTime.UtcNow,
                UserId = userId
            }};

            await _eventBus.PublishAsync(evt);
        }}");
                }
            }

            sourceBuilder.AppendLine(@"
    }
}");
        }

        private bool HasMember(ClassDeclarationSyntax classDeclaration, string memberName)
        {
            return classDeclaration.Members
                .OfType<FieldDeclarationSyntax>()
                .SelectMany(f => f.Declaration.Variables)
                .Any(v => v.Identifier.ValueText == memberName) ||
                classDeclaration.Members
                .OfType<MethodDeclarationSyntax>()
                .Any(m => m.Identifier.ValueText == memberName);
        }

        private bool IsStaticEventBusField(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.Members
                .OfType<FieldDeclarationSyntax>()
                .Any(f => f.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) &&
                         f.Declaration.Variables.Any(v => v.Identifier.ValueText == "_eventBus"));
        }

        private bool InheritsFromCoreService(ClassDeclarationSyntax classDeclaration)
        {
            if (classDeclaration.BaseList == null)
                return false;

            foreach (var baseType in classDeclaration.BaseList.Types)
            {
                var baseTypeName = baseType.Type.ToString();
                if (baseTypeName.Contains("CoreService"))
                {
                    return true;
                }
            }

            return false;
        }

        private string GetUsingStatementsForNamespace(string namespaceName)
        {
            if (namespaceName.StartsWith("WabbitBot.Core"))
            {
                return @"using System;
using System.Threading.Tasks;
using WabbitBot.Common.Events;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Common.Events;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.BotCore;";
            }
            else if (namespaceName.StartsWith("WabbitBot.DiscBot"))
            {
                return @"using System;
using System.Threading.Tasks;
using WabbitBot.Common.Events;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Scrimmages;
using WabbitBot.DiscBot.DiscBot.Events;";
            }
            else
            {
                return @"using System;
using System.Threading.Tasks;
using WabbitBot.Common.Events;
using WabbitBot.Common.Events.EventInterfaces;";
            }
        }

        private string GetNamespace(ClassDeclarationSyntax classDeclaration)
        {
            // First, try to extract namespace from the class declaration itself
            var namespaceDeclaration = classDeclaration.Ancestors()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault();

            if (namespaceDeclaration != null)
            {
                return namespaceDeclaration.Name.ToString();
            }

            // Check for file-scoped namespace in the syntax tree
            var syntaxTree = classDeclaration.SyntaxTree;
            var root = syntaxTree.GetRoot();

            // Look for file-scoped namespace at the root level
            var fileScopedNamespace = root.ChildNodes()
                .OfType<FileScopedNamespaceDeclarationSyntax>()
                .FirstOrDefault();

            if (fileScopedNamespace != null)
            {
                return fileScopedNamespace.Name.ToString();
            }

            // Also check for regular namespace declarations at the root level
            var rootNamespace = root.ChildNodes()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault();

            if (rootNamespace != null)
            {
                return rootNamespace.Name.ToString();
            }

            // Fallback: determine namespace based on class name and file path
            var className = classDeclaration.Identifier.ValueText;
            var filePath = classDeclaration.SyntaxTree.FilePath;

            // Determine namespace based on file path - be more specific
            if (filePath.Contains("\\WabbitBot.Core\\Common\\Services\\") || filePath.Contains("/WabbitBot.Core/Common/Services/"))
            {
                return "WabbitBot.Core.Common.Services";
            }
            else if (filePath.Contains("\\WabbitBot.Core\\Common\\Handlers\\") || filePath.Contains("/WabbitBot.Core/Common/Handlers/"))
            {
                return "WabbitBot.Core.Common.Handlers";
            }
            else if (filePath.Contains("\\WabbitBot.Core\\Common\\Events\\") || filePath.Contains("/WabbitBot.Core/Common/Events/"))
            {
                return "WabbitBot.Core.Common.Events";
            }
            else if (filePath.Contains("\\WabbitBot.Core\\") || filePath.Contains("/WabbitBot.Core/"))
            {
                // Generic Core project fallback - try to determine from class name
                if (className.EndsWith("Service"))
                {
                    return "WabbitBot.Core.Common.Services";
                }
                else if (className.EndsWith("Handler"))
                {
                    return "WabbitBot.Core.Common.Handlers";
                }
                else if (className.EndsWith("Event"))
                {
                    return "WabbitBot.Core.Common.Events";
                }
                else
                {
                    return "WabbitBot.Core.Common.Handlers";
                }
            }
            else if (filePath.Contains("WabbitBot.DiscBot"))
            {
                return className switch
                {
                    "CommandRegistrationHandler" => "WabbitBot.DiscBot.DSharpPlus.Commands",
                    "ScrimmageDiscordEventHandler" => "WabbitBot.DiscBot.DiscBot.Events",
                    "DiscordEventHandler" => "WabbitBot.DiscBot.DiscBot.Events",
                    "DiscordEventBus" => "WabbitBot.DiscBot.DiscBot.Events",
                    _ => "WabbitBot.DiscBot.DSharpPlus.Generated"
                };
            }

            // Default fallback
            return "WabbitBot.DiscBot.DSharpPlus.Generated";
        }

        private IEnumerable<MethodDeclarationSyntax> GetEventHandlerMethods(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(method =>
                {
                    var methodName = method.Identifier.Text;
                    // Check for handler method naming pattern: Handle*Async
                    return methodName.StartsWith("Handle") && methodName.EndsWith("Async") &&
                           method.ParameterList.Parameters.Count == 1 &&
                           (method.ReturnType?.ToString() == "Task" ||
                            method.ReturnType?.ToString() == "System.Threading.Tasks.Task" ||
                            method.Modifiers.Any(m => m.ValueText == "async"));
                });
        }

        private string GetEventTypeFromMethod(MethodDeclarationSyntax method)
        {
            // Extract event type from method parameter
            var parameter = method.ParameterList.Parameters.FirstOrDefault();
            return parameter?.Type?.ToString() ?? "object";
        }

        private bool IsRequestResponseMethod(MethodDeclarationSyntax method)
        {
            // Check if method has return type (request-response pattern)
            return method.ReturnType?.ToString() != "Task";
        }

        private string GetResponseTypeFromMethod(MethodDeclarationSyntax method)
        {
            // Extract response type from method return type
            var returnType = method.ReturnType.ToString();
            if (returnType.StartsWith("Task<"))
            {
                return returnType.Substring(5, returnType.Length - 6); // Remove Task< and >
            }
            return returnType;
        }

        private IEnumerable<string> GetEventTypesForClass(string className)
        {
            // Map service class names to our event naming convention
            var entityName = GetEntityNameFromServiceClass(className);

            return new[]
            {
                $"{entityName}Created",
                $"{entityName}Updated",
                $"{entityName}Archived",
                $"{entityName}Deleted",
                $"{entityName}Completed"
            };
        }

        private IEnumerable<INamedTypeSymbol> FindEventClasses(Compilation compilation, string entityName)
        {
            var eventClasses = new List<INamedTypeSymbol>();

            // Look for event classes that match the entity name pattern
            var eventTypes = new[] { "Created", "Updated", "Archived", "Deleted", "Completed", "Started", "Cancelled", "Forfeited" };

            foreach (var eventType in eventTypes)
            {
                var eventClassName = $"{entityName}{eventType}Event";
                var eventClass = compilation.GetTypeByMetadataName($"WabbitBot.Core.Common.Events.{eventClassName}");

                if (eventClass != null)
                {
                    eventClasses.Add(eventClass);
                }
            }

            return eventClasses;
        }

        private void GenerateSimpleIdBasedPublisherMethods(StringBuilder sourceBuilder, GeneratorExecutionContext context, string entityName)
        {
            // Find actual event classes and generate methods based on their constructors
            var eventClasses = FindEventClasses(context.Compilation, entityName);

            foreach (var eventClass in eventClasses)
            {
                GeneratePublisherMethodForEvent(sourceBuilder, eventClass, entityName);
            }
        }


        private void GeneratePublisherMethodForEvent(StringBuilder sourceBuilder, INamedTypeSymbol eventClass, string entityName)
        {
            var eventClassName = eventClass.Name;
            var methodName = $"Publish{eventClassName.Replace("Event", "")}";

            // Find the constructor with the most parameters (usually the primary one)
            // For records, we need to include implicitly declared constructors
            var constructor = eventClass.Constructors
                .OrderByDescending(c => c.Parameters.Length)
                .FirstOrDefault();

            if (constructor == null)
            {
                // Fallback for events with parameterless constructor
                sourceBuilder.AppendLine($@"
        public async Task {methodName}()
        {{
            var evt = new {eventClassName}();

            await _eventBus.PublishAsync(evt);
        }}");
                return;
            }

            // Generate method parameters based on constructor
            var parameters = string.Join(", ", constructor.Parameters.Select(p =>
                $"{p.Type.ToDisplayString()} {p.Name}"));

            var arguments = string.Join(", ", constructor.Parameters.Select(p => p.Name));

            sourceBuilder.AppendLine($@"
        public async Task {methodName}({parameters})
        {{
            var evt = new {eventClassName}({arguments});

            await _eventBus.PublishAsync(evt);
        }}");
        }

        private string GetEntityNameFromServiceClass(string className)
        {
            // Remove "Service" suffix to get entity name
            if (className.EndsWith("Service"))
            {
                return className.Substring(0, className.Length - 7); // Remove "Service"
            }

            // Handle special cases or return as-is
            return className;
        }

        private string GetEventPropertyName(string className)
        {
            // Get the entity name (remove "Service" suffix)
            var entityName = GetEntityNameFromServiceClass(className);
            return entityName;
        }

        private string GetEventBusType(ClassDeclarationSyntax classDeclaration)
        {
            // Check for specific event handler attributes first
            if (HasAttribute(classDeclaration, "GenerateEventSubscriptions") ||
                HasAttribute(classDeclaration, "GenerateCoreEventPublisher"))
                return "Core";

            if (HasAttribute(classDeclaration, "GenerateDiscordEventHandler") ||
                HasAttribute(classDeclaration, "GenerateDiscordEventPublisher"))
                return "Discord";

            // Look for GenerateEventHandler or GenerateEventPublisher attribute with EventBusType parameter
            var attributes = classDeclaration.AttributeLists
                .SelectMany(attrList => attrList.Attributes)
                .Where(attr => attr.Name.ToString().Contains("GenerateEventHandler") ||
                              attr.Name.ToString().Contains("GenerateEventPublisher"));

            foreach (var attr in attributes)
            {
                // Check for EventBusType parameter
                if (attr.ArgumentList != null)
                {
                    foreach (var arg in attr.ArgumentList.Arguments)
                    {
                        if (arg.NameEquals?.Name.Identifier.Text == "EventBusType")
                        {
                            // Extract the string value from the argument
                            if (arg.Expression is LiteralExpressionSyntax literal)
                            {
                                return literal.Token.ValueText;
                            }
                        }
                    }
                }
            }

            // Default to "Core" if no EventBusType specified
            return "Core";
        }

        private string GetEventBusFieldName(ClassDeclarationSyntax classDeclaration)
        {
            // Check if the class inherits from CoreHandler
            if (classDeclaration.BaseList != null)
            {
                foreach (var baseType in classDeclaration.BaseList.Types)
                {
                    var baseTypeName = baseType.Type.ToString();
                    if (baseTypeName.Contains("CoreHandler"))
                    {
                        return "EventBus"; // CoreHandler uses EventBus field
                    }
                    if (baseTypeName.Contains("DiscordBaseHandler"))
                    {
                        return "EventBus"; // DiscordBaseHandler also uses EventBus field
                    }
                }
            }

            // Default to _eventBus for classes that don't inherit from handler base classes
            return "_eventBus";
        }

        private bool HasAttribute(ClassDeclarationSyntax classDeclaration, string attributeName)
        {
            return classDeclaration.AttributeLists
                .SelectMany(attrList => attrList.Attributes)
                .Any(attr =>
                {
                    var attrName = attr.Name.ToString();
                    return attrName.Contains(attributeName) ||
                           attrName.EndsWith(attributeName) ||
                           attrName.EndsWith(attributeName + "Attribute");
                });
        }

        private string GetEventBusInterface(string eventBusType)
        {
            return eventBusType switch
            {
                "Core" => "ICoreEventBus",
                "Discord" => "IGlobalEventBus",
                "Global" => "IGlobalEventBus",
                _ => "ICoreEventBus"
            };
        }

        private string GetEventBusInstance(string eventBusType)
        {
            return eventBusType switch
            {
                "Core" => "CoreEventBus.Instance",
                "Discord" => "EventBus", // Uses the EventBus property from base class
                "Global" => "EventBus", // Uses the EventBus property from base class
                _ => "CoreEventBus.Instance"
            };
        }
    }

    public class EventHandlerSyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> EventHandlerClasses { get; } = new();
        public List<ClassDeclarationSyntax> EventPublisherClasses { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax classDeclaration)
            {
                // Check for specific event handler attributes
                if (HasAttribute(classDeclaration, "GenerateEventSubscriptions") ||
                    HasAttribute(classDeclaration, "GenerateDiscordEventHandler") ||
                    HasAttribute(classDeclaration, "GenerateEventHandler"))
                {
                    EventHandlerClasses.Add(classDeclaration);
                }

                // Check for specific event publisher attributes
                if (HasAttribute(classDeclaration, "GenerateCoreEventPublisher") ||
                    HasAttribute(classDeclaration, "GenerateIdBasedEventPublisher") ||
                    HasAttribute(classDeclaration, "GenerateDiscordEventPublisher") ||
                    HasAttribute(classDeclaration, "GenerateEventPublisher"))
                {
                    EventPublisherClasses.Add(classDeclaration);
                }
            }
        }

        private bool HasAttribute(ClassDeclarationSyntax classDeclaration, string attributeName)
        {
            return classDeclaration.AttributeLists
                .SelectMany(attrList => attrList.Attributes)
                .Any(attr =>
                {
                    var attrName = attr.Name.ToString();
                    return attrName.Contains(attributeName) ||
                           attrName.EndsWith(attributeName) ||
                           attrName.EndsWith(attributeName + "Attribute");
                });
        }
    }
}