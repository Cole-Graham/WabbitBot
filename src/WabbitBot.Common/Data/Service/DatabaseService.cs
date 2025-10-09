using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Schema;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Service;

/// <summary>
/// Unified database service that handles repository, cache, and archive operations for a single entity type.
/// Uses partial classes for clean organization of different operation types.
/// Implements the same IDatabaseService interface as individual services for compatibility.
/// </summary>
public partial class DatabaseService<TEntity> : IDatabaseService<TEntity>
    where TEntity : Entity
{
    private IRepositoryAdapter<TEntity>? _repositoryAdapter;
    private ICacheProvider<TEntity>? _cacheProvider;

    /// <summary>
    /// Creates a new DatabaseService with default configuration
    /// </summary>
    public DatabaseService()
    {
        // Initialize with default settings
        // These would be overridden by entity-specific factory methods
        InitializeRepository("entities", new[] { "Id", "Name", "CreatedAt", "UpdatedAt" });
        InitializeCache(1000, TimeSpan.FromHours(1));
        // InitializeArchive("entities_archive", new[] { "Id", "Name", "CreatedAt", "UpdatedAt", "ArchivedAt" });
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
        string idColumn = "Id"
    )
    {
        InitializeRepository(tableName, columns, idColumn);
        InitializeCache(cacheMaxSize, cacheDefaultExpiry);
        // InitializeArchive(archiveTableName, archiveColumns);
    }

    public void UseRepositoryAdapter(IRepositoryAdapter<TEntity> adapter)
    {
        _repositoryAdapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
    }

    public void UseCacheProvider(ICacheProvider<TEntity> cacheProvider)
    {
        _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
    }

    #region IDatabaseService Implementation

    public async Task<Result<TEntity>> CreateAsync(TEntity entity, DatabaseComponent component)
    {
        Result<TEntity>? result;
        switch (component)
        {
            case DatabaseComponent.Repository:
                result = await CreateInRepositoryAsync(entity);
                break;
            case DatabaseComponent.Cache:
                result = await CreateInCacheAsync(entity);
                break;
            // case DatabaseComponent.Archive:
            //     result = await CreateInArchiveAsync(entity);
            //     break;
            default:
                throw new ArgumentException($"Unsupported component: {component}", nameof(component));
        }

        if (result is null)
        {
            throw new ArgumentException(
                $"Create operation for component '{component}' returned null.",
                nameof(component)
            );
        }

        return result;
    }

    public async Task<Result<TEntity>> UpdateAsync(TEntity entity, DatabaseComponent component)
    {
        Result<TEntity>? result;
        switch (component)
        {
            case DatabaseComponent.Repository:
                result = await UpdateInRepositoryAsync(entity);
                if (result.Success)
                {
                    await UpdateInCacheAsync(result.Data!);
                }
                break;
            case DatabaseComponent.Cache:
                result = await UpdateInCacheAsync(entity);
                break;
            // case DatabaseComponent.Archive:
            //     result = await UpdateInArchiveAsync(entity);
            //     break;
            default:
                throw new ArgumentException($"Unsupported component for update: {component}", nameof(component));
        }

        if (result is null)
        {
            throw new ArgumentException(
                $"Update operation for component '{component}' returned null.",
                nameof(component)
            );
        }

        return result;
    }

    public async Task<Result<TEntity>> DeleteAsync(object id, DatabaseComponent component)
    {
        Result<TEntity>? result;
        switch (component)
        {
            case DatabaseComponent.Repository:
                // Optional: archive snapshot on delete if provider configured
                var existing = await GetByIdFromRepositoryAsync(id);
                if (existing is not null)
                {
                    await ArchiveOnDeleteAsync(existing, Guid.Empty, "Repository delete");
                }
                result = await DeleteFromRepositoryAsync(id);
                if (result.Success)
                {
                    await DeleteFromCacheAsync(id);
                }
                break;
            case DatabaseComponent.Cache:
                result = await DeleteFromCacheAsync(id);
                break;
            // case DatabaseComponent.Archive:
            //     result = await DeleteFromArchiveAsync(id);
            //     break;
            default:
                throw new ArgumentException($"Unsupported component for delete: {component}", nameof(component));
        }

        if (result is null)
        {
            throw new ArgumentException(
                $"Delete operation for component '{component}' returned null.",
                nameof(component)
            );
        }

        return result;
    }

    public async Task<Result<bool>> ExistsAsync(object id, DatabaseComponent component)
    {
        switch (component)
        {
            case DatabaseComponent.Repository:
                if (await ExistsInCacheAsync(id))
                {
                    return Result<bool>.CreateSuccess(true);
                }
                return Result<bool>.CreateSuccess(await ExistsInRepositoryAsync(id));
            case DatabaseComponent.Cache:
                return Result<bool>.CreateSuccess(await ExistsInCacheAsync(id));
            default:
                return Result<bool>.Failure($"Unsupported component for exists: {component}");
        }
    }

    public async Task<Result<TEntity?>> GetByIdAsync(object id, DatabaseComponent component)
    {
        TEntity? entity;
        switch (component)
        {
            case DatabaseComponent.Repository:
                entity = await GetByIdFromCacheAsync(id);
                if (entity is not null)
                {
                    return Result<TEntity?>.CreateSuccess(entity);
                }
                entity = await GetByIdFromRepositoryAsync(id);
                return Result<TEntity?>.CreateSuccess(entity);

            case DatabaseComponent.Cache:
                entity = await GetByIdFromCacheAsync(id);
                return Result<TEntity?>.CreateSuccess(entity);

            default:
                return Result<TEntity?>.Failure($"Unsupported component for GetById: {component}");
        }
    }

    public async Task<Result<TEntity?>> GetByStringIdAsync(string id, DatabaseComponent component)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return Result<TEntity?>.CreateSuccess(null, "Invalid GUID format.");
        }

        TEntity? entity;
        switch (component)
        {
            case DatabaseComponent.Repository:
                entity = await GetByIdFromCacheAsync(guid);
                if (entity is not null)
                {
                    return Result<TEntity?>.CreateSuccess(entity);
                }
                entity = await GetByIdFromRepositoryAsync(guid);
                return Result<TEntity?>.CreateSuccess(entity);
            case DatabaseComponent.Cache:
                entity = await GetByIdFromCacheAsync(guid);
                return Result<TEntity?>.CreateSuccess(entity);
            default:
                return Result<TEntity?>.Failure($"Unsupported component for GetByStringId: {component}");
        }
    }

    public async Task<Result<TEntity?>> GetByNameAsync(string name, DatabaseComponent component)
    {
        TEntity? entity;
        switch (component)
        {
            case DatabaseComponent.Repository:
                entity = await GetByNameFromCacheAsync(name);
                if (entity is not null)
                {
                    return Result<TEntity?>.CreateSuccess(entity);
                }
                entity = await GetByNameFromRepositoryAsync(name);
                return Result<TEntity?>.CreateSuccess(entity);
            case DatabaseComponent.Cache:
                entity = await GetByNameFromCacheAsync(name);
                return Result<TEntity?>.CreateSuccess(entity);
            default:
                return Result<TEntity?>.Failure($"Unsupported component for GetByName: {component}");
        }
    }

    public async Task<Result<IEnumerable<TEntity>>> GetAllAsync(DatabaseComponent component)
    {
        IEnumerable<TEntity>? result;
        switch (component)
        {
            case DatabaseComponent.Repository:
                result = await GetAllFromRepositoryAsync();
                return Result<IEnumerable<TEntity>>.CreateSuccess(result);
            case DatabaseComponent.Cache:
                result = await GetAllFromCacheAsync();
                return Result<IEnumerable<TEntity>>.CreateSuccess(result);
            default:
                return Result<IEnumerable<TEntity>>.Failure($"Unsupported component for GetAll: {component}");
        }
    }

    public async Task<Result<IEnumerable<TEntity>>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        DatabaseComponent component
    )
    {
        IEnumerable<TEntity>? result;
        switch (component)
        {
            case DatabaseComponent.Repository:
                result = await GetByDateRangeFromRepositoryAsync(startDate, endDate);
                return Result<IEnumerable<TEntity>>.CreateSuccess(result);
            default:
                return Result<IEnumerable<TEntity>>.Failure($"Unsupported component for GetByDateRange: {component}");
        }
    }

    // QueryAsync removed. Use CoreService.WithDbContext for complex queries.

    #endregion

    // Implementation methods are defined in partial classes:
    // - DatabaseService.Repository.cs: Repository operations
    // - DatabaseService.Cache.cs: Cache operations
    // - DatabaseService.Archive.cs: Archive operations
}
