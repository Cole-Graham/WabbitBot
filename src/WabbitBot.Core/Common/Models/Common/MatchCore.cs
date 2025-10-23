using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Events.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Events;
using WabbitBot.Core.Common.Interfaces;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.Core.Common.Models.Common
{
    public partial class MatchCore : IMatchCore
    {
        // No constructor by design - Entities accessed via CoreService static properties

        /// <inheritdoc />
        public Task InitializeAsync() => Task.CompletedTask;

        /// <inheritdoc />
        public Task ValidateAsync() => Task.CompletedTask;

        #region Factory
        public static partial class Factory
        {
            // ------------------------ Factory & Initialization ---------------------------
            public static Match CreateMatch(
                TeamSize teamSize,
                Scrimmage.Scrimmage scrimmage,
                MatchParentType parentType,
                int bestOf = 1,
                bool playToCompletion = false
            )
            {
                var match = new Match
                {
                    Team1Id = scrimmage.ChallengerTeamId,
                    Team2Id = scrimmage.OpponentTeamId,
                    TeamSize = teamSize,
                    ParentId = scrimmage.Id,
                    ParentType = parentType,
                    BestOf = bestOf,
                    PlayToCompletion = playToCompletion,
                };
                // Add initial state to match
                var initialState = CreateMatchStateSnapshot(match.Id);
                match.StateHistory.Add(initialState);
                return match;
            }

            public static MatchStateSnapshot CreateMatchStateSnapshot(Guid matchId)
            {
                return new MatchStateSnapshot { MatchId = matchId, StartedAt = DateTime.UtcNow };
            }

            public static MatchStateSnapshot CreateMatchStateSnapshotFromOther(MatchStateSnapshot other)
            {
                // Manual copy to avoid reference issues
                return new MatchStateSnapshot
                {
                    MatchId = other.MatchId,
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
                    CurrentGameNumber = other.CurrentGameNumber,
                    CurrentMapId = other.CurrentMapId,
                    FinalScore = other.FinalScore,
                    AvailableMaps = [.. other.AvailableMaps],
                    Team1MapBans = [.. other.Team1MapBans],
                    Team2MapBans = [.. other.Team2MapBans],
                    Team1BansSubmitted = other.Team1BansSubmitted,
                    Team2BansSubmitted = other.Team2BansSubmitted,
                    Team1BansConfirmed = other.Team1BansConfirmed,
                    Team2BansConfirmed = other.Team2BansConfirmed,
                    FinalMapPool = [.. other.FinalMapPool],
                };
            }
        }
        #endregion

        #region Accessors
        public static partial class Accessors
        {
            // ------------------------ State & Logic Accessors ----------------------------
            public static MatchStateSnapshot GetCurrentSnapshot(Match match)
            {
                return match.StateHistory.LastOrDefault() ?? Factory.CreateMatchStateSnapshot(match.Id);
            }

            public static MatchStatus GetCurrentStatus(Match match)
            {
                var snapshot = GetCurrentSnapshot(match);

                if (snapshot.CompletedAt.HasValue)
                    return MatchStatus.Completed;

                if (snapshot.CancelledAt.HasValue)
                    return MatchStatus.Cancelled;

                if (snapshot.ForfeitedAt.HasValue)
                    return MatchStatus.Forfeited;

                if (snapshot.StartedAt.HasValue)
                    return MatchStatus.InProgress;

                return MatchStatus.Created;
            }

            public static bool IsTeamMatch(Match match)
            {
                return match.TeamSize != TeamSize.OneVOne;
            }

            public static string GetRequiredAction(Match match)
            {
                return GetCurrentStatus(match) switch
                {
                    MatchStatus.Created => "Waiting for match to start",
                    MatchStatus.InProgress => "Match in progress",
                    MatchStatus.Completed => "Match completed",
                    MatchStatus.Cancelled => "Match cancelled",
                    MatchStatus.Forfeited => "Match forfeited",
                    _ => "Unknown state",
                };
            }

            public static bool AreMapBansSubmitted(Match match)
            {
                var snapshot = GetCurrentSnapshot(match);
                return snapshot.Team1BansSubmitted && snapshot.Team2BansSubmitted;
            }

            public static bool AreMapBansConfirmed(Match match)
            {
                var snapshot = GetCurrentSnapshot(match);
                return snapshot.Team1BansConfirmed && snapshot.Team2BansConfirmed;
            }

            public static bool IsReadyToStart(Match match)
            {
                return AreMapBansSubmitted(match) && AreMapBansConfirmed(match);
            }
        }
        #endregion

        #region Validation
        public static partial class Validation { }
        #endregion

        #region CoreLogic
        /// <summary>
        /// Creates a match from a scrimmage
        /// </summary>
        /// <param name="scrimmageId"></param>
        /// <returns></returns>
        public static async Task<Result<Match>> CreateScrimmageMatchAsync(Guid scrimmageId)
        {
            try
            {
                // 1) Load scrimmage
                var getScrimmage = await CoreService.Scrimmages.GetByIdAsync(scrimmageId, DatabaseComponent.Repository);
                if (!getScrimmage.Success || getScrimmage.Data is null)
                    return Result<Match>.Failure("Scrimmage not found");

                var Scrimmage = getScrimmage.Data;

                // 2) Build match (parent linkage + initial state),
                //    then set teams/players from the scrimmage
                var NewMatch = Factory.CreateMatch(
                    Scrimmage.TeamSize,
                    Scrimmage,
                    MatchParentType.Scrimmage,
                    Scrimmage.BestOf
                );

                NewMatch.Team1Id = Scrimmage.ChallengerTeamId;
                NewMatch.Team2Id = Scrimmage.OpponentTeamId;
                NewMatch.Team1Players = [.. Scrimmage.ChallengerTeamPlayers];
                NewMatch.Team2Players = [.. Scrimmage.OpponentTeamPlayers];

                // 3) Persist
                var createResult = await CoreService.Matches.CreateAsync(NewMatch, DatabaseComponent.Repository);
                if (!createResult.Success || createResult.Data is null)
                    return Result<Match>.Failure("Failed to create match");

                NewMatch = createResult.Data;

                // Optional: link back to scrimmage entity and persist if needed
                Scrimmage.MatchId = NewMatch.Id;
                await CoreService.Scrimmages.UpdateAsync(Scrimmage, DatabaseComponent.Repository);

                return Result<Match>.CreateSuccess(NewMatch);
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to create match",
                    nameof(CreateScrimmageMatchAsync)
                );
                return Result<Match>.Failure($"An unexpected error occurred while creating the match: {ex.Message}");
            }
        }

        /// <summary>
        /// Completes a match by updating its state with the winner.
        /// </summary>
        /// <param name="matchId">The match ID to complete</param>
        /// <param name="winnerTeamId">The ID of the winning team</param>
        /// <returns>Result indicating success or failure</returns>
        public static async Task<Result> CompleteMatchAsync(Guid matchId, Guid winnerTeamId)
        {
            try
            {
                await CoreService.WithDbContext(async db =>
                {
                    var match = await db.Matches.Include(m => m.StateHistory).FirstOrDefaultAsync(m => m.Id == matchId);
                    if (match is not null)
                    {
                        var currentSnapshot = Accessors.GetCurrentSnapshot(match);
                        var newSnapshot = Factory.CreateMatchStateSnapshotFromOther(currentSnapshot);
                        newSnapshot.CompletedAt = DateTime.UtcNow;
                        newSnapshot.WinnerId = winnerTeamId;

                        match.StateHistory.Add(newSnapshot);
                        await db.SaveChangesAsync();
                    }
                });

                return Result.CreateSuccess("Match completed successfully");
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to complete match {matchId}",
                    nameof(CompleteMatchAsync)
                );
                return Result.Failure($"Failed to complete match: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts the next game in a match by selecting a map and creating the game entity.
        /// </summary>
        /// <param name="match">The match to start the next game for</param>
        /// <returns>Result containing the created game or error</returns>
        public static async Task<Result<Game>> StartNextGameAsync(Match match)
        {
            try
            {
                // Get current snapshot to access final map pool
                var matchSnapshot = Accessors.GetCurrentSnapshot(match);

                if (matchSnapshot.FinalMapPool.Count == 0)
                {
                    return Result<Game>.Failure("No maps available in final map pool");
                }

                // Determine next game number
                var nextGameNumber = match.Games.Count + 1;

                // Select next map from the pool (cycling through maps)
                var mapIndex = (nextGameNumber - 1) % matchSnapshot.FinalMapPool.Count;
                var selectedMapName = matchSnapshot.FinalMapPool[mapIndex];

                // Get map ID from database
                var mapResult = await CoreService.WithDbContext(async db =>
                {
                    return await db.Maps.Where(m => m.Name == selectedMapName).FirstOrDefaultAsync();
                });

                if (mapResult is null)
                {
                    return Result<Game>.Failure($"Map '{selectedMapName}' not found in database");
                }

                var mapId = mapResult.Id;

                // Create the next game
                var createGameResult = await CreateScrimmageGameAsync(match, mapId);
                if (!createGameResult.Success || createGameResult.Data is null)
                {
                    return Result<Game>.Failure($"Failed to create game: {createGameResult.ErrorMessage}");
                }

                return Result<Game>.CreateSuccess(createGameResult.Data, "Next game started successfully");
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to start next game for match {match.Id}",
                    nameof(StartNextGameAsync)
                );
                return Result<Game>.Failure($"Failed to start next game: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a match has reached its victory condition.
        /// </summary>
        /// <param name="match">The match to check</param>
        /// <returns>Result containing the winner team ID if match is won, or null if ongoing</returns>
        public static Result<Guid?> CheckMatchVictoryCondition(Match match)
        {
            try
            {
                // Count wins for each team
                var team1Wins = 0;
                var team2Wins = 0;

                foreach (var game in match.Games)
                {
                    var gameSnapshot = Accessors.GetCurrentSnapshot(game);
                    if (gameSnapshot.CompletedAt.HasValue && gameSnapshot.WinnerId.HasValue)
                    {
                        if (gameSnapshot.WinnerId.Value == match.Team1Id)
                        {
                            team1Wins++;
                        }
                        else if (gameSnapshot.WinnerId.Value == match.Team2Id)
                        {
                            team2Wins++;
                        }
                    }
                }

                // Calculate games needed to win (best of N)
                var gamesToWin = (match.BestOf + 1) / 2;

                // Check if either team has won the match
                if (team1Wins >= gamesToWin)
                {
                    return Result<Guid?>.CreateSuccess(match.Team1Id, "Team 1 won the match");
                }
                else if (team2Wins >= gamesToWin)
                {
                    return Result<Guid?>.CreateSuccess(match.Team2Id, "Team 2 won the match");
                }

                // Check if all games have been played with PlayToCompletion
                if (match.PlayToCompletion && (team1Wins + team2Wins) >= match.BestOf)
                {
                    // All games played - determine winner by total wins
                    var winnerId = team1Wins > team2Wins ? match.Team1Id : match.Team2Id;
                    return Result<Guid?>.CreateSuccess(winnerId, "Match completed - all games played");
                }

                // Match is still ongoing
                return Result<Guid?>.CreateSuccess(null, "Match is still ongoing");
            }
            catch (Exception ex)
            {
                return Result<Guid?>.Failure($"Failed to check match victory condition: {ex.Message}");
            }
        }
        #endregion
    }
}
