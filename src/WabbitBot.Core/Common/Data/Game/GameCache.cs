using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Data.Interface;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data;

/// <summary>
/// Implementation of Game-specific cache operations
/// </summary>
public class GameCache : IGameCache
{
    private readonly ConcurrentDictionary<string, (Game entity, DateTime expiry)> _cache = new();
    private readonly ConcurrentDictionary<string, (IEnumerable<Game> games, DateTime expiry)> _matchCache = new();
    private const int MaxCacheSize = 1000;

    public async Task<Game?> GetAsync(string key)
    {
        if (_cache.TryGetValue(key, out var cached) && cached.expiry > DateTime.UtcNow)
        {
            return cached.entity;
        }

        // Remove expired entry
        if (_cache.TryGetValue(key, out _))
        {
            _cache.TryRemove(key, out _);
        }

        return await Task.FromResult<Game?>(null);
    }

    public async Task SetAsync(string key, Game entity, TimeSpan? expiry = null)
    {
        var expiryTime = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromHours(1));
        _cache.AddOrUpdate(key, (entity, expiryTime), (_, _) => (entity, expiryTime));
        await Task.CompletedTask;
    }

    public async Task<bool> RemoveAsync(string key)
    {
        var removed = _cache.TryRemove(key, out _);
        return await Task.FromResult(removed);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        var exists = _cache.TryGetValue(key, out var cached) && cached.expiry > DateTime.UtcNow;
        return await Task.FromResult(exists);
    }

    public async Task<bool> ExpireAsync(string key, TimeSpan expiry)
    {
        if (_cache.TryGetValue(key, out var cached))
        {
            var newExpiry = DateTime.UtcNow.Add(expiry);
            _cache.TryUpdate(key, (cached.entity, newExpiry), cached);
            return await Task.FromResult(true);
        }
        return await Task.FromResult(false);
    }

    public int Count => _cache.Count;

    public int MaxSize => MaxCacheSize;

    public double UtilizationPercentage => (double)Count / MaxSize * 100;

    public void CleanExpiredEntries()
    {
        var expiredKeys = _cache
            .Where(kvp => kvp.Value.expiry <= DateTime.UtcNow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }
    }

    public void Clear()
    {
        _cache.Clear();
        _matchCache.Clear();
    }

    public CacheStatistics GetStatistics()
    {
        var now = DateTime.UtcNow;
        var activeEntries = _cache.Count(kvp => kvp.Value.expiry > now);
        var expiredEntries = _cache.Count - activeEntries;

        var oldestEntry = _cache.Values.Min(v => v.expiry);
        var newestEntry = _cache.Values.Max(v => v.expiry);

        return new CacheStatistics
        {
            TotalEntries = _cache.Count,
            ActiveEntries = activeEntries,
            ExpiredEntries = expiredEntries,
            MaxSize = MaxSize,
            UtilizationPercentage = UtilizationPercentage,
            OldestEntryAge = now - oldestEntry,
            NewestEntryAge = now - newestEntry
        };
    }

    public async Task<IEnumerable<Game>?> GetGamesByMatchAsync(string matchId)
    {
        var cacheKey = $"games:match:{matchId}";
        if (_matchCache.TryGetValue(cacheKey, out var cached) && cached.expiry > DateTime.UtcNow)
        {
            return cached.games;
        }

        // Remove expired entry
        if (_matchCache.TryGetValue(cacheKey, out _))
        {
            _matchCache.TryRemove(cacheKey, out _);
        }

        return await Task.FromResult<IEnumerable<Game>?>(null);
    }

    public async Task SetGamesByMatchAsync(string matchId, IEnumerable<Game> games, TimeSpan expiry)
    {
        var cacheKey = $"games:match:{matchId}";
        var expiryTime = DateTime.UtcNow.Add(expiry);
        _matchCache.AddOrUpdate(cacheKey, (games, expiryTime), (_, _) => (games, expiryTime));
        await Task.CompletedTask;
    }

    public async Task RemoveGamesByMatchAsync(string matchId)
    {
        var cacheKey = $"games:match:{matchId}";
        _matchCache.TryRemove(cacheKey, out _);
        await Task.CompletedTask;
    }
}
