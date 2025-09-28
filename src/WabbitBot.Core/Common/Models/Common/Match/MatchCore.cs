using WabbitBot.Core.Common.Services;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Events;
using WabbitBot.Core.Common.Interfaces;

namespace WabbitBot.Core.Common.Models
{
    public partial class MatchCore : IMatchCore
    {

        // No constructor by design - Entities accessed via CoreService static properties

        /// <inheritdoc />
        public Task InitializeAsync() => Task.CompletedTask;

        /// <inheritdoc />
        public Task ValidateAsync() => Task.CompletedTask;

        public static partial class Factory
        {
            // ------------------------ Factory & Initialization ---------------------------
            public static Match CreateMatch(
                TeamSize teamSize,
                int bestOf = 1,
                bool playToCompletion = false)
            {
                var match = new Match
                {
                    TeamSize = teamSize,
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
                return new MatchStateSnapshot
                {
                    MatchId = matchId,
                    Timestamp = DateTime.UtcNow,
                };
            }

            public static MatchStateSnapshot CreateMatchStateSnapshotFromOther(MatchStateSnapshot other)
            {
                // Manual copy to avoid reference issues
                return new MatchStateSnapshot
                {
                    MatchId = other.MatchId,
                    Timestamp = DateTime.UtcNow,
                    UserId = other.UserId,
                    PlayerName = other.PlayerName,
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
                    Games = new List<Game>(other.Games),
                    CurrentMapId = other.CurrentMapId,
                    FinalScore = other.FinalScore,
                    FinalGames = new List<Game>(other.FinalGames),
                    AvailableMaps = new List<string>(other.AvailableMaps),
                    Team1MapBans = new List<string>(other.Team1MapBans),
                    Team2MapBans = new List<string>(other.Team2MapBans),
                    Team1BansSubmitted = other.Team1BansSubmitted,
                    Team2BansSubmitted = other.Team2BansSubmitted,
                    Team1BansConfirmed = other.Team1BansConfirmed,
                    Team2BansConfirmed = other.Team2BansConfirmed,
                    FinalMapPool = new List<string>(other.FinalMapPool),
                };
            }


        }

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


        public static partial class Validation
        {
            // ------------------------ Validation Logic -----------------------------------
            // No validation needed for TeamSize as it's a simple enum
            // Future: Add business rule validations here
        }

        /// <summary>
        /// Starts a match with full business logic orchestration
        /// </summary>
        public async Task<Result> StartMatchAsync(Guid matchId, Guid team1Id, Guid team2Id, List<Guid> team1PlayerIds, List<Guid> team2PlayerIds)
        {
            try
            {
                var matchResult = await CoreService.Matches.GetByIdAsync(matchId,
                    DatabaseComponent.Repository);
                if (!matchResult.Success)
                {
                    return Result.Failure(
                        $"Failed to retrieve match: {matchResult.ErrorMessage}"
                    );
                }
                var match = matchResult.Data;

                if (match == null)
                    return Result.Failure("Match not found.");

                // Validate match can be started
                if (Accessors.GetCurrentStatus(match) != MatchStatus.Created)
                    return Result.Failure("Match can only be started when in Created state.");

                // Set start time
                match.StartedAt = DateTime.UtcNow;

                // Create match participants
                var team1Participant = new MatchParticipant
                {
                    MatchId = matchId,
                    TeamId = team1Id,
                    TeamNumber = 1,
                    PlayerIds = team1PlayerIds,
                    JoinedAt = DateTime.UtcNow,
                };

                var team2Participant = new MatchParticipant
                {
                    MatchId = matchId,
                    TeamId = team2Id,
                    TeamNumber = 2,
                    PlayerIds = team2PlayerIds,
                    JoinedAt = DateTime.UtcNow,
                };

                var participant1Result = await CoreService.MatchParticipants.CreateAsync(team1Participant,
                    DatabaseComponent.Repository);
                var participant2Result = await CoreService.MatchParticipants.CreateAsync(team2Participant,
                    DatabaseComponent.Repository);

                if (!participant1Result.Success || !participant2Result.Success)
                    return Result.Failure("Failed to create match participants.");

                // Create opponent encounters
                var encounter1 = new TeamOpponentEncounter
                {
                    TeamId = team1Id,
                    OpponentId = team2Id,
                    MatchId = matchId,
                    TeamSize = (int)match.TeamSize,
                    EncounteredAt = DateTime.UtcNow,
                    Won = false, // Will be updated when match completes
                };

                var encounter2 = new TeamOpponentEncounter
                {
                    TeamId = team2Id,
                    OpponentId = team1Id,
                    MatchId = matchId,
                    TeamSize = (int)match.TeamSize,
                    EncounteredAt = DateTime.UtcNow,
                    Won = false, // Will be updated when match completes
                };

                var encounter1Result = await CoreService.TeamOpponentEncounters.CreateAsync(encounter1,
                    DatabaseComponent.Repository);
                var encounter2Result = await CoreService.TeamOpponentEncounters.CreateAsync(encounter2,
                    DatabaseComponent.Repository);

                if (!encounter1Result.Success || !encounter2Result.Success)
                    return Result.Failure("Failed to create opponent encounters.");

                // TODO: Re-implement map pool logic without MapService dependency
                // match.AvailableMaps = GetDefaultMapPool();

                // Create the first game
                var game = new Game
                {
                    MatchId = match.Id,
                    TeamSize = match.TeamSize,
                    Team1PlayerIds = team1PlayerIds,
                    Team2PlayerIds = team2PlayerIds,
                    GameNumber = 1,
                };

                var createGameResult = await CoreService.Games.CreateAsync(game,
                    DatabaseComponent.Repository);
                if (!createGameResult.Success)
                    return Result.Failure("Failed to create the first game for the match.");

                match.Games.Add(createGameResult.Data!);

                var updateMatchRepoResult = await CoreService.Matches.UpdateAsync(match,
                    DatabaseComponent.Repository);
                var updateMatchCacheResult = await CoreService.Matches.UpdateAsync(match,
                    DatabaseComponent.Cache);

                if (!updateMatchRepoResult.Success || !updateMatchCacheResult.Success)
                {
                    return Result.Failure(
                        "Failed to update match in repository or cache after game creation."
                    );
                }

                // Publish event for Discord integration
                await CoreService.PublishAsync(new MatchStartedEvent
                {
                    MatchId = match.Id,
                    StartedAt = match.StartedAt.Value,
                    GameId = createGameResult.Data!.Id,
                });

                return Result.CreateSuccess();
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(ex, "Failed to start match", nameof(StartMatchAsync));
                return Result.Failure(
                    $"An unexpected error occurred while starting the match: {ex.Message}"
                );
            }
        }


        #region CoreLogic
        /// <summary>
        /// Gets the participants for a given team in this match
        /// </summary>
        public static List<MatchParticipant> GetParticipantsForTeam(Match match, Guid teamId)
        {
            return match.Participants.Where(p => p.TeamId == teamId).ToList();
        }

        /// <summary>
        /// Gets the opponent encounters for a given team in this match
        /// </summary>
        public static List<TeamOpponentEncounter> GetOpponentEncountersForTeam(Match match, Guid teamId)
        {
            return match.OpponentEncounters.Where(oe => oe.TeamId == teamId).ToList();
        }

        /// <summary>
        /// Gets all teams participating in this match
        /// </summary>
        public static List<Guid> GetParticipatingTeamIds(Match match)
        {
            return match.Participants.Select(p => p.TeamId).Distinct().ToList();
        }

        /// <summary>
        /// Gets all opponent encounters for this match
        /// </summary>
        public static List<(Guid TeamId, Guid OpponentId)> GetAllOpponentPairs(Match match)
        {
            var pairs = new List<(Guid, Guid)>();
            foreach (var encounter in match.OpponentEncounters)
            {
                pairs.Add((encounter.TeamId, encounter.OpponentId));
            }
            return pairs.Distinct().ToList();
        }

        /// <summary>
        /// Calculates variety entropy for a team's opponents in this match
        /// </summary>
        public static double CalculateVarietyEntropyForTeam(Match match, Guid teamId)
        {
            var encounters = GetOpponentEncountersForTeam(match, teamId);
            if (!encounters.Any()) return 0.0;

            var totalEncounters = encounters.Count;
            var uniqueOpponents = encounters.Select(e => e.OpponentId).Distinct().Count();

            // Shannon entropy calculation for opponent distribution
            var entropy = 0.0;
            var opponentGroups = encounters.GroupBy(e => e.OpponentId);

            foreach (var group in opponentGroups)
            {
                var probability = (double)group.Count() / totalEncounters;
                entropy -= probability * Math.Log(probability);
            }

            // Normalize by log of unique opponents to get value between 0 and 1
            var maxEntropy = Math.Log(uniqueOpponents);
            return maxEntropy == 0 ? 0 : entropy / maxEntropy;
        }

        /// <summary>
        /// Calculates variety bonus based on opponent distribution
        /// </summary>
        public static double CalculateVarietyBonusForTeam(Match match, Guid teamId)
        {
            var encounters = GetOpponentEncountersForTeam(match, teamId);
            var uniqueOpponents = encounters.Select(e => e.OpponentId).Distinct().Count();
            var totalEncounters = encounters.Count;

            // Bonus increases with more unique opponents, but decreases with repeated encounters
            var uniqueBonus = Math.Min(uniqueOpponents * 0.1, 1.0); // Cap at 1.0 for 10+ opponents
            var repeatPenalty = totalEncounters > uniqueOpponents ? (totalEncounters - uniqueOpponents) * 0.05 : 0.0;

            return Math.Max(uniqueBonus - repeatPenalty, 0.0);
        }

        /// <summary>
        /// Completes a match with winner determination and variety statistics updates
        /// </summary>
        public async Task<Result> CompleteMatchAsync(Guid matchId, Guid winnerId)
        {
            try
            {
                var matchResult = await CoreService.Matches.GetByIdAsync(matchId,
                    DatabaseComponent.Repository);
                if (!matchResult.Success)
                {
                    return Result.Failure(
                        $"Failed to retrieve match: {matchResult.ErrorMessage}"
                    );
                }
                var match = matchResult.Data;

                if (match == null)
                    return Result.Failure("Match not found.");

                // Validate match can be completed
                if (Accessors.GetCurrentStatus(match) != MatchStatus.InProgress)
                    return Result.Failure("Match can only be completed when in InProgress state.");

                // Validate winner is a participant
                var winnerParticipant = match.Participants.FirstOrDefault(p => p.TeamId == winnerId);
                if (winnerParticipant == null)
                    return Result.Failure("Winner team is not a participant in this match.");

                // Set completion data
                match.CompletedAt = DateTime.UtcNow;
                match.WinnerId = winnerId;

                // Update match participants with winner information
                foreach (var participant in match.Participants)
                {
                    participant.IsWinner = participant.TeamId == winnerId;
                    participant.UpdatedAt = DateTime.UtcNow;
                    var updateParticipantResult = await CoreService.MatchParticipants.UpdateAsync(participant,
                        DatabaseComponent.Repository);
                    if (!updateParticipantResult.Success)
                    {
                        // Log and continue, don't fail entire operation
                        await CoreService.ErrorHandler.CaptureAsync(new Exception(
                                $"Failed to update participant {participant.Id}: " +
                                $"{updateParticipantResult.ErrorMessage}"
                            ),
                            "Match Completion Warning",
                            nameof(CompleteMatchAsync)
                        );
                    }
                }

                // Update opponent encounters with winner information
                foreach (var encounter in match.OpponentEncounters)
                {
                    encounter.Won = encounter.TeamId == winnerId;
                    encounter.UpdatedAt = DateTime.UtcNow;
                    var updateEncounterResult = await CoreService.TeamOpponentEncounters.UpdateAsync(encounter,
                        DatabaseComponent.Repository);
                    if (!updateEncounterResult.Success)
                    {
                        // Log and continue, don't fail entire operation
                        await CoreService.ErrorHandler.CaptureAsync(new Exception(
                                $"Failed to update opponent encounter {encounter.Id}: " +
                                $"{updateEncounterResult.ErrorMessage}"
                            ),
                            "Match Completion Warning",
                            nameof(CompleteMatchAsync)
                        );
                    }
                }

                // Update match and save
                var updateMatchRepoResult = await CoreService.Matches.UpdateAsync(match,
                    DatabaseComponent.Repository);
                var updateMatchCacheResult = await CoreService.Matches.UpdateAsync(match,
                    DatabaseComponent.Cache);

                if (!updateMatchRepoResult.Success || !updateMatchCacheResult.Success)
                {
                    return Result.Failure(
                        "Failed to update match in repository or cache after completion."
                    );
                }

                // Trigger variety statistics updates for participating teams
                await UpdateTeamVarietyStatsAsync(match);

                // Publish match completion event
                await CoreService.PublishAsync(new MatchCompletedEvent
                {
                    MatchId = match.Id,
                    WinnerId = winnerId,
                    CompletedAt = match.CompletedAt.Value,
                });

                return Result.CreateSuccess();
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(ex, "Failed to complete match", nameof(CompleteMatchAsync));
                return Result.Failure(
                    $"An unexpected error occurred while completing the match: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Updates variety statistics for all teams that participated in a match
        /// </summary>
        private async Task UpdateTeamVarietyStatsAsync(Match match)
        {
            try
            {
                // var teamData = new DatabaseService<Team>(); // Replaced by CoreService.Teams field
                // var varietyStatsData = new DatabaseService<TeamVarietyStats>(); // Replaced by _varietyStatsData field

                foreach (var participant in match.Participants)
                {
                    var teamResult = await CoreService.Teams.GetByIdAsync(participant.TeamId,
                        DatabaseComponent.Repository);
                    if (!teamResult.Success)
                    {
                        await CoreService.ErrorHandler.CaptureAsync(new Exception(
                                $"Failed to retrieve team {participant.TeamId}: " +
                                $"{teamResult.ErrorMessage}"
                            ),
                            "Variety Stats Update Warning",
                            nameof(UpdateTeamVarietyStatsAsync)
                        );
                        continue; // Skip this team and continue with others
                    }
                    var team = teamResult.Data;

                    if (team == null)
                    {
                        continue; // Skip if team not found
                    }

                    // Get or create variety stats for this team and team size
                    var varietyStats = team.VarietyStats.GetValueOrDefault((TeamSize)match.TeamSize);
                    if (varietyStats == null)
                    {
                        varietyStats = new TeamVarietyStats
                        {
                            TeamId = team.Id,
                            TeamSize = (TeamSize)match.TeamSize,
                            VarietyEntropy = 0.0,
                            VarietyBonus = 0.0,
                            TotalOpponents = 0,
                            UniqueOpponents = 0,
                            LastCalculated = DateTime.UtcNow,
                            LastUpdated = DateTime.UtcNow,
                        };

                        // Use _varietyStatsData from constructor
                        var createResult = await CoreService.TeamVarietyStats.CreateAsync(varietyStats,
                            DatabaseComponent.Repository);
                        if (createResult.Success)
                        {
                            team.VarietyStats[(TeamSize)match.TeamSize] = varietyStats;
                        }
                        else
                        {
                            await CoreService.ErrorHandler.CaptureAsync(new Exception(
                                    $"Failed to create variety stats for team {team.Id}: " +
                                    $"{createResult.ErrorMessage}"
                                ),
                                "Variety Stats Update Warning",
                                nameof(UpdateTeamVarietyStatsAsync)
                            );
                            continue; // Skip this team if stats cannot be created
                        }
                    }

                    // Update variety stats using recent encounters
                    // Note: This still assumes team.RecentOpponents is available.
                    // The relational-refactoring plan will address this data structure.
                    var recentEncounters = team.RecentOpponents?
                        .Where(oe => oe.TeamSize == (int)match.TeamSize)
                        .OrderByDescending(oe => oe.EncounteredAt)
                        .Take(100) // Use last 100 encounters for stats calculation
                        .ToList() ?? new List<TeamOpponentEncounter>();

                    TeamCore.Stats.UpdateVarietyStats(varietyStats, recentEncounters);
                    varietyStats.LastUpdated = DateTime.UtcNow;

                    // Use _varietyStatsData and CoreService.Teams from constructor
                    var updateVarietyResult = await CoreService.TeamVarietyStats.UpdateAsync(varietyStats,
                        DatabaseComponent.Repository);
                    if (!updateVarietyResult.Success)
                    {
                        await CoreService.ErrorHandler.CaptureAsync(new Exception(
                                $"Failed to update variety stats for team {team.Id}: " +
                                $"{updateVarietyResult.ErrorMessage}"
                            ),
                            "Variety Stats Update Warning",
                            nameof(UpdateTeamVarietyStatsAsync)
                        );
                    }

                    var updateTeamResult = await CoreService.Teams.UpdateAsync(team,
                        DatabaseComponent.Repository);
                    if (!updateTeamResult.Success)
                    {
                        await CoreService.ErrorHandler.CaptureAsync(new Exception(
                                $"Failed to update team {team.Id} variety stats: " +
                                $"{updateTeamResult.ErrorMessage}"
                            ),
                            "Variety Stats Update Warning",
                            nameof(UpdateTeamVarietyStatsAsync)
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(ex,
                    "Failed to update team variety stats",
                    nameof(UpdateTeamVarietyStatsAsync)
                );
                // Don't throw - variety stats update failure shouldn't fail match completion
            }
        }

        // TODO: The rest of the business logic methods from MatchService.deprecated.cs need to be migrated here.
        // - CancelMatchAsync
        // - ForfeitMatchAsync
        // - SubmitMapBansAsync
        // - CheckMatchVictoryConditionsAsync
        #endregion
    }
}
