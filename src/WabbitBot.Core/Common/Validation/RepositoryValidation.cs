using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common;

/// <summary>
/// Repository-specific validation rules and operations
/// </summary>
public static partial class CoreValidation
{
    /// <summary>
    /// Validates that a repository operation is valid
    /// </summary>
    public static Result<T> ValidateRepositoryOperation<T>(T entity, string operation) where T : class
    {
        if (entity == null)
            return Result<T>.Failure($"Entity cannot be null for {operation} operation");

        return Result<T>.CreateSuccess(entity);
    }

    /// <summary>
    /// Validates that a repository query is valid
    /// </summary>
    public static Result<T> ValidateRepositoryQuery<T>(T query, string queryType) where T : class
    {
        if (query == null)
            return Result<T>.Failure($"Query cannot be null for {queryType}");

        return Result<T>.CreateSuccess(query);
    }

    /// <summary>
    /// Validates that an entity exists in the repository
    /// </summary>
    public static async Task<bool> EntityExists<T>(string entityId, Func<string, Task<T?>> getEntityFunc) where T : class
    {
        if (string.IsNullOrEmpty(entityId))
            return false;

        var entity = await getEntityFunc(entityId);
        return entity != null;
    }

    /// <summary>
    /// Validates that an entity doesn't already exist in the repository
    /// </summary>
    public static async Task<bool> EntityDoesNotExist<T>(string entityId, Func<string, Task<T?>> getEntityFunc) where T : class
    {
        return !await EntityExists(entityId, getEntityFunc);
    }

    /// <summary>
    /// Validates that a collection of entities all exist
    /// </summary>
    public static async Task<bool> AllEntitiesExist<T>(IEnumerable<string> entityIds, Func<string, Task<T?>> getEntityFunc) where T : class
    {
        if (entityIds == null || !entityIds.Any())
            return true;

        foreach (var entityId in entityIds)
        {
            if (!await EntityExists(entityId, getEntityFunc))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that a collection of entities don't exist
    /// </summary>
    public static async Task<bool> NoEntitiesExist<T>(IEnumerable<string> entityIds, Func<string, Task<T?>> getEntityFunc) where T : class
    {
        if (entityIds == null || !entityIds.Any())
            return true;

        foreach (var entityId in entityIds)
        {
            if (await EntityExists(entityId, getEntityFunc))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that an entity can be created
    /// </summary>
    public static async Task<Result<T>> ValidateForCreation<T>(T entity, Func<string, Task<T?>> getEntityFunc, string? entityId = null) where T : class
    {
        if (entity == null)
            return Result<T>.Failure("Entity cannot be null");

        if (!string.IsNullOrEmpty(entityId) && await EntityExists(entityId, getEntityFunc))
            return Result<T>.Failure($"Entity with ID {entityId} already exists");

        return Result<T>.CreateSuccess(entity);
    }

    /// <summary>
    /// Validates that an entity can be updated
    /// </summary>
    public static async Task<Result<T>> ValidateForUpdate<T>(T entity, string entityId, Func<string, Task<T?>> getEntityFunc) where T : class
    {
        if (entity == null)
            return Result<T>.Failure("Entity cannot be null");

        if (string.IsNullOrEmpty(entityId))
            return Result<T>.Failure("Entity ID cannot be null or empty");

        if (!await EntityExists(entityId, getEntityFunc))
            return Result<T>.Failure($"Entity with ID {entityId} does not exist");

        return Result<T>.CreateSuccess(entity);
    }

    /// <summary>
    /// Validates that an entity can be deleted
    /// </summary>
    public static async Task<Result<T>> ValidateForDeletion<T>(string entityId, Func<string, Task<T?>> getEntityFunc) where T : class
    {
        if (string.IsNullOrEmpty(entityId))
            return Result<T>.Failure("Entity ID cannot be null or empty");

        if (!await EntityExists(entityId, getEntityFunc))
            return Result<T>.Failure($"Entity with ID {entityId} does not exist");

        return Result<T>.CreateSuccess(default(T)!);
    }
}