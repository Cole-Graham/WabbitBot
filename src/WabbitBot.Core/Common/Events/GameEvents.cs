using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Events;

#region Core-Only Events (Internal to Core Project)

/// <summary>
/// Event published when a game is created
/// </summary>
public partial record GameCreatedEvent(
    string GameId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a game is updated
/// </summary>
public partial record GameUpdatedEvent(
    string GameId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a game is archived
/// </summary>
public partial record GameArchivedEvent(
    string GameId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a game is completed
/// </summary>
public partial record GameCompletedEvent(
    string GameId,
    string WinnerId,
    string CompletedByUserId,
    string CompletedByPlayerName
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a game is deleted
/// </summary>
public partial record GameDeletedEvent(
    string GameId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a game is started
/// </summary>
public partial record GameStartedEvent(
    string GameId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a game is cancelled
/// </summary>
public partial record GameCancelledEvent(
    string GameId,
    string Reason
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a game is forfeited
/// </summary>
public partial record GameForfeitedEvent(
    string GameId,
    string ForfeitedTeamId,
    string Reason
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

#endregion

#region Global Events (Cross Project Boundaries)

/// <summary>
/// Event published when a game creation is requested (e.g., from MatchService)
/// </summary>
public partial record GameCreationRequestedEvent(
    string MatchId,
    string MapId,
    EvenTeamFormat EvenTeamFormat,
    List<string> Team1PlayerIds,
    List<string> Team2PlayerIds,
    int GameNumber,
    string RequestedByUserId,
    string RequestedByPlayerName
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

#endregion
