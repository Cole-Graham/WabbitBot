// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using WabbitBot.Core.Common.Models;
// using WabbitBot.Core.Common.BotCore;
// using WabbitBot.Common.Events.EventInterfaces;
// using WabbitBot.Common.ErrorHandling;
// using WabbitBot.Core.Common.Data.Interface;
// using WabbitBot.Core.Common.Data;
// using WabbitBot.Core.Common.Data.Cache;
// using WabbitBot.Core.Common;
// using WabbitBot.Core.Common.Events;
// using WabbitBot.Common.Attributes;

// namespace WabbitBot.Core.Common.Services;

// /// <summary>
// /// Player-specific business logic service operations with multi-source data access
// /// </summary>
// [GenerateEventPublisher(EventBusType = EventBusType.Core, EnableValidation = true, EnableTimestamps = true)]
// public partial class PlayerService : CoreService, ICoreDataService<Player>
// {

//     public PlayerService()
//         : base(CoreEventBus.Instance, CoreErrorHandler.Instance)
//     {
//     }

//     private IPlayerRepository PlayerRepository =>
//         WabbitBot.Core.Common.Data.DataServiceManager.PlayerRepository;

//     private IPlayerCache PlayerCache =>
//         WabbitBot.Core.Common.Data.DataServiceManager.PlayerCache;

//     private IPlayerArchive PlayerArchive =>
//         WabbitBot.Core.Common.Data.DataServiceManager.PlayerArchive;


//     /// <summary>
//     /// Creates a new player with business logic validation
//     /// </summary>
//     public async Task<Result<Models.Player>> CreatePlayerAsync(string playerName)
//     {
//         try
//         {
//             // Basic validation
//             if (string.IsNullOrWhiteSpace(playerName))
//                 return Result<Models.Player>.Failure("Player name cannot be empty");

//             // Check if player name already exists
//             var existingPlayer = await GetByNameAsync(playerName);
//             if (existingPlayer != null)
//             {
//                 return Result<Models.Player>.Failure($"Player with name '{playerName}' already exists");
//             }

//             // Create new player
//             var player = new Models.Player
//             {
//                 Id = Guid.NewGuid(),
//                 Name = playerName,
//                 CreatedAt = DateTime.UtcNow,
//                 LastActive = DateTime.UtcNow,
//                 IsArchived = false
//             };

//             // Use the interface method which handles cache and events
//             return await CreateEntityAsync(player);
//         }
//         catch (Exception ex)
//         {
//             await ErrorHandler.HandleErrorAsync(ex);
//             return Result<Models.Player>.Failure($"Failed to create player: {ex.Message}");
//         }
//     }

//     /// <summary>
//     /// Updates an existing player with business logic validation
//     /// </summary>
//     public async Task<Result<Models.Player>> UpdatePlayerAsync(Models.Player player)
//     {
//         try
//         {
//             // Basic validation
//             if (player == null)
//                 return Result<Models.Player>.Failure("Player cannot be null");

//             if (string.IsNullOrWhiteSpace(player.Name))
//                 return Result<Models.Player>.Failure("Player name cannot be empty");

//             // Use the base class method which handles cache and events
//             return await UpdateEntityAsync(player);
//         }
//         catch (Exception ex)
//         {
//             await ErrorHandler.HandleErrorAsync(ex);
//             return Result<Models.Player>.Failure($"Failed to update player: {ex.Message}");
//         }
//     }

//     /// <summary>
//     /// Archives a player with business logic validation
//     /// </summary>
//     public async Task<Result<Models.Player>> ArchivePlayerAsync(Models.Player player)
//     {
//         try
//         {
//             // Basic validation
//             if (player == null)
//                 return Result<Models.Player>.Failure("Player cannot be null");

//             if (player.IsArchived)
//                 return Result<Models.Player>.Failure("Player is already archived");

//             // Use the base class method which handles cache and events
//             return await ArchiveEntityAsync(player);
//         }
//         catch (Exception ex)
//         {
//             await ErrorHandler.HandleErrorAsync(ex);
//             return Result<Models.Player>.Failure($"Failed to archive player: {ex.Message}");
//         }
//     }

//     /// <summary>
//     /// Gets a player by name using multi-source data access
//     /// </summary>
//     public async Task<Models.Player?> GetByNameAsync(string playerName)
//     {
//         // Try cache first
//         var cached = await PlayerCache.GetByNameAsync(playerName);
//         if (cached != null)
//         {
//             return cached;
//         }

//         // Fallback to repository
//         var player = await PlayerRepository.GetByNameAsync(playerName);
//         if (player != null)
//         {
//             await PlayerCache.SetByNameAsync(playerName, player, GetDefaultCacheExpiry());
//         }

//         return player;
//     }

//     #region Helper Methods

//     /// <summary>
//     /// Generates a cache key for a single player entity
//     /// </summary>
//     private string GetCacheKey(object id)
//     {
//         return $"player:{id}";
//     }

//     /// <summary>
//     /// Generates a cache key for player collections
//     /// </summary>
//     private string GetCollectionCacheKey()
//     {
//         return "players:active";
//     }

//     /// <summary>
//     /// Gets the default cache expiry time for players
//     /// </summary>
//     private TimeSpan GetDefaultCacheExpiry()
//     {
//         return TimeSpan.FromHours(2); // Players cache for 2 hours
//     }

//     /// <summary>
//     /// Builds the WHERE clause for player search operations
//     /// </summary>
//     private string GetSearchWhereClause(string searchTerm, int limit)
//     {
//         return $"Name LIKE '%{searchTerm}%' LIMIT {limit}";
//     }

//     /// <summary>
//     /// Invalidates collection cache when individual entities are modified
//     /// </summary>
//     private async Task InvalidateCollectionCache()
//     {
//         await PlayerCache.RemoveActivePlayersAsync();
//     }

//     #endregion


//     #region ICoreDataService<Player> Implementation

//     /// <summary>
//     /// Creates a new player entity with business logic validation and event publishing
//     /// </summary>
//     public async Task<Result<Player>> CreateEntityAsync(Player entity)
//     {
//         try
//         {
//             // Add to repository
//             var result = await PlayerRepository.AddAsync(entity);

//             if (result > 0)
//             {
//                 // Update cache
//                 var cacheKey = GetCacheKey(entity.Id);
//                 await PlayerCache.SetAsync(cacheKey, entity, GetDefaultCacheExpiry());

//                 // Invalidate collection cache
//                 await InvalidateCollectionCache();

//                 // Publish event using generated publisher method
//                 await PublishPlayerCreated(entity.Id.ToString());

//                 return Result<Player>.CreateSuccess(entity);
//             }

//             return Result<Player>.Failure("Failed to create player");
//         }
//         catch (Exception ex)
//         {
//             await ErrorHandler.HandleErrorAsync(ex);
//             return Result<Player>.Failure($"Failed to create player: {ex.Message}");
//         }
//     }

//     /// <summary>
//     /// Updates a player entity with business logic validation and event publishing
//     /// </summary>
//     public async Task<Result<Player>> UpdateEntityAsync(Player entity)
//     {
//         try
//         {
//             // Update repository
//             var result = await PlayerRepository.UpdateAsync(entity);

//             if (result)
//             {
//                 // Update cache
//                 var cacheKey = GetCacheKey(entity.Id);
//                 await PlayerCache.SetAsync(cacheKey, entity, GetDefaultCacheExpiry());

//                 // Invalidate collection cache
//                 await InvalidateCollectionCache();

//                 // Publish event using generated publisher method
//                 await PublishPlayerUpdated(entity.Id.ToString());

//                 return Result<Player>.CreateSuccess(entity);
//             }

//             return Result<Player>.Failure("Failed to update player");
//         }
//         catch (Exception ex)
//         {
//             await ErrorHandler.HandleErrorAsync(ex);
//             return Result<Player>.Failure($"Failed to update player: {ex.Message}");
//         }
//     }

//     /// <summary>
//     /// Archives a player entity with business logic validation and event publishing
//     /// </summary>
//     public async Task<Result<Player>> ArchiveEntityAsync(Player entity)
//     {
//         try
//         {
//             // Archive the entity
//             var result = await PlayerArchive.ArchiveAsync(entity);

//             if (result > 0)
//             {
//                 // Remove from active cache
//                 var cacheKey = GetCacheKey(entity.Id);
//                 await PlayerCache.RemoveAsync(cacheKey);

//                 // Invalidate collection cache
//                 await InvalidateCollectionCache();

//                 // Publish event using generated publisher method
//                 await PublishPlayerArchived(entity.Id.ToString());

//                 return Result<Player>.CreateSuccess(entity);
//             }

//             return Result<Player>.Failure("Failed to archive player");
//         }
//         catch (Exception ex)
//         {
//             await ErrorHandler.HandleErrorAsync(ex);
//             return Result<Player>.Failure($"Failed to archive player: {ex.Message}");
//         }
//     }

//     /// <summary>
//     /// Deletes a player entity with business logic validation and event publishing
//     /// </summary>
//     public async Task<Result<Player>> DeleteEntityAsync(object id)
//     {
//         try
//         {
//             // Get entity before deletion for event publishing
//             var entity = await GetByIdAsync(id);
//             if (entity == null)
//             {
//                 return Result<Player>.Failure("Player not found");
//             }

//             // Delete from repository
//             var result = await PlayerRepository.DeleteAsync(id);

//             if (result)
//             {
//                 // Remove from cache
//                 var cacheKey = GetCacheKey(id);
//                 await PlayerCache.RemoveAsync(cacheKey);

//                 // Invalidate collection cache
//                 await InvalidateCollectionCache();

//                 // Publish event using generated publisher method
//                 await PublishPlayerDeleted(entity.Id.ToString());

//                 return Result<Player>.CreateSuccess(entity);
//             }

//             return Result<Player>.Failure("Failed to delete player");
//         }
//         catch (Exception ex)
//         {
//             await ErrorHandler.HandleErrorAsync(ex);
//             return Result<Player>.Failure($"Failed to delete player: {ex.Message}");
//         }
//     }

//     #endregion

//     #region IBaseDataService<Player> Implementation

//     /// <summary>
//     /// Gets a player by ID using cache-first strategy
//     /// </summary>
//     public async Task<Player?> GetByIdAsync(object id)
//     {
//         if (id == null)
//         {
//             throw new ArgumentNullException(nameof(id));
//         }

//         var cacheKey = GetCacheKey(id);

//         // Cache-first strategy
//         var cached = await PlayerCache.GetAsync(cacheKey);
//         if (cached != null)
//         {
//             return cached;
//         }

//         // Fallback to repository
//         var entity = await PlayerRepository.GetByIdAsync(id);
//         if (entity != null)
//         {
//             await PlayerCache.SetAsync(cacheKey, entity, GetDefaultCacheExpiry());
//         }

//         return entity;
//     }

//     /// <summary>
//     /// Gets all players using cache-first strategy
//     /// </summary>
//     public async Task<IEnumerable<Player>> GetAllAsync()
//     {
//         // Try cache first
//         var cached = await PlayerCache.GetActivePlayersAsync();
//         if (cached != null && cached.Any())
//         {
//             return cached;
//         }

//         // Fallback to repository
//         var entities = await PlayerRepository.GetAllAsync();
//         if (entities.Any())
//         {
//             await PlayerCache.SetActivePlayersAsync(entities, GetDefaultCacheExpiry());
//         }

//         return entities;
//     }

//     /// <summary>
//     /// Searches for players across multiple data sources
//     /// </summary>
//     public async Task<IEnumerable<Player>> SearchAsync(string searchTerm, int limit = 25)
//     {
//         if (string.IsNullOrWhiteSpace(searchTerm))
//         {
//             throw new ArgumentException("Search term cannot be null or empty", nameof(searchTerm));
//         }

//         // For search operations, we typically want fresh data from repository
//         return await PlayerRepository.QueryAsync(GetSearchWhereClause(searchTerm, limit));
//     }

//     /// <summary>
//     /// Adds a player to repository and updates cache
//     /// </summary>
//     public async Task<int> AddAsync(Player entity)
//     {
//         if (entity == null)
//         {
//             throw new ArgumentNullException(nameof(entity));
//         }

//         // Add to repository
//         var result = await PlayerRepository.AddAsync(entity);

//         if (result > 0)
//         {
//             // Update cache
//             var cacheKey = GetCacheKey(entity.Id);
//             await PlayerCache.SetAsync(cacheKey, entity, GetDefaultCacheExpiry());

//             // Invalidate collection cache
//             await InvalidateCollectionCache();
//         }

//         return result;
//     }

//     /// <summary>
//     /// Updates a player in repository and cache
//     /// </summary>
//     public async Task<bool> UpdateAsync(Player entity)
//     {
//         if (entity == null)
//         {
//             throw new ArgumentNullException(nameof(entity));
//         }

//         // Update repository
//         var result = await PlayerRepository.UpdateAsync(entity);

//         if (result)
//         {
//             // Update cache
//             var cacheKey = GetCacheKey(entity.Id);
//             await PlayerCache.SetAsync(cacheKey, entity, GetDefaultCacheExpiry());

//             // Invalidate collection cache
//             await InvalidateCollectionCache();
//         }

//         return result;
//     }

//     /// <summary>
//     /// Deletes a player from repository and cache
//     /// </summary>
//     public async Task<bool> DeleteAsync(object id)
//     {
//         if (id == null)
//         {
//             throw new ArgumentNullException(nameof(id));
//         }

//         // Delete from repository
//         var result = await PlayerRepository.DeleteAsync(id);

//         if (result)
//         {
//             // Remove from cache
//             var cacheKey = GetCacheKey(id);
//             await PlayerCache.RemoveAsync(cacheKey);

//             // Invalidate collection cache
//             await InvalidateCollectionCache();
//         }

//         return result;
//     }

//     /// <summary>
//     /// Archives a player and removes from active cache
//     /// </summary>
//     public async Task<bool> ArchiveAsync(Player entity)
//     {
//         if (entity == null)
//         {
//             throw new ArgumentNullException(nameof(entity));
//         }

//         // Archive the entity
//         var result = await PlayerArchive.ArchiveAsync(entity);

//         if (result > 0)
//         {
//             // Remove from active cache
//             var cacheKey = GetCacheKey(entity.Id);
//             await PlayerCache.RemoveAsync(cacheKey);

//             // Invalidate collection cache
//             await InvalidateCollectionCache();
//         }

//         return result > 0;
//     }

//     /// <summary>
//     /// Checks if a player exists using cache-first strategy
//     /// </summary>
//     public async Task<bool> ExistsAsync(object id)
//     {
//         if (id == null)
//         {
//             throw new ArgumentNullException(nameof(id));
//         }

//         var cacheKey = GetCacheKey(id);

//         // Check cache first
//         if (await PlayerCache.ExistsAsync(cacheKey))
//         {
//             return true;
//         }

//         // Fallback to repository
//         return await PlayerRepository.ExistsAsync(id);
//     }

//     #endregion

//     /// <summary>
//     /// Unarchives a player (business logic)
//     /// </summary>
//     public async Task<Result<Models.Player>> UnarchivePlayerAsync(Models.Player player)
//     {
//         try
//         {
//             // Basic validation
//             if (player == null)
//                 return Result<Models.Player>.Failure("Player cannot be null");

//             if (!player.IsArchived)
//                 return Result<Models.Player>.Failure("Player is not archived");

//             // Update player to unarchived
//             player.IsArchived = false;
//             player.ArchivedAt = null;
//             player.UpdatedAt = DateTime.UtcNow;

//             // Use the base class method which handles cache and events
//             return await UpdateEntityAsync(player);
//         }
//         catch (Exception ex)
//         {
//             await ErrorHandler.HandleErrorAsync(ex);
//             return Result<Models.Player>.Failure($"Failed to unarchive player: {ex.Message}");
//         }
//     }
// }
