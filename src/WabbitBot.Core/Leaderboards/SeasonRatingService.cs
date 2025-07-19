using System;
using System.Threading.Tasks;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.ErrorHandling;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Core.Leaderboards.Data.Interface;
using System.Collections.Generic; // Added for Dictionary
using System.Linq; // Added for ToDictionary

namespace WabbitBot.Core.Leaderboards
{
    /// <summary>
    /// Centralized service for all team rating operations using Season as the source of truth.
    /// This service ensures all rating updates go through the Season system.
    /// </summary>
    public class SeasonRatingService
    {
        private readonly ICoreEventBus _eventBus;
        private readonly ICoreErrorHandler _errorHandler;
        private readonly ISeasonRepository _seasonRepo;
        private readonly ISeasonCache _seasonCache;

        public SeasonRatingService(
            ICoreEventBus eventBus,
            ICoreErrorHandler errorHandler,
            ISeasonRepository seasonRepo,
            ISeasonCache seasonCache)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _seasonRepo = seasonRepo ?? throw new ArgumentNullException(nameof(seasonRepo));
            _seasonCache = seasonCache ?? throw new ArgumentNullException(nameof(seasonCache));
        }

        /// <summary>
        /// Gets the current rating for a team in a specific game size.
        /// </summary>
        public async Task<int> GetTeamRatingAsync(string teamId, GameSize gameSize)
        {
            try
            {
                var activeSeason = await GetActiveSeasonAsync();
                if (activeSeason == null)
                {
                    return Leaderboard.InitialRating; // Default rating if no active season
                }

                if (!activeSeason.TeamStats.ContainsKey(gameSize) ||
                    !activeSeason.TeamStats[gameSize].ContainsKey(teamId))
                {
                    return Leaderboard.InitialRating; // Default rating for new teams
                }

                return activeSeason.TeamStats[gameSize][teamId].CurrentRating;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleError(ex);
                return Leaderboard.InitialRating;
            }
        }

        /// <summary>
        /// Updates a team's rating through the Season system.
        /// </summary>
        public async Task UpdateTeamRatingAsync(string teamId, GameSize gameSize, int newRating, string reason = "")
        {
            try
            {
                var activeSeason = await GetActiveSeasonAsync();
                if (activeSeason == null)
                {
                    throw new InvalidOperationException("No active season found for rating update");
                }

                // Ensure the team exists in the season
                if (!activeSeason.TeamStats[gameSize].ContainsKey(teamId))
                {
                    activeSeason.TeamStats[gameSize][teamId] = new SeasonTeamStats
                    {
                        TeamId = teamId,
                        GameSize = gameSize,
                        InitialRating = Leaderboard.InitialRating,
                        CurrentRating = Leaderboard.InitialRating,
                        LastUpdated = DateTime.UtcNow
                    };
                }

                // Update the rating
                var stats = activeSeason.TeamStats[gameSize][teamId];
                var oldRating = stats.CurrentRating;
                stats.CurrentRating = newRating;
                stats.LastUpdated = DateTime.UtcNow;

                // Save the updated season
                await _seasonRepo.UpdateAsync(activeSeason);
                await _seasonCache.SetSeasonAsync(activeSeason);
                await _seasonCache.SetActiveSeasonAsync(activeSeason);

                // Publish event for other systems to react (e.g., leaderboard refresh)
                await _eventBus.PublishAsync(new TeamRatingUpdatedEvent
                {
                    TeamId = teamId,
                    GameSize = gameSize,
                    OldRating = oldRating,
                    NewRating = newRating,
                    Reason = reason
                });

                Console.WriteLine($"Team rating updated: {teamId} ({gameSize}) {oldRating} -> {newRating} ({reason})");
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleError(ex);
                throw;
            }
        }

        /// <summary>
        /// Applies a rating change (delta) to a team's current rating.
        /// </summary>
        public async Task ApplyRatingChangeAsync(string teamId, GameSize gameSize, int ratingChange, string reason = "")
        {
            var currentRating = await GetTeamRatingAsync(teamId, gameSize);
            var newRating = currentRating + ratingChange;
            await UpdateTeamRatingAsync(teamId, gameSize, newRating, reason);
        }

        /// <summary>
        /// Gets the active season, creating one if none exists.
        /// </summary>
        private async Task<Season> GetActiveSeasonAsync()
        {
            // Try cache first
            var cachedSeason = await _seasonCache.GetActiveSeasonAsync();
            if (cachedSeason != null)
            {
                return cachedSeason;
            }

            // Try database
            var dbSeason = await _seasonRepo.GetActiveSeasonAsync();
            if (dbSeason != null)
            {
                await _seasonCache.SetActiveSeasonAsync(dbSeason);
                return dbSeason;
            }

            // Create new active season if none exists
            var newSeason = Season.Create(
                "Default Season",
                DateTime.UtcNow,
                DateTime.UtcNow.AddYears(1),
                new SeasonConfig
                {
                    RatingDecayEnabled = false,
                    DecayRatePerWeek = 0,
                    MinimumRating = 1000
                }
            );

            await _seasonRepo.AddAsync(newSeason);
            await _seasonCache.SetSeasonAsync(newSeason);
            await _seasonCache.SetActiveSeasonAsync(newSeason);

            return newSeason;
        }

        /// <summary>
        /// Gets all team ratings for a specific game size.
        /// </summary>
        public async Task<Dictionary<string, int>> GetAllTeamRatingsAsync(GameSize gameSize)
        {
            try
            {
                var activeSeason = await GetActiveSeasonAsync();
                if (activeSeason == null || !activeSeason.TeamStats.ContainsKey(gameSize))
                {
                    return new Dictionary<string, int>();
                }

                return activeSeason.TeamStats[gameSize]
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.CurrentRating);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleError(ex);
                return new Dictionary<string, int>();
            }
        }
    }

    /// <summary>
    /// Event published when a team's rating is updated.
    /// </summary>
    public class TeamRatingUpdatedEvent : ICoreEvent
    {
        public string TeamId { get; init; } = string.Empty;
        public GameSize GameSize { get; init; }
        public int OldRating { get; init; }
        public int NewRating { get; init; }
        public string Reason { get; init; } = string.Empty;
    }
}