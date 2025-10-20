using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Events.Core;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.Core.Common.Models.Common
{
    #region MatchHandler
    public partial class MatchHandler
    {
        /// <summary>
        /// Handles game completion by checking match victory conditions and starting the next game if needed.
        /// </summary>
        public static async Task HandleGameCompletedAsync(GameCompleted evt)
        {
            try
            {
                // Load the match with all games
                var matchResult = await CoreService.WithDbContext(async db =>
                {
                    return await db
                        .Matches.Include(m => m.Games)
                        .ThenInclude(g => g.StateHistory)
                        .Include(m => m.Team1)
                        .Include(m => m.Team2)
                        .Where(m => m.Id == evt.MatchId)
                        .FirstOrDefaultAsync();
                });

                if (matchResult is null)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("Match not found"),
                        $"Failed to load match {evt.MatchId}",
                        nameof(HandleGameCompletedAsync)
                    );
                    return;
                }

                var match = matchResult;

                // Check match victory condition using Core logic
                var victoryResult = MatchCore.CheckMatchVictoryCondition(match);
                if (!victoryResult.Success)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("Failed to check match victory condition"),
                        $"Failed to check victory for match {evt.MatchId}: {victoryResult.ErrorMessage}",
                        nameof(HandleGameCompletedAsync)
                    );
                    return;
                }

                var winnerTeamId = victoryResult.Data;

                // If match is won, complete it
                if (winnerTeamId.HasValue)
                {
                    var completeResult = await MatchCore.CompleteMatchAsync(match.Id, winnerTeamId.Value);
                    if (!completeResult.Success)
                    {
                        await CoreService.ErrorHandler.CaptureAsync(
                            new InvalidOperationException("Failed to complete match"),
                            $"Failed to complete match {match.Id}: {completeResult.ErrorMessage}",
                            nameof(HandleGameCompletedAsync)
                        );
                    }

                    // Publish match completed event based on match type
                    if (match.ParentType == MatchParentType.Scrimmage && match.ParentId.HasValue)
                    {
                        var publishResult = await PublishScrimmageMatchCompletedAsync(match.Id, winnerTeamId.Value);
                        if (!publishResult.Success)
                        {
                            await CoreService.ErrorHandler.CaptureAsync(
                                new InvalidOperationException("Failed to publish ScrimmageMatchCompleted event"),
                                $"Failed to publish ScrimmageMatchCompleted event for match {match.Id}: {publishResult.ErrorMessage}",
                                nameof(HandleGameCompletedAsync)
                            );
                        }
                    }
                    // TODO: Add other match type handlers (Tournament, etc.)
                }
                else
                {
                    // Match is still ongoing - start the next game
                    var startGameResult = await MatchCore.StartNextGameAsync(match);
                    if (!startGameResult.Success || startGameResult.Data is null)
                    {
                        await CoreService.ErrorHandler.CaptureAsync(
                            new InvalidOperationException("Failed to start next game"),
                            $"Failed to start next game for match {match.Id}: {startGameResult.ErrorMessage}",
                            nameof(HandleGameCompletedAsync)
                        );
                        return;
                    }

                    // Publish ScrimmageGameCreated event to trigger Discord UI creation
                    if (match.ParentType == MatchParentType.Scrimmage && match.ParentId.HasValue)
                    {
                        var publishResult = await PublishScrimmageGameCreatedAsync(
                            match.ParentId.Value,
                            match.Id,
                            startGameResult.Data.Id
                        );
                        if (!publishResult.Success)
                        {
                            await CoreService.ErrorHandler.CaptureAsync(
                                new InvalidOperationException("Failed to publish game created event"),
                                $"Failed to publish event for match {match.Id}: {publishResult.ErrorMessage}",
                                nameof(HandleGameCompletedAsync)
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle GameCompleted event for match {evt.MatchId}",
                    nameof(HandleGameCompletedAsync)
                );
            }
        }

        /// <summary>
        /// Handles the event when map bans are confirmed for a scrimmage match.
        /// Processes the bans to create the final map pool and creates the first game entity.
        /// </summary>
        public static async Task HandleScrimmageMapBansConfirmedAsync(ScrimmageMapBansConfirmed evt)
        {
            try
            {
                // 1) Load the match
                var getMatch = await CoreService.Matches.GetByIdAsync(evt.MatchId, DatabaseComponent.Repository);
                if (!getMatch.Success || getMatch.Data is null)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("Match not found"),
                        $"Failed to load match {evt.MatchId}",
                        nameof(HandleScrimmageMapBansConfirmedAsync)
                    );
                    return;
                }

                var match = getMatch.Data;

                // 2) Get the configured map pool for the team size
                var mapsOptionsConfig = ConfigurationProvider.GetSection<MapsOptions>(MapsOptions.SectionName);
                var teamSizeString = match.TeamSize.ToSizeString();
                var availableMapNames = mapsOptionsConfig
                    .Maps.Where(m =>
                        m.IsInTournamentPool
                        && string.Equals(m.Size, teamSizeString, StringComparison.OrdinalIgnoreCase)
                    )
                    .Select(m => m.Name)
                    .ToList();

                if (availableMapNames.Count == 0)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("No maps available for team size"),
                        $"No maps configured for team size {teamSizeString}",
                        nameof(HandleScrimmageMapBansConfirmedAsync)
                    );
                    return;
                }

                // 3) Get the ban configuration
                var mapBansConfig = ConfigurationProvider.GetSection<MapBansOptions>(MapBansOptions.SectionName);
                var banConfig = mapBansConfig.GetBanConfig(match.BestOf, availableMapNames.Count);
                var guaranteedBans = banConfig.GuaranteedBans;
                var coinflipBans = banConfig.CoinflipBans;

                // 4) Get the current snapshot with submitted bans
                var currentSnapshot = MatchCore.Accessors.GetCurrentSnapshot(match);

                // 5) Separate guaranteed and coinflip bans for each team
                var team1GuaranteedBans = currentSnapshot.Team1MapBans.Take(guaranteedBans).ToList();
                var team1CoinflipBans = currentSnapshot.Team1MapBans.Skip(guaranteedBans).Take(coinflipBans).ToList();

                var team2GuaranteedBans = currentSnapshot.Team2MapBans.Take(guaranteedBans).ToList();
                var team2CoinflipBans = currentSnapshot.Team2MapBans.Skip(guaranteedBans).Take(coinflipBans).ToList();

                // 6) For each coinflip ban position, randomly pick one map from the pair (Team1[i] vs Team2[i])
                var random = new Random();
                var selectedCoinflipBans = new List<string>();
                var coinflipPairCount = Math.Min(team1CoinflipBans.Count, team2CoinflipBans.Count);

                for (int i = 0; i < coinflipPairCount; i++)
                {
                    // Randomly pick between team 1's coinflip ban and team 2's coinflip ban at this index
                    var selectedMap = random.Next(2) == 0 ? team1CoinflipBans[i] : team2CoinflipBans[i];
                    selectedCoinflipBans.Add(selectedMap);
                }

                // 7) Create FinalMapPool by removing all banned maps
                var allActiveBans = team1GuaranteedBans
                    .Concat(team2GuaranteedBans)
                    .Concat(selectedCoinflipBans)
                    .Distinct()
                    .ToList();

                var finalMapPool = availableMapNames
                    .Where(m => !allActiveBans.Contains(m, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (finalMapPool.Count == 0)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("All maps were banned"),
                        $"No maps remaining after bans for match {evt.MatchId}",
                        nameof(HandleScrimmageMapBansConfirmedAsync)
                    );
                    return;
                }

                // 8) Update the match state snapshot with the computed map pools
                var newSnapshot = MatchCore.Factory.CreateMatchStateSnapshotFromOther(currentSnapshot);
                newSnapshot.FinalMapPool = [.. finalMapPool];
                newSnapshot.AvailableMaps = [.. finalMapPool];
                match.StateHistory.Add(newSnapshot);

                // Persist the updated match
                var updateMatch = await CoreService.Matches.UpdateAsync(match, DatabaseComponent.Repository);
                if (!updateMatch.Success)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("Failed to update match"),
                        $"Failed to update match {evt.MatchId}: {updateMatch.ErrorMessage}",
                        nameof(HandleScrimmageMapBansConfirmedAsync)
                    );
                    return;
                }

                // 9) Select the first map from the final map pool
                var mapName = finalMapPool[0];

                // 10) Look up the Map entity by name
                var getMaps = await CoreService.Maps.GetAllAsync(DatabaseComponent.Repository);
                if (!getMaps.Success || getMaps.Data is null)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("Failed to load maps"),
                        "Failed to load maps from database",
                        nameof(HandleScrimmageMapBansConfirmedAsync)
                    );
                    return;
                }

                var map = getMaps.Data.FirstOrDefault(m =>
                    string.Equals(m.Name, mapName, StringComparison.OrdinalIgnoreCase)
                );

                if (map is null)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException($"Map not found: {mapName}"),
                        $"Map '{mapName}' not found in database",
                        nameof(HandleScrimmageMapBansConfirmedAsync)
                    );
                    return;
                }

                // 11) Create the first game for the match
                var createGameResult = await MatchCore.CreateScrimmageGameAsync(match, map.Id);
                if (!createGameResult.Success || createGameResult.Data is null)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("Failed to create game"),
                        $"Failed to create game for match {evt.MatchId}: {createGameResult.ErrorMessage}",
                        nameof(HandleScrimmageMapBansConfirmedAsync)
                    );
                    return;
                }

                var publishResult = await PublishScrimmageGameCreatedAsync(
                    evt.ScrimmageId,
                    evt.MatchId,
                    createGameResult.Data.Id
                );
                if (!publishResult.Success)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("Failed to publish game created"),
                        $"Failed to publish game created for match {evt.MatchId}: {publishResult.ErrorMessage}",
                        nameof(HandleScrimmageMapBansConfirmedAsync)
                    );
                    return;
                }
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to handle scrimmage map bans confirmed",
                    nameof(HandleScrimmageMapBansConfirmedAsync)
                );
            }
        }
    }
    #endregion

    #region GameHandler
    public partial class GameHandler
    {
        /// <summary>
        /// Handles player deck submission event by updating GameStateSnapshot with deck code.
        /// </summary>
        public static async Task HandlePlayerDeckSubmittedAsync(PlayerDeckSubmitted evt)
        {
            try
            {
                await CoreService.WithDbContext(async db =>
                {
                    // Fetch game with state history
                    var game = await db
                        .Games.Include(g => g.StateHistory)
                        .Where(g => g.Id == evt.GameId)
                        .FirstOrDefaultAsync();
                    if (game is null)
                    {
                        await CoreService.ErrorHandler.CaptureAsync(
                            new Exception($"Game {evt.GameId} not found"),
                            "Game not found for deck submission",
                            nameof(HandlePlayerDeckSubmittedAsync)
                        );
                        return;
                    }

                    // Get current snapshot and create new one
                    var currentSnapshot = MatchCore.Accessors.GetCurrentSnapshot(game);
                    var newSnapshot = MatchCore.Factory.CreateGameStateSnapshotFromOther(currentSnapshot);

                    // Update deck code for the player
                    newSnapshot.PlayerDeckCodes[evt.PlayerId] = evt.DeckCode;
                    newSnapshot.PlayerDeckSubmittedAt[evt.PlayerId] = DateTime.UtcNow;

                    // Remove from confirmed set if they're revising
                    newSnapshot.PlayerDeckConfirmed.Remove(evt.PlayerId);
                    newSnapshot.PlayerDeckConfirmedAt.Remove(evt.PlayerId);

                    // Add snapshot to history
                    game.StateHistory.Add(newSnapshot);

                    // Save changes
                    await db.SaveChangesAsync();
                });
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle PlayerDeckSubmitted event for game {evt.GameId}",
                    nameof(HandlePlayerDeckSubmittedAsync)
                );
            }
        }

        /// <summary>
        /// Handles player deck confirmation event by updating GameStateSnapshot.
        /// </summary>
        public static async Task HandlePlayerDeckConfirmedAsync(PlayerDeckConfirmed evt)
        {
            try
            {
                await CoreService.WithDbContext(async db =>
                {
                    // Fetch game with state history
                    var game = await db
                        .Games.Include(g => g.StateHistory)
                        .Where(g => g.Id == evt.GameId)
                        .FirstOrDefaultAsync();
                    if (game is null)
                    {
                        await CoreService.ErrorHandler.CaptureAsync(
                            new Exception($"Game {evt.GameId} not found"),
                            "Game not found for deck confirmation",
                            nameof(HandlePlayerDeckConfirmedAsync)
                        );
                        return;
                    }

                    // Get current snapshot and create new one
                    var currentSnapshot = MatchCore.Accessors.GetCurrentSnapshot(game);
                    var newSnapshot = MatchCore.Factory.CreateGameStateSnapshotFromOther(currentSnapshot);

                    // Mark deck as confirmed for the player
                    newSnapshot.PlayerDeckConfirmed.Add(evt.PlayerId);
                    newSnapshot.PlayerDeckConfirmedAt[evt.PlayerId] = DateTime.UtcNow;

                    // Add snapshot to history
                    game.StateHistory.Add(newSnapshot);

                    // Save changes
                    await db.SaveChangesAsync();
                });
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle PlayerDeckConfirmed event for game {evt.GameId}",
                    nameof(HandlePlayerDeckConfirmedAsync)
                );
            }
        }

        /// <summary>
        /// Handles player deck revision event by resetting deck confirmation state.
        /// </summary>
        public static async Task HandlePlayerDeckRevisedAsync(PlayerDeckRevised evt)
        {
            try
            {
                await CoreService.WithDbContext(async db =>
                {
                    // Fetch game with state history
                    var game = await db
                        .Games.Include(g => g.StateHistory)
                        .Where(g => g.Id == evt.GameId)
                        .FirstOrDefaultAsync();
                    if (game is null)
                    {
                        await CoreService.ErrorHandler.CaptureAsync(
                            new Exception($"Game {evt.GameId} not found"),
                            "Game not found for deck revision",
                            nameof(HandlePlayerDeckRevisedAsync)
                        );
                        return;
                    }

                    // Get current snapshot and create new one
                    var currentSnapshot = MatchCore.Accessors.GetCurrentSnapshot(game);
                    var newSnapshot = MatchCore.Factory.CreateGameStateSnapshotFromOther(currentSnapshot);

                    // Remove deck code and confirmation for the player
                    newSnapshot.PlayerDeckCodes.Remove(evt.PlayerId);
                    newSnapshot.PlayerDeckSubmittedAt.Remove(evt.PlayerId);
                    newSnapshot.PlayerDeckConfirmed.Remove(evt.PlayerId);
                    newSnapshot.PlayerDeckConfirmedAt.Remove(evt.PlayerId);

                    // Add snapshot to history
                    game.StateHistory.Add(newSnapshot);

                    // Save changes
                    await db.SaveChangesAsync();
                });
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle PlayerDeckRevised event for game {evt.GameId}",
                    nameof(HandlePlayerDeckRevisedAsync)
                );
            }
        }

        /// <summary>
        /// Handles player replay submission event.
        /// Checks if all players have submitted replays, and if so, triggers game finalization.
        /// </summary>
        public static async Task HandlePlayerReplaySubmittedAsync(PlayerReplaySubmitted evt)
        {
            try
            {
                // Load the game
                var gameResult = await CoreService.Games.GetByIdAsync(evt.GameId, DatabaseComponent.Repository);
                if (!gameResult.Success || gameResult.Data is null)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("Game not found"),
                        $"Failed to load game {evt.GameId}",
                        nameof(HandlePlayerReplaySubmittedAsync)
                    );
                    return;
                }

                var game = gameResult.Data;

                // Check if all players have submitted replays
                var allReplaysSubmitted = await MatchCore.Accessors.AreAllReplaysSubmittedAsync(game);

                if (allReplaysSubmitted)
                {
                    // Publish AllReplaysSubmitted event to trigger finalization
                    var publishResult = await PublishAllReplaysSubmittedAsync(evt.GameId, game.MatchId);
                    if (!publishResult.Success)
                    {
                        await CoreService.ErrorHandler.CaptureAsync(
                            new InvalidOperationException("Failed to publish AllReplaysSubmitted event"),
                            $"Failed to publish event for game {evt.GameId}: {publishResult.ErrorMessage}",
                            nameof(HandlePlayerReplaySubmittedAsync)
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle PlayerReplaySubmitted event for game {evt.GameId}",
                    nameof(HandlePlayerReplaySubmittedAsync)
                );
            }
        }

        /// <summary>
        /// Handles AllReplaysSubmitted event by determining the game winner and completing the game.
        /// </summary>
        public static async Task HandleAllReplaysSubmittedAsync(AllReplaysSubmitted evt)
        {
            try
            {
                // Load the game with all relationships
                var getGame = await CoreService.WithDbContext(async db =>
                {
                    return await db
                        .Games.Include(g => g.StateHistory)
                        .Include(g => g.Match)
                        .ThenInclude(m => m!.Team1)
                        .Include(g => g.Match)
                        .ThenInclude(m => m!.Team2)
                        .Where(g => g.Id == evt.GameId)
                        .FirstOrDefaultAsync();
                });

                if (getGame is null)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("Game not found"),
                        $"Failed to load game {evt.GameId}",
                        nameof(HandleAllReplaysSubmittedAsync)
                    );
                    return;
                }

                var game = getGame;

                // Determine winner from replays using Core logic
                var winnerResult = await MatchCore.DetermineWinnerFromReplaysAsync(game);
                if (!winnerResult.Success)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("Failed to determine game winner"),
                        $"Failed to determine winner for game {evt.GameId}: {winnerResult.ErrorMessage}",
                        nameof(HandleAllReplaysSubmittedAsync)
                    );
                    return;
                }

                var winnerTeamId = winnerResult.Data;

                // Complete the game using Core logic
                var completeResult = await MatchCore.CompleteGameAsync(evt.GameId, winnerTeamId);
                if (!completeResult.Success)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("Failed to complete game"),
                        $"Failed to complete game {evt.GameId}: {completeResult.ErrorMessage}",
                        nameof(HandleAllReplaysSubmittedAsync)
                    );
                    return;
                }

                // Publish GameCompleted event
                var publishResult = await PublishGameCompletedAsync(evt.GameId, evt.MatchId, winnerTeamId);
                if (!publishResult.Success)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("Failed to publish GameCompleted event"),
                        $"Failed to publish event for game {evt.GameId}: {publishResult.ErrorMessage}",
                        nameof(HandleAllReplaysSubmittedAsync)
                    );
                }
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle AllReplaysSubmitted event for game {evt.GameId}",
                    nameof(HandleAllReplaysSubmittedAsync)
                );
            }
        }
    }
    #endregion
}
