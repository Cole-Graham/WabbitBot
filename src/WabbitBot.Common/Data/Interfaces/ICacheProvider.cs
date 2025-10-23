using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Interfaces
{
    /// <summary>
    /// Pluggable cache provider for DatabaseService. Default can be NoOp; alternative could be in-memory LRU.
    /// </summary>
    public interface ICacheProvider<TEntity>
        where TEntity : Entity
    {
        Task<bool> TryGetAsync(
            object id,
            out TEntity? entity,
            System.Threading.CancellationToken cancellationToken = default
        );
        Task SetAsync(object id, TEntity entity, System.Threading.CancellationToken cancellationToken = default);
        Task RemoveAsync(object id, System.Threading.CancellationToken cancellationToken = default);
        Task<IEnumerable<TEntity>> GetAllAsync(System.Threading.CancellationToken cancellationToken = default);
    }
}
