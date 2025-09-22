using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Events;

namespace WabbitBot.Core.Common;

/// <summary>
/// Archive-specific validation rules and operations
/// </summary>
public static partial class CoreValidation
{
    /// <summary>
    /// Validates that an entity is not already archived
    /// </summary>
    public static Func<T, bool> NotAlreadyArchived<T>() where T : class
    {
        return entity =>
        {
            var isArchivedProperty = typeof(T).GetProperty("IsArchived");
            if (isArchivedProperty?.GetValue(entity) is bool isArchived)
            {
                return !isArchived;
            }
            return true; // If no IsArchived property, assume it can be archived
        };
    }

    /// <summary>
    /// Validates that an entity is already archived (for unarchive operations)
    /// </summary>
    public static Func<T, bool> IsAlreadyArchived<T>() where T : class
    {
        return entity =>
        {
            var isArchivedProperty = typeof(T).GetProperty("IsArchived");
            if (isArchivedProperty?.GetValue(entity) is bool isArchived)
            {
                return isArchived;
            }
            return false; // If no IsArchived property, assume it's not archived
        };
    }

    /// <summary>
    /// Validates inactivity threshold for archiving
    /// </summary>
    public static Func<T, bool> InactiveForThreshold<T>(TimeSpan threshold) where T : class
    {
        return entity =>
        {
            var lastActiveProperty = typeof(T).GetProperty("LastActive");
            if (lastActiveProperty?.GetValue(entity) is DateTime lastActive)
            {
                return DateTime.UtcNow - lastActive >= threshold;
            }
            return true; // If no LastActive property, assume it can be archived
        };
    }

    /// <summary>
    /// Validates an entity for archiving with common rules
    /// </summary>
    public static Result<T> ValidateForArchiving<T>(T entity, TimeSpan? inactivityThreshold = null, params Func<T, bool>[] customValidators) where T : class
    {
        if (entity == null)
            return Result<T>.Failure("Entity cannot be null");

        var validators = new List<(Func<T, bool> validator, string errorMessage)>
        {
            (NotAlreadyArchived<T>(), "Entity is already archived")
        };

        // Add inactivity threshold if provided
        if (inactivityThreshold.HasValue)
        {
            validators.Add((InactiveForThreshold<T>(inactivityThreshold.Value),
                $"Entity has been active within the last {inactivityThreshold.Value.TotalDays} days"));
        }

        // Add custom validators
        foreach (var validator in customValidators)
        {
            validators.Add((validator, $"Custom validation failed for {typeof(T).Name}"));
        }

        return Validate(entity, validators.ToArray());
    }

    /// <summary>
    /// Validates an entity for unarchiving with common rules
    /// </summary>
    public static Result<T> ValidateForUnarchiving<T>(T entity, params Func<T, bool>[] customValidators) where T : class
    {
        if (entity == null)
            return Result<T>.Failure("Entity cannot be null");

        var validators = new List<(Func<T, bool> validator, string errorMessage)>
        {
            (IsAlreadyArchived<T>(), "Entity is not archived")
        };

        // Add custom validators
        foreach (var validator in customValidators)
        {
            validators.Add((validator, $"Custom validation failed for {typeof(T).Name}"));
        }

        return Validate(entity, validators.ToArray());
    }

    /// <summary>
    /// Async validation for archiving with event-based checks
    /// </summary>
    public static async Task<Result<T>> ValidateForArchivingAsync<T>(T entity, TimeSpan? inactivityThreshold = null, params Func<T, Task<bool>>[] asyncValidators) where T : class
    {
        if (entity == null)
            return Result<T>.Failure("Entity cannot be null");

        var validators = new List<Func<T, Task<bool>>>();

        // Add common validators
        validators.Add(e => Task.FromResult(NotAlreadyArchived<T>()(e)));

        if (inactivityThreshold.HasValue)
        {
            validators.Add(e => Task.FromResult(InactiveForThreshold<T>(inactivityThreshold.Value)(e)));
        }

        // Add custom async validators
        validators.AddRange(asyncValidators);

        return await ValidateAsync(entity, validators.ToArray());
    }

    /// <summary>
    /// Generic archive operation with validation
    /// </summary>
    public static async Task<Result<T>> ArchiveEntityAsync<T>(T entity, Func<T, Task> archiveAction, TimeSpan? inactivityThreshold = null, params Func<T, bool>[] customValidators) where T : class
    {
        var validationResult = ValidateForArchiving(entity, inactivityThreshold, customValidators);
        if (!validationResult.Success)
        {
            return validationResult;
        }

        try
        {
            await archiveAction(entity);
            return Result<T>.CreateSuccess(entity, "Entity archived successfully");
        }
        catch (Exception ex)
        {
            return Result<T>.Failure($"Failed to archive entity: {ex.Message}");
        }
    }

    /// <summary>
    /// Generic unarchive operation with validation
    /// </summary>
    public static async Task<Result<T>> UnarchiveEntityAsync<T>(T entity, Func<T, Task> unarchiveAction, params Func<T, bool>[] customValidators) where T : class
    {
        var validationResult = ValidateForUnarchiving(entity, customValidators);
        if (!validationResult.Success)
        {
            return validationResult;
        }

        try
        {
            await unarchiveAction(entity);
            return Result<T>.CreateSuccess(entity, "Entity unarchived successfully");
        }
        catch (Exception ex)
        {
            return Result<T>.Failure($"Failed to unarchive entity: {ex.Message}");
        }
    }
}
