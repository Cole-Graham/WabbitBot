using System;
using System.Collections.Concurrent;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data
{
    /// <summary>
    /// Registry for repository adapters used by DatabaseService. Higher layers (Core) should register
    /// adapters at startup. This project remains provider-agnostic.
    /// </summary>
    public static class RepositoryAdapterRegistry
    {
        private static readonly ConcurrentDictionary<Type, object> _adapters = new();

        public static void RegisterAdapter<TEntity>(IRepositoryAdapter<TEntity> adapter)
            where TEntity : Entity
        {
            _adapters[typeof(TEntity)] = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

        public static IRepositoryAdapter<TEntity>? GetAdapter<TEntity>()
            where TEntity : Entity
        {
            if (_adapters.TryGetValue(typeof(TEntity), out var adapter) && adapter is IRepositoryAdapter<TEntity> typed)
            {
                return typed;
            }
            return null;
        }
    }
}
