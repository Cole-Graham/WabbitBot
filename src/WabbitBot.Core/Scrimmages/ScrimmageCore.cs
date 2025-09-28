using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Services;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.ErrorService;
using WabbitBot.Common.Models;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Database;
using WabbitBot.Core.Common.Interfaces;
using Microsoft.VisualBasic;

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
                Guid team1Id,
                Guid team2Id,
                List<Guid> team1RosterIds,
                List<Guid> team2RosterIds,
                TeamSize teamSize,
                int bestOf = 1)
            {
                var scrimmage = new Scrimmage
                {
                    Team1Id = team1Id,
                    Team2Id = team2Id,
                    Team1RosterIds = team1RosterIds,
                    Team2RosterIds = team2RosterIds,
                    TeamSize = teamSize,
                    BestOf = bestOf,
                    ChallengeExpiresAt = DateTime.UtcNow.AddHours(24), // 24 hour challenge window
                };
                return scrimmage;
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
                [ScrimmageStatus.Created] = new() {
                     ScrimmageStatus.Accepted, ScrimmageStatus.Declined, ScrimmageStatus.Cancelled },
                [ScrimmageStatus.Accepted] = new() {
                     ScrimmageStatus.InProgress, ScrimmageStatus.Cancelled },
                [ScrimmageStatus.InProgress] = new() {
                     ScrimmageStatus.Completed, ScrimmageStatus.Cancelled, ScrimmageStatus.Forfeited },
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
            // ------------------------ Validation Logic -----------------------------------
            // No validation needed for TeamSize as it's a simple enum
            // Future: Add business rule validations here
        }
        #endregion

        #region Core Logic
        /// <summary>
        /// Creates a new scrimmage challenge
        /// </summary>
        public async Task<Result<Scrimmage>> CreateScrimmageAsync(
            Guid challengerTeamId,
            Guid opponentTeamId,
            List<Guid> challengerRosterIds,
            List<Guid> opponentRosterIds,
            TeamSize teamSize,
            int bestOf = 1)
        {
            try
            {
                var scrimmage = Factory.CreateScrimmage(
                    challengerTeamId, opponentTeamId, challengerRosterIds,
                    opponentRosterIds, teamSize, bestOf);

                var result = await CoreService.Scrimmages.CreateAsync(scrimmage,
                    DatabaseComponent.Repository);
                if (!result.Success)
                    return Result<Scrimmage>.Failure("Failed to create scrimmage challenge.");

                // TODO: Publish event for Discord integration
                // await EventBus.PublishAsync(new NewScrimmageReadyEvent
                // {
                //     ScrimmageId = scrimmage.Id,
                //     ChallengerTeamId = challengerTeamId,
                //     OpponentTeamId = opponentTeamId,
                //     CreatedAt = scrimmage.CreatedAt
                // });

                return Result<Scrimmage>.CreateSuccess(scrimmage);
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    ex, "Failed to create scrimmage", nameof(CreateScrimmageAsync));
                return Result<Scrimmage>.Failure(
                    $"An unexpected error occurred while creating the scrimmage: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Accepts a scrimmage challenge
        /// </summary>
        public async Task<Result> AcceptScrimmageAsync(Guid scrimmageId)
        {
            try
            {
                var scrimmageResult = await CoreService.Scrimmages.GetByIdAsync(scrimmageId,
                    DatabaseComponent.Repository);
                if (!scrimmageResult.Success)
                {
                    return Result.Failure(
                        $"Failed to retrieve scrimmage: {scrimmageResult.ErrorMessage}"
                    );
                }
                var scrimmage = scrimmageResult.Data;

                if (scrimmage == null)
                    return Result.Failure("Scrimmage not found.");

                if (!Accessors.IsTeamMatch(scrimmage))
                    return Result.Failure("Scrimmage cannot be accepted.");

                var currentState = scrimmage.StateHistory.Last();
                if (!State.IsValidTransition(currentState.Status, ScrimmageStatus.Accepted))
                    return Result.Failure("Failed to accept scrimmage.");

                var updateResult = await CoreService.Scrimmages.UpdateAsync(scrimmage,
                    DatabaseComponent.Repository);
                if (!updateResult.Success)
                {
                    return Result.Failure(
                        $"Failed to update scrimmage: {updateResult.ErrorMessage}"
                    );
                }

                // Publish event
                await CoreService.PublishAsync(new ScrimmageAcceptedEvent
                {
                    ScrimmageId = scrimmageId,
                    AcceptedAt = DateTime.UtcNow,
                });

                return Result.CreateSuccess();
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    ex, "Failed to accept scrimmage", nameof(AcceptScrimmageAsync));
                return Result.Failure(
                    $"An unexpected error occurred while accepting the scrimmage: " +
                    $"{ex.Message}"
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
