namespace WabbitBot.Generator.Shared.Metadata;

/// <summary>
/// Metadata for event boundary generation.
/// </summary>
public record EventBoundaryInfo(
    string ClassName,
    bool GenerateRequestResponse = false,
    EventBusType? BusType = null,
    string? TargetProjects = null
)
{
    // Note: Creation from symbols is handled in AttributeAnalyzer to avoid circular dependencies
}
