using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.ErrorHandling;
using WabbitBot.Core.Common.Data.Interface;
using WabbitBot.Core.Common.Data;
using WabbitBot.Core.Common.Data.Cache;
using WabbitBot.Core.Common;
using WabbitBot.Core.Common.Events;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Services;

/// <summary>
/// Team-specific business logic service operations with multi-source data access
/// </summary>
[GenerateEventPublisher(EventBusType = EventBusType.Core, EnableValidation = true, EnableTimestamps = true)]
public partial class TeamService : CoreService<Team>, ICoreDataService<Team>
{

    public TeamService()
        : base(CoreEventBus.Instance, CoreErrorHandler.Instance)
    {
    }

    private ITeamRepository TeamRepository =>
        WabbitBot.Core.Common.Data.DataServiceManager.TeamRepository;

    private ITeamCache TeamCache =>
        WabbitBot.Core.Common.Data.DataServiceManager.TeamCache;

    private ITeamArchive TeamArchive =>
        WabbitBot.Core.Common.Data.DataServiceManager.TeamArchive;


    /// <summary>
    /// Creates a new team with business logic validation
    /// </summary>
    public async Task<Result<Models.Team>> CreateTeamAsync(string teamName, int teamSize, string captainId)
    {
        try
        {
            // Use comprehensive validation
            var validation = await CoreValidation.ValidateForCreationAsync(teamName, teamSize, captainId, GetByNameAsync);
            if (!validation.Success)
                return Result<Models.Team>.Failure(validation.ErrorMessage ?? "Validation failed");

            // Create new team
            var team = new Models.Team
            {
                Id = Guid.NewGuid(),
                Name = teamName,
                TeamSize = (EvenTeamFormat)teamSize,
                TeamCaptainId = captainId,
                MaxRosterSize = teamSize,
                Roster = new List<TeamMember>(),
                CreatedAt = DateTime.UtcNow,
                LastActive = DateTime.UtcNow,
                IsArchived = false
            };

            // Use the interface method which handles cache and events
            return await CreateEntityAsync(team);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex, "Failed to create team");
            return Result<Models.Team>.Failure($"Failed to create team: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing team with business logic validation
    /// </summary>
    public async Task<Result<Models.Team>> UpdateTeamAsync(Models.Team team)
    {
        try
        {
            // Use validation
            var validation = CoreValidation.ValidateForUpdate(team);
            if (!validation.Success)
                return Result<Models.Team>.Failure(validation.ErrorMessage ?? "Validation failed");

            // Use the base class method which handles cache and events
            return await UpdateEntityAsync(team);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex, "Failed to update team");
            return Result<Models.Team>.Failure($"Failed to update team: {ex.Message}");
        }
    }

    /// <summary>
    /// Archives a team with business logic validation
    /// </summary>
    public async Task<Result<Models.Team>> ArchiveTeamAsync(Models.Team team)
    {
        try
        {
            // Use validation
            var validation = await CoreValidation.ValidateForArchivingAsync(team);
            if (!validation.Success)
                return Result<Models.Team>.Failure(validation.ErrorMessage ?? "Validation failed");

            // Use the base class method which handles cache and events
            return await ArchiveEntityAsync(team);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex, "Failed to archive team");
            return Result<Models.Team>.Failure($"Failed to archive team: {ex.Message}");
        }
    }

    /// <summary>
    /// Unarchives a team with business logic validation
    /// </summary>
    public async Task<Result<Models.Team>> UnarchiveTeamAsync(Models.Team team)
    {
        try
        {
            // Use validation
            var validation = CoreValidation.ValidateForUnarchiving(team);
            if (!validation.Success)
                return Result<Models.Team>.Failure(validation.ErrorMessage ?? "Validation failed");

            // Update team to unarchived
            team.IsArchived = false;
            team.ArchivedAt = null;
            team.UpdatedAt = DateTime.UtcNow;

            // Use the base class method which handles cache and events
            return await UpdateEntityAsync(team);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex, "Failed to unarchive team");
            return Result<Models.Team>.Failure($"Failed to unarchive team: {ex.Message}");
        }
    }



    /// <summary>
    /// Gets a team by name using multi-source data access
    /// </summary>
    public async Task<Models.Team?> GetByNameAsync(string teamName)
    {
        // Try cache first
        var cacheKey = $"team:name:{teamName}";
        var cached = await TeamCache.GetAsync(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        // Fallback to repository
        var team = await TeamRepository.GetByNameAsync(teamName);
        if (team != null)
        {
            await TeamCache.SetAsync(cacheKey, team, GetDefaultCacheExpiry());
        }

        return team;
    }

    /// <summary>
    /// Gets a team by tag using multi-source data access
    /// </summary>
    public async Task<Models.Team?> GetByTagAsync(string teamTag)
    {
        // Try cache first
        var cacheKey = $"team:tag:{teamTag}";
        var cached = await TeamCache.GetAsync(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        // Fallback to repository
        var team = await TeamRepository.GetByTagAsync(teamTag);
        if (team != null)
        {
            await TeamCache.SetAsync(cacheKey, team, GetDefaultCacheExpiry());
        }

        return team;
    }

    /// <summary>
    /// Searches teams using multi-source data access
    /// </summary>
    public async Task<IEnumerable<Models.Team>> SearchTeamsAsync(string searchTerm, int limit = 25)
    {
        return await SearchAsync(searchTerm, limit);
    }

    /// <summary>
    /// Searches teams by game size using multi-source data access
    /// </summary>
    public async Task<IEnumerable<Models.Team>> SearchTeamsByEvenTeamFormatAsync(string searchTerm, EvenTeamFormat evenTeamFormat, int limit = 25)
    {
        return await TeamRepository.SearchTeamsByEvenTeamFormatAsync(searchTerm, evenTeamFormat, limit);
    }

    /// <summary>
    /// Gets all teams where a user is a member
    /// </summary>
    public async Task<IEnumerable<Models.Team>> GetUserTeamsAsync(string userId)
    {
        try
        {
            var allTeams = await GetAllAsync();
            return allTeams.Where(team => team.Roster.Any(member => member.PlayerId == userId));
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex, "Failed to get teams by user");
            return Enumerable.Empty<Models.Team>();
        }
    }

    #region Helper Methods

    /// <summary>
    /// Generates a cache key for a single team entity
    /// </summary>
    private string GetCacheKey(object id)
    {
        return $"team:{id}";
    }

    /// <summary>
    /// Generates a cache key for team collections
    /// </summary>
    private string GetCollectionCacheKey()
    {
        return "teams:active";
    }

    /// <summary>
    /// Gets the default cache expiry time for teams
    /// </summary>
    private TimeSpan GetDefaultCacheExpiry()
    {
        return TimeSpan.FromHours(2); // Teams cache for 2 hours
    }

    /// <summary>
    /// Builds the WHERE clause for team search operations
    /// </summary>
    private string GetSearchWhereClause(string searchTerm, int limit)
    {
        return $"Name LIKE '%{searchTerm}%' OR Tag LIKE '%{searchTerm}%' LIMIT {limit}";
    }

    /// <summary>
    /// Invalidates collection cache when individual entities are modified
    /// </summary>
    private async Task InvalidateCollectionCache()
    {
        await TeamCache.RemoveActiveTeamsAsync();
    }

    #endregion


    #region ICoreDataService<Team> Implementation

    /// <summary>
    /// Creates a new team entity with business logic validation and event publishing
    /// </summary>
    public async Task<Result<Team>> CreateEntityAsync(Team entity)
    {
        try
        {
            // Add to repository
            var result = await TeamRepository.AddAsync(entity);

            if (result > 0)
            {
                // Update cache
                var cacheKey = GetCacheKey(entity.Id);
                await TeamCache.SetAsync(cacheKey, entity, GetDefaultCacheExpiry());

                // Invalidate collection cache
                await InvalidateCollectionCache();

                // Publish event using generated method
                await PublishTeamCreated(entity.Id);

                return Result<Team>.CreateSuccess(entity);
            }

            return Result<Team>.Failure("Failed to create team");
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex, "Failed to create team (legacy method)");
            return Result<Team>.Failure($"Failed to create team: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates a team entity with business logic validation and event publishing
    /// </summary>
    public async Task<Result<Team>> UpdateEntityAsync(Team entity)
    {
        try
        {
            // Update repository
            var result = await TeamRepository.UpdateAsync(entity);

            if (result)
            {
                // Update cache
                var cacheKey = GetCacheKey(entity.Id);
                await TeamCache.SetAsync(cacheKey, entity, GetDefaultCacheExpiry());

                // Invalidate collection cache
                await InvalidateCollectionCache();

                // Publish event using generated publisher method
                await PublishTeamUpdated(entity.Id);

                return Result<Team>.CreateSuccess(entity);
            }

            return Result<Team>.Failure("Failed to update team");
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex, "Failed to update team (legacy method)");
            return Result<Team>.Failure($"Failed to update team: {ex.Message}");
        }
    }

    /// <summary>
    /// Archives a team entity with business logic validation and event publishing
    /// </summary>
    public async Task<Result<Team>> ArchiveEntityAsync(Team entity)
    {
        try
        {
            // Use existing validation
            var validation = await CoreValidation.ValidateForArchivingAsync(entity);
            if (!validation.Success)
                return Result<Team>.Failure(validation.ErrorMessage ?? "Validation failed");

            // Archive the entity
            var result = await TeamArchive.ArchiveAsync(entity);

            if (result > 0)
            {
                // Remove from active cache
                var cacheKey = GetCacheKey(entity.Id);
                await TeamCache.RemoveAsync(cacheKey);

                // Invalidate collection cache
                await InvalidateCollectionCache();

                // Publish event using generated publisher method
                await PublishTeamArchived(entity.Id);

                return Result<Team>.CreateSuccess(entity);
            }

            return Result<Team>.Failure("Failed to archive team");
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex, "Failed to archive team (legacy method)");
            return Result<Team>.Failure($"Failed to archive team: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a team entity with business logic validation and event publishing
    /// </summary>
    public async Task<Result<Team>> DeleteEntityAsync(object id)
    {
        try
        {
            // Get entity before deletion for event publishing
            var entity = await GetByIdAsync(id);
            if (entity == null)
            {
                return Result<Team>.Failure("Team not found");
            }

            // Delete from repository
            var result = await TeamRepository.DeleteAsync(id);

            if (result)
            {
                // Remove from cache
                var cacheKey = GetCacheKey(id);
                await TeamCache.RemoveAsync(cacheKey);

                // Invalidate collection cache
                await InvalidateCollectionCache();

                // Publish event using generated publisher method
                await PublishTeamDeleted(entity.Id);

                return Result<Team>.CreateSuccess(entity);
            }

            return Result<Team>.Failure("Failed to delete team");
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex, "Failed to delete team (legacy method)");
            return Result<Team>.Failure($"Failed to delete team: {ex.Message}");
        }
    }

    #endregion

    #region IBaseDataService<Team> Implementation

    /// <summary>
    /// Gets a team by ID using cache-first strategy
    /// </summary>
    public async Task<Team?> GetByIdAsync(object id)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        var cacheKey = GetCacheKey(id);

        // Cache-first strategy
        var cached = await TeamCache.GetAsync(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        // Fallback to repository
        var entity = await TeamRepository.GetByIdAsync(id);
        if (entity != null)
        {
            await TeamCache.SetAsync(cacheKey, entity, GetDefaultCacheExpiry());
        }

        return entity;
    }

    /// <summary>
    /// Gets all teams using cache-first strategy
    /// </summary>
    public async Task<IEnumerable<Team>> GetAllAsync()
    {
        // Try cache first
        var cached = await TeamCache.GetActiveTeamsAsync();
        if (cached != null && cached.Teams.Any())
        {
            return cached.GetFilteredTeams();
        }

        // Fallback to repository
        var entities = await TeamRepository.GetAllAsync();
        if (entities.Any())
        {
            // TODO: Update to use CoreService methods once Team methods are implemented
            // For now, skip caching since ListWrapper classes have been eliminated
            // await TeamCache.SetActiveTeamsAsync(entities, GetDefaultCacheExpiry());
        }

        return entities;
    }

    /// <summary>
    /// Searches for teams across multiple data sources
    /// </summary>
    public async Task<IEnumerable<Team>> SearchAsync(string searchTerm, int limit = 25)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            throw new ArgumentException("Search term cannot be null or empty", nameof(searchTerm));
        }

        // For search operations, we typically want fresh data from repository
        return await TeamRepository.QueryAsync(GetSearchWhereClause(searchTerm, limit));
    }

    /// <summary>
    /// Adds a team to repository and updates cache
    /// </summary>
    public async Task<int> AddAsync(Team entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        // Add to repository
        var result = await TeamRepository.AddAsync(entity);

        if (result > 0)
        {
            // Update cache
            var cacheKey = GetCacheKey(entity.Id);
            await TeamCache.SetAsync(cacheKey, entity, GetDefaultCacheExpiry());

            // Invalidate collection cache
            await InvalidateCollectionCache();
        }

        return result;
    }

    /// <summary>
    /// Updates a team in repository and cache
    /// </summary>
    public async Task<bool> UpdateAsync(Team entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        // Update repository
        var result = await TeamRepository.UpdateAsync(entity);

        if (result)
        {
            // Update cache
            var cacheKey = GetCacheKey(entity.Id);
            await TeamCache.SetAsync(cacheKey, entity, GetDefaultCacheExpiry());

            // Invalidate collection cache
            await InvalidateCollectionCache();
        }

        return result;
    }

    /// <summary>
    /// Deletes a team from repository and cache
    /// </summary>
    public async Task<bool> DeleteAsync(object id)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        // Delete from repository
        var result = await TeamRepository.DeleteAsync(id);

        if (result)
        {
            // Remove from cache
            var cacheKey = GetCacheKey(id);
            await TeamCache.RemoveAsync(cacheKey);

            // Invalidate collection cache
            await InvalidateCollectionCache();
        }

        return result;
    }

    /// <summary>
    /// Archives a team and removes from active cache
    /// </summary>
    public async Task<bool> ArchiveAsync(Team entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        // Archive the entity
        var result = await TeamArchive.ArchiveAsync(entity);

        if (result > 0)
        {
            // Remove from active cache
            var cacheKey = GetCacheKey(entity.Id);
            await TeamCache.RemoveAsync(cacheKey);

            // Invalidate collection cache
            await InvalidateCollectionCache();
        }

        return result > 0;
    }

    /// <summary>
    /// Gets a team by ID with business logic
    /// </summary>
    public async Task<Result<Team>> GetTeamAsync(string teamId)
    {
        try
        {
            var team = await GetByIdAsync(teamId);
            if (team == null)
                return Result<Team>.Failure("Team not found");

            return Result<Team>.CreateSuccess(team);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<Team>.Failure($"Failed to get team: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a team exists using cache-first strategy
    /// </summary>
    public async Task<bool> ExistsAsync(object id)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        var cacheKey = GetCacheKey(id);

        // Check cache first
        if (await TeamCache.ExistsAsync(cacheKey))
        {
            return true;
        }

        // Fallback to repository
        return await TeamRepository.ExistsAsync(id);
    }

    #endregion



    /// <summary>
    /// Creates a 1v1 team for a player if it doesn't exist
    /// </summary>
    public async Task<Result<Team>> EnsurePlayerOneVOneTeamAsync(string playerId, string playerName)
    {
        try
        {
            var teamId = $"{playerId}_1v1";

            // Check if the 1v1 team already exists
            var existingTeamResult = await GetTeamAsync(teamId);
            if (existingTeamResult.Success)
                return Result<Team>.CreateSuccess(existingTeamResult.Data!);

            // Create the 1v1 team (team size = 1 for 1v1)
            var createResult = await CreateTeamAsync($"{playerName}'s 1v1 Team", 1, playerId);
            if (!createResult.Success)
                return Result<Team>.Failure(createResult.ErrorMessage ?? "Failed to create 1v1 team");

            var team = createResult.Data;

            // Mark this as a 1v1 team (if we add that property later)
            // For now, the naming convention indicates it's a 1v1 team

            return Result<Team>.CreateSuccess(team!);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<Team>.Failure($"Failed to ensure 1v1 team: {ex.Message}");
        }
    }
}