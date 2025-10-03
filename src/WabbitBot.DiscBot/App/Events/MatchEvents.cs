using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.DiscBot.App.Events;

#region Match Provisioning
/// <summary>
/// DiscBot-local event requesting thread creation for a match.
/// </summary>
public record MatchThreadCreateRequested(
    Guid MatchId,
    Guid ScrimmageId
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
}

/// <summary>
/// DiscBot-local event requesting container creation for a match.
/// </summary>
public record MatchContainerRequested(
    Guid MatchId,
    ulong ThreadId
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
}

/// <summary>
/// DiscBot-local event confirming a match thread was created.
/// </summary>
public record MatchThreadCreated(
    Guid MatchId,
    ulong ThreadId
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
}
#endregion

#region Map Ban
/// <summary>
/// DiscBot-local event requesting map ban DM start for a player.
/// </summary>
public record MapBanDmStartRequested(
    Guid MatchId,
    ulong PlayerDiscordId
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
}

/// <summary>
/// DiscBot-local event requesting map ban DM update (preview).
/// </summary>
public record MapBanDmUpdateRequested(
    Guid MatchId,
    ulong PlayerId,
    string[] Selections
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
}

/// <summary>
/// DiscBot-local event requesting map ban DM confirmation (lock UI).
/// </summary>
public record MapBanDmConfirmRequested(
    Guid MatchId,
    ulong PlayerId,
    string[] Selections
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
}

/// <summary>
/// DiscBot-local event from interaction when player selects provisional map bans.
/// </summary>
public record PlayerMapBanSelected(
    Guid MatchId,
    ulong PlayerId,
    string[] Selections
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
}

/// <summary>
/// DiscBot-local event from interaction when player confirms their map bans.
/// </summary>
public record PlayerMapBanConfirmed(
    Guid MatchId,
    ulong PlayerId,
    string[] Selections
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
}
#endregion

#region Global Match Events

/// <summary>
/// Global event confirming match is provisioned (Discord UI ready).
/// Published by MatchRenderer after creating thread and container.
/// Owned by DiscBot; Core will receive generated copy.
/// </summary>
public record MatchProvisioned(
    Guid MatchId,
    ulong ThreadId
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
}

/// <summary>
/// Global event published when a match series completes.
/// Cross-boundary integration fact for persistence, rating updates, leaderboards.
/// Owned by DiscBot; Core will receive generated copy.
/// Can be auto-generated via [EventTrigger] in step 6.
/// </summary>
public record MatchCompleted(
    Guid MatchId,
    Guid WinnerTeamId
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
}

#endregion

