using System;
using System.Collections.Concurrent;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data
{
    /// <summary>
    /// Registry for archive providers used by DatabaseService. Higher layers can register
    /// a provider; if none is registered, DatabaseService should treat archiving as disabled.
    /// </summary>
    public static class ArchiveProviderRegistry
    {
        private static readonly ConcurrentDictionary<Type, object> _providers = new();

        public static void RegisterProvider<TEntity>(IArchiveProvider<TEntity> provider) where TEntity : Entity
        {
            _providers[typeof(TEntity)] = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public static IArchiveProvider<TEntity>? GetProvider<TEntity>() where TEntity : Entity
        {
            if (_providers.TryGetValue(typeof(TEntity), out var provider) && provider is IArchiveProvider<TEntity> typed)
            {
                return typed;
            }
            return null;
        }

        public static IEnumerable<Type> GetRegisteredEntityTypes()
        {
            return _providers.Keys.ToArray();
        }
    }
}



