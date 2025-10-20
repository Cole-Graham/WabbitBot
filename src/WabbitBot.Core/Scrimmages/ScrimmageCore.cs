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
                ICollection<Guid> ChallengerTeamPlayerIds,
                ICollection<Guid> OpponentTeamPlayerIds,
                Guid IssuedByPlayerId,
                Guid AcceptedByPlayerId,
                TeamSize teamSize,
                int bestOf = 1,
                ScrimmageStatus state = ScrimmageStatus.Accepted
            )
            {
                var scrimmage = new Scrimmage
                {
                    ScrimmageChallengeId = ScrimmageChallengeId,
                    ChallengerTeamId = ChallengerTeamId,
                    OpponentTeamId = OpponentTeamId,
                    ChallengerTeamPlayerIds = ChallengerTeamPlayerIds.ToList(),
                    OpponentTeamPlayerIds = OpponentTeamPlayerIds.ToList(),
                    IssuedByPlayerId = IssuedByPlayerId,
                    AcceptedByPlayerId = AcceptedByPlayerId,
                    TeamSize = teamSize,
                    BestOf = bestOf,
                    StateHistory = new List<ScrimmageStateSnapshot> { new() { Status = state } },
                    StartedAt = DateTime.UtcNow,
                };
                return scrimmage;
            }

            public static async Task<Result<ScrimmageChallenge>> CreateChallenge(
                Guid ChallengerTeamId,
                Guid OpponentTeamId,
                Guid IssuedByPlayerId,
                ICollection<Guid> SelectedPlayerIds,
                TeamSize teamSize,
                int bestOf = 1
            )
            {
                var challengerTeam = await CoreService.Teams.GetByIdAsync(
                    ChallengerTeamId,
                    DatabaseComponent.Repository
                );
                if (!challengerTeam.Success || challengerTeam.Data is null)
                {
                    return Result<ScrimmageChallenge>.Failure("Challenger team not found");
                }
                var opponentTeam = await CoreService.Teams.GetByIdAsync(OpponentTeamId, DatabaseComponent.Repository);
                if (!opponentTeam.Success || opponentTeam.Data is null)
                {
                    return Result<ScrimmageChallenge>.Failure("Opponent team not found");
                }

                // Validate that the issued by player exists
                var issuedByPlayerExists = await CoreService.WithDbContext(async db =>
                    await db.Players.AnyAsync(p => p.Id == IssuedByPlayerId)
                );
                if (!issuedByPlayerExists)
                {
                    return Result<ScrimmageChallenge>.Failure("Issued by player not found");
                }

                // Combine challenge issuer and selected team members IDs
                var challengerTeamPlayerIds = new List<Guid> { IssuedByPlayerId };
                challengerTeamPlayerIds.AddRange(SelectedPlayerIds);

                var scrimmageChallenge = new ScrimmageChallenge
                {
                    // Only set foreign key properties to avoid EF Core tracking conflicts
                    ChallengerTeamId = ChallengerTeamId,
                    OpponentTeamId = OpponentTeamId,
                    IssuedByPlayerId = IssuedByPlayerId,
                    ChallengerTeamPlayerIds = challengerTeamPlayerIds,
                    ChallengeStatus = ScrimmageChallengeStatus.Pending,
                    TeamSize = teamSize,
                    BestOf = bestOf,
                    ChallengeExpiresAt = DateTime.UtcNow.AddHours(1), // 1 hour challenge window
                };
                return Result<ScrimmageChallenge>.CreateSuccess(scrimmageChallenge);
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
            ICollection<Guid> ChallengerTeamPlayerIds,
            ICollection<Guid> OpponentTeamPlayerIds,
            Guid IssuedByPlayerId,
            Guid AcceptedByPlayerId,
            TeamSize teamSize,
            int bestOf = 1
        )
        {
            try
            {
                var Scrimmage = Factory.CreateScrimmage(
                    ScrimmageChallengeId,
                    ChallengerTeamId,
                    OpponentTeamId,
                    ChallengerTeamPlayerIds,
                    OpponentTeamPlayerIds,
                    IssuedByPlayerId,
                    AcceptedByPlayerId,
                    teamSize,
                    bestOf
                );

                // Navigation properties are set automatically by EF Core based on foreign keys
                // Load team stats separately to avoid navigation property access
                var challengerTeamStats = await CoreService.WithDbContext(async db =>
                    await db
                        .ScrimmageTeamStats.Where(s => s.TeamId == ChallengerTeamId && s.TeamSize == teamSize)
                        .FirstOrDefaultAsync()
                );
                if (challengerTeamStats is null)
                {
                    return Result<Scrimmage>.Failure("Challenger team stats not found");
                }

                var opponentTeamStats = await CoreService.WithDbContext(async db =>
                    await db
                        .ScrimmageTeamStats.Where(s => s.TeamId == OpponentTeamId && s.TeamSize == teamSize)
                        .FirstOrDefaultAsync()
                );
                if (opponentTeamStats is null)
                {
                    return Result<Scrimmage>.Failure("Opponent team stats not found");
                }

                // Data properties
                Scrimmage.ChallengerTeamRating = challengerTeamStats.CurrentRating;
                Scrimmage.OpponentTeamRating = opponentTeamStats.CurrentRating;
                Scrimmage.ChallengerTeamConfidence = challengerTeamStats.Confidence;
                Scrimmage.OpponentTeamConfidence = opponentTeamStats.Confidence;
                // Calculate higher rated team
                if (Scrimmage.ChallengerTeamRating > Scrimmage.OpponentTeamRating)
                {
                    Scrimmage.HigherRatedTeamId = Scrimmage.ChallengerTeamId;
                }
                else
                {
                    Scrimmage.HigherRatedTeamId = Scrimmage.OpponentTeamId;
                }

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
        #endregion
    }
}
