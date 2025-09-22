using System;
using System.Threading.Tasks;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.ErrorHandling;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Core.Leaderboards.Data.Interface;
using WabbitBot.Common.Attributes;
using WabbitBot.Core.Common.BotCore;
using System.Collections.Generic; // Added for Dictionary
using System.Linq; // Added for ToDictionary

namespace WabbitBot.Core.Leaderboards
{
    /// <summary>
    /// Centralized service for all team rating operations using Season as the source of truth.
    /// This service ensures all rating updates go through the Season system.
    /// </summary>
    [GenerateEventPublisher(EventBusType = EventBusType.Core, EnableValidation = true, EnableTimestamps = true)]
    public partial class SeasonRatingService : CoreService, ILeaderboardService
    {
        public SeasonRatingService()
            : base(CoreEventBus.Instance, CoreErrorHandler.Instance)
        {
        }

        private ISeasonRepository SeasonRepository =>
            WabbitBot.Core.Common.Data.DataServiceManager.SeasonRepository;

        private ISeasonCache SeasonCache =>
            WabbitBot.Core.Common.Data.DataServiceManager.SeasonCache;

        /// <summary>
        /// Gets the current rating for a team in a specific game size.
        /// </summary>
        public async Task<double> GetTeamRatingAsync(string teamId, EvenTeamFormat evenTeamFormat)
        {
            try
            {
                var activeSeason = await GetActiveSeasonAsync(evenTeamFormat);
                if (activeSeason == null)
                {
                    return Leaderboard.InitialRating; // Default rating if no active season
                }

                if (!activeSeason.ParticipatingTeams.ContainsKey(teamId))
                {
                    return Leaderboard.InitialRating; // Default rating for new teams
                }

                // Load the actual team to get current stats
                var team = await WabbitBot.Core.Common.Data.DataServiceManager.TeamRepository.GetByIdAsync(teamId);
                if (team == null || !team.Stats.ContainsKey(evenTeamFormat))
                {
                    return Leaderboard.InitialRating;
                }

                return team.Stats[evenTeamFormat].CurrentRating;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Leaderboard.InitialRating;
            }
        }

        /// <summary>
        /// Updates a team's rating through the Season system.
        /// </summary>
        public async Task UpdateTeamRatingAsync(string teamId, EvenTeamFormat evenTeamFormat, double newRating, string reason = "")
        {
            try
            {
                var activeSeason = await GetActiveSeasonAsync(evenTeamFormat);
                if (activeSeason == null)
                {
                    throw new InvalidOperationException($"No active season found for game size {evenTeamFormat}. Cannot update rating for team {teamId}.");
                }

                // Ensure the team exists in the season
                if (!activeSeason.ParticipatingTeams.ContainsKey(teamId))
                {
                    // Team should be registered with the season before rating updates
                    // This is likely an error condition - teams should be registered first
                    throw new InvalidOperationException($"Team {teamId} is not registered for the active season. Teams must be registered with seasons before rating updates.");
                }

                // Load the actual team from the database
                var team = await WabbitBot.Core.Common.Data.DataServiceManager.TeamRepository.GetByIdAsync(teamId);
                if (team == null)
                {
                    throw new InvalidOperationException($"Team {teamId} not found in database.");
                }

                // Ensure the team has stats for this game size
                if (!team.Stats.ContainsKey(evenTeamFormat))
                {
                    team.Stats[evenTeamFormat] = new Stats
                    {
                        TeamId = teamId,
                        EvenTeamFormat = evenTeamFormat,
                        InitialRating = Leaderboard.InitialRating,
                        CurrentRating = Leaderboard.InitialRating,
                        HighestRating = Leaderboard.InitialRating,
                        LastUpdated = DateTime.UtcNow
                    };
                }

                // Update the rating
                var stats = team.Stats[evenTeamFormat];
                var oldRating = stats.CurrentRating;
                stats.CurrentRating = newRating;
                stats.LastUpdated = DateTime.UtcNow;

                // Save the updated team
                await WabbitBot.Core.Common.Data.DataServiceManager.TeamRepository.UpdateAsync(team);

                // Update the season cache (ParticipatingTeams references don't change)
                await SeasonCache.SetActiveSeasonAsync(activeSeason);

                // Publish event for other systems to react (e.g., leaderboard refresh)
                // Event handlers should look up team rating details from cache/repository
                await EventBus.PublishAsync(new TeamRatingUpdatedEvent(teamId, evenTeamFormat.ToString()));

                Console.WriteLine($"Team rating updated: {teamId} ({evenTeamFormat}) {oldRating} -> {newRating} ({reason})");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                throw;
            }
        }

        /// <summary>
        /// Applies a rating change (delta) to a team's current rating.
        /// </summary>
        public async Task ApplyRatingChangeAsync(string teamId, EvenTeamFormat evenTeamFormat, double ratingChange, string reason = "")
        {
            var currentRating = await GetTeamRatingAsync(teamId, evenTeamFormat);
            var newRating = currentRating + ratingChange;
            await UpdateTeamRatingAsync(teamId, evenTeamFormat, newRating, reason);
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
        /// Gets all team ratings for a specific game size.
        /// </summary>
        public async Task<Dictionary<string, double>> GetAllTeamRatingsAsync(EvenTeamFormat evenTeamFormat)
        {
            try
            {
                var activeSeason = await GetActiveSeasonAsync(evenTeamFormat);
                if (activeSeason == null)
                {
                    throw new InvalidOperationException($"No active season found for game size {evenTeamFormat}. Cannot retrieve team ratings.");
                }

                var ratings = new Dictionary<string, double>();

                // Load each team's current stats
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
                throw;
            }
        }
    }

}