using System;
using System.Collections.Concurrent;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data
{
    /// <summary>
    /// Base cache class for in-memory caching operations
    /// </summary>
    public abstract class Cache<TEntity, TCollection>
        where TEntity : Entity
        where TCollection : Entity
    {
        private class CacheEntry
        {
            public required object Value { get; set; }
            public DateTime? ExpiryTime { get; set; }
            public DateTime LastAccessed { get; set; }
            public bool IsExpired => ExpiryTime.HasValue && DateTime.UtcNow > ExpiryTime.Value;
        }

        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly int _maxSize;
        private readonly object _evictionLock = new();

        protected Cache(int maxSize = 1000, TimeSpan? defaultExpiry = null)
        {
            _maxSize =
                maxSize > 0 ? maxSize : throw new ArgumentException("Max size must be greater than 0", nameof(maxSize));
            _cache = new ConcurrentDictionary<string, CacheEntry>();
            DefaultExpiry = defaultExpiry ?? TimeSpan.FromHours(1);
        }

        public TimeSpan DefaultExpiry { get; }

        #region Basic Cache Operations

        public virtual Task<TEntity?> GetAsync(string key)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (!entry.IsExpired)
                {
                    entry.LastAccessed = DateTime.UtcNow;
                    return Task.FromResult((TEntity?)entry.Value);
                }

                _cache.TryRemove(key, out _);
            }

            return Task.FromResult<TEntity?>(default);
        }

        public virtual Task SetAsync(string key, TEntity value, TimeSpan? expiry = null)
        {
            var entry = new CacheEntry
            {
                Value = value,
                ExpiryTime = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : null,
                LastAccessed = DateTime.UtcNow,
            };

            if (_cache.Count >= _maxSize && !_cache.ContainsKey(key))
            {
                EvictOldestEntries(1);
            }

            _cache.AddOrUpdate(key, entry, (_, _) => entry);
            return Task.CompletedTask;
        }

        public virtual Task<bool> RemoveAsync(string key)
        {
            return Task.FromResult(_cache.TryRemove(key, out _));
        }

        public virtual Task<bool> ExistsAsync(string key)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (!entry.IsExpired)
                {
                    entry.LastAccessed = DateTime.UtcNow;
                    return Task.FromResult(true);
                }

                _cache.TryRemove(key, out _);
            }

            return Task.FromResult(false);
        }

        public virtual Task<bool> ExpireAsync(string key, TimeSpan expiry)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                entry.ExpiryTime = DateTime.UtcNow.Add(expiry);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        #endregion

        #region Collection Cache Operations

        public virtual Task<TCollection?> GetCollectionAsync(string key)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (!entry.IsExpired)
                {
                    entry.LastAccessed = DateTime.UtcNow;
                    return Task.FromResult((TCollection?)entry.Value);
                }

                _cache.TryRemove(key, out _);
            }

            return Task.FromResult<TCollection?>(default);
        }

        public virtual Task SetCollectionAsync(string key, TCollection collection, TimeSpan? expiry = null)
        {
            var entry = new CacheEntry
            {
                Value = collection,
                ExpiryTime = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : null,
                LastAccessed = DateTime.UtcNow,
            };

            if (_cache.Count >= _maxSize && !_cache.ContainsKey(key))
            {
                EvictOldestEntries(1);
            }

            _cache.AddOrUpdate(key, entry, (_, _) => entry);
            return Task.CompletedTask;
        }

        public virtual Task<bool> RemoveCollectionAsync(string key)
        {
            return Task.FromResult(_cache.TryRemove(key, out _));
        }

        public virtual Task<bool> CollectionExistsAsync(string key)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (!entry.IsExpired)
                {
                    entry.LastAccessed = DateTime.UtcNow;
                    return Task.FromResult(true);
                }

                _cache.TryRemove(key, out _);
            }

            return Task.FromResult(false);
        }

        #endregion

        #region Cache Management

        public virtual void CleanExpiredEntries()
        {
            foreach (var kvp in _cache)
            {
                if (kvp.Value.IsExpired)
                {
                    _cache.TryRemove(kvp.Key, out _);
                }
            }
        }

        public virtual void Clear()
        {
            _cache.Clear();
        }

        public virtual CacheStatistics GetStatistics()
        {
            var now = DateTime.UtcNow;
            var expiredCount = _cache.Values.Count(e => e.IsExpired);
            var activeCount = _cache.Count - expiredCount;

            return new CacheStatistics
            {
                TotalEntries = _cache.Count,
                ActiveEntries = activeCount,
                ExpiredEntries = expiredCount,
                MaxSize = _maxSize,
                UtilizationPercentage = UtilizationPercentage,
                OldestEntryAge = _cache.Values.Any() ? now - _cache.Values.Min(e => e.LastAccessed) : TimeSpan.Zero,
                NewestEntryAge = _cache.Values.Any() ? now - _cache.Values.Max(e => e.LastAccessed) : TimeSpan.Zero,
            };
        }

        #endregion

        #region Cache Properties

        public virtual int Count => _cache.Count;
        public virtual int MaxSize => _maxSize;
        public virtual double UtilizationPercentage => (double)Count / _maxSize * 100;

        #endregion

        #region Private Helper Methods

        private void EvictOldestEntries(int count)
        {
            lock (_evictionLock)
            {
                var oldestEntries = _cache.OrderBy(kvp => kvp.Value.LastAccessed).Take(count).ToList();

                foreach (var entry in oldestEntries)
                {
                    _cache.TryRemove(entry.Key, out _);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Statistics about the cache for monitoring purposes
    /// </summary>
    public class CacheStatistics
    {
        public int TotalEntries { get; set; }
        public int ActiveEntries { get; set; }
        public int ExpiredEntries { get; set; }
        public int MaxSize { get; set; }
        public double UtilizationPercentage { get; set; }
        public TimeSpan OldestEntryAge { get; set; }
        public TimeSpan NewestEntryAge { get; set; }
    }
}
