using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.Interfaces;

namespace WabbitBot.Core.Common.Events;

/// <summary>
/// Core event indicating a match has started.
/// Published locally within Core when match begins.
/// </summary>
public record ScrimmageMatchStarted(
    Guid MatchId,
    ulong ScrimmageChannelId,
    ulong ChallengerThreadId,
    ulong OpponentThreadId
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
public record ScrimmageMatchCompleted(
    Guid MatchId,
    Guid WinnerTeamId,
    ulong ScrimmageChannelId,
    ulong ChallengerThreadId,
    ulong OpponentThreadId
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
}
