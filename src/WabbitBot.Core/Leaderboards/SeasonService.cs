using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.ErrorHandling;
using WabbitBot.Core.Common.Data.Interface;
using WabbitBot.Core.Leaderboards.Data.Interface;
using WabbitBot.Core.Common.Data;
using WabbitBot.Core.Common.Data.Cache;
using WabbitBot.Core.Common;
using WabbitBot.Core.Common.Events;
using WabbitBot.Common.Attributes;
using WabbitBot.Core.Common.Services;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Data.Interfaces;

namespace WabbitBot.Core.Leaderboards;

/// <summary>
/// Season-specific business logic service operations with multi-source data access
/// </summary>
[GenerateEventPublisher(EventBusType = EventBusType.Core, EnableValidation = true, EnableTimestamps = true)]
public partial class SeasonService : CoreService, ICoreDataService<Season>, ILeaderboardService
{
    public SeasonService()
        : base(CoreEventBus.Instance, CoreErrorHandler.Instance)
    {
    }

    private ISeasonRepository SeasonRepository =>
        WabbitBot.Core.Common.Data.DataServiceManager.SeasonRepository;

    private ISeasonCache SeasonCache =>
        WabbitBot.Core.Common.Data.DataServiceManager.SeasonCache;

    private ISeasonArchive SeasonArchive { get; } = new WabbitBot.Core.Leaderboards.Data.SeasonArchive(
        WabbitBot.Core.Common.Data.DataServiceManager.DatabaseConnection);

    private TeamService TeamService { get; } = new();

    /// <summary>
    /// Creates a new season with business logic validation
    /// </summary>
    public async Task<Result<Season>> CreateSeasonAsync(string seasonGroupId, EvenTeamFormat evenTeamFormat, DateTime startDate, DateTime endDate, SeasonConfig config)
    {
        try
        {
            // Validate inputs using Common validation methods
            var seasonGroupValidation = CoreValidation.ValidateString(seasonGroupId, "Season group ID", required: true);
            if (!seasonGroupValidation.Success)
                return Result<Season>.Failure(seasonGroupValidation.ErrorMessage ?? "Invalid season group ID");

            // Validate date ranges using custom validation
            if (startDate >= endDate)
                return Result<Season>.Failure("Season start date must be before end date");

            if (startDate < DateTime.UtcNow.Date)
                return Result<Season>.Failure("Season cannot start in the past");

            // Check for overlapping seasons
            var overlappingSeasons = await SeasonRepository.GetSeasonsByDateRangeAsync(startDate, endDate);
            var conflictingSeason = overlappingSeasons.FirstOrDefault(s => s.EvenTeamFormat == evenTeamFormat && s.IsActive);

            if (conflictingSeason != null)
                return Result<Season>.Failure($"An active season already exists for {evenTeamFormat} during this time period");

            // Create the season
            var season = Season.Create(seasonGroupId, evenTeamFormat, startDate, endDate, config);

            // Save to repository
            await SeasonRepository.AddAsync(season);
            await SeasonCache.SetSeasonAsync(season);

            // Publish event
            await EventBus.PublishAsync(new SeasonCreatedEvent(season.Id.ToString()));

            return Result<Season>.CreateSuccess(season);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<Season>.Failure($"Failed to create season: {ex.Message}");
        }
    }

    /// <summary>
    /// Ends a season with proper cleanup
    /// </summary>
    public async Task<Result<bool>> EndSeasonAsync(string seasonId)
    {
        try
        {
            var season = await GetSeasonAsync(seasonId);
            if (season == null)
                return Result<bool>.Failure("Season not found");

            if (!season.IsActive)
                return Result<bool>.Failure("Season is already ended");

            season.IsActive = false;
            season.EndDate = DateTime.UtcNow;

            // Update in repository
            await SeasonRepository.UpdateAsync(season);
            await SeasonCache.SetSeasonAsync(season);

            // Publish event
            await EventBus.PublishAsync(new SeasonEndedEvent(season.Id.ToString()));

            return Result<bool>.CreateSuccess(true);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<bool>.Failure($"Failed to end season: {ex.Message}");
        }
    }

    /// <summary>
    /// Registers a team for participation in a season
    /// </summary>
    public async Task<Result<bool>> RegisterTeamAsync(string seasonId, string teamId)
    {
        try
        {
            var season = await GetSeasonAsync(seasonId);
            if (season == null)
                return Result<bool>.Failure("Season not found");

            if (!season.IsActive)
                return Result<bool>.Failure("Cannot register teams for an inactive season");

            // Get the team from TeamService
            var teamResult = await TeamService.GetTeamAsync(teamId);
            if (!teamResult.Success)
                return Result<bool>.Failure($"Team not found: {teamResult.ErrorMessage}");

            var team = teamResult.Data;
            if (team == null)
                return Result<bool>.Failure("Team data is null");

            // Register the team with the season
            var teamKey = team.Id.ToString();
            if (!season.ParticipatingTeams.ContainsKey(teamKey))
            {
                season.ParticipatingTeams[teamKey] = team.Name;
                team.UpdateLastActive();
            }

            // Update in repository
            await SeasonRepository.UpdateAsync(season);
            await SeasonCache.SetSeasonAsync(season);

            // Publish event
            await EventBus.PublishAsync(new TeamRegisteredForSeasonEvent(seasonId, teamId));

            return Result<bool>.CreateSuccess(true);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<bool>.Failure($"Failed to register team: {ex.Message}");
        }
    }

    /// <summary>
    /// Adds a team result to a season (called after a match)
    /// </summary>
    public async Task<Result<bool>> AddTeamResultAsync(string seasonId, string teamId, EvenTeamFormat evenTeamFormat, double ratingChange, bool isWin)
    {
        try
        {
            var season = await GetSeasonAsync(seasonId);
            if (season == null)
                return Result<bool>.Failure("Season not found");

            // Ensure the team is participating in this season
            if (!season.ParticipatingTeams.ContainsKey(teamId))
            {
                return Result<bool>.Failure($"Team {teamId} is not participating in this season");
            }

            // Load the actual team from the database
            var team = await WabbitBot.Core.Common.Data.DataServiceManager.TeamRepository.GetByIdAsync(teamId);
            if (team == null)
            {
                return Result<bool>.Failure("Team not found");
            }

            // Update the team's stats
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

            var teamStats = team.Stats[evenTeamFormat];
            teamStats.UpdateStats(isWin);
            teamStats.UpdateRating(teamStats.CurrentRating + ratingChange);

            // Update team's last active time
            team.UpdateLastActive();

            // Update team in database
            await WabbitBot.Core.Common.Data.DataServiceManager.TeamRepository.UpdateAsync(team);

            // Update season (ParticipatingTeams reference doesn't change)
            await SeasonRepository.UpdateAsync(season);
            await SeasonCache.SetSeasonAsync(season);

            // Publish event
            await EventBus.PublishAsync(new TeamResultAddedEvent(seasonId, teamId, evenTeamFormat.ToString(), ratingChange, isWin));

            return Result<bool>.CreateSuccess(true);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<bool>.Failure($"Failed to add team result: {ex.Message}");
        }
    }

    /// <summary>
    /// Applies rating decay to all teams in a season
    /// </summary>
    public async Task<Result<bool>> ApplyRatingDecayAsync(string seasonId)
    {
        try
        {
            var season = await GetSeasonAsync(seasonId);
            if (season == null)
                return Result<bool>.Failure("Season not found");

            // Apply rating decay to all teams in the season
            if (!season.Config.RatingDecayEnabled)
                return Result<bool>.Failure("Rating decay is not enabled for this season");

            var now = DateTime.UtcNow;
            foreach (var teamEntry in season.ParticipatingTeams)
            {
                // Load the actual team from database
                var team = await WabbitBot.Core.Common.Data.DataServiceManager.TeamRepository.GetByIdAsync(teamEntry.Key);
                if (team == null)
                    continue;

                var teamUpdated = false;

                // Apply decay to all game sizes for this team
                foreach (var stats in team.Stats.Values)
                {
                    var weeksSinceUpdate = (now - stats.LastUpdated).TotalDays / 7;
                    if (weeksSinceUpdate >= 1)
                    {
                        var decayAmount = weeksSinceUpdate * season.Config.DecayRatePerWeek;
                        stats.CurrentRating = Math.Max(
                            stats.CurrentRating - decayAmount,
                            season.Config.MinimumRating
                        );
                        stats.LastUpdated = now;
                        teamUpdated = true;
                    }
                }

                // Update team if it was modified
                if (teamUpdated)
                {
                    await WabbitBot.Core.Common.Data.DataServiceManager.TeamRepository.UpdateAsync(team);
                }
            }

            // Update season in repository (ParticipatingTeams references don't change)
            await SeasonRepository.UpdateAsync(season);
            await SeasonCache.SetSeasonAsync(season);

            // Publish event
            await EventBus.PublishAsync(new SeasonRatingDecayAppliedEvent(seasonId));

            return Result<bool>.CreateSuccess(true);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<bool>.Failure($"Failed to apply rating decay: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all team stats for a season (for leaderboards)
    /// </summary>
    public async Task<Result<IEnumerable<Stats>>> GetSeasonTeamStatsAsync(string seasonId, EvenTeamFormat evenTeamFormat)
    {
        try
        {
            // Parameter validation using Common validation methods
            var seasonIdValidation = CoreValidation.ValidateString(seasonId, "Season ID", required: true);
            if (!seasonIdValidation.Success)
                return Result<IEnumerable<Stats>>.Failure(seasonIdValidation.ErrorMessage ?? "Invalid season ID");

            var season = await GetSeasonAsync(seasonId);
            if (season == null)
                return Result<IEnumerable<Stats>>.Failure("Season not found");

            var statsList = new List<Stats>();
            foreach (var teamEntry in season.ParticipatingTeams)
            {
                var team = await WabbitBot.Core.Common.Data.DataServiceManager.TeamRepository.GetByIdAsync(teamEntry.Key);
                if (team != null && team.Stats.ContainsKey(evenTeamFormat))
                {
                    statsList.Add(team.Stats[evenTeamFormat]);
                }
            }

            var stats = statsList.OrderByDescending(s => s.CurrentRating);
            return Result<IEnumerable<Stats>>.CreateSuccess(stats);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<IEnumerable<Stats>>.Failure($"Failed to get team stats: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a team's stats for a specific season and game size
    /// </summary>
    public async Task<Result<Stats?>> GetTeamSeasonStatsAsync(string seasonId, string teamId, EvenTeamFormat evenTeamFormat)
    {
        try
        {
            // Parameter validation using Common validation methods
            var seasonIdValidation = CoreValidation.ValidateString(seasonId, "Season ID", required: true);
            if (!seasonIdValidation.Success)
                return Result<Stats?>.Failure(seasonIdValidation.ErrorMessage ?? "Invalid season ID");

            var teamIdValidation = CoreValidation.ValidateString(teamId, "Team ID", required: true);
            if (!teamIdValidation.Success)
                return Result<Stats?>.Failure(teamIdValidation.ErrorMessage ?? "Invalid team ID");

            var season = await GetSeasonAsync(seasonId);
            if (season == null)
                return Result<Stats?>.Failure("Season not found");

            // Check if team is participating in this season
            if (!season.ParticipatingTeams.ContainsKey(teamId))
            {
                return Result<Stats?>.CreateSuccess(null);
            }

            // Load the actual team and get its stats
            var team = await WabbitBot.Core.Common.Data.DataServiceManager.TeamRepository.GetByIdAsync(teamId);
            if (team == null)
            {
                return Result<Stats?>.CreateSuccess(null);
            }

            var stats = team.Stats.ContainsKey(evenTeamFormat) ? team.Stats[evenTeamFormat] : null;
            return Result<Stats?>.CreateSuccess(stats);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<Stats?>.Failure($"Failed to get team stats: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the active season for a game size
    /// </summary>
    public async Task<Result<Season?>> GetActiveSeasonAsync(EvenTeamFormat evenTeamFormat)
    {
        try
        {
            // TODO: Update to use CoreService methods once Season methods are implemented
            // For now, use EF Core directly since ListWrapper classes have been eliminated

            // Try cache first (simplified since GetAllSeasonsAsync no longer exists)
            // TODO: Implement season caching in CoreService

            // For now, return null - this will be implemented when Season methods are added to CoreService
            return Result<Season?>.CreateSuccess(null);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<Season?>.Failure($"Failed to get active season: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a season by ID
    /// </summary>
    public async Task<Season?> GetSeasonAsync(string seasonId)
    {
        try
        {
            if (string.IsNullOrEmpty(seasonId))
                return null;

            // Try cache first
            if (Guid.TryParse(seasonId, out var guidId))
            {
                var season = await SeasonCache.GetSeasonAsync(guidId);
                if (season != null)
                    return season;
            }

            // Try repository
            return await SeasonRepository.GetByIdAsync(seasonId);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return null;
        }
    }

    /// <summary>
    /// Gets all seasons
    /// </summary>
    public async Task<Result<IEnumerable<Season>>> GetAllSeasonsAsync()
    {
        try
        {
            var seasons = await SeasonRepository.GetAllAsync();
            return Result<IEnumerable<Season>>.CreateSuccess(seasons);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<IEnumerable<Season>>.Failure($"Failed to get seasons: {ex.Message}");
        }
    }

    // IBaseDataService implementation
    public async Task<Season?> GetByIdAsync(object id)
    {
        if (id == null)
            return null;

        // 🎓 Same defensive pattern
        if (!(id is Guid))
            return null;

        Guid guidId = (Guid)id;
        string seasonId = guidId.ToString();
        return await GetSeasonAsync(seasonId!);
    }

    public async Task<IEnumerable<Season>> GetAllAsync()
    {
        var result = await GetAllSeasonsAsync();
        return result.Success && result.Data != null ? result.Data : Enumerable.Empty<Season>();
    }

    public async Task<IEnumerable<Season>> SearchAsync(string searchTerm, int limit = 25)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();

            // Search by season ID or group ID
            var allSeasons = await GetAllAsync();
            var results = allSeasons
                .Where(s =>
                    s.Id.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    s.SeasonGroupId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .Take(limit);

            return results;
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Enumerable.Empty<Season>();
        }
    }

    public async Task<int> AddAsync(Season entity)
    {
        var result = await CreateSeasonAsync(entity.SeasonGroupId, entity.EvenTeamFormat, entity.StartDate, entity.EndDate, entity.Config);
        return result.Success ? 1 : 0;
    }

    public async Task<bool> UpdateAsync(Season entity)
    {
        var result = await UpdateEntityAsync(entity);
        return result.Success;
    }

    public async Task<bool> DeleteAsync(object id)
    {
        if (id == null)
            return false;

        // 🎓 Same defensive pattern
        if (!(id is Guid))
            return false;

        Guid guidId = (Guid)id;
        string seasonId = guidId.ToString();
        var result = await DeleteEntityAsync(seasonId!);
        return result.Success;
    }

    public async Task<bool> ArchiveAsync(Season entity)
    {
        try
        {
            if (entity == null)
                return false;

            if (entity.IsActive)
                return false; // Cannot archive active seasons

            // Archive the season
            var result = await SeasonArchive.ArchiveAsync(entity);
            if (result > 0)
            {
                // Remove from active cache
                await SeasonCache.RemoveSeasonAsync(entity.Id);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(object id)
    {
        if (id == null)
            return false;

        // 🎓 Same defensive pattern
        if (!(id is Guid))
            return false;

        Guid guidId = (Guid)id;
        string seasonId = guidId.ToString();
        var season = await GetSeasonAsync(seasonId!);
        return season != null;
    }

    // ICoreDataService implementation
    public async Task<Result<Season>> CreateEntityAsync(Season entity)
    {
        return await CreateSeasonAsync(entity.SeasonGroupId, entity.EvenTeamFormat, entity.StartDate, entity.EndDate, entity.Config);
    }

    public async Task<Result<Season>> UpdateEntityAsync(Season entity)
    {
        try
        {
            await SeasonRepository.UpdateAsync(entity);
            await SeasonCache.SetSeasonAsync(entity);
            await EventBus.PublishAsync(new SeasonUpdatedEvent(entity.Id.ToString()));
            return Result<Season>.CreateSuccess(entity);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<Season>.Failure($"Failed to update season: {ex.Message}");
        }
    }

    public async Task<Result<Season>> DeleteEntityAsync(object id)
    {
        try
        {
            if (id == null)
                return Result<Season>.Failure("Season ID cannot be null");

            // 🎓 EDUCATIONAL: Always validate your type assumptions!
            if (!(id is Guid))
                return Result<Season>.Failure("Season ID must be a Guid");

            Guid guidId = (Guid)id;
            string seasonId = guidId.ToString();

            var season = await GetSeasonAsync(seasonId);
            if (season == null)
                return Result<Season>.Failure("Season not found");

            await SeasonRepository.DeleteAsync(seasonId);
            await SeasonCache.RemoveSeasonAsync(guidId);
            await EventBus.PublishAsync(new SeasonDeletedEvent(seasonId));
            return Result<Season>.CreateSuccess(season);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<Season>.Failure($"Failed to delete season: {ex.Message}");
        }
    }

    public async Task<Result<Season>> ArchiveEntityAsync(Season entity)
    {
        try
        {
            if (entity == null)
                return Result<Season>.Failure("Season entity cannot be null");

            if (entity.IsActive)
                return Result<Season>.Failure("Cannot archive an active season - end it first");

            // Archive the season using SeasonArchive
            var result = await SeasonArchive.ArchiveAsync(entity);
            if (result > 0)
            {
                // Remove from active cache
                await SeasonCache.RemoveSeasonAsync(entity.Id);

                // Publish archive event
                await EventBus.PublishAsync(new SeasonArchivedEvent(entity.Id.ToString()));

                return Result<Season>.CreateSuccess(entity);
            }

            return Result<Season>.Failure("Failed to archive season");
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex, "Failed to archive season");
            return Result<Season>.Failure($"Failed to archive season: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all season IDs that a team is participating in
    /// </summary>
    public async Task<Result<IEnumerable<string>>> GetTeamSeasonIdsAsync(string teamId)
    {
        try
        {
            // Get all seasons and extract IDs where team is participating
            var allSeasons = await SeasonRepository.GetAllAsync();
            var participatingSeasonIds = (allSeasons ?? Enumerable.Empty<Season>())
                .Where(s => s.ParticipatingTeams.ContainsKey(teamId))
                .Select(s => s.Id.ToString())
                .ToList();

            return Result<IEnumerable<string>>.CreateSuccess(participatingSeasonIds);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<IEnumerable<string>>.Failure($"Failed to get team season IDs: {ex.Message}");
        }
    }
}
