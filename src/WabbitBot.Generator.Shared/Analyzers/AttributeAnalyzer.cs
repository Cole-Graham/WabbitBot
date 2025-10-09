using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WabbitBot.Generator.Shared.Metadata;

namespace WabbitBot.Generator.Shared.Analyzers;


/// <summary>
/// Defines the target projects for event generation.
/// </summary>
[Flags]
public enum GenerationTargets
{
    /// <summary>
    /// Publish to the Common project.
    /// </summary>
    Common = 1,

    /// <summary>
    /// Publish to the Core project.
    /// </summary>
    Core = 2,

    /// <summary>
    /// Publish to the DiscBot project.
    /// </summary>
    DiscBot = 4,
}
/// <summary>
/// Defines the target event buses for event publishing.
/// Used with EventTrigger to specify where events should be published.
/// </summary>
[Flags]
public enum EventTargets
{
    /// <summary>
    /// Publish to the local/default event bus only.
    /// </summary>
    Local = 1,

    /// <summary>
    /// Publish to the Global event bus only.
    /// </summary>
    Global = 2,

    /// <summary>
    /// Publish to both local and Global event buses (dual-publish).
    /// </summary>
    Both = Local | Global
}
/// <summary>
/// Utility class for analyzing attributes on symbols.
/// </summary>
public static class AttributeAnalyzer
{
    /// <summary>
    /// Extracts command information from a WabbitCommand-attributed class.
    /// </summary>
    public static CommandInfo? ExtractCommandInfo(INamedTypeSymbol classSymbol)
    {
        var attribute = classSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == AttributeNames.WabbitCommand);

        if (attribute == null)
            return null;

        var name = attribute.ConstructorArguments.FirstOrDefault().Value as string;
        var group = attribute.NamedArguments
            .FirstOrDefault(kvp => kvp.Key == "Group")
            .Value.Value as string;

        return new CommandInfo(classSymbol.Name, name ?? classSymbol.Name.ToLower(), group);
    }

    /// <summary>
    /// Checks if a class has the specified attribute.
    /// </summary>
    public static bool HasAttribute(INamedTypeSymbol classSymbol, string attributeName)
    {
        return classSymbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.ToDisplayString() == attributeName);
    }

    /// <summary>
    /// Gets all methods in a class that should trigger event generation.
    /// </summary>
    public static IEnumerable<IMethodSymbol> GetEventTriggeringMethods(INamedTypeSymbol classSymbol)
    {
        return classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(method => method.DeclaredAccessibility == Accessibility.Public &&
                           !method.IsStatic &&
                           method.MethodKind == MethodKind.Ordinary);
    }

    /// <summary>
    /// Extracts all metadata from the compilation.
    /// </summary>
    public static CompilationAnalysisResult ExtractAll(Compilation compilation)
    {
        var entities = ExtractEntityMetadata(compilation);
        var eventGenerators = ExtractEventGeneratorMetadata(compilation);
        // Add other extractions here as needed
        return new CompilationAnalysisResult(entities, eventGenerators);
    }

    /// <summary>
    /// Extracts entity metadata from all classes with [EntityMetadata] attributes.
    /// </summary>
    private static IEnumerable<EntityMetadataInfo> ExtractEntityMetadata(Compilation compilation)
    {
        var entityMetadata = new List<EntityMetadataInfo>();

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var classDeclarations = syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>();

            foreach (var classDecl in classDeclarations)
            {
                var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                if (classSymbol == null) continue;

                var entityMetadataAttr = classSymbol.GetAttributes()
                    .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == AttributeNames.EntityMetadata);

                if (entityMetadataAttr != null)
                {
                    var metadata = ExtractEntityMetadataInfo(classSymbol, entityMetadataAttr);
                    if (metadata != null)
                    {
                        entityMetadata.Add(metadata);
                    }
                }
            }
        }

        return entityMetadata;
    }

    /// <summary>
    /// Extracts entity metadata info from a single entity class.
    /// </summary>
    public static EntityMetadataInfo? ExtractEntityMetadataInfo(INamedTypeSymbol classSymbol, AttributeData attribute)
    {
        try
        {
            // Extract table name from constructor arguments or named arguments
            var tableName = attribute.ConstructorArguments.FirstOrDefault().Value as string ??
                           attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == "TableName").Value.Value as string ??
                           classSymbol.Name.ToLower();

            var archiveTableName = attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == "ArchiveTableName").Value.Value as string ??
                                  $"{tableName}_archive";

            var maxCacheSize = (int)(attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == "MaxCacheSize").Value.Value ?? 1000);
            var cacheExpiryMinutes = (int)(attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == "CacheExpiryMinutes").Value.Value ?? 60);
            bool emitCacheRegistration = false;
            bool emitArchiveRegistration = false;
            var ctorArgs = attribute.ConstructorArguments;
            // Constructor parameters order:
            // 0: tableName, 1: archiveTableName, 2: idColumn, 3: maxCacheSize, 4: cacheExpiryMinutes,
            // 5: explicitColumns, 6: explicitJsonbColumns, 7: explicitIndexedColumns, 8: servicePropertyName,
            // 9: emitCacheRegistration, 10: emitArchiveRegistration
            if (ctorArgs.Length >= 10 && ctorArgs[9].Value is bool c)
                emitCacheRegistration = c;
            if (ctorArgs.Length >= 11 && ctorArgs[10].Value is bool a)
                emitArchiveRegistration = a;
            // Fallback to named args if ever exposed as settable properties in future
            if (!emitCacheRegistration)
            {
                emitCacheRegistration = (bool)(attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == "EmitCacheRegistration").Value.Value ?? false);
            }
            if (!emitArchiveRegistration)
            {
                emitArchiveRegistration = (bool)(attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == "EmitArchiveRegistration").Value.Value ?? false);
            }

            // Extract column names from public properties
            var columnNames = classSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(prop => prop.DeclaredAccessibility == Accessibility.Public && !prop.IsStatic)
                .Select(prop => prop.Name)
                .ToArray();

            // Optional service property override
            var servicePropertyName = attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == "ServicePropertyName").Value.Value as string;
            if (string.IsNullOrEmpty(servicePropertyName))
            {
                if (ctorArgs.Length >= 9 && ctorArgs[8].Value is string s && !string.IsNullOrWhiteSpace(s))
                {
                    servicePropertyName = s;
                }
            }

            return new EntityMetadataInfo(
                ClassName: classSymbol.Name,
                TableName: tableName,
                ArchiveTableName: archiveTableName,
                MaxCacheSize: maxCacheSize,
                CacheExpiryMinutes: cacheExpiryMinutes,
                EmitCacheRegistration: emitCacheRegistration,
                EmitArchiveRegistration: emitArchiveRegistration,
                ColumnNames: columnNames,
                EntityType: classSymbol,
                ServicePropertyName: servicePropertyName);
        }
        catch
        {
            // Return null if extraction fails
            return null;
        }
    }

    /// <summary>
    /// Extracts event generator metadata from all classes/methods with [EventGenerator] attributes.
    /// </summary>
    private static IEnumerable<EventGeneratorInfo> ExtractEventGeneratorMetadata(Compilation compilation)
    {
        var eventGenerators = new List<EventGeneratorInfo>();

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var classDeclarations = syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>();

            foreach (var classDecl in classDeclarations)
            {
                var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                if (classSymbol == null) continue;

                // Check classes and their methods
                var eventGenAttrOnClass = classSymbol.GetAttributes()
                    .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == AttributeNames.EventGenerator);
                if (eventGenAttrOnClass != null)
                {
                    var metadata = ExtractEventGeneratorInfo(classSymbol, eventGenAttrOnClass, isMethod: false);
                    if (metadata != null) eventGenerators.Add(metadata);
                }

                foreach (var method in classSymbol.GetMembers().OfType<IMethodSymbol>()
                    .Where(m => m.DeclaredAccessibility == Accessibility.Public && !m.IsStatic))
                {
                    var eventGenAttrOnMethod = method.GetAttributes()
                        .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == AttributeNames.EventGenerator);
                    if (eventGenAttrOnMethod != null)
                    {
                        var metadata = ExtractEventGeneratorInfo(classSymbol, eventGenAttrOnMethod, isMethod: true, methodSymbol: method);
                        if (metadata != null) eventGenerators.Add(metadata);
                    }
                }
            }
        }

        return eventGenerators;
    }

    /// <summary>
    /// Extracts event generator info from a single attribute (on class or method).
    /// </summary>
    public static EventGeneratorInfo? ExtractEventGeneratorInfo(
        INamedTypeSymbol classSymbol,
        AttributeData attribute,
        bool isMethod,
        IMethodSymbol? methodSymbol = null)
    {
        try
        {
            // Parse generationTargets: string[] to List<GenerationTargets> via Enum.Parse
            var genTargetsArg = attribute.ConstructorArguments
                .FirstOrDefault().Value as string[] ?? Array.Empty<string>();
            var generationTargets = genTargetsArg.Length > 0
                ? genTargetsArg.Select(gt => (GenerationTargets)Enum
                    .Parse(typeof(GenerationTargets), gt, ignoreCase: true))
                    .ToList() : new List<GenerationTargets>();

            // Parse eventTargets: string[] to EventTargets (combine with | for Flags)
            var evtTargetsArg = attribute.ConstructorArguments
                .Skip(1).FirstOrDefault().Value as string[] ?? new[] { "Global" }; // Default
            var eventTargets = evtTargetsArg.Length > 0
                ? evtTargetsArg.Select(et => (EventTargets)Enum
                    .Parse(typeof(EventTargets), et, ignoreCase: true))
                    .Aggregate((a, b) => a | b) : EventTargets.Global;

            // Bool flags from named args or constructor (order: generateEvents, generatePublishers, etc.)
            var generateEvents = (bool)(attribute.ConstructorArguments.ElementAtOrDefault(2).Value ?? false);
            var generatePublishers = (bool)(attribute.ConstructorArguments.ElementAtOrDefault(3).Value ?? false);
            var generateSubscribers = (bool)(attribute.ConstructorArguments.ElementAtOrDefault(4).Value ?? false);

            // Fallback to named args for bools
            generateEvents = generateEvents || (bool)(attribute.NamedArguments
                .FirstOrDefault(kvp => kvp.Key == "GenerateEvents").Value.Value ?? false);
            generatePublishers = generatePublishers || (bool)(attribute.NamedArguments
                .FirstOrDefault(kvp => kvp.Key == "GeneratePublishers").Value.Value ?? false);
            generateSubscribers = generateSubscribers || (bool)(attribute.NamedArguments
                .FirstOrDefault(kvp => kvp.Key == "GenerateSubscribers").Value.Value ?? false);

            var enableMetrics = (bool)(attribute.NamedArguments
                .FirstOrDefault(kvp => kvp.Key == "EnableMetrics").Value.Value ?? true);
            var enableErrorHandling = (bool)(attribute.NamedArguments
                .FirstOrDefault(kvp => kvp.Key == "EnableErrorHandling").Value.Value ?? true);
            var enableLogging = (bool)(attribute.NamedArguments
                .FirstOrDefault(kvp => kvp.Key == "EnableLogging").Value.Value ?? true);

            // Get custom event name if provided
            var customEventName = attribute.NamedArguments
                .FirstOrDefault(kvp => kvp.Key == "EventName").Value.Value as string;

            // Get namespace
            var namespaceName = classSymbol.ContainingNamespace?.ToDisplayString() ?? "Generated";

            return new EventGeneratorInfo(
                className: classSymbol.Name,
                @namespace: namespaceName,
                isMethod: isMethod,
                methodName: methodSymbol?.Name,
                methodSymbol: methodSymbol,
                classSymbol: classSymbol,
                generationTargets: generationTargets,
                eventTargets: eventTargets,
                generateEvents: generateEvents,
                generatePublishers: generatePublishers,
                generateSubscribers: generateSubscribers,
                enableMetrics: enableMetrics,
                enableErrorHandling: enableErrorHandling,
                enableLogging: enableLogging,
                customEventName: customEventName);
        }
        catch
        {
            return null; // Fail silently if parsing fails
        }
    }
}

/// <summary>
/// Information about a command class.
/// </summary>
public class CommandInfo
{
    public string ClassName { get; }
    public string CommandName { get; }
    public string? Group { get; } = null;

    public CommandInfo(string className, string commandName, string? group = null)
    {
        ClassName = className;
        CommandName = commandName;
        Group = group;
    }
}

/// <summary>
/// Information extracted from an [EventGenerator] attribute.
/// </summary>
public class EventGeneratorInfo
{
    public string ClassName { get; }
    public string Namespace { get; }
    public bool IsMethod { get; }
    public string? MethodName { get; }
    public IMethodSymbol? MethodSymbol { get; }
    public INamedTypeSymbol ClassSymbol { get; }
    public List<GenerationTargets> GenerationTargets { get; }
    public EventTargets EventTargets { get; }
    public bool GenerateEvents { get; }
    public bool GeneratePublishers { get; }
    public bool GenerateSubscribers { get; }
    public bool EnableMetrics { get; }
    public bool EnableErrorHandling { get; }
    public bool EnableLogging { get; }
    public string? CustomEventName { get; }

    public EventGeneratorInfo(
        string className,
        string @namespace,
        bool isMethod,
        string? methodName,
        IMethodSymbol? methodSymbol,
        INamedTypeSymbol classSymbol,
        List<GenerationTargets> generationTargets,
        EventTargets eventTargets,
        bool generateEvents,
        bool generatePublishers,
        bool generateSubscribers,
        bool enableMetrics,
        bool enableErrorHandling,
        bool enableLogging,
        string? customEventName = null)
    {
        ClassName = className;
        Namespace = @namespace;
        IsMethod = isMethod;
        MethodName = methodName;
        MethodSymbol = methodSymbol;
        ClassSymbol = classSymbol;
        GenerationTargets = generationTargets;
        EventTargets = eventTargets;
        GenerateEvents = generateEvents;
        GeneratePublishers = generatePublishers;
        GenerateSubscribers = generateSubscribers;
        EnableMetrics = enableMetrics;
        EnableErrorHandling = enableErrorHandling;
        EnableLogging = enableLogging;
        CustomEventName = customEventName;
    }
}

/// <summary>
/// Result of analyzing the entire compilation.
/// </summary>
public class CompilationAnalysisResult
{
    public IEnumerable<EntityMetadataInfo> Entities { get; }
    public IEnumerable<EventGeneratorInfo> EventGenerators { get; }
    public CompilationAnalysisResult(IEnumerable<EntityMetadataInfo> entities, IEnumerable<EventGeneratorInfo> eventGenerators)
    {
        Entities = entities;
        EventGenerators = eventGenerators;
    }
}
