using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Events;

#region Core-Only Events (Internal to Core Project)

// CRUD events removed: GameCreatedEvent, GameUpdatedEvent, GameArchivedEvent
// These were database operations and violate the critical principle that events are not for CRUD.

/// <summary>
/// Event published when a game is completed
/// </summary>
public partial record GameCompletedEvent(
    Guid GameId,
    Guid WinnerId,
    Guid CompletedByUserId,
    string CompletedByPlayerName
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// Additional CRUD event removed: GameDeletedEvent
// This was a database operation and violates the critical principle that events are not for CRUD.

/// <summary>
/// Event published when a game is started
/// </summary>
public partial record GameStartedEvent(
    Guid GameId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a game is cancelled
/// </summary>
public partial record GameCancelledEvent(
    Guid GameId,
    string Reason
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a game is forfeited
/// </summary>
public partial record GameForfeitedEvent(
    Guid GameId,
    Guid ForfeitedTeamId,
    string Reason
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

#endregion

#region Global Events (Cross Project Boundaries)

/// <summary>
/// Event published when a game creation is requested (e.g., from MatchService)
/// </summary>
public partial record GameCreationRequestedEvent(
    Guid MatchId,
    Guid MapId,
    TeamSize TeamSize,
    List<Guid> Team1PlayerIds,
    List<Guid> Team2PlayerIds,
    int GameNumber,
    Guid RequestedByUserId,
    string RequestedByPlayerName
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

#endregion
