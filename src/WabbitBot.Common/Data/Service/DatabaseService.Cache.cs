using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Service;

/// <summary>
/// Cache operations for DatabaseService using in-memory ConcurrentDictionary
/// Provides fast access to recently used entities with LRU eviction
/// </summary>
public partial class DatabaseService<TEntity> where TEntity : Entity
{
    /// <summary>
    /// No-op. Cache is provided via ICacheProvider and configured by generator/startup.
    /// </summary>
    private void InitializeCache(int maxSize = 1000, TimeSpan? defaultExpiry = null)
    {
        // Intentionally empty: legacy internal cache removed in favor of ICacheProvider
    }

    #region Cache Operations (provider-backed)

    protected virtual async Task<Result<TEntity>> CreateInCacheAsync(TEntity entity)
    {
        if (_cacheProvider is null)
        {
            return Result<TEntity>.Failure("No cache provider configured");
        }

        await _cacheProvider.SetAsync(entity.Id, entity);
        return Result<TEntity>.CreateSuccess(entity);
    }

    protected virtual async Task<Result<TEntity>> UpdateInCacheAsync(TEntity entity)
    {
        if (_cacheProvider is null)
        {
            return Result<TEntity>.Failure("No cache provider configured");
        }

        await _cacheProvider.SetAsync(entity.Id, entity);
        return Result<TEntity>.CreateSuccess(entity);
    }

    protected virtual async Task<Result<TEntity>> DeleteFromCacheAsync(object id)
    {
        if (_cacheProvider is null)
        {
            return Result<TEntity>.Failure("No cache provider configured");
        }

        // Best-effort remove; we cannot return the removed value via provider contract
        await _cacheProvider.RemoveAsync(id);
        return Result<TEntity>.Failure("Removed from cache (value not available)");
    }

    protected virtual async Task<bool> ExistsInCacheAsync(object id)
    {
        if (_cacheProvider is null)
        {
            return false;
        }

        var found = await _cacheProvider.TryGetAsync(id, out var _);
        return found;
    }

    protected virtual async Task<TEntity?> GetByIdFromCacheAsync(object id)
    {
        if (_cacheProvider is null)
        {
            return default;
        }

        var found = await _cacheProvider.TryGetAsync(id, out var entity);
        return found ? entity : default;
    }

    protected virtual async Task<TEntity?> GetByNameFromCacheAsync(string name)
    {
        // Not supported by default provider. Could be implemented with a secondary index provider.
        await Task.CompletedTask;
        return default;
    }

    protected virtual async Task<IEnumerable<TEntity>> GetAllFromCacheAsync()
    {
        if (_cacheProvider is null)
        {
            return Array.Empty<TEntity>();
        }

        return await _cacheProvider.GetAllAsync();
    }

    #endregion
}
