using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.Interfaces;

namespace WabbitBot.Common.Events.Core;

/// <summary>
/// Core event indicating a match has been created.
/// Published locally within Core when match is created.
/// </summary>
[EventGenerator(
    pubTargetClass: "WabbitBot.Core.Common.Models.Common.MatchHandler",
    subTargetClasses: ["WabbitBot.DiscBot.App.Handlers"]
)]
public record ScrimmageMatchCreated(Guid ScrimmageId, Guid MatchId) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
}

/// <summary>
/// Core event indicating map bans have been confirmed for a scrimmage match.
/// Published when all map bans are complete and the final map pool is ready.
/// </summary>
[EventGenerator(
    pubTargetClass: "WabbitBot.DiscBot.App.MatchApp",
    subTargetClasses: ["WabbitBot.Core.Common.Models.Common.MatchHandler"]
)]
public record ScrimmageMapBansConfirmed(Guid ScrimmageId, Guid MatchId) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
}

/// <summary>
/// Core event indicating a game has been created for a scrimmage match.
/// Published after the map pool is finalized and a game instance is created.
/// </summary>
[EventGenerator(
    pubTargetClass: "WabbitBot.Core.Common.Models.Common.MatchHandler",
    subTargetClasses: ["WabbitBot.DiscBot.App.Handlers.GameHandler"]
)]
public record ScrimmageGameCreated(Guid ScrimmageId, Guid MatchId, Guid GameId) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
}

/// <summary>
/// Global event published when a match series completes.
/// Triggers rating calculations, scrimmage completion, leaderboard updates, etc.
/// Cross-boundary integration fact for persistence and rating updates.
/// </summary>
[EventGenerator(
    pubTargetClass: "WabbitBot.Core.Common.Models.Common.MatchHandler",
    subTargetClasses: ["WabbitBot.Core.Scrimmages.ScrimmageHandler", "WabbitBot.DiscBot.App.Handlers.MatchHandler"],
    writeHandlers: ["WabbitBot.Core.Scrimmages.ScrimmageHandler"]
)]
public record ScrimmageMatchCompleted(Guid MatchId, Guid WinnerTeamId) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
}
