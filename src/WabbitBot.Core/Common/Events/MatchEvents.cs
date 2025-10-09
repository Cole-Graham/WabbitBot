using WabbitBot.Common.Events.Interfaces;
using WabbitBot.Common.Attributes;

namespace WabbitBot.Core.Common.Events;

/// <summary>
/// Core-owned match events that cross project boundaries.
/// Source generators will copy these to DiscBot in step 6.
/// </summary>

/// <summary>
/// Core event indicating a match has been created.
/// Published locally within Core when match is created.
/// </summary>
[EventGenerator(
    pubTargetClass: "WabbitBot.Core.Common.Models.Common.MatchCore",
    subTargetClasses: ["WabbitBot.DiscBot.App.Handlers.MatchHandler"])]
public record ScrimmageMatchCreated(
    ulong ScrimmageChannelId,
    Guid MatchId
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
public record ScrimmageMatchStartedEvent(
    ulong ScrimmageChannelId,
    ulong team1ThreadId,
    ulong team2ThreadId,
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
public record ScrimmageMatchCompletedEvent(
    ulong ScrimmageChannelId,
    ulong team1ThreadId,
    ulong team2ThreadId,
    Guid MatchId,
    Guid WinnerTeamId
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
}