using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Service
{
    public class NoOpCacheProvider<TEntity> : ICacheProvider<TEntity>
        where TEntity : Entity
    {
        public Task<bool> TryGetAsync(
            object id,
            out TEntity? entity,
            System.Threading.CancellationToken cancellationToken = default
        )
        {
            entity = default;
            return Task.FromResult(false);
        }

        public Task SetAsync(object id, TEntity entity, System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RemoveAsync(object id, System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IEnumerable<TEntity>> GetAllAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<TEntity>>(System.Array.Empty<TEntity>());
        }
    }
}
