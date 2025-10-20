using System.Collections.Concurrent;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Options;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Events.Core;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Services;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Services.DiscBot;

/// <summary>
/// Handles button and component interactions for game flows.
/// Publishes DiscBot-local interaction events to the event bus.
/// Also handles game-related "Requested" events and calls appropriate Renderer methods.
/// </summary>
namespace WabbitBot.DiscBot.App.Handlers
{
    /// <summary>
    /// Handles button and component interactions for game flows.
    /// Publishes DiscBot-local interaction events to the event bus.
    /// Also handles game-related "Requested" events and calls appropriate Renderer methods.
    /// </summary>
    public partial class GameHandler
    {
        // Batching mechanism for replay submissions to prevent Discord message flickering
        private static readonly ConcurrentDictionary<Guid, HashSet<Guid>> _pendingGameUpdates = new();
        private static readonly SemaphoreSlim _batchLock = new(1, 1);
        private static Timer? _batchTimer;
        private static readonly TimeSpan _batchInterval = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Starts the background batch processor for game container updates.
        /// Should be called once during bot initialization.
        /// </summary>
        public static void StartBatchProcessor()
        {
            _batchTimer = new Timer(ProcessBatchedUpdates, null, _batchInterval, _batchInterval);
        }

        /// <summary>
        /// Stops the background batch processor.
        /// Should be called during bot shutdown.
        /// </summary>
        public static void StopBatchProcessor()
        {
            _batchTimer?.Dispose();
            _batchTimer = null;
        }

        /// <summary>
        /// Processes all pending game container updates in a batch.
        /// Called automatically by the timer every 2 seconds.
        /// </summary>
        private static async void ProcessBatchedUpdates(object? state)
        {
            if (!_pendingGameUpdates.Any())
            {
                return;
            }

            await _batchLock.WaitAsync();
            try
            {
                // Snapshot the current pending updates and clear the queue
                var gamesToUpdate = _pendingGameUpdates.ToList();
                _pendingGameUpdates.Clear();

                // Process each game's update
                foreach (var (gameId, playerIds) in gamesToUpdate)
                {
                    try
                    {
                        // Use the first player ID for the update (all players are shown in the update anyway)
                        var playerId = playerIds.FirstOrDefault();
                        if (playerId == Guid.Empty)
                        {
                            continue;
                        }

                        await Renderers.GameRenderer.UpdateGameContainerReplayStatusAsync(gameId, playerId);
                    }
                    catch (Exception ex)
                    {
                        await DiscBotService.ErrorHandler.CaptureAsync(
                            ex,
                            $"Failed to process batched update for game {gameId}",
                            nameof(ProcessBatchedUpdates)
                        );
                    }
                }
            }
            finally
            {
                _batchLock.Release();
            }
        }

        public static async Task HandleScrimmageGameCreatedAsync(ScrimmageGameCreated evt)
        {
            // Retrieve the game from the database
            var gameResult = await CoreService.Games.GetByIdAsync(evt.GameId, DatabaseComponent.Repository);
            if (!gameResult.Success || gameResult.Data is null)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    new InvalidOperationException($"Failed to get game: {gameResult.ErrorMessage}"),
                    "Failed to get game",
                    nameof(HandleScrimmageGameCreatedAsync)
                );
                return;
            }
            var game = gameResult.Data;

            // Retrieve the match to get thread IDs
            var matchResult = await CoreService.Matches.GetByIdAsync(evt.MatchId, DatabaseComponent.Repository);
            if (!matchResult.Success || matchResult.Data is null)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    new InvalidOperationException($"Failed to get match: {matchResult.ErrorMessage}"),
                    "Failed to get match",
                    nameof(HandleScrimmageGameCreatedAsync)
                );
                return;
            }
            var match = matchResult.Data;

            // Validate that threads exist
            if (match.Team1ThreadId is null || match.Team2ThreadId is null)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    new InvalidOperationException("Match threads not found"),
                    "Match threads not found",
                    nameof(HandleScrimmageGameCreatedAsync)
                );
                return;
            }

            // Get the Discord client and threads
            var client = DiscBotService.Client;
            if (client is null)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    new InvalidOperationException("Discord client not initialized"),
                    "Discord client not initialized",
                    nameof(HandleScrimmageGameCreatedAsync)
                );
                return;
            }

            try
            {
                var team1Thread = await client.GetChannelAsync(match.Team1ThreadId.Value) as DiscordThreadChannel;
                var team2Thread = await client.GetChannelAsync(match.Team2ThreadId.Value) as DiscordThreadChannel;

                if (team1Thread is null || team2Thread is null)
                {
                    await DiscBotService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("Failed to retrieve Discord thread channels"),
                        "Failed to retrieve Discord thread channels",
                        nameof(HandleScrimmageGameCreatedAsync)
                    );
                    return;
                }

                // Validate that teams are loaded
                if (match.Team1 is null || match.Team2 is null)
                {
                    await DiscBotService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("Match teams not found"),
                        "Match teams not found",
                        nameof(HandleScrimmageGameCreatedAsync)
                    );
                    return;
                }

                // Render the game container
                var renderResult = await Renderers.GameRenderer.RenderGameContainerAsync(
                    game.Id,
                    game.GameNumber,
                    game.Map.Name,
                    match.Team1.Name,
                    match.Team2.Name,
                    match.Team1PlayerIds,
                    match.Team2PlayerIds,
                    team1Thread,
                    team2Thread
                );

                if (!renderResult.Success || renderResult.Data is null)
                {
                    await DiscBotService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException($"Failed to render game container: {renderResult.ErrorMessage}"),
                        "Failed to render game container",
                        nameof(HandleScrimmageGameCreatedAsync)
                    );
                    return;
                }

                // Store the message IDs in the game
                game.Team1GameContainerMsgId = renderResult.Data.Team1Message.Id;
                game.Team2GameContainerMsgId = renderResult.Data.Team2Message.Id;

                var updateResult = await CoreService.Games.UpdateAsync(game, DatabaseComponent.Repository);
                if (!updateResult.Success)
                {
                    await DiscBotService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException(
                            $"Failed to update game with message IDs: {updateResult.ErrorMessage}"
                        ),
                        "Failed to update game with message IDs",
                        nameof(HandleScrimmageGameCreatedAsync)
                    );
                }
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle ScrimmageGameCreated event: {ex.Message}",
                    nameof(HandleScrimmageGameCreatedAsync)
                );
            }
        }

        public static async Task HandlePlayerDeckSubmittedAsync(PlayerDeckSubmitted evt)
        {
            try
            {
                // Update the game embed immediately to show confirm/revise buttons
                await Renderers.GameRenderer.UpdateGameContainerReplayStatusAsync(evt.GameId, evt.PlayerId);
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle PlayerDeckSubmitted event: {ex.Message}",
                    nameof(HandlePlayerDeckSubmittedAsync)
                );
            }
        }

        public static async Task HandlePlayerDeckConfirmedAsync(PlayerDeckConfirmed evt)
        {
            try
            {
                // Update the game embed immediately to show replay button once all decks confirmed
                await Renderers.GameRenderer.UpdateGameContainerReplayStatusAsync(evt.GameId, evt.PlayerId);
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle PlayerDeckConfirmed event: {ex.Message}",
                    nameof(HandlePlayerDeckConfirmedAsync)
                );
            }
        }

        public static async Task HandlePlayerDeckRevisedAsync(PlayerDeckRevised evt)
        {
            try
            {
                // Update the game embed immediately to show submit deck button again
                await Renderers.GameRenderer.UpdateGameContainerReplayStatusAsync(evt.GameId, evt.PlayerId);
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle PlayerDeckRevised event: {ex.Message}",
                    nameof(HandlePlayerDeckRevisedAsync)
                );
            }
        }

        public static async Task HandlePlayerReplaySubmittedAsync(PlayerReplaySubmitted evt)
        {
            try
            {
                // Queue the update for batch processing instead of updating immediately
                // This prevents Discord message flickering when multiple players submit at once
                _pendingGameUpdates.AddOrUpdate(
                    evt.GameId,
                    _ => new HashSet<Guid> { evt.PlayerId },
                    (_, existingPlayers) =>
                    {
                        existingPlayers.Add(evt.PlayerId);
                        return existingPlayers;
                    }
                );
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to queue PlayerReplaySubmitted event: {ex.Message}",
                    nameof(HandlePlayerReplaySubmittedAsync)
                );
            }
        }

        public static async Task HandleGameCompletedAsync(Common.Events.Core.GameCompleted evt)
        {
            try
            {
                // Update the game container to show the game results
                // Use a dummy player ID since all players will see the same result
                await Renderers.GameRenderer.UpdateGameContainerReplayStatusAsync(evt.GameId, Guid.Empty);
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle GameCompleted event: {ex.Message}",
                    nameof(HandleGameCompletedAsync)
                );
            }
        }
    }
}
