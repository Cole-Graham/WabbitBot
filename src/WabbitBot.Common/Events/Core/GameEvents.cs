using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.Interfaces;

namespace WabbitBot.Common.Events.Core;

/// <summary>
/// Published when a player submits a deck code for a game.
/// Used to update GameStateSnapshot and trigger game container message updates.
/// </summary>
/// <param name="GameId">The game the deck code was submitted for</param>
/// <param name="PlayerId">The player who submitted the deck code</param>
/// <param name="DeckCode">The submitted deck code</param>
/// <param name="DivisionId">The division ID parsed from the deck code</param>
/// <param name="DivisionName">The division name parsed from the deck code, if available</param>
[EventGenerator(
    pubTargetClass: "WabbitBot.DiscBot.App.GameApp",
    subTargetClasses: ["WabbitBot.DiscBot.App.Handlers.GameHandler", "WabbitBot.Core.Common.Models.Common.GameHandler"],
    writeHandlers: ["WabbitBot.Core.Common.Models.Common.GameHandler"]
)]
public record PlayerDeckSubmitted(Guid GameId, Guid PlayerId, string DeckCode, int DivisionId, string? DivisionName)
    : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
}

/// <summary>
/// Published when a player confirms their submitted deck code for a game.
/// Used to update GameStateSnapshot and trigger game container message updates.
/// </summary>
/// <param name="GameId">The game the deck code was confirmed for</param>
/// <param name="PlayerId">The player who confirmed the deck code</param>
[EventGenerator(
    pubTargetClass: "WabbitBot.DiscBot.App.GameApp",
    subTargetClasses: ["WabbitBot.DiscBot.App.Handlers.GameHandler", "WabbitBot.Core.Common.Models.Common.GameHandler"],
    writeHandlers: ["WabbitBot.Core.Common.Models.Common.GameHandler"]
)]
public record PlayerDeckConfirmed(Guid GameId, Guid PlayerId) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
}

/// <summary>
/// Published when a player revises (resets) their submitted deck code for a game.
/// Used to update GameStateSnapshot and trigger game container message updates.
/// </summary>
/// <param name="GameId">The game the deck code was revised for</param>
/// <param name="PlayerId">The player who revised the deck code</param>
[EventGenerator(
    pubTargetClass: "WabbitBot.DiscBot.App.GameApp",
    subTargetClasses: ["WabbitBot.DiscBot.App.Handlers.GameHandler", "WabbitBot.Core.Common.Models.Common.GameHandler"],
    writeHandlers: ["WabbitBot.Core.Common.Models.Common.GameHandler"]
)]
public record PlayerDeckRevised(Guid GameId, Guid PlayerId) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
}

/// <summary>
/// Published when a player submits a replay for a game.
/// Used to trigger game container message updates.
/// </summary>
/// <param name="GameId">The game the replay was submitted for</param>
/// <param name="PlayerId">The player who submitted the replay</param>
/// <param name="ReplayId">The ID of the submitted replay</param>
[EventGenerator(
    pubTargetClass: "WabbitBot.DiscBot.App.GameApp",
    subTargetClasses: ["WabbitBot.DiscBot.App.Handlers.GameHandler", "WabbitBot.Core.Common.Models.Common.GameHandler"],
    writeHandlers: ["WabbitBot.Core.Common.Models.Common.GameHandler"]
)]
public record PlayerReplaySubmitted(Guid GameId, Guid PlayerId, Guid ReplayId) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
}

/// <summary>
/// Published when all players on both teams have submitted their replays for a game.
/// Triggers game finalization: determining winner, updating UI, checking match victory.
/// </summary>
/// <param name="GameId">The game for which all replays have been submitted</param>
/// <param name="MatchId">The match this game belongs to</param>
[EventGenerator(
    pubTargetClass: "WabbitBot.Core.Common.Models.Common.GameHandler",
    subTargetClasses: ["WabbitBot.Core.Common.Models.Common.GameHandler"]
)]
public record AllReplaysSubmitted(Guid GameId, Guid MatchId) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
}

/// <summary>
/// Published when a game has been completed (winner determined from replays).
/// Triggers UI updates to show game results and checks for match victory.
/// </summary>
/// <param name="GameId">The completed game ID</param>
/// <param name="MatchId">The match this game belongs to</param>
/// <param name="WinnerTeamId">The ID of the team that won the game</param>
[EventGenerator(
    pubTargetClass: "WabbitBot.Core.Common.Models.Common.GameHandler",
    subTargetClasses: [
        "WabbitBot.DiscBot.App.Handlers.GameHandler",
        "WabbitBot.Core.Common.Models.Common.MatchHandler",
    ],
    writeHandlers: ["WabbitBot.Core.Common.Models.Common.MatchHandler"]
)]
public record GameCompleted(Guid GameId, Guid MatchId, Guid WinnerTeamId) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
}
