using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.ErrorService;
using WabbitBot.Common.Events.Core;
using WabbitBot.Common.Events.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Database;
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
                Guid ChallengeId,
                Team ChallengerTeam,
                Team OpponentTeam,
                List<Player> Team1Players,
                List<Player> Team2Players,
                Player IssuedByPlayer,
                Player AcceptedByPlayer,
                TeamSize teamSize,
                int bestOf = 1,
                ScrimmageStatus state = ScrimmageStatus.Accepted
            )
            {
                var scrimmage = new Scrimmage
                {
                    ChallengeId = ChallengeId,
                    Team1Id = ChallengerTeam.Id,
                    Team2Id = OpponentTeam.Id,
                    Team1PlayerIds = [.. Team1Players.Select(p => p.Id)],
                    Team2PlayerIds = [.. Team2Players.Select(p => p.Id)],
                    IssuedByPlayer = IssuedByPlayer,
                    AcceptedByPlayer = AcceptedByPlayer,
                    TeamSize = teamSize,
                    BestOf = bestOf,
                    StateHistory = new List<ScrimmageStateSnapshot> { new() { Status = state } },
                    StartedAt = DateTime.UtcNow,
                };
                return scrimmage;
            }

            public static Result<ScrimmageChallenge> CreateChallenge(
                Team ChallengerTeam,
                Team OpponentTeam,
                Player IssuedByPlayer,
                Player[] SelectedPlayers,
                TeamSize teamSize,
                int bestOf = 1
            )
            {
                if (ChallengerTeam == null)
                {
                    return Result<ScrimmageChallenge>.Failure("Challenger team not found");
                }
                if (OpponentTeam == null)
                {
                    return Result<ScrimmageChallenge>.Failure("Opponent team not found");
                }

                // Combine challenge issuer and selected team members
                var Team1Players = new List<Player> { IssuedByPlayer };
                Team1Players.AddRange(SelectedPlayers);

                var scrimmageChallenge = new ScrimmageChallenge
                {
                    ChallengerTeam = ChallengerTeam,
                    OpponentTeam = OpponentTeam,
                    ChallengerTeamId = ChallengerTeam.Id,
                    OpponentTeamId = OpponentTeam.Id,
                    IssuedByPlayer = IssuedByPlayer,
                    IssuedByPlayerId = IssuedByPlayer.Id,
                    Team1Players = Team1Players,
                    ChallengeStatus = ScrimmageChallengeStatus.Pending,
                    TeamSize = teamSize,
                    BestOf = bestOf,
                    ChallengeExpiresAt = DateTime.UtcNow.AddHours(1), // 1 hour challenge window
                };
                return Result<ScrimmageChallenge>.CreateSuccess(scrimmageChallenge);
            }
            #endregion
        }

        #region Accessors
        public static class Accessors
        {
            // ------------------------ State & Logic Accessors ----------------------------
            public static bool IsTeamMatch(Scrimmage scrimmage)
            {
                return scrimmage.TeamSize != TeamSize.OneVOne;
            }

            // TODO: Implement ScrimmageStateSnapshot
            // public static string GetRequiredAction(Scrimmage scrimmage)
            // {
            //     return scrimmage.ScrimmageStatus switch
            //     {
            //         ScrimmageStatus.Created => "Waiting for acceptance",
            //         ScrimmageStatus.Accepted => "Waiting to start",
            //         ScrimmageStatus.InProgress => "Scrimmage in progress",
            //         ScrimmageStatus.Completed => "Scrimmage completed",
            //         ScrimmageStatus.Cancelled => "Scrimmage cancelled",
            //         ScrimmageStatus.Forfeited => "Scrimmage forfeited",
            //         ScrimmageStatus.Declined => "Scrimmage declined",
            //         _ => "Unknown state",
            //     };
            // }

            // public static bool IsExpired(Scrimmage scrimmage)
            // {
            //     return scrimmage.Status == ScrimmageStatus.Created &&
            //            scrimmage.ChallengeExpiresAt.HasValue &&
            //            scrimmage.ChallengeExpiresAt.Value < DateTime.UtcNow;
            // }

            // public static bool CanBeAccepted(Scrimmage scrimmage)
            // {
            //     return scrimmage.Status == ScrimmageStatus.Created && !IsExpired(scrimmage);
            // }

            // public static bool CanBeDeclined(Scrimmage scrimmage)
            // {
            //     return scrimmage.Status == ScrimmageStatus.Created && !IsExpired(scrimmage);
            // }

            // public static bool CanBeStarted(Scrimmage scrimmage)
            // {
            //     return scrimmage.Status == ScrimmageStatus.Accepted;
            // }
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

            /// <summary>
            /// Captures a scrimmage in the state machine
            /// </summary>
            // public void AddScrimmage(Scrimmage scrimmage)
            // {
            //     var scrimmageId = scrimmage.Id;
            //     _activeScrimmages[scrimmageId] = scrimmage;
            //     _scrimmageStates[scrimmageId] = scrimmage.Status;
            // }

            /// <summary>
            /// Updates the state of a scrimmage
            /// </summary>
            // public void UpdateState(Guid scrimmageId, ScrimmageStatus newStatus)
            // {
            //     if (_scrimmageStates.ContainsKey(scrimmageId))
            //     {
            //         _scrimmageStates[scrimmageId] = newStatus;
            //         if (_activeScrimmages.TryGetValue(scrimmageId, out var scrimmage))
            //         {
            //             scrimmage.Status = newStatus;
            //         }
            //     }
            // }

            /// <summary>
            /// Removes a scrimmage from the state machine (when completed/archived)
            /// </summary>
            public static void RemoveScrimmage(Guid scrimmageId)
            {
                _activeScrimmages.Remove(scrimmageId);
                _scrimmageStates.Remove(scrimmageId);
            }

            /// <summary>
            /// Validates if a state transition is allowed
            /// </summary>
            public static bool IsValidTransition(ScrimmageStatus from, ScrimmageStatus to)
            {
                return _validTransitions.ContainsKey(from) && _validTransitions[from].Contains(to);
            }

            /// <summary>
            /// Gets all active scrimmages for a team
            /// </summary>
            // public IEnumerable<Scrimmage> GetActiveScrimmagesForTeam(Guid teamId)
            // {
            //     foreach (var scrimmage in _activeScrimmages.Values)
            //     {
            //         if ((scrimmage.Team1Id == teamId || scrimmage.Team2Id == teamId) &&
            //             scrimmage.Status != ScrimmageStatus.Completed &&
            //             scrimmage.Status != ScrimmageStatus.Declined &&
            //             scrimmage.Status != ScrimmageStatus.Cancelled &&
            //             scrimmage.Status != ScrimmageStatus.Forfeited)
            //         {
            //             yield return scrimmage;
            //         }
            //     }
            // }

            /// <summary>
            /// Gets all expired challenges that need cleanup
            /// </summary>
            // public IEnumerable<Scrimmage> GetExpiredChallenges()
            // {
            //     var now = DateTime.UtcNow;
            //     foreach (var scrimmage in _activeScrimmages.Values)
            //     {
            //         if (scrimmage.Status == ScrimmageStatus.Created &&
            //             scrimmage.ChallengeExpiresAt.HasValue &&
            //             scrimmage.ChallengeExpiresAt.Value < now)
            //         {
            //             yield return scrimmage;
            //         }
            //     }
            // }

            /// <summary>
            /// Checks if a scrimmage is currently in the state machine
            /// </summary>
            public static bool ContainsScrimmage(Guid scrimmageId)
            {
                return _activeScrimmages.ContainsKey(scrimmageId);
            }

            /// <summary>
            /// Gets all scrimmages currently in the state machine
            /// </summary>
            public static IEnumerable<Scrimmage> GetAllActiveScrimmages()
            {
                return _activeScrimmages.Values;
            }

            /// <summary>
            /// Gets the count of active scrimmages
            /// </summary>
            public static int ActiveScrimmageCount => _activeScrimmages.Count;

            // ------------------------ State Machine Logic --------------------------------
            // public static bool CanTransition(Scrimmage scrimmage, ScrimmageStatus to)
            // {
            //     return _validTransitions.ContainsKey(
            //         scrimmage.Status) && _validTransitions[scrimmage.Status].Contains(to);
            // }

            // public static List<ScrimmageStatus> GetValidTransitions(Scrimmage scrimmage)
            // {
            //     return _validTransitions.ContainsKey(scrimmage.Status)
            //         ? new List<ScrimmageStatus>(_validTransitions[scrimmage.Status])
            //         : new List<ScrimmageStatus>();
            // }

            // public static bool TryTransition(Scrimmage scrimmage, ScrimmageStatus toState)
            // {
            //     if (!CanTransition(scrimmage, toState))
            //         return false;

            //     scrimmage.Status = toState;

            //     // Set timestamps based on new status
            //     switch (toState)
            //     {
            //         case ScrimmageStatus.Accepted:
            //             break;
            //         case ScrimmageStatus.InProgress:
            //             scrimmage.StartedAt = DateTime.UtcNow;
            //             break;
            //         case ScrimmageStatus.Completed:
            //         case ScrimmageStatus.Cancelled:
            //         case ScrimmageStatus.Forfeited:
            //         case ScrimmageStatus.Declined:
            //             scrimmage.CompletedAt = DateTime.UtcNow;
            //             break;
            //     }

            //     return true;
            // }
        }
        #endregion

        #region Validation
        public static class Validation
        {
            public static async Task<Result<ScrimmageChallenge>> ValidateChallengeAsync(Guid challengeId)
            {
                // Get challenge
                var challengeResult = await CoreService.ScrimmageChallenges.GetByIdAsync(
                    challengeId,
                    DatabaseComponent.Repository
                );
                if (!challengeResult.Success)
                {
                    return Result<ScrimmageChallenge>.Failure("Challenge not found");
                }
                var challenge = challengeResult.Data;
                if (challenge == null)
                {
                    return Result<ScrimmageChallenge>.Failure("Challenge not found");
                }

                // Get teams
                var team1Result = await CoreService.Teams.GetByIdAsync(
                    challenge.ChallengerTeamId,
                    DatabaseComponent.Repository
                );
                if (!team1Result.Success)
                {
                    return Result<ScrimmageChallenge>.Failure("Team 1 not found");
                }
                var team2Result = await CoreService.Teams.GetByIdAsync(
                    challenge.OpponentTeamId,
                    DatabaseComponent.Repository
                );
                if (!team2Result.Success)
                {
                    return Result<ScrimmageChallenge>.Failure("Team 2 not found");
                }
                var team1 = team1Result.Data;
                var team2 = team2Result.Data;
                if (team1 == null)
                {
                    return Result<ScrimmageChallenge>.Failure("Team 1 not found");
                }
                if (team2 == null)
                {
                    return Result<ScrimmageChallenge>.Failure("Team 2 not found");
                }
                var issuedByPlayerResult = await CoreService.Players.GetByIdAsync(
                    challenge.IssuedByPlayerId!,
                    DatabaseComponent.Repository
                );
                if (!issuedByPlayerResult.Success)
                {
                    return Result<ScrimmageChallenge>.Failure("Issued by player not found");
                }
                var issuedByPlayer = issuedByPlayerResult.Data!;
                var acceptedByPlayerResult = await CoreService.Players.GetByIdAsync(
                    challenge.AcceptedByPlayerId!,
                    DatabaseComponent.Repository
                );
                if (!acceptedByPlayerResult.Success)
                {
                    return Result<ScrimmageChallenge>.Failure("Accepted by player not found");
                }
                var acceptedByPlayer = acceptedByPlayerResult.Data;
                // Validate team player ids
                foreach (var playerId in challenge.Team1Players.Select(p => p.Id))
                {
                    var playerResult = await CoreService.Players.GetByIdAsync(playerId, DatabaseComponent.Repository);
                    if (!playerResult.Success)
                    {
                        return Result<ScrimmageChallenge>.Failure("Team 1 player not found");
                    }
                }
                foreach (var playerId in challenge.Team2Players.Select(p => p.Id))
                {
                    var playerResult = await CoreService.Players.GetByIdAsync(playerId, DatabaseComponent.Repository);
                    if (!playerResult.Success)
                    {
                        return Result<ScrimmageChallenge>.Failure("Team 2 player not found");
                    }
                }

                // Attach loaded teams to challenge so callers can consume them
                challenge.ChallengerTeam = team1;
                challenge.OpponentTeam = team2;
                challenge.IssuedByPlayer = issuedByPlayer;
                challenge.AcceptedByPlayer = acceptedByPlayer;

                return Result<ScrimmageChallenge>.CreateSuccess(challenge);
            }
        }
        #endregion

        #region Core Logic
        /// <summary>
        /// Creates a new scrimmage challenge
        /// </summary>
        public async Task<Result<Scrimmage>> CreateScrimmageAsync(
            Guid ChallengeId,
            Team ChallengerTeam,
            Team OpponentTeam,
            List<Player> Team1Players,
            List<Player> Team2Players,
            Player IssuedByPlayer,
            Player AcceptedByPlayer
        )
        {
            try
            {
                var validateResult = await Validation.ValidateChallengeAsync(ChallengeId);
                if (!validateResult.Success)
                {
                    return Result<Scrimmage>.Failure("Challenge not found");
                }
                var challenge = validateResult.Data!;

                var scrimmage = Factory.CreateScrimmage(
                    ChallengeId,
                    ChallengerTeam,
                    OpponentTeam,
                    Team1Players,
                    Team2Players,
                    IssuedByPlayer,
                    AcceptedByPlayer,
                    challenge.TeamSize,
                    challenge.BestOf
                );

                var challengerTeam = challenge.ChallengerTeam;
                var opponentTeam = challenge.OpponentTeam;
                if (challengerTeam == null)
                {
                    return Result<Scrimmage>.Failure("Challenger team not found");
                }
                if (opponentTeam == null)
                {
                    return Result<Scrimmage>.Failure("Opponent team not found");
                }

                var issuedByPlayer = challenge.IssuedByPlayer;
                var acceptedByPlayer = challenge.AcceptedByPlayer;

                // Nav properties
                scrimmage.IssuedByPlayer = challenge.IssuedByPlayer;
                scrimmage.AcceptedByPlayer = challenge.AcceptedByPlayer;
                // Data properties
                scrimmage.Team1Rating = challengerTeam.ScrimmageTeamStats[challenge.TeamSize].CurrentRating;
                scrimmage.Team2Rating = opponentTeam.ScrimmageTeamStats[challenge.TeamSize].CurrentRating;
                scrimmage.Team1Confidence = challengerTeam.ScrimmageTeamStats[challenge.TeamSize].Confidence;
                scrimmage.Team2Confidence = opponentTeam.ScrimmageTeamStats[challenge.TeamSize].Confidence;
                // Calculate higher rated team
                if (scrimmage.Team1Rating > scrimmage.Team2Rating)
                {
                    scrimmage.HigherRatedTeamId = scrimmage.Team1Id;
                }
                else
                {
                    scrimmage.HigherRatedTeamId = scrimmage.Team2Id;
                }

                var dbResult = await CoreService.Scrimmages.CreateAsync(scrimmage, DatabaseComponent.Repository);
                if (!dbResult.Success)
                {
                    return Result<Scrimmage>.Failure("Failed to create scrimmage challenge.");
                }

                // Commented out pending generator publishers
                // var publishResult = await PublishScrimmageCreatedAsync(scrimmage.Id);
                // if (!publishResult.Success)
                // {
                //     return Result<Scrimmage>.Failure("Failed to publish scrimmage created");
                // }

                return Result<Scrimmage>.CreateSuccess(scrimmage);
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to create scrimmage",
                    nameof(CreateScrimmageAsync)
                );
                return Result<Scrimmage>.Failure(
                    $"An unexpected error occurred while creating the scrimmage: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Publishes MatchProvisioningRequested to both Core (local) and Global (cross-boundary) buses.
        /// Auto-generated publisher will dual-publish this event.
        /// </summary>
        // public ValueTask PublishMatchProvisioningRequestedAsync(Guid matchId, Guid scrimmageId)
        // {
        //     // TODO: Source generator will replace this with actual dual-publish logic
        //     return ValueTask.CompletedTask;
        // }

        /// <summary>
        /// Accepts a scrimmage challenge
        /// </summary>
        public async Task<Result> AcceptScrimmageAsync(Guid scrimmageId)
        {
            try
            {
                var scrimmageResult = await CoreService.Scrimmages.GetByIdAsync(
                    scrimmageId,
                    DatabaseComponent.Repository
                );
                if (!scrimmageResult.Success)
                {
                    return Result.Failure($"Failed to retrieve scrimmage: {scrimmageResult.ErrorMessage}");
                }
                var scrimmage = scrimmageResult.Data;

                if (scrimmage == null)
                    return Result.Failure("Scrimmage not found.");

                if (!Accessors.IsTeamMatch(scrimmage))
                    return Result.Failure("Scrimmage cannot be accepted.");

                var currentState = scrimmage.StateHistory.Last();
                if (!State.IsValidTransition(currentState.Status, ScrimmageStatus.Accepted))
                    return Result.Failure("Failed to accept scrimmage.");

                var updateResult = await CoreService.Scrimmages.UpdateAsync(scrimmage, DatabaseComponent.Repository);
                if (!updateResult.Success)
                {
                    return Result.Failure($"Failed to update scrimmage: {updateResult.ErrorMessage}");
                }

                // Create match and publish provisioning request to both Core and Global
                // TODO: Actually create the match entity here once match creation logic is ready
                var matchId = Guid.NewGuid(); // Placeholder - should be actual match ID after creation
                // Commented out pending generator publishers
                // await PublishMatchProvisioningRequestedAsync(matchId, scrimmageId);

                return Result.CreateSuccess();
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to accept scrimmage",
                    nameof(AcceptScrimmageAsync)
                );
                return Result.Failure(
                    $"An unexpected error occurred while accepting the scrimmage: " + $"{ex.Message}"
                );
            }
        }

        /// <summary>
        /// Starts a scrimmage match
        /// </summary>
        // public async Task<Result> StartScrimmageAsync(Guid scrimmageId)
        // {
        //     try
        //     {
        //         var scrimmage = await _scrimmageData.GetByIdAsync(scrimmageId, DatabaseComponent.Repository);
        //         if (scrimmage == null)
        //             return Result.Failure("Scrimmage not found.");

        //         if (!Accessors.CanBeStarted(scrimmage))
        //             return Result.Failure("Scrimmage cannot be started.");

        //         if (!StateMachine.TryTransition(scrimmage, ScrimmageStatus.InProgress))
        //             return Result.Failure("Failed to start scrimmage.");

        //         // Create the match
        //         var match = Match.MatchCore.Factory.CreateMatch(
        //             scrimmage.Team1Id, scrimmage.Team2Id,
        //             scrimmage.Team1RosterIds, scrimmage.Team2RosterIds,
        //             scrimmage.TeamSize, scrimmage.BestOf, true);

        //         var matchResult = await _matchData.CreateAsync(match, DatabaseComponent.Repository);
        //         if (!matchResult.Success)
        //             return Result.Failure("Failed to create match for scrimmage.");

        //         scrimmage.Match = match;
        //         await _scrimmageData.UpdateAsync(scrimmage, DatabaseComponent.Repository);

        //         // Publish event
        //         await CoreService.EventBus.PublishAsync(new ScrimmageStartedEvent
        //         {
        //             ScrimmageId = scrimmageId,
        //             MatchId = match.Id,
        //             StartedAt = DateTime.UtcNow
        //         });

        //         return Result.Success();
        //     }
        //     catch (Exception ex)
        //     {
        //         await CoreService.ErrorHandler.CaptureAsync(
        //             ex, "Failed to start scrimmage", nameof(StartScrimmageAsync));
        //         return Result.Failure($"An unexpected error occurred while starting the scrimmage: {ex.Message}");
        //     }
        // }

        // TODO: Add remaining business logic methods:
        // - DeclineScrimmageAsync
        // - CancelScrimmageAsync
        // - CompleteScrimmageAsync
        // - ForfeitScrimmageAsync
        // - GetActiveScrimmagesForTeamAsync
        // - CleanupExpiredChallengesAsync
        #endregion
    }
}
