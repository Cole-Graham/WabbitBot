using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data
{
    public class BaseCache<TEntity, TCollection> : IBaseCache<TEntity>, ICollectionCache<TEntity, TCollection>
        where TEntity : BaseEntity
        where TCollection : BaseEntity
    {
        private class CacheEntry
        {
            public required object Value { get; set; }
            public DateTime? ExpiryTime { get; set; }
            public bool IsExpired => ExpiryTime.HasValue && DateTime.UtcNow > ExpiryTime.Value;
        }

        private readonly ConcurrentDictionary<string, CacheEntry> _cache;

        public BaseCache()
        {
            _cache = new ConcurrentDictionary<string, CacheEntry>();
        }

        public Task<TEntity?> GetAsync(string key)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (!entry.IsExpired)
                {
                    return Task.FromResult((TEntity?)entry.Value);
                }

                // Remove expired entry
                _cache.TryRemove(key, out _);
            }

            return Task.FromResult<TEntity?>(null);
        }

        public Task SetAsync(string key, TEntity value, TimeSpan? expiry = null)
        {
            var entry = new CacheEntry
            {
                Value = value,
                ExpiryTime = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : null
            };

            _cache.AddOrUpdate(key, entry, (_, _) => entry);
            return Task.CompletedTask;
        }

        public Task<bool> RemoveAsync(string key)
        {
            return Task.FromResult(_cache.TryRemove(key, out _));
        }

        public Task<bool> ExistsAsync(string key)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (!entry.IsExpired)
                {
                    return Task.FromResult(true);
                }

                // Remove expired entry
                _cache.TryRemove(key, out _);
            }

            return Task.FromResult(false);
        }

        public Task<bool> ExpireAsync(string key, TimeSpan expiry)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                entry.ExpiryTime = DateTime.UtcNow.Add(expiry);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<TCollection?> GetCollectionAsync(string key)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (!entry.IsExpired)
                {
                    return Task.FromResult((TCollection?)entry.Value);
                }

                // Remove expired entry
                _cache.TryRemove(key, out _);
            }

            return Task.FromResult<TCollection?>(null);
        }

        public Task SetCollectionAsync(string key, TCollection collection, TimeSpan? expiry = null)
        {
            var entry = new CacheEntry
            {
                Value = collection,
                ExpiryTime = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : null
            };

            _cache.AddOrUpdate(key, entry, (_, _) => entry);
            return Task.CompletedTask;
        }

        public Task<bool> RemoveCollectionAsync(string key)
        {
            return Task.FromResult(_cache.TryRemove(key, out _));
        }

        public Task<bool> CollectionExistsAsync(string key)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (!entry.IsExpired)
                {
                    return Task.FromResult(true);
                }

                // Remove expired entry
                _cache.TryRemove(key, out _);
            }

            return Task.FromResult(false);
        }

        public void CleanExpiredEntries()
        {
            foreach (var kvp in _cache)
            {
                if (kvp.Value.IsExpired)
                {
                    _cache.TryRemove(kvp.Key, out _);
                }
            }
        }
    }
}
