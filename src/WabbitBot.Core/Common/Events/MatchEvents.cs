using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Common.Events;

/// <summary>
/// Core-owned match events that cross project boundaries.
/// Source generators will copy these to DiscBot in step 6.
/// </summary>

/// <summary>
/// Global event requesting match provisioning (threads, containers).
/// Published by Core to request DiscBot create Discord UI.
/// Owned by Core; DiscBot will receive generated copy.
/// Can be auto-generated via [EventTrigger] in step 6 with targets: Both.
/// </summary>
public record MatchProvisioningRequested(
    Guid MatchId,
    Guid ScrimmageId
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
}

/// <summary>
/// Core event indicating a match has started.
/// Published locally within Core when match begins.
/// </summary>
public record MatchStartedEvent(
    Guid MatchId
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
}

/// <summary>
/// Core event indicating a match has completed.
/// Published locally within Core when match finishes.
/// </summary>
public record MatchCompletedEvent(
    Guid MatchId,
    Guid WinnerTeamId
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
}