using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Common.ErrorHandling;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Tournaments.Data;
using WabbitBot.Core.Tournaments.Data.Interface;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Services;
using WabbitBot.Core.Common;
using WabbitBot.Common.Data.Interfaces;

namespace WabbitBot.Core.Tournaments
{
    /// <summary>
    /// Service for managing basic tournament business logic and state transitions
    /// </summary>
    [GenerateEventPublisher(EventBusType = EventBusType.Core, EnableValidation = true, EnableTimestamps = true)]
    public partial class TournamentService : CoreService, ICoreDataService<Tournament>
    {
        private readonly TournamentStateMachine _stateMachine;

        public TournamentService()
            : base(CoreEventBus.Instance, CoreErrorHandler.Instance)
        {
            _stateMachine = new TournamentStateMachine(CoreEventBus.Instance);
        }

        private ITournamentRepository TournamentRepository =>
            WabbitBot.Core.Common.Data.DataServiceManager.TournamentRepository;

        private ITournamentCache TournamentCache =>
            WabbitBot.Core.Common.Data.DataServiceManager.TournamentCache;

        private ITournamentArchive TournamentArchive =>
            (ITournamentArchive)WabbitBot.Core.Common.Data.DataServiceManager.TournamentArchive;

        #region Tournament Business Logic

        /// <summary>
        /// Creates a new tournament entity with basic validation
        /// </summary>
        public async Task<Result<Tournament>> CreateTournamentAsync(
            string name,
            string description,
            EvenTeamFormat evenTeamFormat,
            DateTime startDate,
            int maxParticipants,
            int bestOf)
        {
            try
            {
                // Basic business logic validation using Common validation methods
                var nameValidation = CoreValidation.ValidateString(name, "Tournament name", required: true);
                if (!nameValidation.Success)
                    return Result<Tournament>.Failure(nameValidation.ErrorMessage ?? "Invalid tournament name");

                var maxParticipantsValidation = CoreValidation.ValidateNumber(maxParticipants, "Max participants", minValue: 2);
                if (!maxParticipantsValidation.Success)
                    return Result<Tournament>.Failure(maxParticipantsValidation.ErrorMessage ?? "Invalid max participants");

                var bestOfValidation = CoreValidation.ValidateNumber(bestOf, "BestOf", minValue: 1);
                if (!bestOfValidation.Success)
                    return Result<Tournament>.Failure(bestOfValidation.ErrorMessage ?? "Invalid BestOf value");

                var tournament = new Tournament
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = description,
                    EvenTeamFormat = evenTeamFormat,
                    StartDate = startDate,
                    MaxParticipants = maxParticipants,
                    BestOf = bestOf
                };

                // Save to repository
                await TournamentRepository.AddAsync(tournament);

                return Result<Tournament>.CreateSuccess(tournament);
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex, $"Failed to create tournament: {name}");
                return Result<Tournament>.Failure($"Failed to create tournament: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates tournament properties with basic validation
        /// </summary>
        public async Task<Result<Tournament>> UpdateTournamentAsync(
            Guid tournamentId,
            string? name = null,
            string? description = null,
            DateTime? startDate = null,
            int? maxParticipants = null,
            int? bestOf = null)
        {
            try
            {
                var tournament = await TournamentRepository.GetByIdAsync(tournamentId.ToString());
                if (tournament == null)
                    return Result<Tournament>.Failure("Tournament not found");

                // Basic business logic validation using Common validation methods
                if (name != null)
                {
                    var nameValidation = CoreValidation.ValidateString(name, "Tournament name", required: true);
                    if (!nameValidation.Success)
                        return Result<Tournament>.Failure(nameValidation.ErrorMessage ?? "Invalid tournament name");
                }

                if (maxParticipants.HasValue)
                {
                    var maxParticipantsValidation = CoreValidation.ValidateNumber(maxParticipants.Value, "Max participants", minValue: 2);
                    if (!maxParticipantsValidation.Success)
                        return Result<Tournament>.Failure(maxParticipantsValidation.ErrorMessage ?? "Invalid max participants");
                }

                if (bestOf.HasValue)
                {
                    var bestOfValidation = CoreValidation.ValidateNumber(bestOf.Value, "BestOf", minValue: 1);
                    if (!bestOfValidation.Success)
                        return Result<Tournament>.Failure(bestOfValidation.ErrorMessage ?? "Invalid BestOf value");
                }

                // Update properties
                if (name != null) tournament.Name = name;
                if (description != null) tournament.Description = description;
                if (startDate.HasValue) tournament.StartDate = startDate.Value;
                if (maxParticipants.HasValue) tournament.MaxParticipants = maxParticipants.Value;
                if (bestOf.HasValue) tournament.BestOf = bestOf.Value;

                tournament.UpdatedAt = DateTime.UtcNow;

                await TournamentRepository.UpdateAsync(tournament);

                return Result<Tournament>.CreateSuccess(tournament);
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex, $"Failed to update tournament: {tournamentId}");
                return Result<Tournament>.Failure($"Failed to update tournament: {ex.Message}");
            }
        }

        #endregion

        #region Tournament State Management

        /// <summary>
        /// Updates tournament state snapshot (basic state management)
        /// </summary>
        public async Task<Result<Tournament>> UpdateTournamentStateAsync(
            Guid tournamentId,
            TournamentStateSnapshot newState)
        {
            try
            {
                var tournament = await TournamentRepository.GetByIdAsync(tournamentId.ToString());
                if (tournament == null)
                    return Result<Tournament>.Failure("Tournament not found");

                // Basic state transition validation
                if (!IsValidStateTransition(tournament.CurrentStateSnapshot, newState))
                    return Result<Tournament>.Failure($"Invalid state transition from {tournament.CurrentStateSnapshot?.GetType().Name} to {newState.GetType().Name}");

                newState.Timestamp = DateTime.UtcNow;
                tournament.CurrentStateSnapshot = newState;
                tournament.StateHistory.Add(newState);
                tournament.UpdatedAt = DateTime.UtcNow;

                await TournamentRepository.UpdateAsync(tournament);

                return Result<Tournament>.CreateSuccess(tournament);
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex, $"Failed to update tournament state: {tournamentId}");
                return Result<Tournament>.Failure($"Failed to update tournament state: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates state transitions (basic business logic)
        /// </summary>
        private bool IsValidStateTransition(TournamentStateSnapshot? from, TournamentStateSnapshot to)
        {
            var fromType = from?.GetType().Name ?? "None";
            var toType = to.GetType().Name;

            return (fromType, toType) switch
            {
                ("None", "TournamentCreated") => true,
                ("TournamentCreated", "TournamentRegistration") => true,
                ("TournamentRegistration", "TournamentInProgress") => true,
                ("TournamentInProgress", "TournamentCompleted") => true,
                (_, "TournamentCancelled") => true,
                _ => false
            };
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets tournaments by state
        /// </summary>
        public async Task<IEnumerable<Tournament>> GetTournamentsByStateAsync(TournamentState state)
        {
            var allTournaments = await TournamentRepository.GetAllAsync();
            return allTournaments.Where(t => t.CurrentState == state);
        }

        /// <summary>
        /// Gets active tournaments (in progress)
        /// </summary>
        public async Task<IEnumerable<Tournament>> GetActiveTournamentsAsync()
        {
            return await GetTournamentsByStateAsync(TournamentState.InProgress);
        }

        /// <summary>
        /// Gets tournaments open for registration
        /// </summary>
        public async Task<IEnumerable<Tournament>> GetRegistrationOpenTournamentsAsync()
        {
            return await GetTournamentsByStateAsync(TournamentState.Registration);
        }

        /// <summary>
        /// Gets completed tournaments
        /// </summary>
        public async Task<IEnumerable<Tournament>> GetCompletedTournamentsAsync()
        {
            return await GetTournamentsByStateAsync(TournamentState.Completed);
        }

        #endregion

        #region ICoreDataService<Tournament> Implementation

        /// <summary>
        /// Gets a tournament by ID
        /// </summary>
        public async Task<Tournament?> GetByIdAsync(object id)
        {
            try
            {
                if (id is string tournamentId && Guid.TryParse(tournamentId, out var tournamentGuid))
                {
                    // Try cache first
                    var cached = await TournamentCache.GetTournamentAsync(tournamentGuid);
                    if (cached != null)
                        return cached;

                    // Try repository
                    return await TournamentRepository.GetByIdAsync(id);
                }
                return null;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return null;
            }
        }

        /// <summary>
        /// Gets all tournaments
        /// </summary>
        public async Task<IEnumerable<Tournament>> GetAllAsync()
        {
            try
            {
                // Get from repository (cache doesn't have GetAll method)
                var all = await TournamentRepository.GetAllAsync();
                return all ?? Array.Empty<Tournament>();
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Array.Empty<Tournament>();
            }
        }

        /// <summary>
        /// Searches for tournaments
        /// </summary>
        public async Task<IEnumerable<Tournament>> SearchAsync(string searchTerm, int limit = 25)
        {
            try
            {
                // For now, return all tournaments (can be enhanced with proper search logic)
                var all = await GetAllAsync();
                return all.Take(limit);
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Array.Empty<Tournament>();
            }
        }

        /// <summary>
        /// Adds a new tournament
        /// </summary>
        public async Task<int> AddAsync(Tournament entity)
        {
            try
            {
                var result = await TournamentRepository.AddAsync(entity);
                if (result > 0)
                {
                    await TournamentCache.SetTournamentAsync(entity);
                }
                return result;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return 0;
            }
        }

        /// <summary>
        /// Updates an existing tournament
        /// </summary>
        public async Task<bool> UpdateAsync(Tournament entity)
        {
            try
            {
                var result = await TournamentRepository.UpdateAsync(entity);
                if (result)
                {
                    await TournamentCache.SetTournamentAsync(entity);
                }
                return result;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return false;
            }
        }

        /// <summary>
        /// Deletes a tournament
        /// </summary>
        public async Task<bool> DeleteAsync(object id)
        {
            try
            {
                var result = await TournamentRepository.DeleteAsync(id);
                if (result && id is string tournamentId && Guid.TryParse(tournamentId, out var tournamentGuid))
                {
                    await TournamentCache.RemoveTournamentAsync(tournamentGuid);
                }
                return result;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return false;
            }
        }

        /// <summary>
        /// Archives a tournament
        /// </summary>
        public async Task<bool> ArchiveAsync(Tournament entity)
        {
            try
            {
                var result = await TournamentArchive.ArchiveAsync(entity);
                if (result > 0)
                {
                    await TournamentCache.RemoveTournamentAsync(entity.Id);
                }
                return result > 0;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return false;
            }
        }

        /// <summary>
        /// Checks if a tournament exists
        /// </summary>
        public async Task<bool> ExistsAsync(object id)
        {
            try
            {
                if (id is string tournamentId && Guid.TryParse(tournamentId, out var tournamentGuid))
                {
                    // Try cache first
                    if (await TournamentCache.TournamentExistsAsync(tournamentGuid))
                        return true;

                    // Try repository
                    return await TournamentRepository.ExistsAsync(id);
                }
                return false;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return false;
            }
        }

        /// <summary>
        /// Creates a new tournament with business logic validation
        /// </summary>
        public async Task<Result<Tournament>> CreateEntityAsync(Tournament entity)
        {
            try
            {
                // Business logic validation for tournament creation
                if (entity == null)
                    return Result<Tournament>.Failure("Tournament cannot be null");

                if (string.IsNullOrEmpty(entity.Id.ToString()))
                    return Result<Tournament>.Failure("Tournament ID is required");

                var result = await AddAsync(entity);
                if (result > 0)
                {
                    await PublishTournamentCreatedEventAsync(entity);
                    return Result<Tournament>.CreateSuccess(entity);
                }

                return Result<Tournament>.Failure("Failed to create tournament");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Result<Tournament>.Failure($"Failed to create tournament: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates a tournament with business logic validation
        /// </summary>
        public async Task<Result<Tournament>> UpdateEntityAsync(Tournament entity)
        {
            try
            {
                // Business logic validation for tournament updates
                if (entity == null)
                    return Result<Tournament>.Failure("Tournament cannot be null");

                var result = await UpdateAsync(entity);
                if (result)
                {
                    await PublishTournamentUpdatedEventAsync(entity);
                    return Result<Tournament>.CreateSuccess(entity);
                }

                return Result<Tournament>.Failure("Failed to update tournament");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Result<Tournament>.Failure($"Failed to update tournament: {ex.Message}");
            }
        }

        /// <summary>
        /// Archives a tournament with business logic validation
        /// </summary>
        public async Task<Result<Tournament>> ArchiveEntityAsync(Tournament entity)
        {
            try
            {
                // Business logic validation for tournament archiving
                if (entity == null)
                    return Result<Tournament>.Failure("Tournament cannot be null");

                // Cannot archive active tournaments
                if (entity.CurrentState != TournamentState.Completed &&
                    entity.CurrentState != TournamentState.Cancelled)
                {
                    return Result<Tournament>.Failure("Cannot archive active tournament");
                }

                var result = await ArchiveAsync(entity);
                if (result)
                {
                    await PublishTournamentArchivedEventAsync(entity);
                    return Result<Tournament>.CreateSuccess(entity);
                }

                return Result<Tournament>.Failure("Failed to archive tournament");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Result<Tournament>.Failure($"Failed to archive tournament: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a tournament with business logic validation
        /// </summary>
        public async Task<Result<Tournament>> DeleteEntityAsync(object id)
        {
            try
            {
                // Get entity before deletion for event publishing
                var entity = await GetByIdAsync(id);
                if (entity == null)
                {
                    return Result<Tournament>.Failure("Tournament not found");
                }

                // Business logic validation for tournament deletion
                // Only allow deletion of completed/cancelled tournaments
                if (entity.CurrentState != TournamentState.Completed &&
                    entity.CurrentState != TournamentState.Cancelled)
                {
                    return Result<Tournament>.Failure("Cannot delete active tournament");
                }

                var result = await DeleteAsync(id);
                if (result)
                {
                    await PublishTournamentDeletedEventAsync(entity);
                    return Result<Tournament>.CreateSuccess(entity);
                }

                return Result<Tournament>.Failure("Failed to delete tournament");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Result<Tournament>.Failure($"Failed to delete tournament: {ex.Message}");
            }
        }

        #region Event Publishing Methods

        /// <summary>
        /// Publishes tournament created event
        /// </summary>
        protected virtual async Task PublishTournamentCreatedEventAsync(Tournament entity)
        {
            // Publish to event bus for cross-system notifications
            await Task.CompletedTask; // Placeholder for generated event publisher
        }

        /// <summary>
        /// Publishes tournament updated event
        /// </summary>
        protected virtual async Task PublishTournamentUpdatedEventAsync(Tournament entity)
        {
            // Default implementation - can be enhanced with specific update events
            await Task.CompletedTask;
        }

        /// <summary>
        /// Publishes tournament archived event
        /// </summary>
        protected virtual async Task PublishTournamentArchivedEventAsync(Tournament entity)
        {
            // Default implementation - can be enhanced with archive events
            await Task.CompletedTask;
        }

        /// <summary>
        /// Publishes tournament deleted event
        /// </summary>
        protected virtual async Task PublishTournamentDeletedEventAsync(Tournament entity)
        {
            // Default implementation - can be enhanced with delete events
            await Task.CompletedTask;
        }

        #endregion

        #endregion
    }
}
