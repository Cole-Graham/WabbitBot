using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Interfaces
{
    /// <summary>
    /// Pluggable archive provider for immutable history snapshots.
    /// </summary>
    public interface IArchiveProvider<TEntity> where TEntity : Entity
    {
        Task SaveSnapshotAsync(TEntity entity, Guid archivedBy, string? reason);
        Task<IEnumerable<TEntity>> GetHistoryAsync(Guid entityId, int? limit = null);
        Task<TEntity?> GetLatestAsync(Guid entityId);
        Task RestoreAsync(TEntity snapshot);
        Task PurgeAsync(Guid entityId, DateTime? olderThan = null);
    }
}


