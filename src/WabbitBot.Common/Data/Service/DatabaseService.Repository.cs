using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Service;

/// <summary>
/// Repository operations for DatabaseService using PostgreSQL with Npgsql
/// Implements database CRUD operations using raw SQL queries
/// </summary>
public partial class DatabaseService<TEntity>
    where TEntity : Entity
{
    // Repository-specific properties and configuration
    private string? _tableName;
    private IEnumerable<string>? _columns;
    private string? _idColumn;

    /// <summary>
    /// Initialize repository configuration for the entity type
    /// This would be called from a factory method or configuration
    /// </summary>
    private void InitializeRepository(string tableName, IEnumerable<string> columns, string idColumn = "Id")
    {
        _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
        _columns = columns ?? throw new ArgumentNullException(nameof(columns));
        _idColumn = idColumn ?? throw new ArgumentNullException(nameof(idColumn));
    }

    #region Repository Operations Implementation

    protected virtual async Task<Result<TEntity>> CreateInRepositoryAsync(TEntity entity)
    {
        if (_repositoryAdapter is null)
        {
            return Result<TEntity>.Failure("Repository adapter not configured.");
        }
        try
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = entity.CreatedAt;
            return await _repositoryAdapter.CreateAsync(entity);
        }
        catch (Exception ex)
        {
            return Result<TEntity>.Failure($"Repository create failed: {ex.Message}");
        }
    }

    protected virtual async Task<Result<TEntity>> UpdateInRepositoryAsync(TEntity entity)
    {
        if (_repositoryAdapter is null)
        {
            return Result<TEntity>.Failure("Repository adapter not configured.");
        }
        try
        {
            entity.UpdatedAt = DateTime.UtcNow;
            return await _repositoryAdapter.UpdateAsync(entity);
        }
        catch (Exception ex)
        {
            return Result<TEntity>.Failure($"Repository update failed: {ex.Message}");
        }
    }

    protected virtual async Task<Result<TEntity>> DeleteFromRepositoryAsync(object id)
    {
        if (_repositoryAdapter is null)
        {
            return Result<TEntity>.Failure("Repository adapter not configured.");
        }
        try
        {
            return await _repositoryAdapter.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            return Result<TEntity>.Failure($"Repository delete failed: {ex.Message}");
        }
    }

    protected virtual async Task<bool> ExistsInRepositoryAsync(object id)
    {
        if (_repositoryAdapter is null)
        {
            return false;
        }
        try
        {
            return await _repositoryAdapter.ExistsAsync(id);
        }
        catch
        {
            return false;
        }
    }

    protected virtual async Task<TEntity?> GetByIdFromRepositoryAsync(object id)
    {
        if (_repositoryAdapter is null)
        {
            Console.WriteLine($"🔍 DEBUG: Repository adapter is null for {typeof(TEntity).Name}");
            return default;
        }
        try
        {
            Console.WriteLine(
                $"🔍 DEBUG: Calling repository adapter GetByIdAsync for {typeof(TEntity).Name} with ID: {id}"
            );
            var result = await _repositoryAdapter.GetByIdAsync(id);
            Console.WriteLine($"🔍 DEBUG: Repository adapter returned: {result?.ToString() ?? "null"}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"🔍 DEBUG: Exception in GetByIdFromRepositoryAsync for {typeof(TEntity).Name}: {ex.Message}"
            );
            Console.WriteLine($"🔍 DEBUG: Exception type: {ex.GetType().Name}");
            Console.WriteLine($"🔍 DEBUG: Stack trace: {ex.StackTrace}");
            return default;
        }
    }

    protected virtual async Task<TEntity?> GetByNameFromRepositoryAsync(string name)
    {
        if (_repositoryAdapter is null)
        {
            return default;
        }
        try
        {
            return await _repositoryAdapter.GetByNameAsync(name);
        }
        catch
        {
            return default;
        }
    }

    protected virtual async Task<IEnumerable<TEntity>> GetAllFromRepositoryAsync()
    {
        if (_repositoryAdapter is not null)
        {
            return await _repositoryAdapter.GetAllAsync();
        }
        return Array.Empty<TEntity>();
    }

    protected virtual async Task<IEnumerable<TEntity>> GetByDateRangeFromRepositoryAsync(
        DateTime startDate,
        DateTime endDate
    )
    {
        // Prefer EF-level implementation in Core if needed; not provided by adapter by default
        return Array.Empty<TEntity>();
    }

    protected virtual async Task<IEnumerable<TEntity>> QueryFromRepositoryAsync(string whereClause, object? parameters)
    {
        // Removed in favor of EF queries in Core via WithDbContext
        return Array.Empty<TEntity>();
    }

    #endregion
}
