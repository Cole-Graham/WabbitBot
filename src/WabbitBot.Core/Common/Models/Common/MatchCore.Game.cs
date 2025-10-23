using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.Core.Common.Models.Common
{
    public partial class MatchCore
    {
        #region Factory
        public static partial class Factory
        {
            // ------------------------ Factory & Initialization ---------------------------
            public static Game CreateGame(Guid matchId, Match Match, Guid mapId, TeamSize teamSize, int gameNumber)
            {
                var game = new Game
                {
                    MatchId = matchId,
                    MapId = mapId,
                    TeamSize = teamSize,
                    GameNumber = gameNumber,
                };
                InitializeGameState(game);
                return game;
            }

            private static void InitializeGameState(Game game)
            {
                var initialState = new GameStateSnapshot
                {
                    GameId = game.Id,
                    MatchId = game.MatchId,
                    MapId = game.MapId,
                    TeamSize = game.TeamSize,
                    Team1PlayerIds = game.Team1PlayerIds,
                    Team2PlayerIds = game.Team2PlayerIds,
                    GameNumber = game.GameNumber,
                    Timestamp = DateTime.UtcNow,
                };
                game.StateHistory.Add(initialState);
            }

            public static GameStateSnapshot CreateGameStateSnapshotFromOther(GameStateSnapshot other)
            {
                // Manual copy to avoid reference issues
                return new GameStateSnapshot
                {
                    GameId = other.GameId,
                    MatchId = other.MatchId,
                    Timestamp = DateTime.UtcNow,
                    TriggeredByUserId = other.TriggeredByUserId,
                    TriggeredByUserName = other.TriggeredByUserName,
                    AdditionalData = new Dictionary<string, object>(other.AdditionalData),
                    StartedAt = other.StartedAt,
                    CompletedAt = other.CompletedAt,
                    CancelledAt = other.CancelledAt,
                    ForfeitedAt = other.ForfeitedAt,
                    WinnerId = other.WinnerId,
                    CancelledByUserId = other.CancelledByUserId,
                    ForfeitedByUserId = other.ForfeitedByUserId,
                    ForfeitedTeamId = other.ForfeitedTeamId,
                    CancellationReason = other.CancellationReason,
                    ForfeitReason = other.ForfeitReason,
                    PlayerDeckCodes = new Dictionary<Guid, string>(other.PlayerDeckCodes),
                    PlayerDeckSubmittedAt = new Dictionary<Guid, DateTime>(other.PlayerDeckSubmittedAt),
                    PlayerDeckConfirmed = [.. other.PlayerDeckConfirmed],
                    PlayerDeckConfirmedAt = new Dictionary<Guid, DateTime>(other.PlayerDeckConfirmedAt),
                    MapId = other.MapId,
                    TeamSize = other.TeamSize,
                    Team1PlayerIds = [.. other.Team1PlayerIds],
                    Team2PlayerIds = [.. other.Team2PlayerIds],
                    GameNumber = other.GameNumber,
                };
            }
        }
        #endregion

        #region Accessors
        public partial class Accessors
        {
            // ------------------------ State & Logic Accessors ----------------------------
            public static GameStateSnapshot GetCurrentSnapshot(Game game)
            {
                return game.StateHistory.LastOrDefault() ?? new GameStateSnapshot();
            }

            public static GameStatus GetCurrentStatus(Game game)
            {
                var snapshot = GetCurrentSnapshot(game);
                if (snapshot.CompletedAt.HasValue)
                    return GameStatus.Completed;
                if (snapshot.CancelledAt.HasValue)
                    return GameStatus.Cancelled;
                if (snapshot.ForfeitedAt.HasValue)
                    return GameStatus.Forfeited;
                if (snapshot.StartedAt.HasValue)
                    return GameStatus.InProgress;
                return GameStatus.InProgress;
            }

            public static string GetRequiredAction(Game game)
            {
                return GetCurrentStatus(game) switch
                {
                    GameStatus.Created => "Waiting for deck submissions",
                    GameStatus.InProgress => "Game in progress",
                    GameStatus.Completed => "Game completed",
                    GameStatus.Cancelled => "Game cancelled",
                    GameStatus.Forfeited => "Game forfeited",
                    _ => "Unknown state",
                };
            }

            public static bool AreDeckCodesSubmitted(Game game)
            {
                var snapshot = GetCurrentSnapshot(game);
                var allPlayerIds = game.Team1PlayerIds.Concat(game.Team2PlayerIds);
                return allPlayerIds.All(playerId => snapshot.PlayerDeckCodes.ContainsKey(playerId));
            }

            public static bool AreDeckCodesConfirmed(Game game)
            {
                var snapshot = GetCurrentSnapshot(game);
                var allPlayerIds = game.Team1PlayerIds.Concat(game.Team2PlayerIds);
                return allPlayerIds.All(playerId => snapshot.PlayerDeckConfirmed.Contains(playerId));
            }

            public static bool IsReadyToStart(Game game)
            {
                return AreDeckCodesSubmitted(game) && AreDeckCodesConfirmed(game);
            }

            /// <summary>
            /// Checks if all players on both teams have submitted replays for this game.
            /// Requires a database context to query replay data.
            /// </summary>
            public static async Task<bool> AreAllReplaysSubmittedAsync(Game game)
            {
                return await CoreService.WithDbContext(async db =>
                {
                    var allPlayerIds = game.Team1PlayerIds.Concat(game.Team2PlayerIds).ToList();
                    var players = await db.Players.Where(p => allPlayerIds.Contains(p.Id)).ToListAsync();

                    var replays = await db
                        .Replays.Where(r => r.GameId == game.Id)
                        .Include(r => r.Players)
                        .ToListAsync();

                    // Check if each player has submitted a replay
                    foreach (var player in players)
                    {
                        var hasReplay = false;
                        foreach (var replay in replays)
                        {
                            foreach (var rp in replay.Players)
                            {
                                if (
                                    (
                                        !string.IsNullOrEmpty(rp.PlayerUserId)
                                        && (
                                            player.CurrentPlatformIds.GetValueOrDefault("EugenSystems")
                                                == rp.PlayerUserId
                                            || player
                                                .PreviousPlatformIds.GetValueOrDefault("EugenSystems")
                                                ?.Contains(rp.PlayerUserId) == true
                                        )
                                    )
                                    || (
                                        ExtractSteamId(rp.PlayerAvatar) is string steamId
                                        && (
                                            player.CurrentPlatformIds.GetValueOrDefault("Steam") == steamId
                                            || player.PreviousPlatformIds.GetValueOrDefault("Steam")?.Contains(steamId)
                                                == true
                                        )
                                    )
                                    || (
                                        !string.IsNullOrEmpty(rp.PlayerName)
                                        && (
                                            player.CurrentSteamUsername == rp.PlayerName
                                            || player.PreviousSteamUsernames.Contains(rp.PlayerName)
                                        )
                                    )
                                )
                                {
                                    hasReplay = true;
                                    break;
                                }
                            }
                            if (hasReplay)
                            {
                                break;
                            }
                        }

                        if (!hasReplay)
                        {
                            return false;
                        }
                    }

                    return true;
                });
            }

            private static string? ExtractSteamId(string? avatarUrl)
            {
                if (string.IsNullOrEmpty(avatarUrl))
                {
                    return null;
                }
                var segments = avatarUrl.Split('/');
                return
                    segments.Length >= 3
                    && segments[^2].Equals("SteamGamerPicture", StringComparison.OrdinalIgnoreCase)
                    && long.TryParse(segments[^1], out _)
                    ? segments[^1]
                    : null;
            }
        }
        #endregion

        #region State
        public partial class State
        {
            public class GameState
            {
                // ------------------------ State Machine Definition ---------------------------
                private static readonly Dictionary<GameStatus, List<GameStatus>> _validTransitions = new()
                {
                    [GameStatus.Created] = [GameStatus.InProgress, GameStatus.Cancelled],
                    [GameStatus.InProgress] = [GameStatus.Completed, GameStatus.Cancelled, GameStatus.Forfeited],
                    [GameStatus.Completed] = [], // Terminal state
                    [GameStatus.Cancelled] = [], // Terminal state
                    [GameStatus.Forfeited] = [], // Terminal state
                };

                // ------------------------ State Machine Logic --------------------------------
                public static bool CanTransition(Game game, GameStatus to)
                {
                    var from = Accessors.GetCurrentStatus(game);
                    return _validTransitions.ContainsKey(from) && _validTransitions[from].Contains(to);
                }

                public static List<GameStatus> GetValidTransitions(Game game)
                {
                    var from = Accessors.GetCurrentStatus(game);
                    return _validTransitions.ContainsKey(from) ? [.. _validTransitions[from]] : [];
                }

                public static bool TryTransition(
                    Game game,
                    GameStatus toState,
                    Guid userId,
                    string userName,
                    string? reason = null
                )
                {
                    if (!CanTransition(game, toState))
                        return false;

                    var currentSnapshot = Accessors.GetCurrentSnapshot(game);
                    var newSnapshot = Factory.CreateGameStateSnapshotFromOther(currentSnapshot);

                    switch (toState)
                    {
                        case GameStatus.InProgress:
                            newSnapshot.StartedAt = DateTime.UtcNow;
                            break;
                        case GameStatus.Completed:
                            newSnapshot.CompletedAt = DateTime.UtcNow;
                            break;
                        case GameStatus.Cancelled:
                            newSnapshot.CancelledAt = DateTime.UtcNow;
                            newSnapshot.CancelledByUserId = userId;
                            newSnapshot.CancellationReason = reason;
                            break;
                        case GameStatus.Forfeited:
                            newSnapshot.ForfeitedAt = DateTime.UtcNow;
                            newSnapshot.ForfeitedByUserId = userId;
                            newSnapshot.ForfeitReason = reason;
                            break;
                    }

                    newSnapshot.TriggeredByUserId = userId;
                    newSnapshot.TriggeredByUserName = userName;
                    game.StateHistory.Add(newSnapshot);

                    return true;
                }
            }
        }
        #endregion

        #region Validation
        public partial class Validation
        {

            // ------------------------ Validation Logic -----------------------------------
            // No validation needed for TeamSize as it's a simple enum
            // Future: Add business rule validations here
        }
        #endregion

        #region CoreLogic
        /// <summary>
        /// Creates a game from a match
        /// </summary>
        /// <param name="matchId">The match ID</param>
        /// <param name="match">The match</param>
        /// <param name="mapId">The map ID for this game</param>
        /// <returns>Result containing the created game or error</returns>
        public static async Task<Result<Game>> CreateScrimmageGameAsync(Match Match, Guid mapId)
        {
            try
            {
                // 1) Determine game number (next game in sequence)
                var gameNumber = Match.Games.Count + 1;

                // 2) Build game with initial state
                var NewGame = Factory.CreateGame(Match.Id, Match, mapId, Match.TeamSize, gameNumber);

                // 3) Set teams/players from the match
                if (Match.Team1Players is not null)
                {
                    NewGame.Team1PlayerIds = [.. Match.Team1Players.Select(p => p.Id)];
                }
                else
                {
                    return Result<Game>.Failure("Team 1 players not found");
                }
                if (Match.Team2Players is not null)
                {
                    NewGame.Team2PlayerIds = [.. Match.Team2Players.Select(p => p.Id)];
                }
                else
                {
                    return Result<Game>.Failure("Team 2 players not found");
                }

                // 4) Persist
                var createResult = await CoreService.Games.CreateAsync(NewGame, DatabaseComponent.Repository);
                if (!createResult.Success || createResult.Data is null)
                    return Result<Game>.Failure("Failed to create game");

                NewGame = createResult.Data;

                // 5) Link game to match and update match
                Match.Games.Add(NewGame);
                await CoreService.Matches.UpdateAsync(Match, DatabaseComponent.Repository);

                return Result<Game>.CreateSuccess(NewGame);
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to create game",
                    nameof(CreateScrimmageGameAsync)
                );
                return Result<Game>.Failure($"An unexpected error occurred while creating the game: {ex.Message}");
            }
        }

        /// <summary>
        /// Determines the winner of a game from submitted replays.
        /// Uses victory codes from replay files to determine which team won.
        /// </summary>
        /// <param name="game">The game to analyze</param>
        /// <returns>Result containing the winner team ID</returns>
        public static async Task<Result<Guid>> DetermineWinnerFromReplaysAsync(Game game)
        {
            try
            {
                // Load all replays for this game with their players
                var replays = await CoreService.WithDbContext(async db =>
                {
                    return await db.Replays.Where(r => r.GameId == game.Id).Include(r => r.Players).ToListAsync();
                });

                if (replays.Count == 0)
                {
                    return Result<Guid>.Failure("No replays found for this game");
                }

                // Load all players for this game
                var allPlayerIds = game.Team1PlayerIds.Concat(game.Team2PlayerIds).ToList();
                var players = await CoreService.WithDbContext(async db =>
                {
                    return await db.Players.Where(p => allPlayerIds.Contains(p.Id)).ToListAsync();
                });

                // Count victories for each team
                var team1Victories = 0;
                var team2Victories = 0;

                // For each replay, determine which player it belongs to and their result
                foreach (var replay in replays)
                {
                    foreach (var player in players)
                    {
                        foreach (var rp in replay.Players)
                        {
                            // Try to match replay player to our player
                            var isMatch =
                                (
                                    !string.IsNullOrEmpty(rp.PlayerUserId)
                                    && (
                                        player.CurrentPlatformIds.GetValueOrDefault("EugenSystems") == rp.PlayerUserId
                                        || player
                                            .PreviousPlatformIds.GetValueOrDefault("EugenSystems")
                                            ?.Contains(rp.PlayerUserId) == true
                                    )
                                )
                                || (
                                    ExtractSteamId(rp.PlayerAvatar) is string steamId
                                    && (
                                        player.CurrentPlatformIds.GetValueOrDefault("Steam") == steamId
                                        || player.PreviousPlatformIds.GetValueOrDefault("Steam")?.Contains(steamId)
                                            == true
                                    )
                                )
                                || (
                                    !string.IsNullOrEmpty(rp.PlayerName)
                                    && (
                                        player.CurrentSteamUsername == rp.PlayerName
                                        || player.PreviousSteamUsernames.Contains(rp.PlayerName)
                                    )
                                );

                            if (isMatch)
                            {
                                // Determine if this player won
                                var result = ReplayCore.InterpretVictoryCode(replay.VictoryCode, rp.PlayerAlliance);

                                if (string.Equals(result, "Victory", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Check which team this player is on
                                    if (game.Team1PlayerIds.Contains(player.Id))
                                    {
                                        team1Victories++;
                                    }
                                    else if (game.Team2PlayerIds.Contains(player.Id))
                                    {
                                        team2Victories++;
                                    }
                                }

                                break;
                            }
                        }
                    }
                }

                // Determine winner based on majority
                if (team1Victories > team2Victories)
                {
                    return Result<Guid>.CreateSuccess(game.Match.Team1Id, "Team 1 won");
                }
                else if (team2Victories > team1Victories)
                {
                    return Result<Guid>.CreateSuccess(game.Match.Team2Id, "Team 2 won");
                }
                else
                {
                    // In case of a tie, we need a tiebreaker - for now, report it as an error
                    return Result<Guid>.Failure(
                        $"Tie detected: Team 1 has {team1Victories} victories, Team 2 has {team2Victories} victories"
                    );
                }
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to determine game winner for game {game.Id}",
                    nameof(DetermineWinnerFromReplaysAsync)
                );
                return Result<Guid>.Failure($"Failed to determine game winner: {ex.Message}");
            }
        }

        /// <summary>
        /// Completes a game by updating its state with the winner.
        /// </summary>
        /// <param name="gameId">The game ID to complete</param>
        /// <param name="winnerTeamId">The ID of the winning team</param>
        /// <returns>Result indicating success or failure</returns>
        public static async Task<Result> CompleteGameAsync(Guid gameId, Guid winnerTeamId)
        {
            try
            {
                await CoreService.WithDbContext(async db =>
                {
                    var game = await db.Games.Include(g => g.StateHistory).FirstOrDefaultAsync(g => g.Id == gameId);
                    if (game is not null)
                    {
                        var currentSnapshot = Accessors.GetCurrentSnapshot(game);
                        var newSnapshot = Factory.CreateGameStateSnapshotFromOther(currentSnapshot);
                        newSnapshot.CompletedAt = DateTime.UtcNow;
                        newSnapshot.WinnerId = winnerTeamId;

                        game.StateHistory.Add(newSnapshot);
                        await db.SaveChangesAsync();
                    }
                });

                return Result.CreateSuccess("Game completed successfully");
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to complete game {gameId}",
                    nameof(CompleteGameAsync)
                );
                return Result.Failure($"Failed to complete game: {ex.Message}");
            }
        }

        private static string? ExtractSteamId(string? avatarUrl)
        {
            if (string.IsNullOrEmpty(avatarUrl))
            {
                return null;
            }
            var segments = avatarUrl.Split('/');
            return
                segments.Length >= 3
                && segments[^2].Equals("SteamGamerPicture", StringComparison.OrdinalIgnoreCase)
                && long.TryParse(segments[^1], out _)
                ? segments[^1]
                : null;
        }
        #endregion
    }
}
