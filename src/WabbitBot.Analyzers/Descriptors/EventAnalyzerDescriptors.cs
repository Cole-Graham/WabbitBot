using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WabbitBot.Analyzers.Descriptors;

/// <summary>
/// Diagnostic descriptors for event-related analyzers.
/// </summary>
public static class EventAnalyzerDescriptors
{
    public static readonly DiagnosticDescriptor EventBoundaryOnRecord = new(
        id: "WB001",
        title: "EventBoundary on record",
        messageFormat: "Consider using EventBoundary on classes instead of records for better extensibility",
        category: "WabbitBot.Event",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "EventBoundary attributes work better on classes that can be extended with partial methods.",
        helpLinkUri: "https://github.com/Cole-Graham/WabbitBot/blob/main/contributors/docs/analyzers/rules/WB001.md",
        customTags: new[] { WellKnownDiagnosticTags.Telemetry }
    );

    public static readonly DiagnosticDescriptor MissingEventBusInjection = new(
        id: "WB002",
        title: "Missing event bus injection",
        messageFormat: "Classes with EventBoundary should have an injected ICoreEventBus or IDiscordEventBus parameter",
        category: "WabbitBot.Event",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Event boundary classes need access to an event bus for publishing events.",
        helpLinkUri: "https://github.com/Cole-Graham/WabbitBot/blob/main/contributors/docs/analyzers/rules/WB002.md",
        customTags: new[] { WellKnownDiagnosticTags.Telemetry }
    );

    public static readonly DiagnosticDescriptor CrudEventDetected = new(
        id: "WB003",
        title: "CRUD event detected",
        messageFormat: "Event '{0}' appears to be a CRUD operation. Consider using direct repository calls instead of +"
            + "events for database operations.",
        category: "WabbitBot.Event",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "CRUD operations should be handled via repositories, not event buses.",
        helpLinkUri: "https://github.com/Cole-Graham/WabbitBot/blob/main/contributors/docs/analyzers/rules/WB003.md",
        customTags: new[] { WellKnownDiagnosticTags.Telemetry }
    );

    public static readonly DiagnosticDescriptor EventWithoutIEvent = new(
        id: "WB004",
        title: "Event class without IEvent interface",
        messageFormat: "Event class '{0}' should implement IEvent interface",
        category: "WabbitBot.Event",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All event classes must implement IEvent for proper bus routing.",
        helpLinkUri: "https://github.com/Cole-Graham/WabbitBot/blob/main/contributors/docs/analyzers/rules/WB004.md",
        customTags: new[] { WellKnownDiagnosticTags.Telemetry }
    );

    public static readonly DiagnosticDescriptor MissingTableName = new(
        id: "WB005",
        title: "Missing TableName in EntityMetadata",
        messageFormat: "Entity class '{0}' has [EntityMetadata] but no tableName specified",
        category: "WabbitBot.Database",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "EntityMetadata attributes must specify a TableName for database mapping.",
        helpLinkUri: "https://github.com/Cole-Graham/WabbitBot/blob/main/contributors/docs/analyzers/rules/WB005.md",
        customTags: new[] { WellKnownDiagnosticTags.Telemetry }
    );

    public static readonly DiagnosticDescriptor InvalidCacheSize = new(
        id: "WB006",
        title: "Invalid cache size",
        messageFormat: "Entity class '{0}' has invalid MaxCacheSize '{1}'. Must be positive.",
        category: "WabbitBot.Database",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Cache sizes should be positive integers for proper memory management.",
        helpLinkUri: "https://github.com/Cole-Graham/WabbitBot/blob/main/contributors/docs/analyzers/rules/WB006.md",
        customTags: new[] { WellKnownDiagnosticTags.Telemetry }
    );
}
