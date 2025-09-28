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
            public static Leaderboard CreateLeaderboard()
            {
                var leaderboard = new Leaderboard
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                };

                // Initialize rankings dictionary for each team size
                foreach (TeamSize size in Enum.GetValues(typeof(TeamSize)))
                {
                    leaderboard.Rankings[size] = new Dictionary<string, LeaderboardItem>();
                }

                return leaderboard;
            }
        }

        /// <summary>
        /// Gets all team ratings for a specific game size from the active season.
        /// </summary>
        private async Task<Dictionary<string, double>> GetAllTeamRatingsAsync(TeamSize TeamSize)
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
                        nameof(GetAllTeamRatingsAsync)
                    );
                    return new Dictionary<string, double>();
                }
                var activeSeason = allSeasonsResult.Data?.
                    FirstOrDefault(s => s.IsActive && s.TeamSize == TeamSize);

                if (activeSeason == null)
                {
                    return new Dictionary<string, double>();
                }

                var ratings = new Dictionary<string, double>();
                foreach (var teamEntry in activeSeason.ParticipatingTeams)
                {
                    var teamResult = await CoreService.Teams.GetByIdAsync(Guid.Parse(teamEntry.Key),
                        DatabaseComponent.Repository);
                    if (!teamResult.Success)
                    {
                        await CoreService.ErrorHandler.CaptureAsync(new Exception(
                                $"Failed to retrieve team {teamEntry.Key}: " +
                                $"{teamResult.ErrorMessage}"
                            ),
                            "Leaderboard Warning",
                            nameof(GetAllTeamRatingsAsync)
                        );
                        continue; // Skip this team and continue with others
                    }
                    var team = teamResult.Data;

                    if (team != null && team.Stats.ContainsKey(TeamSize))
                    {
                        ratings[teamEntry.Key] = team.Stats[TeamSize].CurrentRating;
                    }
                }
                return ratings;
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(ex,
                    "Failed to get all team ratings",
                    nameof(GetAllTeamRatingsAsync)
                );
                return new Dictionary<string, double>();
            }
        }

        /// <summary>
        /// Refreshes the leaderboard for a specific game size from Season data.
        /// </summary>
        public async Task RefreshLeaderboardAsync(TeamSize TeamSize)
        {
            try
            {
                // Get all team ratings directly from the database
                var teamRatings = await GetAllTeamRatingsAsync(TeamSize);

                // Create leaderboard entries
                var rankings = new Dictionary<string, LeaderboardItem>();
                foreach (var (teamId, rating) in teamRatings)
                {
                    rankings[teamId] = new LeaderboardItem
                    {
                        Name = teamId,
                        Rating = rating,
                        IsTeam = true,
                        LastUpdated = DateTime.UtcNow,
                    };
                }

                // Get or create leaderboard
                // TODO: This GetLeaderboardsByTeamSizeAsync is a custom query that doesn't exist on the base DatabaseService.
                // Assuming it will be implemented as a custom method in a partial class.
                // For now, this will be a placeholder.
                // var leaderboards = await CoreService.Leaderboards.GetLeaderboardsByTeamSizeAsync(TeamSize, DatabaseComponent.Repository);
                var leaderboardsResult = await CoreService.Leaderboards.GetAllAsync(DatabaseComponent.Repository);
                if (!leaderboardsResult.Success)
                {
                    await CoreService.ErrorHandler.CaptureAsync(new Exception(
                            $"Failed to retrieve leaderboards: " +
                            $"{leaderboardsResult.ErrorMessage}"
                        ),
                        "Leaderboard Refresh Warning",
                        nameof(RefreshLeaderboardAsync)
                    );
                    return; // Exit if leaderboards cannot be retrieved
                }
                var leaderboard = leaderboardsResult.Data?.
                    FirstOrDefault(l => l.Rankings.ContainsKey(TeamSize));

                if (leaderboard == null)
                {
                    leaderboard = Factory.CreateLeaderboard();
                }

                // Update rankings
                leaderboard.Rankings[TeamSize] = rankings;

                // Save to database
                if (leaderboard.Id == Guid.Empty)
                {
                    var createResult = await CoreService.Leaderboards.CreateAsync(leaderboard,
                        DatabaseComponent.Repository);
                    if (!createResult.Success)
                    {
                        await CoreService.ErrorHandler.CaptureAsync(new Exception(
                                "Failed to create leaderboard: " +
                                $"{createResult.ErrorMessage}"
                            ),
                            "Leaderboard Refresh Warning",
                            nameof(RefreshLeaderboardAsync)
                        );
                        return; // Exit if leaderboard cannot be created
                    }
                }
                else
                {
                    var updateResult = await CoreService.Leaderboards.UpdateAsync(leaderboard,
                        DatabaseComponent.Repository);
                    if (!updateResult.Success)
                    {
                        await CoreService.ErrorHandler.CaptureAsync(new Exception(
                                "Failed to update leaderboard: " +
                                $"{updateResult.ErrorMessage}"
                            ),
                            "Leaderboard Refresh Warning",
                            nameof(RefreshLeaderboardAsync)
                        );
                        return; // Exit if leaderboard cannot be updated
                    }
                }
                await CoreService.Leaderboards.UpdateAsync(leaderboard, DatabaseComponent.Cache);

                Console.WriteLine($"Leaderboard refreshed for {TeamSize}: " +
                                $"{rankings.Count} teams");

                // Publish success event
                await PublishLeaderboardRefreshed(TeamSize, rankings.Count);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing leaderboard for {TeamSize}: " +
                                $"{ex.Message}");

                // Publish failure event
                await PublishLeaderboardRefreshFailed(TeamSize, ex.Message);
                await CoreService.ErrorHandler.CaptureAsync(ex,
                    $"Error refreshing leaderboard for {TeamSize}",
                    nameof(RefreshLeaderboardAsync)
                );
            }
        }

        /// <summary>
        /// Manually triggers a leaderboard refresh for all game sizes.
        /// </summary>
        public async Task RefreshAllLeaderboardsAsync()
        {
            foreach (TeamSize TeamSize in Enum.GetValues(typeof(TeamSize)))
            {
                if (TeamSize != TeamSize.OneVOne) // Teams don't participate in 1v1
                {
                    try
                    {
                        await RefreshLeaderboardAsync(TeamSize);
                    }
                    catch (Exception)
                    {
                        // Errors are already handled and logged in RefreshLeaderboardAsync
                    }
                }
            }

            // Publish event for all leaderboards refreshed
            await PublishAllLeaderboardsRefreshed(0); // TODO: Re-implement team count if needed.
        }

        /// <summary>
        /// Publishes a leaderboard refreshed event.
        /// </summary>
        private async Task PublishLeaderboardRefreshed(TeamSize TeamSize, int teamCount)
        {
            var evt = new LeaderboardRefreshedEvent(TeamSize, teamCount);
            await CoreService.PublishAsync(evt);
        }

        /// <summary>
        /// Publishes a leaderboard refresh failed event.
        /// </summary>
        private async Task PublishLeaderboardRefreshFailed(TeamSize TeamSize, string errorMessage)
        {
            var evt = new LeaderboardRefreshFailedEvent(TeamSize, errorMessage);
            await CoreService.PublishAsync(evt);
        }

        /// <summary>
        /// Publishes an all leaderboards refreshed event.
        /// </summary>
        private async Task PublishAllLeaderboardsRefreshed(int totalTeamsProcessed)
        {
            var evt = new AllLeaderboardsRefreshedEvent(totalTeamsProcessed);
            await CoreService.PublishAsync(evt);
        }
    }
}
