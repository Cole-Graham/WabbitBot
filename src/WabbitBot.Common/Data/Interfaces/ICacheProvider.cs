using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Interfaces
{
    /// <summary>
    /// Pluggable cache provider for DatabaseService. Default can be NoOp; alternative could be in-memory LRU.
    /// </summary>
    public interface ICacheProvider<TEntity> where TEntity : Entity
    {
        Task<bool> TryGetAsync(object id, out TEntity? entity);
        Task SetAsync(object id, TEntity entity);
        Task RemoveAsync(object id);
        Task<IEnumerable<TEntity>> GetAllAsync();
    }
}


