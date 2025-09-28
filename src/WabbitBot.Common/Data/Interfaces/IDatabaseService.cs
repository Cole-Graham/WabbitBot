using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Interfaces
{
    /// <summary>
    /// Interface for database services that handle CRUD operations for entities
    /// Supports different database components (Repository, Cache, Archive)
    /// </summary>
    public interface IDatabaseService<TEntity> where TEntity : Entity
    {
        // Core CRUD operations
        Task<Result<TEntity>> CreateAsync(TEntity entity, DatabaseComponent component);
        Task<Result<TEntity>> UpdateAsync(TEntity entity, DatabaseComponent component);
        Task<Result<TEntity>> DeleteAsync(object id, DatabaseComponent component);
        Task<Result<bool>> ExistsAsync(object id, DatabaseComponent component);

        // Query operations
        Task<Result<TEntity?>> GetByIdAsync(object id, DatabaseComponent component);
        Task<Result<TEntity?>> GetByStringIdAsync(string id, DatabaseComponent component);
        Task<Result<TEntity?>> GetByNameAsync(string name, DatabaseComponent component);
        Task<Result<IEnumerable<TEntity>>> GetAllAsync(DatabaseComponent component);
        Task<Result<IEnumerable<TEntity>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, DatabaseComponent component);
        Task<Result<IEnumerable<TEntity>>> QueryAsync(string whereClause, object? parameters, DatabaseComponent component);
    }

    /// <summary>
    /// Enumeration of database component types
    /// </summary>
    public enum DatabaseComponent
    {
        Repository,
        Cache,
        Archive
    }
}
