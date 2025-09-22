using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common;

/// <summary>
/// Consolidated validation system with generic and cross-cutting functionality
/// All domain-specific validation should be implemented in partial classes
/// </summary>
public static partial class CoreValidation
{
    #region Generic Validation Methods

    /// <summary>
    /// Validates an entity against multiple validation rules
    /// </summary>
    public static Result<T> Validate<T>(T entity, params Func<T, bool>[] validators)
    {
        foreach (var validator in validators)
        {
            if (!validator(entity))
                return Result<T>.Failure($"Validation failed for {typeof(T).Name}");
        }
        return Result<T>.CreateSuccess(entity);
    }

    /// <summary>
    /// Validates an entity against multiple validation rules with custom error messages
    /// </summary>
    public static Result<T> Validate<T>(T entity, params (Func<T, bool> validator, string errorMessage)[] validators)
    {
        foreach (var (validator, errorMessage) in validators)
        {
            if (!validator(entity))
                return Result<T>.Failure(errorMessage);
        }
        return Result<T>.CreateSuccess(entity);
    }

    /// <summary>
    /// Validates an entity asynchronously
    /// </summary>
    public static async Task<Result<T>> ValidateAsync<T>(T entity, params Func<T, Task<bool>>[] validators)
    {
        foreach (var validator in validators)
        {
            if (!await validator(entity))
                return Result<T>.Failure($"Async validation failed for {typeof(T).Name}");
        }
        return Result<T>.CreateSuccess(entity);
    }

    /// <summary>
    /// Validates an entity asynchronously with custom error messages
    /// </summary>
    public static async Task<Result<T>> ValidateAsync<T>(T entity, params (Func<T, Task<bool>> validator, string errorMessage)[] validators)
    {
        foreach (var (validator, errorMessage) in validators)
        {
            if (!await validator(entity))
                return Result<T>.Failure(errorMessage);
        }
        return Result<T>.CreateSuccess(entity);
    }

    #endregion

    #region Common Validation Utilities

    /// <summary>
    /// Validates a string parameter
    /// </summary>
    public static Result<string> ValidateString(string value, string parameterName, bool required = true, int? maxLength = null, int? minLength = null)
    {
        if (required && string.IsNullOrWhiteSpace(value))
            return Result<string>.Failure($"{parameterName} is required");

        if (!required && string.IsNullOrWhiteSpace(value))
            return Result<string>.CreateSuccess(value);

        if (minLength.HasValue && value.Length < minLength.Value)
            return Result<string>.Failure($"{parameterName} must be at least {minLength.Value} characters");

        if (maxLength.HasValue && value.Length > maxLength.Value)
            return Result<string>.Failure($"{parameterName} must be no more than {maxLength.Value} characters");

        return Result<string>.CreateSuccess(value);
    }

    /// <summary>
    /// Validates a numeric parameter
    /// </summary>
    public static Result<T> ValidateNumber<T>(T value, string parameterName, T? minValue = null, T? maxValue = null) where T : struct, IComparable<T>
    {
        if (minValue.HasValue && value.CompareTo(minValue.Value) < 0)
            return Result<T>.Failure($"{parameterName} must be at least {minValue.Value}");

        if (maxValue.HasValue && value.CompareTo(maxValue.Value) > 0)
            return Result<T>.Failure($"{parameterName} must be no more than {maxValue.Value}");

        return Result<T>.CreateSuccess(value);
    }

    /// <summary>
    /// Validates that a collection is not empty
    /// </summary>
    public static Result<T> ValidateNotEmpty<T>(T collection, string parameterName) where T : System.Collections.IEnumerable
    {
        var hasItems = false;
        foreach (var item in collection)
        {
            hasItems = true;
            break;
        }

        if (!hasItems)
            return Result<T>.Failure($"{parameterName} cannot be empty");

        return Result<T>.CreateSuccess(collection);
    }

    /// <summary>
    /// Validates that a collection has a specific count
    /// </summary>
    public static Result<T> ValidateCount<T>(T collection, string parameterName, int expectedCount) where T : System.Collections.IEnumerable
    {
        var count = 0;
        foreach (var item in collection)
        {
            count++;
        }

        if (count != expectedCount)
            return Result<T>.Failure($"{parameterName} must have exactly {expectedCount} items, but has {count}");

        return Result<T>.CreateSuccess(collection);
    }

    /// <summary>
    /// Validates that a collection has a count within a range
    /// </summary>
    public static Result<T> ValidateCountRange<T>(T collection, string parameterName, int minCount, int maxCount) where T : System.Collections.IEnumerable
    {
        var count = 0;
        foreach (var item in collection)
        {
            count++;
        }

        if (count < minCount)
            return Result<T>.Failure($"{parameterName} must have at least {minCount} items, but has {count}");

        if (count > maxCount)
            return Result<T>.Failure($"{parameterName} must have no more than {maxCount} items, but has {count}");

        return Result<T>.CreateSuccess(collection);
    }

    #endregion
}