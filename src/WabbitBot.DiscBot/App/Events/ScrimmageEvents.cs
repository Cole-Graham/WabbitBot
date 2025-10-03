using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.DiscBot.App.Events;

/// <summary>
/// DiscBot-owned scrimmage events that cross project boundaries.
/// Source generators will copy these to Core in step 6.
/// </summary>

/// <summary>
/// Global event published when a scrimmage challenge is requested via Discord command.
/// Owned by DiscBot; Core will receive generated copy.
/// </summary>
public record ScrimmageChallengeRequested(
    string ChallengerTeamName,
    string OpponentTeamName,
    ulong RequesterId,
    ulong ChannelId
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
}

/// <summary>
/// Global event published when a scrimmage challenge is accepted via Discord interaction.
/// Owned by DiscBot; Core will receive generated copy.
/// </summary>
public record ScrimmageAccepted(
    Guid ChallengeId,
    ulong AccepterId
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
}

/// <summary>
/// Global event published when a scrimmage challenge is declined via Discord interaction.
/// Owned by DiscBot; Core will receive generated copy.
/// </summary>
public record ScrimmageDeclined(
    Guid ChallengeId,
    ulong DeclinerId
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
}

/// <summary>
/// Global event published when a scrimmage challenge is cancelled via Discord command.
/// Owned by DiscBot; Core will receive generated copy.
/// </summary>
public record ScrimmageCancelled(
    Guid ChallengeId,
    ulong RequesterId
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
}

