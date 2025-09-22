using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Data.Interface;
using WabbitBot.Common.Data.Interfaces;

namespace WabbitBot.Core.Common.Services;

/// <summary>
/// Cache operations for PlayerService
/// Contains cache-specific operations and Player-specific caching methods
/// </summary>
public partial class PlayerService
{
    #region Player-Specific Cache Operations

    /// <summary>
    /// Gets a player by name using cache-first strategy
    /// </summary>
    public async Task<Player?> GetByNameAsync(string playerName)
    {
        // Try cache first
        var cached = await PlayerCache.GetByNameAsync(playerName);
        if (cached != null)
        {
            return cached;
        }

        // Fallback to repository
        var player = await PlayerRepository.GetByNameAsync(playerName);
        if (player != null)
        {
            await PlayerCache.SetByNameAsync(playerName, player, PlayerCache.DefaultExpiry);
        }

        return player;
    }

    /// <summary>
    /// Gets all players using cache-first strategy with collection caching
    /// </summary>
    public async Task<IEnumerable<Player>> GetAllPlayersAsync()
    {
        // Try cache first
        var cached = await PlayerCache.GetActivePlayersAsync();
        if (cached != null && cached.Any())
        {
            return cached;
        }

        // Fallback to repository
        var entities = await PlayerRepository.GetAllAsync();
        if (entities.Any())
        {
            await PlayerCache.SetActivePlayersAsync(entities, PlayerCache.DefaultExpiry);
        }

        return entities;
    }

    /// <summary>
    /// Searches for players across multiple data sources
    /// </summary>
    public async Task<IEnumerable<Player>> SearchPlayersAsync(string searchTerm, int limit = 25)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            throw new ArgumentException("Search term cannot be null or empty", nameof(searchTerm));
        }

        // For search operations, we typically want fresh data from repository
        return await PlayerRepository.QueryAsync(GetSearchWhereClause(searchTerm, limit));
    }


    #endregion

    #region Cache Helper Methods

    /// <summary>
    /// Builds the WHERE clause for player search operations
    /// </summary>
    private string GetSearchWhereClause(string searchTerm, int limit)
    {
        return $"Name LIKE '%{searchTerm}%' LIMIT {limit}";
    }

    /// <summary>
    /// Invalidates collection cache when individual entities are modified
    /// </summary>
    private async Task InvalidateCollectionCache()
    {
        await PlayerCache.RemoveActivePlayersAsync();
    }

    #endregion
}

