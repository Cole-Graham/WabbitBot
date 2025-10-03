using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Service
{
    public class NoOpArchiveProvider<TEntity> : IArchiveProvider<TEntity> where TEntity : Entity
    {
        public Task SaveSnapshotAsync(TEntity entity, Guid archivedBy, string? reason) => Task.CompletedTask;
        public Task<IEnumerable<TEntity>> GetHistoryAsync(Guid entityId, int? limit = null) => Task.FromResult<IEnumerable<TEntity>>(Array.Empty<TEntity>());
        public Task<TEntity?> GetLatestAsync(Guid entityId) => Task.FromResult<TEntity?>(default);
        public Task RestoreAsync(TEntity snapshot) => Task.CompletedTask;
        public Task PurgeAsync(Guid entityId, DateTime? olderThan = null) => Task.CompletedTask;
    }
}


