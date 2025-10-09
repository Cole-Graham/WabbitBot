using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Models.Leaderboard;
using WabbitBot.Core.Common.Services;
using WabbitBot.Common.Events.Interfaces;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.ErrorService;
using WabbitBot.Common.Models;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Database;
using WabbitBot.Core.Common.Interfaces;

namespace WabbitBot.Core.Leaderboards
{
    /// <summary>
    /// Service for leaderboard business logic operations.
    /// Leaderboards are read-only views generated from Season data.
    /// </summary>
    public partial class LeaderboardCore : ILeaderboardCore
    {
        /// <inheritdoc />
        public Task InitializeAsync() => Task.CompletedTask;

        /// <inheritdoc />
        public Task ValidateAsync() => Task.CompletedTask;

        public static class Factory
        {
            /// <summary>
            /// Creates a new Leaderboard instance with properly initialized rankings.
            /// </summary>
            public static ScrimmageLeaderboard CreateLeaderboard(Season Season, TeamSize TeamSize)
            {
                var leaderboard = new ScrimmageLeaderboard
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    Season = Season,
                    TeamSize = TeamSize
                };

                return leaderboard;
            }
        }

        /// <summary>
        /// Gets all team ratings for a specific game size from the active season.
        /// </summary>
        private async Task<Dictionary<string, double>> GetAllTeamScrimmageRatingsAsync(Season Season, TeamSize TeamSize)
        {
            try
            {
                var allSeasonsResult = await CoreService.Seasons.GetAllAsync(DatabaseComponent.Repository);
                if (!allSeasonsResult.Success)
                {
                    await CoreService.ErrorHandler.CaptureAsync(new Exception(
                            $"Failed to retrieve all seasons: " +
                            $"{allSeasonsResult.ErrorMessage}"
                        ),
                        "Leaderboard Warning",
                        nameof(GetAllTeamScrimmageRatingsAsync)
                    );
                    return new Dictionary<string, double>();
                }
                var activeSeason = allSeasonsResult.Data?.
                    FirstOrDefault(s => s.IsActive);

                if (activeSeason == null)
                {
                    return new Dictionary<string, double>();
                }

                var ratings = new Dictionary<string, double>();
                // Get all the scrimmage leaderboard items for the given team size
                foreach (var teamEntry in activeSeason.ScrimmageLeaderboards.Where(
                    l => l.TeamSize == TeamSize).SelectMany(l => l.LeaderboardItems))
                {
                    var teamResult = await CoreService.Teams.GetByIdAsync(teamEntry.TeamId,
                        DatabaseComponent.Repository);
                    if (!teamResult.Success)
                    {
                        await CoreService.ErrorHandler.CaptureAsync(new Exception(
                                $"Failed to retrieve team {teamEntry.TeamId}: " +
                                $"{teamResult.ErrorMessage}"
                            ),
                            "Leaderboard Warning",
                            nameof(GetAllTeamScrimmageRatingsAsync)
                        );
                        continue; // Skip this team and continue with others
                    }
                    var team = teamResult.Data;

                    if (team != null && team.ScrimmageTeamStats.ContainsKey(TeamSize))
                    {
                        ratings[teamEntry.TeamId.ToString()] = team.ScrimmageTeamStats[TeamSize].CurrentRating;
                    }
                }
                return ratings;
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(ex,
                    "Failed to get all team ratings",
                    nameof(GetAllTeamScrimmageRatingsAsync)
                );
                return new Dictionary<string, double>();
            }
        }

        /// <summary>
        /// Refreshes the leaderboard for a specific game size from Season data.
        /// </summary>
        public async Task RefreshLeaderboardAsync()
        {

        }

        /// <summary>
        /// Manually triggers a leaderboard refresh for all game sizes.
        /// </summary>
        public async Task RefreshAllLeaderboardsAsync()
        {

        }

    }
}
