using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Interfaces
{
    public interface IBaseRepository<TEntity> where TEntity : BaseEntity
    {
        Task<TEntity?> GetByIdAsync(object id);
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<int> AddAsync(TEntity entity);
        Task<bool> UpdateAsync(TEntity entity);
        Task<bool> DeleteAsync(object id);
        Task<bool> ExistsAsync(object id);
        Task<IEnumerable<TEntity>> QueryAsync(string whereClause, object? parameters = null);
    }
}
