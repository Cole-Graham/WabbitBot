using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Service
{
    public class NoOpCacheProvider<TEntity> : ICacheProvider<TEntity> where TEntity : Entity
    {
        public Task<bool> TryGetAsync(object id, out TEntity? entity)
        {
            entity = default;
            return Task.FromResult(false);
        }

        public Task SetAsync(object id, TEntity entity)
        {
            return Task.CompletedTask;
        }

        public Task RemoveAsync(object id)
        {
            return Task.CompletedTask;
        }

        public Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<TEntity>>(System.Array.Empty<TEntity>());
        }
    }
}


