using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Services.DiscBot;

namespace WabbitBot.DiscBot.App;

/// <summary>
/// Handles game flow: per-game containers, deck submission, map selection, and game lifecycle.
/// This app is library-agnostic and communicates only via events.
/// </summary>
[EventGenerator(GenerateSubscribers = true, GeneratePublishers = true, DefaultBus = EventBusType.DiscBot, TriggerMode = "OptIn")]
public partial class GameApp : IGameApp
{
    /// <summary>
    /// Starts the deck submission DM flow for players before a game.
    /// Publishes DM start requests for each player.
    /// </summary>
    public async Task StartDeckSubmissionDMsAsync(Guid matchId, int gameNumber, ulong player1DiscordId, ulong player2DiscordId)
    {
        // Request DM creation for both players; Renderer will send the actual DMs
        await DiscBotService.PublishAsync(new DeckDmStartRequested(matchId, gameNumber, player1DiscordId));
        await DiscBotService.PublishAsync(new DeckDmStartRequested(matchId, gameNumber, player2DiscordId));
    }

    /// <summary>
    /// Handles player deck code submission (provisional).
    /// Updates the DM preview with the submitted deck code.
    /// </summary>
    public async Task OnDeckSubmittedAsync(Guid matchId, int gameNumber, ulong playerId, string deckCode)
    {
        // Publish event to update DM preview; Renderer will update the Discord message
        await DiscBotService.PublishAsync(new DeckDmUpdateRequested(matchId, gameNumber, playerId, deckCode));
    }

    /// <summary>
    /// Handles player deck code confirmation (final).
    /// Locks the DM UI and prepares for game start.
    /// </summary>
    public async Task OnDeckConfirmedAsync(Guid matchId, int gameNumber, ulong playerId, string deckCode)
    {
        // Publish event to lock DM UI; Renderer will disable components
        await DiscBotService.PublishAsync(new DeckDmConfirmRequested(matchId, gameNumber, playerId, deckCode));

        // TODO: Check if both players have confirmed, then trigger game start
        // This will be coordinated via a flow orchestrator or match state tracker
    }

    /// <summary>
    /// Starts the next game in a match series.
    /// Chooses a map and requests per-game container creation.
    /// </summary>
    public async Task StartNextGameAsync(Guid matchId, int gameNumber, string[] remainingMaps)
    {
        // Choose a random map from remaining pool
        var chosenMap = ChooseRandomMap(remainingMaps);

        // Request per-game container creation; Renderer will create and post it
        await DiscBotService.PublishAsync(new GameContainerRequested(matchId, gameNumber, chosenMap));

        // Publish GameStarted to Global for Core/analytics
        await PublishGameStartedAsync(matchId, gameNumber, chosenMap);
    }

    /// <summary>
    /// Publishes GameStarted event to Global bus.
    /// Auto-generated publisher will be created for this method.
    /// </summary>
    [EventTrigger(BusType = EventBusType.Global, Targets = EventTargets.Global)]
    public partial ValueTask PublishGameStartedAsync(Guid matchId, int gameNumber, string chosenMap);

    /// <summary>
    /// Handles game replay submission and winner determination.
    /// </summary>
    public async Task OnReplaySubmittedAsync(Guid matchId, int gameNumber, Guid[] replayFileIds)
    {
        // TODO: Integrate with replay parser to determine winner
        // For now, this is a placeholder for the replay processing flow

        // Parse replay(s) and determine winner (this would call a replay service)
        // var result = await ReplayParser.DetermineWinnerAsync(replayFileIds);

        // Update the per-game container with the winner
        // This will be handled by a Renderer subscribing to game completion events

        // Broadcast result for Core/state consumers
        // await PublishGameCompletedAsync(matchId, gameNumber, result.WinnerTeamId);
    }

    /// <summary>
    /// Publishes GameCompleted event to Global bus.
    /// Auto-generated publisher will be created for this method.
    /// </summary>
    [EventTrigger(BusType = EventBusType.Global, Targets = EventTargets.Global)]
    public partial ValueTask PublishGameCompletedAsync(Guid matchId, int gameNumber, Guid winnerTeamId);

    /// <summary>
    /// Continues to next game or finishes the match based on series state.
    /// </summary>
    public async Task ContinueOrFinishAsync(Guid matchId, bool hasWinner, Guid? winnerTeamId)
    {
        if (hasWinner && winnerTeamId.HasValue)
        {
            // Match is complete; broadcast final result
            await PublishMatchCompletedAsync(matchId, winnerTeamId.Value);
            return;
        }

        // Continue to next game
        // This would be triggered by the flow orchestrator with updated state
        // await StartNextGameAsync(matchId, nextGameNumber, remainingMaps);
    }

    /// <summary>
    /// Publishes MatchCompleted event to Global bus.
    /// Auto-generated publisher will be created for this method.
    /// </summary>
    [EventTrigger(BusType = EventBusType.Global, Targets = EventTargets.Global)]
    public partial ValueTask PublishMatchCompletedAsync(Guid matchId, Guid winnerTeamId);

    /// <summary>
    /// Initializes the app by subscribing to relevant events.
    /// This will be replaced by generated code once EventGenerator is implemented.
    /// </summary>
    public void Initialize()
    {
        DiscBotService.EventBus.Subscribe<GameReplaySubmitted>(evt =>
            OnReplaySubmittedAsync(evt.MatchId, evt.GameNumber, evt.ReplayFileIds));
        DiscBotService.EventBus.Subscribe<PlayerDeckSubmitted>(evt =>
            OnDeckSubmittedAsync(evt.MatchId, evt.GameNumber, evt.PlayerId, evt.DeckCode));
        DiscBotService.EventBus.Subscribe<PlayerDeckConfirmed>(evt =>
            OnDeckConfirmedAsync(evt.MatchId, evt.GameNumber, evt.PlayerId, evt.DeckCode));
    }

    private static string ChooseRandomMap(string[] remainingMaps)
    {
        if (remainingMaps is null || remainingMaps.Length == 0)
        {
            throw new InvalidOperationException("No maps available for selection");
        }

        var random = new Random();
        return remainingMaps[random.Next(remainingMaps.Length)];
    }
}

