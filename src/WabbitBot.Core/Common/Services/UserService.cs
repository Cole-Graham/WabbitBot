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

namespace WabbitBot.Core.Common.Services;

/// <summary>
/// User-specific business logic service operations with multi-source data access
/// </summary>
[GenerateEventPublisher(EventBusType = EventBusType.Core, EnableValidation = true, EnableTimestamps = true)]
public partial class UserService : CoreService, ICoreDataService<User>
{

    public UserService()
        : base(CoreEventBus.Instance, CoreErrorHandler.Instance)
    {
    }

    private IUserRepository UserRepository =>
        WabbitBot.Core.Common.Data.DataServiceManager.UserRepository;

    private IUserCache UserCache =>
        WabbitBot.Core.Common.Data.DataServiceManager.UserCache;

    private IUserArchive UserArchive =>
        WabbitBot.Core.Common.Data.DataServiceManager.UserArchive;


    /// <summary>
    /// Creates a new user with business logic validation
    /// </summary>
    public async Task<Result<Models.User>> CreateUserAsync(ulong discordId, string username)
    {
        try
        {
            // Basic validation
            if (discordId == 0)
                return Result<Models.User>.Failure("Discord ID cannot be zero");

            if (string.IsNullOrWhiteSpace(username))
                return Result<Models.User>.Failure("Username cannot be empty");

            // Check if user already exists
            var existingUser = await GetByDiscordIdAsync(discordId);
            if (existingUser != null)
            {
                return Result<Models.User>.Failure($"User with Discord ID '{discordId}' already exists");
            }

            // Create new user
            var user = new Models.User
            {
                Id = Guid.NewGuid(),
                DiscordId = discordId.ToString(),
                Username = username,
                CreatedAt = DateTime.UtcNow,
                LastActive = DateTime.UtcNow,
                IsActive = true
            };

            // Use the interface method which handles cache and events
            return await CreateEntityAsync(user);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<Models.User>.Failure($"Failed to create user: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing user with business logic validation
    /// </summary>
    public async Task<Result<Models.User>> UpdateUserAsync(Models.User user)
    {
        try
        {
            // Basic validation
            if (user == null)
                return Result<Models.User>.Failure("User cannot be null");

            if (string.IsNullOrWhiteSpace(user.Username))
                return Result<Models.User>.Failure("Username cannot be empty");

            // Use the base class method which handles cache and events
            return await UpdateEntityAsync(user);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<Models.User>.Failure($"Failed to update user: {ex.Message}");
        }
    }

    /// <summary>
    /// Archives a user with business logic validation
    /// </summary>
    public async Task<Result<Models.User>> ArchiveUserAsync(Models.User user)
    {
        try
        {
            // Basic validation
            if (user == null)
                return Result<Models.User>.Failure("User cannot be null");

            if (!user.IsActive)
                return Result<Models.User>.Failure("User is already inactive");

            // Use the base class method which handles cache and events
            return await ArchiveEntityAsync(user);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<Models.User>.Failure($"Failed to archive user: {ex.Message}");
        }
    }

    /// <summary>
    /// Unarchives a user with business logic validation
    /// </summary>
    public async Task<Result<Models.User>> UnarchiveUserAsync(Models.User user)
    {
        try
        {
            // Basic validation
            if (user == null)
                return Result<Models.User>.Failure("User cannot be null");

            if (user.IsActive)
                return Result<Models.User>.Failure("User is already active");

            // Update user to active
            user.IsActive = true;
            user.LastActive = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            // Use the base class method which handles cache and events
            return await UpdateEntityAsync(user);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<Models.User>.Failure($"Failed to unarchive user: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a user by Discord ID using multi-source data access
    /// </summary>
    public async Task<Models.User?> GetByDiscordIdAsync(ulong discordId)
    {
        // Try cache first
        var cached = await UserCache.GetByDiscordIdAsync(discordId);
        if (cached != null)
        {
            return cached;
        }

        // Fallback to repository
        var user = await UserRepository.GetByDiscordIdAsync(discordId);
        if (user != null)
        {
            await UserCache.SetByDiscordIdAsync(discordId, user, GetDefaultCacheExpiry());
        }

        return user;
    }

    /// <summary>
    /// Gets a user by username using multi-source data access
    /// </summary>
    public async Task<Models.User?> GetByUsernameAsync(string username)
    {
        // Try cache first
        var cached = await UserCache.GetByUsernameAsync(username);
        if (cached != null)
        {
            return cached;
        }

        // Fallback to repository
        var user = await UserRepository.GetByUsernameAsync(username);
        if (user != null)
        {
            await UserCache.SetByUsernameAsync(username, user, GetDefaultCacheExpiry());
        }

        return user;
    }

    #region Helper Methods

    /// <summary>
    /// Generates a cache key for a single user entity
    /// </summary>
    private string GetCacheKey(object id)
    {
        return $"user:{id}";
    }

    /// <summary>
    /// Generates a cache key for user collections
    /// </summary>
    private string GetCollectionCacheKey()
    {
        return "users:active";
    }

    /// <summary>
    /// Gets the default cache expiry time for users
    /// </summary>
    private TimeSpan GetDefaultCacheExpiry()
    {
        return TimeSpan.FromHours(2); // Users cache for 2 hours
    }

    /// <summary>
    /// Builds the WHERE clause for user search operations
    /// </summary>
    private string GetSearchWhereClause(string searchTerm, int limit)
    {
        return $"Username LIKE '%{searchTerm}%' OR DiscordId LIKE '%{searchTerm}%' LIMIT {limit}";
    }

    /// <summary>
    /// Invalidates collection cache when individual entities are modified
    /// </summary>
    private async Task InvalidateCollectionCache()
    {
        await UserCache.RemoveActiveUsersAsync();
    }

    #endregion


    #region ICoreDataService<User> Implementation

    /// <summary>
    /// Creates a new user entity with business logic validation and event publishing
    /// </summary>
    public async Task<Result<User>> CreateEntityAsync(User entity)
    {
        try
        {
            // Add to repository
            var result = await UserRepository.AddAsync(entity);

            if (result > 0)
            {
                // Update cache
                var cacheKey = GetCacheKey(entity.Id);
                await UserCache.SetAsync(cacheKey, entity, GetDefaultCacheExpiry());

                // Invalidate collection cache
                await InvalidateCollectionCache();

                // Publish event using generated publisher method
                await PublishUserCreated(entity.Id.ToString());

                return Result<User>.CreateSuccess(entity);
            }

            return Result<User>.Failure("Failed to create user");
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<User>.Failure($"Failed to create user: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates a user entity with business logic validation and event publishing
    /// </summary>
    public async Task<Result<User>> UpdateEntityAsync(User entity)
    {
        try
        {
            // Update repository
            var result = await UserRepository.UpdateAsync(entity);

            if (result)
            {
                // Update cache
                var cacheKey = GetCacheKey(entity.Id);
                await UserCache.SetAsync(cacheKey, entity, GetDefaultCacheExpiry());

                // Invalidate collection cache
                await InvalidateCollectionCache();

                // Publish event using generated publisher method
                await PublishUserUpdated(entity.Id.ToString());

                return Result<User>.CreateSuccess(entity);
            }

            return Result<User>.Failure("Failed to update user");
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<User>.Failure($"Failed to update user: {ex.Message}");
        }
    }

    /// <summary>
    /// Archives a user entity with business logic validation and event publishing
    /// </summary>
    public async Task<Result<User>> ArchiveEntityAsync(User entity)
    {
        try
        {
            // Archive the entity
            var result = await UserArchive.ArchiveAsync(entity);

            if (result > 0)
            {
                // Remove from active cache
                var cacheKey = GetCacheKey(entity.Id);
                await UserCache.RemoveAsync(cacheKey);

                // Invalidate collection cache
                await InvalidateCollectionCache();

                // Publish event using generated publisher method
                await PublishUserArchived(entity.Id.ToString());

                return Result<User>.CreateSuccess(entity);
            }

            return Result<User>.Failure("Failed to archive user");
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<User>.Failure($"Failed to archive user: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a user entity with business logic validation and event publishing
    /// </summary>
    public async Task<Result<User>> DeleteEntityAsync(object id)
    {
        try
        {
            // Get entity before deletion for event publishing
            var entity = await GetByIdAsync(id);
            if (entity == null)
            {
                return Result<User>.Failure("User not found");
            }

            // Delete from repository
            var result = await UserRepository.DeleteAsync(id);

            if (result)
            {
                // Remove from cache
                var cacheKey = GetCacheKey(id);
                await UserCache.RemoveAsync(cacheKey);

                // Invalidate collection cache
                await InvalidateCollectionCache();

                // Publish event using generated publisher method
                await PublishUserDeleted(entity.Id.ToString());

                return Result<User>.CreateSuccess(entity);
            }

            return Result<User>.Failure("Failed to delete user");
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<User>.Failure($"Failed to delete user: {ex.Message}");
        }
    }

    #endregion

    #region IBaseDataService<User> Implementation

    /// <summary>
    /// Gets a user by ID using cache-first strategy
    /// </summary>
    public async Task<User?> GetByIdAsync(object id)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        var cacheKey = GetCacheKey(id);

        // Cache-first strategy
        var cached = await UserCache.GetAsync(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        // Fallback to repository
        var entity = await UserRepository.GetByIdAsync(id);
        if (entity != null)
        {
            await UserCache.SetAsync(cacheKey, entity, GetDefaultCacheExpiry());
        }

        return entity;
    }

    /// <summary>
    /// Gets all users using cache-first strategy
    /// </summary>
    public async Task<IEnumerable<User>> GetAllAsync()
    {
        // Try cache first
        var cached = await UserCache.GetActiveUsersAsync();
        if (cached != null && cached.Any())
        {
            return cached;
        }

        // Fallback to repository
        var entities = await UserRepository.GetAllAsync();
        if (entities.Any())
        {
            await UserCache.SetActiveUsersAsync(entities, GetDefaultCacheExpiry());
        }

        return entities;
    }

    /// <summary>
    /// Searches for users across multiple data sources
    /// </summary>
    public async Task<IEnumerable<User>> SearchAsync(string searchTerm, int limit = 25)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            throw new ArgumentException("Search term cannot be null or empty", nameof(searchTerm));
        }

        // For search operations, we typically want fresh data from repository
        return await UserRepository.QueryAsync(GetSearchWhereClause(searchTerm, limit));
    }

    /// <summary>
    /// Adds a user to repository and updates cache
    /// </summary>
    public async Task<int> AddAsync(User entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        // Add to repository
        var result = await UserRepository.AddAsync(entity);

        if (result > 0)
        {
            // Update cache
            var cacheKey = GetCacheKey(entity.Id);
            await UserCache.SetAsync(cacheKey, entity, GetDefaultCacheExpiry());

            // Invalidate collection cache
            await InvalidateCollectionCache();
        }

        return result;
    }

    /// <summary>
    /// Updates a user in repository and cache
    /// </summary>
    public async Task<bool> UpdateAsync(User entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        // Update repository
        var result = await UserRepository.UpdateAsync(entity);

        if (result)
        {
            // Update cache
            var cacheKey = GetCacheKey(entity.Id);
            await UserCache.SetAsync(cacheKey, entity, GetDefaultCacheExpiry());

            // Invalidate collection cache
            await InvalidateCollectionCache();
        }

        return result;
    }

    /// <summary>
    /// Deletes a user from repository and cache
    /// </summary>
    public async Task<bool> DeleteAsync(object id)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        // Delete from repository
        var result = await UserRepository.DeleteAsync(id);

        if (result)
        {
            // Remove from cache
            var cacheKey = GetCacheKey(id);
            await UserCache.RemoveAsync(cacheKey);

            // Invalidate collection cache
            await InvalidateCollectionCache();
        }

        return result;
    }

    /// <summary>
    /// Archives a user and removes from active cache
    /// </summary>
    public async Task<bool> ArchiveAsync(User entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        // Archive the entity
        var result = await UserArchive.ArchiveAsync(entity);

        if (result > 0)
        {
            // Remove from active cache
            var cacheKey = GetCacheKey(entity.Id);
            await UserCache.RemoveAsync(cacheKey);

            // Invalidate collection cache
            await InvalidateCollectionCache();
        }

        return result > 0;
    }

    /// <summary>
    /// Checks if a user exists using cache-first strategy
    /// </summary>
    public async Task<bool> ExistsAsync(object id)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        var cacheKey = GetCacheKey(id);

        // Check cache first
        if (await UserCache.ExistsAsync(cacheKey))
        {
            return true;
        }

        // Fallback to repository
        return await UserRepository.ExistsAsync(id);
    }

    #endregion
}