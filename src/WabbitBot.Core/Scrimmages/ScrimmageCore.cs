using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Interfaces;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Models.Scrimmage;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.Core.Scrimmages
{
    public partial class ScrimmageCore : IScrimmageCore
    {
        /// <inheritdoc />
        public Task InitializeAsync() => Task.CompletedTask;

        /// <inheritdoc />
        public Task ValidateAsync() => Task.CompletedTask;

        #region Factory
        public static class Factory
        {
            // ------------------------ Factory & Initialization ---------------------------
            public static Scrimmage CreateScrimmage(
                Guid ScrimmageChallengeId,
                Guid ChallengerTeamId,
                Guid OpponentTeamId,
                ICollection<Player> ChallengerTeamPlayers,
                ICollection<Player> OpponentTeamPlayers,
                Guid IssuedByPlayerId,
                Guid AcceptedByPlayerId,
                int bestOf,
                TeamSize teamSize,
                double challengerTeamRating,
                double opponentTeamRating,
                double challengerTeamConfidence,
                double opponentTeamConfidence,
                double ratingRangeAtMatch
            )
            {
                var scrimmage = new Scrimmage
                {
                    ScrimmageChallengeId = ScrimmageChallengeId,
                    ChallengerTeamId = ChallengerTeamId,
                    OpponentTeamId = OpponentTeamId,
                    ChallengerTeamPlayers = ChallengerTeamPlayers,
                    OpponentTeamPlayers = OpponentTeamPlayers,
                    IssuedByPlayerId = IssuedByPlayerId,
                    AcceptedByPlayerId = AcceptedByPlayerId,
                    TeamSize = teamSize,
                    StartedAt = DateTime.UtcNow,
                    ChallengerTeamRating = challengerTeamRating,
                    OpponentTeamRating = opponentTeamRating,
                    ChallengerTeamConfidence = challengerTeamConfidence,
                    OpponentTeamConfidence = opponentTeamConfidence,
                    HigherRatedTeamId = challengerTeamRating > opponentTeamRating ? ChallengerTeamId : OpponentTeamId,
                    RatingRangeAtMatch = ratingRangeAtMatch,
                    BestOf = bestOf,
                };
                return scrimmage;
            }

            public static ScrimmageChallenge CreateChallenge(
                Guid ChallengerTeamId,
                Guid OpponentTeamId,
                Guid IssuedByPlayerId,
                ICollection<Guid> SelectedTeammateIds,
                TeamSize teamSize,
                int bestOf = 1
            )
            {
                var scrimmageChallenge = new ScrimmageChallenge
                {
                    ChallengerTeamId = ChallengerTeamId,
                    OpponentTeamId = OpponentTeamId,
                    IssuedByPlayerId = IssuedByPlayerId,
                    ChallengerTeammateIds = SelectedTeammateIds,
                    ChallengeStatus = ScrimmageChallengeStatus.Pending,
                    TeamSize = teamSize,
                    BestOf = bestOf,
                    ChallengeExpiresAt = DateTime.UtcNow.AddHours(1), // 1 hour challenge window
                };

                return scrimmageChallenge;
            }
        }
        #endregion

        #region Accessors
        public static class Accessors
        {
            // ------------------------ State & Logic Accessors ----------------------------
            public static bool IsTeamMatch(Scrimmage scrimmage)
            {
                return scrimmage.TeamSize != TeamSize.OneVOne;
            }
        }
        #endregion

        #region StateMachine
        public static class State
        {
            private static readonly Dictionary<Guid, Scrimmage> _activeScrimmages = new();
            private static readonly Dictionary<Guid, ScrimmageStatus> _scrimmageStates = new();

            // ------------------------ State Machine Definition ---------------------------
            private static readonly Dictionary<ScrimmageStatus, List<ScrimmageStatus>> _validTransitions = new()
            {
                [ScrimmageStatus.Accepted] = new() { ScrimmageStatus.InProgress, ScrimmageStatus.Cancelled },
                [ScrimmageStatus.InProgress] = new()
                {
                    ScrimmageStatus.Completed,
                    ScrimmageStatus.Cancelled,
                    ScrimmageStatus.Forfeited,
                },
                [ScrimmageStatus.Completed] = new(), // Terminal state
                [ScrimmageStatus.Cancelled] = new(), // Terminal state
                [ScrimmageStatus.Forfeited] = new(), // Terminal state
                [ScrimmageStatus.Declined] = new(), // Terminal state
            };
        }
        #endregion

        #region Validation
        #endregion

        #region Core Logic
        /// <summary>
        /// Creates a new scrimmage challenge
        /// </summary>
        public static async Task<Result<Scrimmage>> CreateScrimmageAsync(
            Guid ScrimmageChallengeId,
            Guid ChallengerTeamId,
            Guid OpponentTeamId,
            ICollection<Player> ChallengerTeamPlayers,
            ICollection<Player> OpponentTeamPlayers,
            Guid IssuedByPlayerId,
            Guid AcceptedByPlayerId,
            TeamSize teamSize,
            int bestOf,
            ScrimmageTeamStats challengerTeamStats,
            ScrimmageTeamStats opponentTeamStats
        )
        {
            try
            {
                var allTeamsResult = await CoreService.Teams.GetAllAsync(DatabaseComponent.Repository);
                if (!allTeamsResult.Success)
                {
                    return Result<Scrimmage>.Failure("Failed to fetch all teams");
                }
                if (allTeamsResult.Data is null)
                {
                    return Result<Scrimmage>.Failure("No teams found");
                }

                var allTeamsList = allTeamsResult.Data.ToList();
                var RatingRangeAtMatch =
                    allTeamsList.Max(t => t.ScrimmageTeamStats[teamSize].CurrentRating)
                    - allTeamsList.Min(t => t.ScrimmageTeamStats[teamSize].CurrentRating);

                var Scrimmage = Factory.CreateScrimmage(
                    ScrimmageChallengeId,
                    ChallengerTeamId,
                    OpponentTeamId,
                    ChallengerTeamPlayers,
                    OpponentTeamPlayers,
                    IssuedByPlayerId,
                    AcceptedByPlayerId,
                    bestOf,
                    teamSize,
                    challengerTeamStats.CurrentRating,
                    opponentTeamStats.CurrentRating,
                    challengerTeamStats.Confidence,
                    opponentTeamStats.Confidence,
                    RatingRangeAtMatch
                );

                var dbResult = await CoreService.Scrimmages.CreateAsync(Scrimmage, DatabaseComponent.Repository);
                if (!dbResult.Success)
                {
                    return Result<Scrimmage>.Failure("Failed to create new Scrimmage.");
                }

                return Result<Scrimmage>.CreateSuccess(Scrimmage);
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to create new Scrimmage.",
                    nameof(CreateScrimmageAsync)
                );
                return Result<Scrimmage>.Failure(
                    $"An unexpected error occurred while creating the new Scrimmage: {ex.Message}"
                );
            }
        }

        public static async Task<Result<ScrimmageChallenge>> CreateChallengeAsync(
            Guid ChallengerTeamId,
            Guid OpponentTeamId,
            Guid IssuedByPlayerId,
            ICollection<Guid> SelectedTeammateIds,
            TeamSize teamSize,
            int bestOf = 1
        )
        {
            try
            {
                // Store teammate IDs directly (not entities) to avoid circular dependencies
                var challenge = Factory.CreateChallenge(
                    ChallengerTeamId,
                    OpponentTeamId,
                    IssuedByPlayerId,
                    SelectedTeammateIds,
                    teamSize,
                    bestOf
                );

                return Result<ScrimmageChallenge>.CreateSuccess(challenge);
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to create new Scrimmage Challenge.",
                    nameof(CreateChallengeAsync)
                );
                return Result<ScrimmageChallenge>.Failure(
                    $"An unexpected error occurred while creating the new Scrimmage Challenge: {ex.Message}"
                );
            }
        }
        #endregion
    }
}
