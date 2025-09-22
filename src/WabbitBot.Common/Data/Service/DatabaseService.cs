using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Service;

/// <summary>
/// Unified database service that handles repository, cache, and archive operations for a single entity type.
/// Uses partial classes for clean organization of different operation types.
/// Implements the same IDatabaseService interface as individual services for compatibility.
/// </summary>
public partial class DatabaseService<TEntity> : IDatabaseService<TEntity> where TEntity : Entity
{
    /// <summary>
    /// Creates a new DatabaseService with default configuration
    /// </summary>
    public DatabaseService()
    {
        // Initialize with default settings
        // These would be overridden by entity-specific factory methods
        InitializeRepository("entities", new[] { "Id", "Name", "CreatedAt", "UpdatedAt" });
        InitializeCache(1000, TimeSpan.FromHours(1));
        InitializeArchive("entities_archive", new[] { "Id", "Name", "CreatedAt", "UpdatedAt", "ArchivedAt" });
    }

    /// <summary>
    /// Creates a new DatabaseService with custom configuration
    /// </summary>
    public DatabaseService(
        string tableName,
        IEnumerable<string> columns,
        string archiveTableName,
        IEnumerable<string> archiveColumns,
        int cacheMaxSize = 1000,
        TimeSpan? cacheDefaultExpiry = null,
        string idColumn = "Id")
    {
        InitializeRepository(tableName, columns, idColumn);
        InitializeCache(cacheMaxSize, cacheDefaultExpiry);
        InitializeArchive(archiveTableName, archiveColumns);
    }

    #region IDatabaseService Implementation

    public async Task<Result<TEntity>> CreateAsync(TEntity entity, DatabaseComponent component)
    {
        return component switch
        {
            DatabaseComponent.Repository => await CreateInRepositoryAsync(entity),
            DatabaseComponent.Cache => await CreateInCacheAsync(entity),
            DatabaseComponent.Archive => await CreateInArchiveAsync(entity),
            _ => throw new ArgumentException($"Unsupported component: {component}", nameof(component))
        };
    }

    public async Task<Result<TEntity>> UpdateAsync(TEntity entity, DatabaseComponent component)
    {
        return component switch
        {
            DatabaseComponent.Repository => await UpdateInRepositoryAsync(entity),
            DatabaseComponent.Cache => await UpdateInCacheAsync(entity),
            _ => throw new ArgumentException($"Unsupported component for update: {component}", nameof(component))
        };
    }

    public async Task<Result<TEntity>> DeleteAsync(object id, DatabaseComponent component)
    {
        return component switch
        {
            DatabaseComponent.Repository => await DeleteFromRepositoryAsync(id),
            DatabaseComponent.Cache => await DeleteFromCacheAsync(id),
            _ => throw new ArgumentException($"Unsupported component for delete: {component}", nameof(component))
        };
    }

    public async Task<bool> ExistsAsync(object id, DatabaseComponent component)
    {
        return component switch
        {
            DatabaseComponent.Repository => await ExistsInRepositoryAsync(id),
            DatabaseComponent.Cache => await ExistsInCacheAsync(id),
            _ => throw new ArgumentException($"Unsupported component for exists: {component}", nameof(component))
        };
    }

    public async Task<TEntity?> GetByIdAsync(object id, DatabaseComponent component)
    {
        return component switch
        {
            DatabaseComponent.Repository => await GetByIdFromRepositoryAsync(id),
            DatabaseComponent.Cache => await GetByIdFromCacheAsync(id),
            _ => throw new ArgumentException($"Unsupported component for GetById: {component}", nameof(component))
        };
    }

    public async Task<TEntity?> GetByStringIdAsync(string id, DatabaseComponent component)
    {
        // Convert string to appropriate type (Guid for now)
        if (Guid.TryParse(id, out var guid))
        {
            return await GetByIdAsync(guid, component);
        }
        return null;
    }

    public async Task<TEntity?> GetByNameAsync(string name, DatabaseComponent component)
    {
        return component switch
        {
            DatabaseComponent.Repository => await GetByNameFromRepositoryAsync(name),
            DatabaseComponent.Cache => await GetByNameFromCacheAsync(name),
            _ => throw new ArgumentException($"Unsupported component for GetByName: {component}", nameof(component))
        };
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync(DatabaseComponent component)
    {
        return component switch
        {
            DatabaseComponent.Repository => await GetAllFromRepositoryAsync(),
            DatabaseComponent.Cache => await GetAllFromCacheAsync(),
            _ => throw new ArgumentException($"Unsupported component for GetAll: {component}", nameof(component))
        };
    }

    public async Task<IEnumerable<TEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, DatabaseComponent component)
    {
        return component switch
        {
            DatabaseComponent.Repository => await GetByDateRangeFromRepositoryAsync(startDate, endDate),
            _ => throw new ArgumentException($"Unsupported component for GetByDateRange: {component}", nameof(component))
        };
    }

    public async Task<IEnumerable<TEntity>> QueryAsync(string whereClause, object? parameters, DatabaseComponent component)
    {
        return component switch
        {
            DatabaseComponent.Repository => await QueryFromRepositoryAsync(whereClause, parameters),
            _ => throw new ArgumentException($"Unsupported component for Query: {component}", nameof(component))
        };
    }

    #endregion

    // Implementation methods are defined in partial classes:
    // - DatabaseService.Repository.cs: Repository operations
    // - DatabaseService.Cache.cs: Cache operations
    // - DatabaseService.Archive.cs: Archive operations
}
