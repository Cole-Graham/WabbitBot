using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Common.Attributes;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Leaderboards.Data.Interface;
using WabbitBot.Core.Common.Data;
using WabbitBot.Core.Common.Services;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.Data.Interfaces;

namespace WabbitBot.Core.Leaderboards
{
    /// <summary>
    /// Marker interface for LeaderboardService extensions like SeasonService
    /// </summary>
    public interface ILeaderboardService
    {
    }

    /// <summary>
    /// Service for leaderboard business logic operations.
    /// Leaderboards are read-only views generated from Season data.
    /// </summary>
    [GenerateEventPublisher(EventBusType = EventBusType.Core, EnableValidation = true, EnableTimestamps = true)]
    public partial class LeaderboardService : CoreService, ICoreDataService<Leaderboard>, ILeaderboardService
    {
        public LeaderboardService()
        : base(CoreEventBus.Instance, CoreErrorHandler.Instance)
        {
        }

        private ILeaderboardRepository LeaderboardRepository => DataServiceManager.LeaderboardRepository;
        private ILeaderboardCache LeaderboardCache => DataServiceManager.LeaderboardCache;
        private ILeaderboardArchive LeaderboardArchive => (ILeaderboardArchive)DataServiceManager.LeaderboardArchive;
        private ISeasonRepository SeasonRepository => DataServiceManager.SeasonRepository;
        private ISeasonCache SeasonCache => DataServiceManager.SeasonCache;

        /// <summary>
        /// Gets all team ratings for a specific game size from the active season.
        /// </summary>
        private async Task<Dictionary<string, double>> GetAllTeamRatingsAsync(EvenTeamFormat evenTeamFormat)
        {
            try
            {
                var activeSeason = await GetActiveSeasonAsync(evenTeamFormat);
                if (activeSeason == null)
                {
                    return new Dictionary<string, double>();
                }

                var ratings = new Dictionary<string, double>();
                foreach (var teamEntry in activeSeason.ParticipatingTeams)
                {
                    var team = await WabbitBot.Core.Common.Data.DataServiceManager.TeamRepository.GetByIdAsync(teamEntry.Key);
                    if (team != null && team.Stats.ContainsKey(evenTeamFormat))
                    {
                        ratings[teamEntry.Key] = team.Stats[evenTeamFormat].CurrentRating;
                    }
                }
                return ratings;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return new Dictionary<string, double>();
            }
        }

        /// <summary>
        /// Gets the active season for the specified game size.
        /// Returns null if no active season exists.
        /// </summary>
        private async Task<Season?> GetActiveSeasonAsync(EvenTeamFormat evenTeamFormat)
        {
            // Try cache first
            var cachedSeason = await SeasonCache.GetActiveSeasonAsync(evenTeamFormat);
            if (cachedSeason != null)
            {
                return cachedSeason;
            }

            // Try database
            var dbSeason = await SeasonRepository.GetActiveSeasonAsync(evenTeamFormat);
            if (dbSeason != null)
            {
                await SeasonCache.SetActiveSeasonAsync(dbSeason);
                return dbSeason;
            }

            // No active season found
            return null;
        }

        /// <summary>
        /// Refreshes the leaderboard for a specific game size from Season data.
        /// </summary>
        public async Task RefreshLeaderboardAsync(EvenTeamFormat evenTeamFormat)
        {
            try
            {
                // Get all team ratings directly from the database
                var teamRatings = await GetAllTeamRatingsAsync(evenTeamFormat);

                // Create leaderboard entries
                var rankings = new Dictionary<string, LeaderboardItem>();
                foreach (var (teamId, rating) in teamRatings)
                {
                    rankings[teamId] = new LeaderboardItem
                    {
                        Name = teamId,
                        Rating = rating,
                        IsTeam = true,
                        LastUpdated = DateTime.UtcNow
                    };
                }

                // Get or create leaderboard
                var leaderboards = await LeaderboardRepository.GetLeaderboardsByEvenTeamFormatAsync(evenTeamFormat);
                var leaderboard = leaderboards.FirstOrDefault();

                if (leaderboard == null)
                {
                    leaderboard = new Leaderboard();
                }

                // Update rankings
                leaderboard.Rankings[evenTeamFormat] = rankings;

                // Save to database
                if (leaderboard.Id == Guid.Empty)
                {
                    await LeaderboardRepository.AddAsync(leaderboard);
                }
                else
                {
                    await LeaderboardRepository.UpdateAsync(leaderboard);
                }

                Console.WriteLine($"Leaderboard refreshed for {evenTeamFormat}: {rankings.Count} teams");

                // Publish success event
                await PublishLeaderboardRefreshed(evenTeamFormat, rankings.Count);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing leaderboard for {evenTeamFormat}: {ex.Message}");

                // Publish failure event
                await PublishLeaderboardRefreshFailed(evenTeamFormat, ex.Message);
            }
        }

        /// <summary>
        /// Manually triggers a leaderboard refresh for all game sizes.
        /// </summary>
        public async Task RefreshAllLeaderboardsAsync()
        {
            int totalTeamsProcessed = 0;

            foreach (EvenTeamFormat evenTeamFormat in Enum.GetValues(typeof(EvenTeamFormat)))
            {
                if (evenTeamFormat != EvenTeamFormat.OneVOne) // Teams don't participate in 1v1
                {
                    try
                    {
                        await RefreshLeaderboardAsync(evenTeamFormat);
                        // Note: We can't easily track teams processed here since RefreshLeaderboardAsync
                        // doesn't return the count. This is a limitation of the current design.
                    }
                    catch (Exception)
                    {
                        // Errors are already handled in RefreshLeaderboardAsync
                    }
                }
            }

            // Publish event for all leaderboards refreshed
            // Note: totalTeamsProcessed is approximate since we can't get individual counts
            await PublishAllLeaderboardsRefreshed(totalTeamsProcessed);
        }

        /// <summary>
        /// Gets the current leaderboard for a specific game size.
        /// </summary>
        public async Task<Leaderboard?> GetLeaderboardAsync(EvenTeamFormat evenTeamFormat)
        {
            var leaderboards = await LeaderboardRepository.GetLeaderboardsByEvenTeamFormatAsync(evenTeamFormat);
            return leaderboards.FirstOrDefault();
        }

        /// <summary>
        /// Gets top rankings for a specific game size.
        /// </summary>
        public async Task<IEnumerable<LeaderboardItem>> GetTopRankingsAsync(EvenTeamFormat evenTeamFormat, int count = 10)
        {
            return await LeaderboardRepository.GetTopRankingsAsync(evenTeamFormat, count);
        }

        /// <summary>
        /// Gets rankings for a specific team.
        /// </summary>
        public async Task<IEnumerable<LeaderboardItem>> GetTeamRankingsAsync(string teamId, EvenTeamFormat evenTeamFormat)
        {
            return await LeaderboardRepository.GetRankingsByTeamIdAsync(teamId, evenTeamFormat);
        }

        /// <summary>
        /// Publishes a leaderboard refreshed event.
        /// </summary>
        private async Task PublishLeaderboardRefreshed(EvenTeamFormat evenTeamFormat, int teamCount)
        {
            var evt = new LeaderboardRefreshedEvent(evenTeamFormat, teamCount);
            await EventBus.PublishAsync(evt);
        }

        /// <summary>
        /// Publishes a leaderboard refresh failed event.
        /// </summary>
        private async Task PublishLeaderboardRefreshFailed(EvenTeamFormat evenTeamFormat, string errorMessage)
        {
            var evt = new LeaderboardRefreshFailedEvent(evenTeamFormat, errorMessage);
            await EventBus.PublishAsync(evt);
        }

        /// <summary>
        /// Publishes an all leaderboards refreshed event.
        /// </summary>
        private async Task PublishAllLeaderboardsRefreshed(int totalTeamsProcessed)
        {
            var evt = new AllLeaderboardsRefreshedEvent(totalTeamsProcessed);
            await EventBus.PublishAsync(evt);
        }

        #region ICoreDataService<Leaderboard> Implementation

        /// <summary>
        /// Gets a leaderboard by ID
        /// </summary>
        public async Task<Leaderboard?> GetByIdAsync(object id)
        {
            try
            {
                if (id is string leaderboardId && Guid.TryParse(leaderboardId, out var leaderboardGuid))
                {
                    // Try cache first
                    var cached = await LeaderboardCache.GetLeaderboardAsync(leaderboardGuid);
                    if (cached != null)
                        return cached;

                    // Try repository
                    return await LeaderboardRepository.GetByIdAsync(id);
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
        /// Gets all leaderboards
        /// </summary>
        public async Task<IEnumerable<Leaderboard>> GetAllAsync()
        {
            try
            {
                // Get from repository (cache doesn't have GetAll method)
                var all = await LeaderboardRepository.GetAllAsync();
                return all ?? Array.Empty<Leaderboard>();
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Array.Empty<Leaderboard>();
            }
        }

        /// <summary>
        /// Searches for leaderboards
        /// </summary>
        public async Task<IEnumerable<Leaderboard>> SearchAsync(string searchTerm, int limit = 25)
        {
            try
            {
                // For now, return all leaderboards (can be enhanced with proper search logic)
                var all = await GetAllAsync();
                return all.Take(limit);
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Array.Empty<Leaderboard>();
            }
        }

        /// <summary>
        /// Adds a new leaderboard
        /// </summary>
        public async Task<int> AddAsync(Leaderboard entity)
        {
            try
            {
                var result = await LeaderboardRepository.AddAsync(entity);
                if (result > 0)
                {
                    await LeaderboardCache.SetLeaderboardAsync(entity);
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
        /// Updates an existing leaderboard
        /// </summary>
        public async Task<bool> UpdateAsync(Leaderboard entity)
        {
            try
            {
                var result = await LeaderboardRepository.UpdateAsync(entity);
                if (result)
                {
                    await LeaderboardCache.SetLeaderboardAsync(entity);
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
        /// Deletes a leaderboard
        /// </summary>
        public async Task<bool> DeleteAsync(object id)
        {
            try
            {
                var result = await LeaderboardRepository.DeleteAsync(id);
                if (result && id is string leaderboardId && Guid.TryParse(leaderboardId, out var leaderboardGuid))
                {
                    await LeaderboardCache.RemoveLeaderboardAsync(leaderboardGuid);
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
        /// Archives a leaderboard
        /// </summary>
        public async Task<bool> ArchiveAsync(Leaderboard entity)
        {
            try
            {
                var result = await LeaderboardArchive.ArchiveAsync(entity);
                if (result > 0)
                {
                    await LeaderboardCache.RemoveLeaderboardAsync(entity.Id);
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
        /// Checks if a leaderboard exists
        /// </summary>
        public async Task<bool> ExistsAsync(object id)
        {
            try
            {
                if (id is string leaderboardId && Guid.TryParse(leaderboardId, out var leaderboardGuid))
                {
                    // Try cache first
                    if (await LeaderboardCache.LeaderboardExistsAsync(leaderboardGuid))
                        return true;

                    // Try repository
                    return await LeaderboardRepository.ExistsAsync(id);
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
        /// Creates a new leaderboard with business logic validation
        /// </summary>
        public async Task<Result<Leaderboard>> CreateEntityAsync(Leaderboard entity)
        {
            try
            {
                // Business logic validation for leaderboard creation
                if (entity == null)
                    return Result<Leaderboard>.Failure("Leaderboard cannot be null");

                if (string.IsNullOrEmpty(entity.Id.ToString()))
                    return Result<Leaderboard>.Failure("Leaderboard ID is required");

                var result = await AddAsync(entity);
                if (result > 0)
                {
                    await PublishLeaderboardCreatedEventAsync(entity);
                    return Result<Leaderboard>.CreateSuccess(entity);
                }

                return Result<Leaderboard>.Failure("Failed to create leaderboard");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Result<Leaderboard>.Failure($"Failed to create leaderboard: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates a leaderboard with business logic validation
        /// </summary>
        public async Task<Result<Leaderboard>> UpdateEntityAsync(Leaderboard entity)
        {
            try
            {
                // Business logic validation for leaderboard updates
                if (entity == null)
                    return Result<Leaderboard>.Failure("Leaderboard cannot be null");

                var result = await UpdateAsync(entity);
                if (result)
                {
                    await PublishLeaderboardUpdatedEventAsync(entity);
                    return Result<Leaderboard>.CreateSuccess(entity);
                }

                return Result<Leaderboard>.Failure("Failed to update leaderboard");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Result<Leaderboard>.Failure($"Failed to update leaderboard: {ex.Message}");
            }
        }

        /// <summary>
        /// Archives a leaderboard with business logic validation
        /// </summary>
        public async Task<Result<Leaderboard>> ArchiveEntityAsync(Leaderboard entity)
        {
            try
            {
                // Business logic validation for leaderboard archiving
                if (entity == null)
                    return Result<Leaderboard>.Failure("Leaderboard cannot be null");

                var result = await ArchiveAsync(entity);
                if (result)
                {
                    await PublishLeaderboardArchivedEventAsync(entity);
                    return Result<Leaderboard>.CreateSuccess(entity);
                }

                return Result<Leaderboard>.Failure("Failed to archive leaderboard");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Result<Leaderboard>.Failure($"Failed to archive leaderboard: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a leaderboard with business logic validation
        /// </summary>
        public async Task<Result<Leaderboard>> DeleteEntityAsync(object id)
        {
            try
            {
                // Get entity before deletion for event publishing
                var entity = await GetByIdAsync(id);
                if (entity == null)
                {
                    return Result<Leaderboard>.Failure("Leaderboard not found");
                }

                var result = await DeleteAsync(id);
                if (result)
                {
                    await PublishLeaderboardDeletedEventAsync(entity);
                    return Result<Leaderboard>.CreateSuccess(entity);
                }

                return Result<Leaderboard>.Failure("Failed to delete leaderboard");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Result<Leaderboard>.Failure($"Failed to delete leaderboard: {ex.Message}");
            }
        }

        #region Event Publishing Methods

        /// <summary>
        /// Publishes leaderboard created event
        /// </summary>
        protected virtual async Task PublishLeaderboardCreatedEventAsync(Leaderboard entity)
        {
            // Publish to event bus for cross-system notifications
            await Task.CompletedTask; // Placeholder for generated event publisher
        }

        /// <summary>
        /// Publishes leaderboard updated event
        /// </summary>
        protected virtual async Task PublishLeaderboardUpdatedEventAsync(Leaderboard entity)
        {
            // Default implementation - can be enhanced with specific update events
            await Task.CompletedTask;
        }

        /// <summary>
        /// Publishes leaderboard archived event
        /// </summary>
        protected virtual async Task PublishLeaderboardArchivedEventAsync(Leaderboard entity)
        {
            // Default implementation - can be enhanced with archive events
            await Task.CompletedTask;
        }

        /// <summary>
        /// Publishes leaderboard deleted event
        /// </summary>
        protected virtual async Task PublishLeaderboardDeletedEventAsync(Leaderboard entity)
        {
            // Default implementation - can be enhanced with delete events
            await Task.CompletedTask;
        }

        #endregion

        #endregion
    }
}
