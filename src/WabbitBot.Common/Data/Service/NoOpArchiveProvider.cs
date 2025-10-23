using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Service
{
    public class NoOpArchiveProvider<TEntity> : IArchiveProvider<TEntity>
        where TEntity : Entity
    {
        public Task SaveSnapshotAsync(
            TEntity entity,
            Guid archivedBy,
            string? reason,
            System.Threading.CancellationToken cancellationToken = default
        ) => Task.CompletedTask;

        public Task<IEnumerable<TEntity>> GetHistoryAsync(
            Guid entityId,
            int? limit = null,
            System.Threading.CancellationToken cancellationToken = default
        ) => Task.FromResult<IEnumerable<TEntity>>(Array.Empty<TEntity>());

        public Task<TEntity?> GetLatestAsync(
            Guid entityId,
            System.Threading.CancellationToken cancellationToken = default
        ) => Task.FromResult<TEntity?>(default);

        public Task RestoreAsync(TEntity snapshot, System.Threading.CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task PurgeAsync(
            Guid entityId,
            DateTime? olderThan = null,
            System.Threading.CancellationToken cancellationToken = default
        ) => Task.CompletedTask;
    }
}
