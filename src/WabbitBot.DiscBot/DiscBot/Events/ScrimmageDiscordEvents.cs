using WabbitBot.Common.Events;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.DiscBot.DiscBot.Events;

/// <summary>
/// Discord-specific events for scrimmage integration
/// These events stay on DiscordEventBus and are not forwarded to GlobalEventBus
/// </summary>
public abstract record ScrimmageDiscordEvent : IEvent
{
    public EventBusType EventBusType => EventBusType.DiscBot;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string ScrimmageId { get; init; } = string.Empty;
}

/// <summary>
/// Discord event when a scrimmage challenge is accepted
/// </summary>
public record ScrimmageAcceptedDiscordEvent : ScrimmageDiscordEvent
{
    public string AcceptedBy { get; init; } = string.Empty;
}

/// <summary>
/// Discord event when a scrimmage challenge is declined
/// </summary>
public record ScrimmageDeclinedDiscordEvent : ScrimmageDiscordEvent
{
    public string DeclinedBy { get; init; } = string.Empty;
}

/// <summary>
/// Discord event when a scrimmage is completed
/// </summary>
public record ScrimmageCompletedDiscordEvent : ScrimmageDiscordEvent
{
    public Guid MatchId { get; init; }
    public string Team1Id { get; init; } = string.Empty;
    public string Team2Id { get; init; } = string.Empty;
    public int Team1Score { get; init; }
    public int Team2Score { get; init; }
    public EvenTeamFormat EvenTeamFormat { get; init; }
    public double Team1Confidence { get; init; } = 0.0;
    public double Team2Confidence { get; init; } = 0.0;
}
