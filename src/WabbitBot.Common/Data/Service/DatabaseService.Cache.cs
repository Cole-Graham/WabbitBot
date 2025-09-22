using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Service;

/// <summary>
/// Cache operations for DatabaseService using in-memory ConcurrentDictionary
/// Provides fast access to recently used entities with LRU eviction
/// </summary>
public partial class DatabaseService<TEntity> where TEntity : Entity
{
    // Cache implementation based on existing CacheService
    private class CacheEntry
    {
        public required object Value { get; set; }
        public DateTime? ExpiryTime { get; set; }
        public DateTime LastAccessed { get; set; }
        public bool IsExpired => ExpiryTime.HasValue && DateTime.UtcNow > ExpiryTime.Value;
    }

    private ConcurrentDictionary<string, CacheEntry>? _cache;
    private int _maxSize;
    private readonly object _evictionLock = new();
    private TimeSpan _defaultExpiry;

    /// <summary>
    /// Initialize cache configuration
    /// </summary>
    private void InitializeCache(int maxSize = 1000, TimeSpan? defaultExpiry = null)
    {
        _maxSize = maxSize > 0 ? maxSize : throw new ArgumentException("Max size must be greater than 0", nameof(maxSize));
        _cache = new ConcurrentDictionary<string, CacheEntry>();
        _defaultExpiry = defaultExpiry ?? TimeSpan.FromHours(1);
    }

    #region Cache Operations Implementation

    protected virtual async Task<Result<TEntity>> CreateInCacheAsync(TEntity entity)
    {
        try
        {
            await SetInCacheAsync(entity.Id.ToString(), entity, _defaultExpiry);
            return Result<TEntity>.CreateSuccess(entity);
        }
        catch (Exception ex)
        {
            return Result<TEntity>.Failure($"Failed to create entity in cache: {ex.Message}");
        }
    }

    protected virtual async Task<Result<TEntity>> UpdateInCacheAsync(TEntity entity)
    {
        try
        {
            await SetInCacheAsync(entity.Id.ToString(), entity, _defaultExpiry);
            return Result<TEntity>.CreateSuccess(entity);
        }
        catch (Exception ex)
        {
            return Result<TEntity>.Failure($"Failed to update entity in cache: {ex.Message}");
        }
    }

    protected virtual async Task<Result<TEntity>> DeleteFromCacheAsync(object id)
    {
        try
        {
            var key = id.ToString();
            if (_cache.TryGetValue(key, out var entry))
            {
                _cache.TryRemove(key, out _);
                return Result<TEntity>.CreateSuccess((TEntity)entry.Value);
            }
            return Result<TEntity>.Failure("Entity not found in cache");
        }
        catch (Exception ex)
        {
            return Result<TEntity>.Failure($"Failed to delete entity from cache: {ex.Message}");
        }
    }

    protected virtual async Task<bool> ExistsInCacheAsync(object id)
    {
        var key = id.ToString();
        if (_cache.TryGetValue(key, out var entry))
        {
            if (!entry.IsExpired)
            {
                // Update last accessed time for LRU tracking
                entry.LastAccessed = DateTime.UtcNow;
                return true;
            }

            // Remove expired entry
            _cache.TryRemove(key, out _);
        }

        return false;
    }

    protected virtual async Task<TEntity?> GetByIdFromCacheAsync(object id)
    {
        var key = id.ToString();
        if (_cache.TryGetValue(key, out var entry))
        {
            if (!entry.IsExpired)
            {
                // Update last accessed time for LRU tracking
                entry.LastAccessed = DateTime.UtcNow;
                return (TEntity?)entry.Value;
            }

            // Remove expired entry
            _cache.TryRemove(key, out _);
        }

        return default;
    }

    protected virtual async Task<TEntity?> GetByNameFromCacheAsync(string name)
    {
        // Cache doesn't support name-based lookups efficiently
        // This could be implemented with a separate name-to-id mapping if needed
        return default;
    }

    protected virtual async Task<IEnumerable<TEntity>> GetAllFromCacheAsync()
    {
        // Getting all cached items is expensive and not typically needed
        // Return empty for now - cache is for individual item access
        return Array.Empty<TEntity>();
    }

    #endregion

    #region Cache Helper Methods

    private async Task SetInCacheAsync(string key, TEntity value, TimeSpan? expiry = null)
    {
        var entry = new CacheEntry
        {
            Value = value,
            ExpiryTime = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : null,
            LastAccessed = DateTime.UtcNow
        };

        // Check if we need to evict entries before adding
        if (_cache.Count >= _maxSize && !_cache.ContainsKey(key))
        {
            EvictOldestEntries(1);
        }

        _cache.AddOrUpdate(key, entry, (_, _) => entry);
        await Task.CompletedTask;
    }

    private void EvictOldestEntries(int count)
    {
        lock (_evictionLock)
        {
            // Get all entries sorted by last accessed time (oldest first)
            var oldestEntries = _cache
                .OrderBy(kvp => kvp.Value.LastAccessed)
                .Take(count)
                .ToList();

            // Remove the oldest entries
            foreach (var entry in oldestEntries)
            {
                _cache.TryRemove(entry.Key, out _);
            }
        }
    }

    #endregion
}
