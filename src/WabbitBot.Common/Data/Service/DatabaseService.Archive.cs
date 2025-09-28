using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Service;

/// <summary>
/// Archive operations for DatabaseService using PostgreSQL
/// Handles archiving of deleted or modified entities for historical tracking
/// </summary>
public partial class DatabaseService<TEntity> where TEntity : Entity
{
    private DatabaseService<TEntity>? _archiveService; // Nullable as it's initialized later

    /// <summary>
    /// Initialize archive configuration for the entity type
    /// </summary>
    private void InitializeArchive(string archiveTableName, IEnumerable<string> archiveColumns)
    {
        _archiveService = new DatabaseService<TEntity>(archiveTableName, archiveColumns, archiveTableName, archiveColumns);
    }

    #region Archive Operations Implementation

    private async Task<Result<TEntity>> CreateInArchiveAsync(TEntity entity)
    {
        return await (_archiveService?.CreateAsync(entity, DatabaseComponent.Archive) ?? Task.FromResult(Result<TEntity>.Failure("Archive service not initialized.")));
    }

    private async Task<Result<TEntity>> UpdateInArchiveAsync(TEntity entity)
    {
        return await (_archiveService?.UpdateAsync(entity, DatabaseComponent.Archive) ?? Task.FromResult(Result<TEntity>.Failure("Archive service not initialized.")));
    }

    private async Task<Result<TEntity>> DeleteFromArchiveAsync(object id)
    {
        return await (_archiveService?.DeleteAsync(id, DatabaseComponent.Archive) ?? Task.FromResult(Result<TEntity>.Failure("Archive service not initialized.")));
    }

    #endregion
}
