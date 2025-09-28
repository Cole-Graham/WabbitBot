using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.ErrorService;
using WabbitBot.Common.Attributes;
using WabbitBot.Core.Common.Services;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Database;

namespace WabbitBot.Core.Leaderboards;

/// <summary>
/// Season-specific business logic service operations with multi-source data access
/// </summary>
public partial class LeaderboardCore
{
    /// <summary>
    /// Creates a new season with business logic validation
    /// </summary>
    public async Task<Result<Season>> CreateSeasonAsync(string seasonGroupId, TeamSize TeamSize, DateTime startDate, DateTime endDate, SeasonConfig config)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(seasonGroupId))
                return Result<Season>.Failure("Season group ID is required");

            // Validate date ranges using custom validation
            if (startDate >= endDate)
                return Result<Season>.Failure("Season start date must be before end date");

            if (startDate < DateTime.UtcNow.Date)
                return Result<Season>.Failure("Season cannot start in the past");

            // Check for overlapping seasons
            var overlappingSeasonsResult = await CoreService.Seasons.GetByDateRangeAsync(startDate, endDate, DatabaseComponent.Repository);
            if (!overlappingSeasonsResult.Success)
            {
                return Result<Season>.Failure(
                    $"Failed to retrieve overlapping seasons: " +
                    $"{overlappingSeasonsResult.ErrorMessage}"
                );
            }
            var conflictingSeason = overlappingSeasonsResult.Data?.
                FirstOrDefault(s => s.TeamSize == TeamSize && s.IsActive);

            if (conflictingSeason != null)
                return Result<Season>.Failure($"An active season already exists for " +
                                             $"{TeamSize} during this time period");

            // 1. Create and save the SeasonConfig
            var configResult = await CoreService.SeasonConfigs.CreateAsync(config,
                DatabaseComponent.Repository);
            if (!configResult.Success)
                return Result<Season>.Failure(
                    $"Failed to create season config: {configResult.ErrorMessage}"
                );

            // 2. Create the season
            var season = new Season
            {
                SeasonGroupId = Guid.Parse(seasonGroupId),
                TeamSize = TeamSize,
                StartDate = startDate,
                EndDate = endDate,
                IsActive = true,
                SeasonConfigId = configResult.Data!.Id,
            };

            // 3. Save the season
            var seasonResult = await CoreService.Seasons.CreateAsync(season,
                DatabaseComponent.Repository);
            if (!seasonResult.Success)
            {
                // Rollback config creation if season creation fails
                await CoreService.SeasonConfigs.DeleteAsync(configResult.Data!.Id,
                    DatabaseComponent.Repository);
                return seasonResult;
            }

            // Publish business logic event if needed

            return seasonResult;
        }
        catch (Exception ex)
        {
            await CoreService.ErrorHandler.CaptureAsync(ex, "Failed to create season", nameof(CreateSeasonAsync));
            return Result<Season>.Failure($"Failed to create season: {ex.Message}");
        }
    }

    /// <summary>
    /// Ends a season with proper cleanup
    /// </summary>
    public async Task<Result<bool>> EndSeasonAsync(Guid seasonId)
    {
        try
        {
            var seasonResult = await CoreService.Seasons.GetByIdAsync(seasonId,
                DatabaseComponent.Repository);
            if (!seasonResult.Success)
            {
                return Result<bool>.Failure(
                    $"Failed to retrieve season: {seasonResult.ErrorMessage}"
                );
            }
            var season = seasonResult.Data;

            if (season == null)
                return Result<bool>.Failure("Season not found");

            if (!season.IsActive)
                return Result<bool>.Failure("Season is already ended");

            season.IsActive = false;
            season.EndDate = DateTime.UtcNow;

            // Update in repository and cache
            var updateResult = await CoreService.Seasons.UpdateAsync(season,
                DatabaseComponent.Repository);
            if (!updateResult.Success)
            {
                return Result<bool>.Failure(
                    $"Failed to update season: {updateResult.ErrorMessage}"
                );
            }

            // Publish business logic event
            await CoreService.PublishAsync(new SeasonEndedEvent(season.Id));

            return Result<bool>.CreateSuccess(true);
        }
        catch (Exception ex)
        {
            await CoreService.ErrorHandler.CaptureAsync(ex, "Failed to end season", nameof(EndSeasonAsync));
            return Result<bool>.Failure($"Failed to end season: {ex.Message}");
        }
    }

    /// <summary>
    /// Registers a team for participation in a season
    /// </summary>
    // public async Task<Result<bool>> RegisterTeamAsync(Guid seasonId, Guid teamId)
    // {
    //     try
    //     {
    //         var season = await _seasonData.GetByIdAsync(seasonId, DatabaseComponent.Repository);
    //         if (season == null)
    //             return Result<bool>.Failure("Season not found");

    //         if (!season.IsActive)
    //             return Result<bool>.Failure("Cannot register teams for an inactive season");

    //         // Get the team
    //         var team = await _teamData.GetByIdAsync(teamId, DatabaseComponent.Repository);
    //         if (team == null)
    //             return Result<bool>.Failure("Team not found");

    //         // Register the team with the season
    //         var teamKey = team.Id.ToString();
    //         if (!season.ParticipatingTeams.ContainsKey(teamKey))
    //         {
    //             season.ParticipatingTeams[teamKey] = team.Name;
    //             team.UpdateLastActive();
    //             await _teamData.UpdateAsync(team, DatabaseComponent.Repository);
    //         }

    //         // Update season in repository and cache
    //         await _seasonData.UpdateAsync(season, DatabaseComponent.Repository);

    //         // Publish business logic event
    //         await EventBus.PublishAsync(new TeamRegisteredForSeasonEvent(seasonId, teamId));

    //         return Result<bool>.CreateSuccess(true);
    //     }
    //     catch (Exception ex)
    //     {
    //         await ErrorHandler.CaptureAsync(ex, "Failed to register team", nameof(RegisterTeamAsync));
    //         return Result<bool>.Failure($"Failed to register team: {ex.Message}");
    //     }
    // }

    /// <summary>
    /// Adds a team result to a season (called after a match)
    /// </summary>
    // public async Task<Result<bool>> AddTeamResultAsync(Guid seasonId, Guid teamId, TeamSize TeamSize, double ratingChange, bool isWin)
    // {
    //     try
    //     {
    //         var season = await _seasonData.GetByIdAsync(seasonId, DatabaseComponent.Repository);
    //         if (season == null)
    //             return Result<bool>.Failure("Season not found");

    //         // Ensure the team is participating in this season
    //         if (!season.ParticipatingTeams.ContainsKey(teamId.ToString()))
    //         {
    //             return Result<bool>.Failure($"Team {teamId} is not participating in this season");
    //         }

    //         // Load the actual team from the database
    //         var team = await _teamData.GetByIdAsync(teamId, DatabaseComponent.Repository);
    //         if (team == null)
    //         {
    //             return Result<bool>.Failure("Team not found");
    //         }

    //         // Update the team's stats
    //         if (!team.Stats.ContainsKey(TeamSize))
    //         {
    //             team.Stats[TeamSize] = new Stats
    //             {
    //                 TeamId = teamId,
    //                 TeamSize = TeamSize,
    //                 InitialRating = Leaderboard.InitialRating,
    //                 CurrentRating = Leaderboard.InitialRating,
    //                 HighestRating = Leaderboard.InitialRating,
    //                 LastUpdated = DateTime.UtcNow
    //             };
    //         }

    //         var teamStats = team.Stats[TeamSize];
    //         teamStats.UpdateStats(isWin);
    //         teamStats.UpdateRating(teamStats.CurrentRating + ratingChange);

    //         // Update team's last active time
    //         team.UpdateLastActive();

    //         // Update team in database
    //         await _teamData.UpdateAsync(team, DatabaseComponent.Repository);

    //         // Update season (ParticipatingTeams reference doesn't change)
    //         await _seasonData.UpdateAsync(season, DatabaseComponent.Repository);

    //         // Publish business logic event
    //         await EventBus.PublishAsync(new TeamResultAddedEvent(seasonId, teamId, TeamSize.ToString(), ratingChange, isWin));

    //         // Also publish that the team's rating has been updated for the leaderboard
    //         await EventBus.PublishAsync(new TeamRatingUpdatedEvent(teamId, TeamSize.ToString()));

    //         return Result<bool>.CreateSuccess(true);
    //     }
    //     catch (Exception ex)
    //     {
    //         await ErrorHandler.CaptureAsync(ex, "Failed to add team result", nameof(AddTeamResultAsync));
    //         return Result<bool>.Failure($"Failed to add team result: {ex.Message}");
    //     }
    // }

    /// <summary>
    /// Applies rating decay to all teams in a season
    /// </summary>
    public async Task<Result<bool>> ApplyRatingDecayAsync(Guid seasonId)
    {
        try
        {
            var seasonResult = await CoreService.Seasons.GetByIdAsync(seasonId,
                DatabaseComponent.Repository);
            if (!seasonResult.Success)
            {
                return Result<bool>.Failure(
                    $"Failed to retrieve season: {seasonResult.ErrorMessage}"
                );
            }
            var season = seasonResult.Data;

            if (season == null)
                return Result<bool>.Failure("Season not found");

            var configResult = await CoreService.SeasonConfigs.GetByIdAsync(season.SeasonConfigId,
                DatabaseComponent.Repository);
            if (!configResult.Success)
            {
                return Result<bool>.Failure(
                    $"Failed to retrieve season config: {configResult.ErrorMessage}"
                );
            }
            var config = configResult.Data;

            if (config == null)
                return Result<bool>.Failure("Season config not found");

            // Apply rating decay to all teams in the season
            if (!config.RatingDecayEnabled)
                return Result<bool>.Failure("Rating decay is not enabled for this season");

            var now = DateTime.UtcNow;
            foreach (var teamEntry in season.ParticipatingTeams)
            {
                // Load the actual team from database
                var teamResult = await CoreService.Teams.GetByIdAsync(Guid.Parse(teamEntry.Key),
                    DatabaseComponent.Repository);
                if (!teamResult.Success)
                {
                    // Log but don't fail for individual team retrieval failures
                    await CoreService.ErrorHandler.CaptureAsync(new Exception(
                            $"Failed to retrieve team {teamEntry.Key}: " +
                            $"{teamResult.ErrorMessage}"
                        ),
                        "Rating Decay Warning",
                        nameof(ApplyRatingDecayAsync)
                    );
                    continue; // Skip this team and continue with others
                }
                var team = teamResult.Data;

                if (team == null)
                    continue;

                var teamUpdated = false;

                // Apply decay to all game sizes for this team
                foreach (var stats in team.Stats.Values)
                {
                    var weeksSinceUpdate = (now - stats.LastUpdated).TotalDays / 7;
                    if (weeksSinceUpdate >= 1)
                    {
                        var decayAmount = weeksSinceUpdate * config.DecayRatePerWeek;
                        stats.CurrentRating = Math.Max(
                            stats.CurrentRating - decayAmount,
                            config.MinimumRating
                        );
                        stats.LastUpdated = now;
                        teamUpdated = true;
                    }
                }

                // Update team if it was modified
                if (teamUpdated)
                {
                    var updateTeamResult = await CoreService.Teams.UpdateAsync(team,
                        DatabaseComponent.Repository);
                    if (!updateTeamResult.Success)
                    {
                        await CoreService.ErrorHandler.CaptureAsync(new Exception(
                                $"Failed to update team {team.Id} during decay: " +
                                $"{updateTeamResult.ErrorMessage}"
                            ),
                            "Rating Decay Warning",
                            nameof(ApplyRatingDecayAsync)
                        );
                        // Don't fail the entire decay operation, log and continue
                    }
                }
            }

            // Update season in repository
            var updateSeasonResult = await CoreService.Seasons.UpdateAsync(season,
                DatabaseComponent.Repository);
            if (!updateSeasonResult.Success)
            {
                return Result<bool>.Failure(
                    $"Failed to update season after decay: " +
                    $"{updateSeasonResult.ErrorMessage}"
                );
            }

            // Publish business logic event
            await CoreService.PublishAsync(new SeasonRatingDecayAppliedEvent(seasonId));

            return Result<bool>.CreateSuccess(true);
        }
        catch (Exception ex)
        {
            await CoreService.ErrorHandler.CaptureAsync(ex, "Failed to apply rating decay", nameof(ApplyRatingDecayAsync));
            return Result<bool>.Failure($"Failed to apply rating decay: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all team stats for a season (for leaderboards) - THIS IS A QUERY METHOD AND SHOULD BE REMOVED
    /// </summary>

    // TODO: Remove this query method. Business logic should not perform complex queries.
    // This logic should be moved to a dedicated query handler or the caller should
    // get the season and then get the teams.

    /// <summary>
    /// Gets a team's stats for a specific season and game size - THIS IS A QUERY METHOD AND SHOULD BE REMOVED
    /// </summary>

    // TODO: Remove this query method.

    /// <summary>
    /// Gets the active season for a game size - THIS IS A QUERY METHOD AND SHOULD BE REMOVED
    /// </summary>

    // TODO: Remove this query method.

    /// <summary>
    /// Gets all season IDs that a team is participating in - THIS IS A QUERY METHOD AND SHOULD BE REMOVED
    /// </summary>

    // TODO: Remove this query method.
}
