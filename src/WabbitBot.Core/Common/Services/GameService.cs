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
using WabbitBot.Core.Common;
using WabbitBot.Core.Common.Events;
using WabbitBot.Common.Attributes;

namespace WabbitBot.Core.Common.Services;

/// <summary>
/// Game-specific business logic service operations with multi-source data access
/// </summary>
[GenerateEventPublisher(EventBusType = EventBusType.Core, EnableValidation = true, EnableTimestamps = true)]
public partial class GameService : CoreService, ICoreDataService<Game>
{

    public GameService()
        : base(CoreEventBus.Instance, CoreErrorHandler.Instance)
    {
    }

    private IGameRepository GameRepository =>
        WabbitBot.Core.Common.Data.DataServiceManager.GameRepository;

    private IGameCache GameCache =>
        WabbitBot.Core.Common.Data.DataServiceManager.GameCache;

    private IGameArchive GameArchive =>
        WabbitBot.Core.Common.Data.DataServiceManager.GameArchive;


    /// <summary>
    /// Creates a new game with business logic validation
    /// </summary>
    public async Task<Result<Game>> CreateGameAsync(string matchId, string mapId, EvenTeamFormat evenTeamFormat,
        List<string> team1PlayerIds, List<string> team2PlayerIds, int gameNumber)
    {
        try
        {
            // Use comprehensive validation
            var validation = await CoreValidation.ValidateForCreation(matchId, mapId, evenTeamFormat, team1PlayerIds, team2PlayerIds, gameNumber);
            if (!validation.Success)
                return Result<Game>.Failure(validation.ErrorMessage ?? "Validation failed");

            // Create new game
            var game = new Game
            {
                Id = Guid.NewGuid(),
                MatchId = matchId,
                MapId = mapId,
                EvenTeamFormat = evenTeamFormat,
                Team1PlayerIds = team1PlayerIds,
                Team2PlayerIds = team2PlayerIds,
                GameNumber = gameNumber,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Use the interface method which handles cache and events
            return await CreateEntityAsync(game);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<Game>.Failure($"Failed to create game: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing game with business logic validation
    /// </summary>
    public async Task<Result<Game>> UpdateGameAsync(Game game)
    {
        try
        {
            // Use validation
            var validation = CoreValidation.ValidateForUpdate(game);
            if (!validation.Success)
                return Result<Game>.Failure(validation.ErrorMessage ?? "Validation failed");

            // Use the base class method which handles cache and events
            return await UpdateEntityAsync(game);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<Game>.Failure($"Failed to update game: {ex.Message}");
        }
    }


    /// <summary>
    /// Gets games by match ID using multi-source data access
    /// </summary>
    public async Task<IEnumerable<Game>> GetGamesByMatchAsync(string matchId)
    {
        try
        {
            // Try cache first
            var cached = await GameCache.GetGamesByMatchAsync(matchId);
            if (cached != null)
            {
                return cached;
            }

            // Fallback to repository
            var games = await GameRepository.GetGamesByMatchAsync(matchId);
            if (games.Any())
            {
                await GameCache.SetGamesByMatchAsync(matchId, games, GetDefaultCacheExpiry());
            }

            return games;
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Enumerable.Empty<Game>();
        }
    }


    #region Helper Methods

    /// <summary>
    /// Generates a cache key for a single game entity
    /// </summary>
    private string GetCacheKey(object id)
    {
        return $"game:{id}";
    }

    /// <summary>
    /// Gets the default cache expiry time for games
    /// </summary>
    private TimeSpan GetDefaultCacheExpiry()
    {
        return TimeSpan.FromHours(1); // Games cache for 1 hour
    }

    /// <summary>
    /// Builds the WHERE clause for game search operations
    /// </summary>
    private string GetSearchWhereClause(string searchTerm, int limit)
    {
        return $"MatchId LIKE '%{searchTerm}%' OR MapId LIKE '%{searchTerm}%' LIMIT {limit}";
    }

    /// <summary>
    /// Invalidates match cache when individual games are modified
    /// </summary>
    private async Task InvalidateMatchCache(string matchId)
    {
        await GameCache.RemoveGamesByMatchAsync(matchId);
    }

    #endregion


    #region ICoreDataService<Game> Implementation

    /// <summary>
    /// Creates a new game entity with business logic validation and event publishing
    /// </summary>
    public async Task<Result<Game>> CreateEntityAsync(Game entity)
    {
        try
        {
            // Add to repository
            var result = await GameRepository.AddAsync(entity);

            if (result > 0)
            {
                // Update cache
                var cacheKey = GetCacheKey(entity.Id);
                await GameCache.SetAsync(cacheKey, entity, GetDefaultCacheExpiry());

                // Invalidate match cache
                await InvalidateMatchCache(entity.MatchId);

                // Publish event using generated publisher method
                await PublishGameCreated(entity.Id.ToString());

                return Result<Game>.CreateSuccess(entity);
            }

            return Result<Game>.Failure("Failed to create game");
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<Game>.Failure($"Failed to create game: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates a game entity with business logic validation and event publishing
    /// </summary>
    public async Task<Result<Game>> UpdateEntityAsync(Game entity)
    {
        try
        {
            // Update repository
            var result = await GameRepository.UpdateAsync(entity);

            if (result)
            {
                // Update cache
                var cacheKey = GetCacheKey(entity.Id);
                await GameCache.SetAsync(cacheKey, entity, GetDefaultCacheExpiry());

                // Invalidate match cache
                await InvalidateMatchCache(entity.MatchId);

                // Event publishing is handled by source generators

                return Result<Game>.CreateSuccess(entity);
            }

            return Result<Game>.Failure("Failed to update game");
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<Game>.Failure($"Failed to update game: {ex.Message}");
        }
    }

    /// <summary>
    /// Archives a game entity with business logic validation and event publishing
    /// </summary>
    public async Task<Result<Game>> ArchiveEntityAsync(Game entity)
    {
        try
        {
            // Use existing validation
            var validation = await CoreValidation.ValidateForArchivingAsync(entity);
            if (!validation.Success)
                return Result<Game>.Failure(validation.ErrorMessage ?? "Validation failed");

            // Archive the entity
            var result = await GameArchive.ArchiveAsync(entity);

            if (result > 0)
            {
                // Remove from active cache
                var cacheKey = GetCacheKey(entity.Id);
                await GameCache.RemoveAsync(cacheKey);

                // Invalidate match cache
                await InvalidateMatchCache(entity.MatchId);

                // Publish event using generated publisher method
                await PublishGameArchived(entity.Id.ToString());

                return Result<Game>.CreateSuccess(entity);
            }

            return Result<Game>.Failure("Failed to archive game");
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<Game>.Failure($"Failed to archive game: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a game entity with business logic validation and event publishing
    /// </summary>
    public async Task<Result<Game>> DeleteEntityAsync(object id)
    {
        try
        {
            // Get entity before deletion for event publishing
            var entity = await GetByIdAsync(id);
            if (entity == null)
            {
                return Result<Game>.Failure("Game not found");
            }

            // Delete from repository
            var result = await GameRepository.DeleteAsync(id);

            if (result)
            {
                // Remove from cache
                var cacheKey = GetCacheKey(id);
                await GameCache.RemoveAsync(cacheKey);

                // Invalidate match cache
                await InvalidateMatchCache(entity.MatchId);

                // Publish event using generated publisher method
                await PublishGameDeleted(entity.Id.ToString());

                return Result<Game>.CreateSuccess(entity);
            }

            return Result<Game>.Failure("Failed to delete game");
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<Game>.Failure($"Failed to delete game: {ex.Message}");
        }
    }

    #endregion

    #region IBaseDataService<Game> Implementation

    /// <summary>
    /// Gets a game by ID using cache-first strategy
    /// </summary>
    public async Task<Game?> GetByIdAsync(object id)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        var cacheKey = GetCacheKey(id);

        // Cache-first strategy
        var cached = await GameCache.GetAsync(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        // Fallback to repository
        var entity = await GameRepository.GetByIdAsync(id);
        if (entity != null)
        {
            await GameCache.SetAsync(cacheKey, entity, GetDefaultCacheExpiry());
        }

        return entity;
    }

    /// <summary>
    /// Gets all games using repository
    /// </summary>
    public async Task<IEnumerable<Game>> GetAllAsync()
    {
        return await GameRepository.GetAllAsync();
    }

    /// <summary>
    /// Searches for games across multiple data sources
    /// </summary>
    public async Task<IEnumerable<Game>> SearchAsync(string searchTerm, int limit = 25)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            throw new ArgumentException("Search term cannot be null or empty", nameof(searchTerm));
        }

        // For search operations, we typically want fresh data from repository
        return await GameRepository.QueryAsync(GetSearchWhereClause(searchTerm, limit));
    }

    /// <summary>
    /// Adds a game to repository and updates cache
    /// </summary>
    public async Task<int> AddAsync(Game entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        // Add to repository
        var result = await GameRepository.AddAsync(entity);

        if (result > 0)
        {
            // Update cache
            var cacheKey = GetCacheKey(entity.Id);
            await GameCache.SetAsync(cacheKey, entity, GetDefaultCacheExpiry());

            // Invalidate match cache
            await InvalidateMatchCache(entity.MatchId);
        }

        return result;
    }

    /// <summary>
    /// Updates a game in repository and cache
    /// </summary>
    public async Task<bool> UpdateAsync(Game entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        // Update repository
        var result = await GameRepository.UpdateAsync(entity);

        if (result)
        {
            // Update cache
            var cacheKey = GetCacheKey(entity.Id);
            await GameCache.SetAsync(cacheKey, entity, GetDefaultCacheExpiry());

            // Invalidate match cache
            await InvalidateMatchCache(entity.MatchId);
        }

        return result;
    }

    /// <summary>
    /// Deletes a game from repository and cache
    /// </summary>
    public async Task<bool> DeleteAsync(object id)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        // Delete from repository
        var result = await GameRepository.DeleteAsync(id);

        if (result)
        {
            // Remove from cache
            var cacheKey = GetCacheKey(id);
            await GameCache.RemoveAsync(cacheKey);
        }

        return result;
    }

    /// <summary>
    /// Archives a game and removes from active cache
    /// </summary>
    public async Task<bool> ArchiveAsync(Game entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        // Archive the entity
        var result = await GameArchive.ArchiveAsync(entity);

        if (result > 0)
        {
            // Remove from active cache
            var cacheKey = GetCacheKey(entity.Id);
            await GameCache.RemoveAsync(cacheKey);

            // Invalidate match cache
            await InvalidateMatchCache(entity.MatchId);
        }

        return result > 0;
    }

    /// <summary>
    /// Checks if a game exists using cache-first strategy
    /// </summary>
    public async Task<bool> ExistsAsync(object id)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        var cacheKey = GetCacheKey(id);

        // Check cache first
        if (await GameCache.ExistsAsync(cacheKey))
        {
            return true;
        }

        // Fallback to repository
        return await GameRepository.ExistsAsync(id);
    }

    #endregion

    #region Game Completion

    /// <summary>
    /// Completes a game with a winner and publishes GameCompletedEvent
    /// </summary>
    public async Task<bool> CompleteGameAsync(Game game, string winnerId, string userId, string playerName)
    {
        try
        {
            // Validate game state
            if (game.Status != GameStatus.Created && game.Status != GameStatus.InProgress)
                throw new InvalidOperationException("Game can only be completed when in Created or InProgress state");

            // Validate winner
            if (winnerId != game.Team1PlayerIds.FirstOrDefault() && winnerId != game.Team2PlayerIds.FirstOrDefault())
                throw new ArgumentException("Winner must be one of the participating teams");

            // Create new state snapshot with game completion
            var newSnapshot = game.CurrentState != null
                ? new GameStateSnapshot(game.CurrentState)
                : new GameStateSnapshot
                {
                    GameId = game.Id,
                    MatchId = game.MatchId,
                    MapId = game.MapId,
                    EvenTeamFormat = game.EvenTeamFormat,
                    Team1PlayerIds = game.Team1PlayerIds,
                    Team2PlayerIds = game.Team2PlayerIds,
                    GameNumber = game.GameNumber
                };

            // Update the new snapshot with current user info
            newSnapshot.UserId = userId;
            newSnapshot.PlayerName = playerName;

            // Transition to completed state
            var gameStateMachine = new GameStateMachine();
            gameStateMachine.TryTransition(newSnapshot, GameStatus.Completed, userId, playerName);
            newSnapshot.WinnerId = winnerId;

            // Update game state
            game.CurrentState = newSnapshot;
            game.StateHistory.Add(newSnapshot);

            // Save to repository
            await GameRepository.UpdateAsync(game);

            // Publish event using generated publisher method
            await PublishGameCompleted(
                game.Id.ToString(),
                winnerId,
                userId,
                playerName
            );

            return true;
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return false;
        }
    }

    #endregion

    #region Game Result Reporting

    /// <summary>
    /// Reports the result of a game and completes it
    /// </summary>
    public async Task<bool> ReportGameResultAsync(Game game, string winnerId, string userId, string playerName)
    {
        try
        {
            // Validate game state
            if (game.Status != GameStatus.Created && game.Status != GameStatus.InProgress)
                throw new InvalidOperationException("Game results can only be reported when game is in Created or InProgress state");

            // Validate winner
            if (winnerId != game.Team1PlayerIds.FirstOrDefault() && winnerId != game.Team2PlayerIds.FirstOrDefault())
                throw new ArgumentException("Winner must be one of the participating teams");

            // Complete the game
            return await CompleteGameAsync(game, winnerId, userId, playerName);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return false;
        }
    }

    #endregion

    #region Deck Code Management

    /// <summary>
    /// Submits a deck code for a game with full business logic orchestration
    /// </summary>
    public async Task<bool> SubmitDeckCodeAsync(Game game, string teamId, string deckCode, string userId, string playerName)
    {
        try
        {
            // Validate game state
            if (game.Status != GameStatus.Created)
                throw new InvalidOperationException("Deck codes can only be submitted when game is in Created state");

            // Validate team ID
            if (teamId != game.Team1PlayerIds.FirstOrDefault() && teamId != game.Team2PlayerIds.FirstOrDefault())
                throw new ArgumentException("Team must be one of the participating teams");

            // Create new state snapshot with deck submission
            var newSnapshot = game.CurrentState != null
                ? new GameStateSnapshot(game.CurrentState)
                : new GameStateSnapshot
                {
                    GameId = game.Id,
                    MatchId = game.MatchId,
                    MapId = game.MapId,
                    EvenTeamFormat = game.EvenTeamFormat,
                    Team1PlayerIds = game.Team1PlayerIds,
                    Team2PlayerIds = game.Team2PlayerIds,
                    GameNumber = game.GameNumber
                };

            // Update the new snapshot with current user info
            newSnapshot.UserId = userId;
            newSnapshot.PlayerName = playerName;

            // Update deck code for the appropriate team
            if (teamId == game.Team1PlayerIds.FirstOrDefault())
            {
                newSnapshot.Team1DeckCode = deckCode;
                newSnapshot.Team1DeckSubmittedAt = DateTime.UtcNow;
            }
            else
            {
                newSnapshot.Team2DeckCode = deckCode;
                newSnapshot.Team2DeckSubmittedAt = DateTime.UtcNow;
            }

            // Update game state
            game.CurrentState = newSnapshot;
            game.StateHistory.Add(newSnapshot);

            // Save to repository
            await GameRepository.UpdateAsync(game);

            // Publish event using generated publisher method
            await PublishGameUpdated(game.Id.ToString());

            return true;
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return false;
        }
    }

    /// <summary>
    /// Revises a deck code for a game with full business logic orchestration
    /// </summary>
    public async Task<bool> ReviseDeckCodeAsync(Game game, string teamId, string deckCode, string userId, string playerName)
    {
        try
        {
            // Validate game state
            if (game.Status != GameStatus.Created)
                throw new InvalidOperationException("Deck codes can only be revised when game is in Created state");

            // Use the same logic as SubmitDeckCode
            return await SubmitDeckCodeAsync(game, teamId, deckCode, userId, playerName);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return false;
        }
    }

    /// <summary>
    /// Confirms a deck code for a game
    /// </summary>
    public async Task<bool> ConfirmDeckCodeAsync(Game game, string teamId, string userId, string playerName)
    {
        try
        {
            // Validate game state
            if (game.Status != GameStatus.Created)
                throw new InvalidOperationException("Deck codes can only be confirmed when game is in Created state");

            // Validate team ID
            if (teamId != game.Team1PlayerIds.FirstOrDefault() && teamId != game.Team2PlayerIds.FirstOrDefault())
                throw new ArgumentException("Team must be one of the participating teams");

            // Create new state snapshot with deck confirmation
            var newSnapshot = game.CurrentState != null
                ? new GameStateSnapshot(game.CurrentState)
                : new GameStateSnapshot
                {
                    GameId = game.Id,
                    MatchId = game.MatchId,
                    MapId = game.MapId,
                    EvenTeamFormat = game.EvenTeamFormat,
                    Team1PlayerIds = game.Team1PlayerIds,
                    Team2PlayerIds = game.Team2PlayerIds,
                    GameNumber = game.GameNumber
                };

            // Update the new snapshot with current user info
            newSnapshot.UserId = userId;
            newSnapshot.PlayerName = playerName;

            // Update deck confirmation for the appropriate team
            if (teamId == game.Team1PlayerIds.FirstOrDefault())
            {
                newSnapshot.Team1DeckConfirmed = true;
                newSnapshot.Team1DeckConfirmedAt = DateTime.UtcNow;
            }
            else
            {
                newSnapshot.Team2DeckConfirmed = true;
                newSnapshot.Team2DeckConfirmedAt = DateTime.UtcNow;
            }

            // Update game state
            game.CurrentState = newSnapshot;
            game.StateHistory.Add(newSnapshot);

            // Save to repository
            await GameRepository.UpdateAsync(game);

            // Publish event using generated publisher method
            await PublishGameUpdated(game.Id.ToString());

            return true;
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return false;
        }
    }

    #endregion

}
