using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.ErrorHandling;
using WabbitBot.Core.Scrimmages.Data;
using WabbitBot.Core.Scrimmages.Data.Interface;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Data;
using WabbitBot.Core.Common.Services;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Attributes;

namespace WabbitBot.Core.Scrimmages
{
    /// <summary>
    /// Service for handling scrimmage-related business logic.
    /// </summary>
    [GenerateEventPublisher(EventBusType = EventBusType.Core, EnableValidation = true, EnableTimestamps = true)]
    public partial class ScrimmageService : CoreService, ICoreDataService<Scrimmage>
    {
        private readonly ScrimmageStateMachine _stateMachine;

        public ScrimmageService()
            : base(CoreEventBus.Instance, CoreErrorHandler.Instance)
        {
            _stateMachine = new ScrimmageStateMachine();
        }

        private IScrimmageRepository ScrimmageRepository =>
            WabbitBot.Core.Common.Data.DataServiceManager.ScrimmageRepository;

        private IScrimmageCache ScrimmageCache =>
            WabbitBot.Core.Common.Data.DataServiceManager.ScrimmageCache;

        private IScrimmageArchive ScrimmageArchive =>
            (IScrimmageArchive)WabbitBot.Core.Common.Data.DataServiceManager.ScrimmageArchive;

        /// <summary>
        /// Adds a new scrimmage to the state machine
        /// </summary>
        public void AddScrimmage(Scrimmage scrimmage)
        {
            _stateMachine.AddScrimmage(scrimmage);
        }

        /// <summary>
        /// Accepts a scrimmage challenge
        /// </summary>
        public async Task<bool> AcceptChallengeAsync(Guid scrimmageId, string userId)
        {
            try
            {
                var scrimmage = _stateMachine.GetCurrentScrimmage(scrimmageId);
                if (scrimmage == null)
                    return false;

                if (!_stateMachine.IsValidTransition(scrimmage.Status, ScrimmageStatus.Accepted))
                    return false;

                scrimmage.Accept();
                _stateMachine.UpdateState(scrimmageId, ScrimmageStatus.Accepted);

                // Persist to database
                await UpdateAsync(scrimmage);

                // Publish event for Discord integration
                await EventBus.PublishAsync(new ScrimmageAcceptedEvent
                {
                    ScrimmageId = scrimmageId.ToString()
                });

                return true;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return false;
            }
        }

        /// <summary>
        /// Declines a scrimmage challenge
        /// </summary>
        public async Task<bool> DeclineChallengeAsync(Guid scrimmageId, string userId)
        {
            try
            {
                var scrimmage = _stateMachine.GetCurrentScrimmage(scrimmageId);
                if (scrimmage == null)
                    return false;

                if (!_stateMachine.IsValidTransition(scrimmage.Status, ScrimmageStatus.Declined))
                    return false;

                scrimmage.Decline();
                _stateMachine.UpdateState(scrimmageId, ScrimmageStatus.Declined);

                // Persist to database
                await UpdateAsync(scrimmage);

                // Publish event for Discord integration - includes essential UX information
                await EventBus.PublishAsync(new ScrimmageDeclinedEvent
                {
                    ScrimmageId = scrimmageId.ToString(),
                    DeclinedBy = userId // Essential for Discord UX, handlers can fallback to database
                });

                return true;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return false;
            }
        }

        /// <summary>
        /// Gets active scrimmages for a team
        /// </summary>
        public System.Collections.Generic.IEnumerable<Scrimmage> GetActiveScrimmagesForTeam(string teamId)
        {
            return _stateMachine.GetActiveScrimmagesForTeam(teamId);
        }

        /// <summary>
        /// Gets expired challenges that need cleanup
        /// </summary>
        public System.Collections.Generic.IEnumerable<Scrimmage> GetExpiredChallenges()
        {
            return _stateMachine.GetExpiredChallenges();
        }

        #region ICoreDataService<Scrimmage> Implementation

        /// <summary>
        /// Gets a scrimmage by ID
        /// </summary>
        public async Task<Scrimmage?> GetByIdAsync(object id)
        {
            try
            {
                if (id is string scrimmageId)
                {
                    // Try cache first
                    var cached = await ScrimmageCache.GetScrimmageAsync(Guid.Parse(scrimmageId));
                    if (cached != null)
                        return cached;

                    // Try repository
                    return await ScrimmageRepository.GetScrimmageAsync(scrimmageId);
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
        /// Gets all scrimmages
        /// </summary>
        public async Task<IEnumerable<Scrimmage>> GetAllAsync()
        {
            try
            {
                // Try repository (no general GetAllScrimmagesAsync in cache)
                var all = await ScrimmageRepository.GetAllAsync();
                return all ?? Array.Empty<Scrimmage>();
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Array.Empty<Scrimmage>();
            }
        }

        /// <summary>
        /// Searches for scrimmages
        /// </summary>
        public async Task<IEnumerable<Scrimmage>> SearchAsync(string searchTerm, int limit = 25)
        {
            try
            {
                // For now, return all scrimmages (can be enhanced with proper search logic)
                var all = await GetAllAsync();
                return all.Take(limit);
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Array.Empty<Scrimmage>();
            }
        }

        /// <summary>
        /// Adds a new scrimmage
        /// </summary>
        public async Task<int> AddAsync(Scrimmage entity)
        {
            try
            {
                var result = await ScrimmageRepository.AddAsync(entity);
                if (result > 0)
                {
                    await ScrimmageCache.SetScrimmageAsync(entity);
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
        /// Updates an existing scrimmage
        /// </summary>
        public async Task<bool> UpdateAsync(Scrimmage entity)
        {
            try
            {
                var result = await ScrimmageRepository.UpdateAsync(entity);
                if (result)
                {
                    await ScrimmageCache.SetScrimmageAsync(entity);
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
        /// Deletes a scrimmage
        /// </summary>
        public async Task<bool> DeleteAsync(object id)
        {
            try
            {
                var result = await ScrimmageRepository.DeleteAsync(id);
                if (result)
                {
                    if (id is string scrimmageId)
                    {
                        await ScrimmageCache.RemoveScrimmageAsync(Guid.Parse(scrimmageId));
                    }
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
        /// Archives a scrimmage
        /// </summary>
        public async Task<bool> ArchiveAsync(Scrimmage entity)
        {
            try
            {
                var result = await ScrimmageArchive.ArchiveAsync(entity);
                if (result > 0)
                {
                    await ScrimmageCache.RemoveScrimmageAsync(entity.Id);
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
        /// Checks if a scrimmage exists
        /// </summary>
        public async Task<bool> ExistsAsync(object id)
        {
            try
            {
                if (id is string scrimmageId)
                {
                    // Try cache first
                    if (await ScrimmageCache.ScrimmageExistsAsync(Guid.Parse(scrimmageId)))
                        return true;

                    // Try repository
                    return await ScrimmageRepository.ExistsAsync(scrimmageId);
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
        /// Creates a new scrimmage with business logic validation
        /// </summary>
        public async Task<Result<Scrimmage>> CreateEntityAsync(Scrimmage entity)
        {
            try
            {
                // Business logic validation for scrimmage creation
                if (entity == null)
                    return Result<Scrimmage>.Failure("Scrimmage cannot be null");

                if (string.IsNullOrEmpty(entity.Id.ToString()))
                    return Result<Scrimmage>.Failure("Scrimmage ID is required");

                var result = await AddAsync(entity);
                if (result > 0)
                {
                    await PublishScrimmageCreatedEventAsync(entity);
                    return Result<Scrimmage>.CreateSuccess(entity);
                }

                return Result<Scrimmage>.Failure("Failed to create scrimmage");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Result<Scrimmage>.Failure($"Failed to create scrimmage: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates a scrimmage with business logic validation
        /// </summary>
        public async Task<Result<Scrimmage>> UpdateEntityAsync(Scrimmage entity)
        {
            try
            {
                // Business logic validation for scrimmage updates
                if (entity == null)
                    return Result<Scrimmage>.Failure("Scrimmage cannot be null");

                var result = await UpdateAsync(entity);
                if (result)
                {
                    await PublishScrimmageUpdatedEventAsync(entity);
                    return Result<Scrimmage>.CreateSuccess(entity);
                }

                return Result<Scrimmage>.Failure("Failed to update scrimmage");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Result<Scrimmage>.Failure($"Failed to update scrimmage: {ex.Message}");
            }
        }

        /// <summary>
        /// Archives a scrimmage with business logic validation
        /// </summary>
        public async Task<Result<Scrimmage>> ArchiveEntityAsync(Scrimmage entity)
        {
            try
            {
                // Business logic validation for scrimmage archiving
                if (entity == null)
                    return Result<Scrimmage>.Failure("Scrimmage cannot be null");

                // Cannot archive active scrimmages
                if (entity.Status != ScrimmageStatus.Completed &&
                    entity.Status != ScrimmageStatus.Declined &&
                    entity.Status != ScrimmageStatus.Cancelled &&
                    entity.Status != ScrimmageStatus.Forfeited)
                {
                    return Result<Scrimmage>.Failure("Cannot archive active scrimmage");
                }

                var result = await ArchiveAsync(entity);
                if (result)
                {
                    await PublishScrimmageArchivedEventAsync(entity);
                    return Result<Scrimmage>.CreateSuccess(entity);
                }

                return Result<Scrimmage>.Failure("Failed to archive scrimmage");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Result<Scrimmage>.Failure($"Failed to archive scrimmage: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a scrimmage with business logic validation
        /// </summary>
        public async Task<Result<Scrimmage>> DeleteEntityAsync(object id)
        {
            try
            {
                // Get entity before deletion for event publishing
                var entity = await GetByIdAsync(id);
                if (entity == null)
                {
                    return Result<Scrimmage>.Failure("Scrimmage not found");
                }

                // Business logic validation for scrimmage deletion
                // Only allow deletion of completed/cancelled scrimmages
                if (entity.Status != ScrimmageStatus.Completed &&
                    entity.Status != ScrimmageStatus.Cancelled &&
                    entity.Status != ScrimmageStatus.Declined &&
                    entity.Status != ScrimmageStatus.Forfeited)
                {
                    return Result<Scrimmage>.Failure("Cannot delete active scrimmage");
                }

                var result = await DeleteAsync(id);
                if (result)
                {
                    await PublishScrimmageDeletedEventAsync(entity);
                    return Result<Scrimmage>.CreateSuccess(entity);
                }

                return Result<Scrimmage>.Failure("Failed to delete scrimmage");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Result<Scrimmage>.Failure($"Failed to delete scrimmage: {ex.Message}");
            }
        }

        #region Event Publishing Methods

        /// <summary>
        /// Publishes scrimmage created event
        /// </summary>
        protected virtual async Task PublishScrimmageCreatedEventAsync(Scrimmage entity)
        {
            // Publish to event bus for cross-system notifications
            await EventBus.PublishAsync(new ScrimmageAcceptedEvent
            {
                ScrimmageId = entity.Id.ToString()
            });
        }

        /// <summary>
        /// Publishes scrimmage updated event
        /// </summary>
        protected virtual async Task PublishScrimmageUpdatedEventAsync(Scrimmage entity)
        {
            // Default implementation - can be enhanced with specific update events
            await Task.CompletedTask;
        }

        /// <summary>
        /// Publishes scrimmage archived event
        /// </summary>
        protected virtual async Task PublishScrimmageArchivedEventAsync(Scrimmage entity)
        {
            // Default implementation - can be enhanced with archive events
            await Task.CompletedTask;
        }

        /// <summary>
        /// Publishes scrimmage deleted event
        /// </summary>
        protected virtual async Task PublishScrimmageDeletedEventAsync(Scrimmage entity)
        {
            // Default implementation - can be enhanced with delete events
            await Task.CompletedTask;
        }

        #endregion

        #endregion
        #region Business
         public async Task CompleteAsync(string winnerId, int team1Score = 1, int team2Score = 0)
        {
            if (Status != ScrimmageStatus.InProgress)
                throw new InvalidOperationException("Scrimmage can only be completed when in Progress state");

            if (Match == null)
                throw new InvalidOperationException("Match must exist to complete scrimmage");

            if (winnerId != Team1Id && winnerId != Team2Id)
                throw new ArgumentException("Winner must be one of the participating teams");

            // Get current team ratings from the season system
            var team1RatingResp = await _eventBus.RequestAsync<GetTeamRatingRequest, GetTeamRatingResponse>(
                new GetTeamRatingRequest { TeamId = Team1Id });
            var team2RatingResp = await _eventBus.RequestAsync<GetTeamRatingRequest, GetTeamRatingResponse>(
                new GetTeamRatingRequest { TeamId = Team2Id });

            var team1Rating = team1RatingResp?.Rating ?? 1500;
            var team2Rating = team2RatingResp?.Rating ?? 1500;

            // Calculate confidence levels at match time using the rating calculator service
            var team1ConfidenceResp = await _eventBus.RequestAsync<CalculateConfidenceRequest, CalculateConfidenceResponse>(
                new CalculateConfidenceRequest { TeamId = Team1Id, EvenTeamFormat = EvenTeamFormat });
            var team2ConfidenceResp = await _eventBus.RequestAsync<CalculateConfidenceRequest, CalculateConfidenceResponse>(
                new CalculateConfidenceRequest { TeamId = Team2Id, EvenTeamFormat = EvenTeamFormat });

            var team1Confidence = team1ConfidenceResp?.Confidence ?? 0.0;
            var team2Confidence = team2ConfidenceResp?.Confidence ?? 0.0;

            // Calculate rating changes using the RatingCalculatorService
            var ratingChangeResp = await _eventBus.RequestAsync<CalculateRatingChangeRequest, CalculateRatingChangeResponse>(
                new CalculateRatingChangeRequest
                {
                    Team1Id = Team1Id,
                    Team2Id = Team2Id,
                    Team1Rating = team1Rating,
                    Team2Rating = team2Rating,
                    EvenTeamFormat = EvenTeamFormat,
                    Team1Score = team1Score,
                    Team2Score = team2Score
                });

            var team1RatingChange = ratingChangeResp?.Team1Change ?? 0.0;
            var team2RatingChange = ratingChangeResp?.Team2Change ?? 0.0;

            // Update scrimmage with calculated values
            CompletedAt = DateTime.UtcNow;
            WinnerId = winnerId;
            Status = ScrimmageStatus.Completed;
            Team1Rating = team1Rating;
            Team2Rating = team2Rating;
            Team1RatingChange = team1RatingChange;
            Team2RatingChange = team2RatingChange;
            Team1Confidence = team1Confidence;
            Team2Confidence = team2Confidence;
            Team1Score = team1Score;
            Team2Score = team2Score;

            // Match.Complete() moved to MatchService - will be handled by ScrimmageService

            // Publish ScrimmageCompletedEvent with minimal data for cross-project communication
            await PublishEventAsync(new ScrimmageCompletedEvent
            {
                ScrimmageId = Id,
                MatchId = Match.Id
            });
        }

        public void Cancel(string reason, string cancelledBy)
        {
            if (Status == ScrimmageStatus.Completed)
                throw new InvalidOperationException("Cannot cancel a completed scrimmage");

            Status = ScrimmageStatus.Cancelled;

            if (Match != null)
            {
                // Match.Cancel() moved to MatchService - will be handled by ScrimmageService
            }

            // Event will be published by source generator
        }

        public void Forfeit(string forfeitedTeamId, string reason)
        {
            if (Status != ScrimmageStatus.InProgress)
                throw new InvalidOperationException("Scrimmage can only be forfeited when in Progress state");

            if (Match == null)
                throw new InvalidOperationException("Match must exist to forfeit scrimmage");

            if (forfeitedTeamId != Team1Id && forfeitedTeamId != Team2Id)
                throw new ArgumentException("Forfeited team must be one of the participating teams");

            Status = ScrimmageStatus.Forfeited;
            WinnerId = forfeitedTeamId == Team1Id ? Team2Id : Team1Id;

            // Match.Forfeit() moved to MatchService - will be handled by ScrimmageService

            // Event will be published by source generator
        }

        public void AddRosterMember(string playerId, int teamNumber)
        {
            if (Status != ScrimmageStatus.Created)
                throw new InvalidOperationException("Roster members can only be added when scrimmage is in Created state");

            if (teamNumber != 1 && teamNumber != 2)
                throw new ArgumentException("Team number must be 1 or 2");

            var teamList = teamNumber == 1 ? Team1RosterIds : Team2RosterIds;
            if (!teamList.Contains(playerId))
            {
                teamList.Add(playerId);
                // Event will be published by source generator
            }
        }

        public void RemoveRosterMember(string playerId, int teamNumber)
        {
            if (Status != ScrimmageStatus.Created)
                throw new InvalidOperationException("Roster members can only be removed when scrimmage is in Created state");

            if (teamNumber != 1 && teamNumber != 2)
                throw new ArgumentException("Team number must be 1 or 2");

            var teamList = teamNumber == 1 ? Team1RosterIds : Team2RosterIds;
            if (teamList.Contains(playerId))
            {
                teamList.Remove(playerId);
                // Event will be published by source generator
            }
        }
    }
}