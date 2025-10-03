using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.DiscBot.App.Events;

/// <summary>
/// DiscBot-local event requesting per-game container creation.
/// </summary>
public record GameContainerRequested(
    Guid MatchId,
    int GameNumber,
    string ChosenMap
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
}

/// <summary>
/// DiscBot-local event from interaction when game replay is submitted.
/// </summary>
public record GameReplaySubmitted(
    Guid MatchId,
    int GameNumber,
    Guid[] ReplayFileIds
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
}

/// <summary>
/// DiscBot-local event requesting deck submission DM start for a player.
/// </summary>
public record DeckDmStartRequested(
    Guid MatchId,
    int GameNumber,
    ulong PlayerDiscordId
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
}

/// <summary>
/// DiscBot-local event requesting deck submission DM update (preview).
/// </summary>
public record DeckDmUpdateRequested(
    Guid MatchId,
    int GameNumber,
    ulong PlayerId,
    string DeckCode
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
}

/// <summary>
/// DiscBot-local event requesting deck submission DM confirmation (lock UI).
/// </summary>
public record DeckDmConfirmRequested(
    Guid MatchId,
    int GameNumber,
    ulong PlayerId,
    string DeckCode
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
}

/// <summary>
/// DiscBot-local event from interaction when player submits a deck code.
/// </summary>
public record PlayerDeckSubmitted(
    Guid MatchId,
    int GameNumber,
    ulong PlayerId,
    string DeckCode
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
}

/// <summary>
/// DiscBot-local event from interaction when player confirms their deck code.
/// </summary>
public record PlayerDeckConfirmed(
    Guid MatchId,
    int GameNumber,
    ulong PlayerId,
    string DeckCode
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
}

#region Global Game Events (Cross-Boundary)

/// <summary>
/// Global event published when a game starts (optional).
/// Owned by DiscBot; Core will receive generated copy.
/// Can be auto-generated via [EventTrigger] in step 6.
/// </summary>
public record GameStarted(
    Guid MatchId,
    int GameNumber,
    string ChosenMap
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
}

/// <summary>
/// Global event published when a game completes.
/// Cross-boundary integration fact for Core stats/ratings.
/// Owned by DiscBot; Core will receive generated copy.
/// Can be auto-generated via [EventTrigger] in step 6.
/// </summary>
public record GameCompleted(
    Guid MatchId,
    int GameNumber,
    Guid WinnerTeamId
) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
}

#endregion

