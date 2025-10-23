using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Interfaces
{
    /// <summary>
    /// Pluggable archive provider for immutable history snapshots.
    /// </summary>
    public interface IArchiveProvider<TEntity>
        where TEntity : Entity
    {
        Task SaveSnapshotAsync(
            TEntity entity,
            Guid archivedBy,
            string? reason,
            System.Threading.CancellationToken cancellationToken = default
        );
        Task<IEnumerable<TEntity>> GetHistoryAsync(
            Guid entityId,
            int? limit = null,
            System.Threading.CancellationToken cancellationToken = default
        );
        Task<TEntity?> GetLatestAsync(Guid entityId, System.Threading.CancellationToken cancellationToken = default);
        Task RestoreAsync(TEntity snapshot, System.Threading.CancellationToken cancellationToken = default);
        Task PurgeAsync(
            Guid entityId,
            DateTime? olderThan = null,
            System.Threading.CancellationToken cancellationToken = default
        );
    }
}
