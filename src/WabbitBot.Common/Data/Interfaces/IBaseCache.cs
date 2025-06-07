using System;
using System.Threading.Tasks;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Interfaces
{
    public interface IBaseCache<TEntity> where TEntity : BaseEntity
    {
        Task<TEntity> GetAsync(string key);
        Task SetAsync(string key, TEntity value, TimeSpan? expiry = null);
        Task<bool> RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task<bool> ExpireAsync(string key, TimeSpan expiry);
    }

    public interface ICollectionCache<TEntity, TCollection>
        where TEntity : BaseEntity
        where TCollection : BaseEntity
    {
        Task<TCollection> GetCollectionAsync(string key);
        Task SetCollectionAsync(string key, TCollection collection, TimeSpan? expiry = null);
        Task<bool> RemoveCollectionAsync(string key);
        Task<bool> CollectionExistsAsync(string key);
    }
}
