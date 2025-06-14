using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Interfaces
{
    public interface IBaseArchive<TEntity> where TEntity : BaseEntity
    {
        Task<TEntity?> GetByIdAsync(object id);
        Task<IEnumerable<TEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<int> ArchiveAsync(TEntity entity);
        Task<bool> DeleteAsync(object id);
        Task<IEnumerable<TEntity>> QueryAsync(string whereClause, object? parameters = null);
    }
}
