using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Interfaces
{
    /// <summary>
    /// Provider-agnostic repository adapter used by DatabaseService to perform persistence operations.
    /// Implementations live in higher layers (e.g., Core via EF).
    /// </summary>
    public interface IRepositoryAdapter<TEntity> where TEntity : Entity
    {
        Task<TEntity?> GetByIdAsync(object id);
        Task<bool> ExistsAsync(object id);
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<Result<TEntity>> CreateAsync(TEntity entity);
        Task<Result<TEntity>> UpdateAsync(TEntity entity);
        Task<Result<TEntity>> DeleteAsync(object id);

        // Optional narrow reads; prefer EF in Core for complex queries.
        Task<TEntity?> GetByNameAsync(string name);
    }
}


