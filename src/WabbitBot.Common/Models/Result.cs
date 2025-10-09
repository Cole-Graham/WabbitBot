using System.Collections.Generic;

namespace WabbitBot.Common.Models;

/// <summary>
/// Generic result type for functional programming approach
/// </summary>
/// <typeparam name="T">The type of data returned on success</typeparam>
public record Result<T>(
    bool Success,
    T? Data = default,
    string? ErrorMessage = null,
    Dictionary<string, object>? Metadata = null
)
{
    /// <summary>
    /// Creates a successful result with data
    /// </summary>
    public static Result<T> CreateSuccess(T data, Dictionary<string, object>? metadata = null) =>
        new(true, data, Metadata: metadata);

    /// <summary>
    /// Creates a successful result with data and message
    /// </summary>
    public static Result<T> CreateSuccess(T data, string message, Dictionary<string, object>? metadata = null) =>
        new(true, data, ErrorMessage: message, Metadata: metadata);

    /// <summary>
    /// Creates a failed result with error message
    /// </summary>
    public static Result<T> Failure(string errorMessage, Dictionary<string, object>? metadata = null) =>
        new(false, ErrorMessage: errorMessage, Metadata: metadata);

    /// <summary>
    /// Implicit conversion from data to successful result
    /// </summary>
    public static implicit operator Result<T>(T data) => CreateSuccess(data);

    /// <summary>
    /// Implicit conversion from error message to failed result
    /// </summary>
    public static implicit operator Result<T>(string errorMessage) => Failure(errorMessage);
}

/// <summary>
/// Non-generic result type for operations that don't return data
/// </summary>
public record Result(
    bool Success,
    string? Message = null,
    string? ErrorMessage = null,
    Dictionary<string, object>? Metadata = null
)
{
    /// <summary>
    /// Creates a successful result with message
    /// </summary>
    public static Result CreateSuccess(string? message = null, Dictionary<string, object>? metadata = null) =>
        new(true, Message: message, Metadata: metadata);

    /// <summary>
    /// Creates a failed result with error message
    /// </summary>
    public static Result Failure(string errorMessage, Dictionary<string, object>? metadata = null) =>
        new(false, ErrorMessage: errorMessage, Metadata: metadata);

    /// <summary>
    /// Implicit conversion from success message to successful result
    /// </summary>
    public static implicit operator Result(string message) => CreateSuccess(message);
}
