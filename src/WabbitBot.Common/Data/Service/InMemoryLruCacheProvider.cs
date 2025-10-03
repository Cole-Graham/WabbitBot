using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Service
{
    public class InMemoryLruCacheProvider<TEntity> : ICacheProvider<TEntity> where TEntity : Entity
    {
        private class CacheEntry
        {
            public required TEntity Value { get; set; }
            public DateTime? ExpiryTime { get; set; }
            public DateTime LastAccessed { get; set; }
            public bool IsExpired => ExpiryTime.HasValue && DateTime.UtcNow > ExpiryTime.Value;
        }

        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
        private readonly int _maxSize;
        private readonly TimeSpan _defaultExpiry;
        private readonly object _evictionLock = new();

        public InMemoryLruCacheProvider(int maxSize, TimeSpan? defaultExpiry = null)
        {
            _maxSize = maxSize > 0 ? maxSize : throw new ArgumentException("maxSize must be > 0", nameof(maxSize));
            _defaultExpiry = defaultExpiry ?? TimeSpan.FromHours(1);
        }

        public Task<bool> TryGetAsync(object id, out TEntity? entity)
        {
            var key = id.ToString()!;
            entity = default;
            if (_cache.TryGetValue(key, out var entry))
            {
                if (!entry.IsExpired)
                {
                    entry.LastAccessed = DateTime.UtcNow;
                    entity = entry.Value;
                    return Task.FromResult(true);
                }
                _cache.TryRemove(key, out _);
            }
            return Task.FromResult(false);
        }

        public Task SetAsync(object id, TEntity entity)
        {
            var key = id.ToString()!;
            var entry = new CacheEntry
            {
                Value = entity,
                ExpiryTime = DateTime.UtcNow.Add(_defaultExpiry),
                LastAccessed = DateTime.UtcNow,
            };

            if (_cache.Count >= _maxSize && !_cache.ContainsKey(key))
            {
                EvictOldest(1);
            }

            _cache.AddOrUpdate(key, entry, (_, _) => entry);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(object id)
        {
            var key = id.ToString()!;
            _cache.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<TEntity>> GetAllAsync()
        {
            var now = DateTime.UtcNow;
            var items = _cache
                .Where(kv => !kv.Value.IsExpired)
                .Select(kv => kv.Value.Value)
                .ToArray()
            ;
            return Task.FromResult<IEnumerable<TEntity>>(items);
        }

        private void EvictOldest(int count)
        {
            lock (_evictionLock)
            {
                foreach (var entry in _cache.OrderBy(kv => kv.Value.LastAccessed).Take(count).ToList())
                {
                    _cache.TryRemove(entry.Key, out _);
                }
            }
        }
    }
}


