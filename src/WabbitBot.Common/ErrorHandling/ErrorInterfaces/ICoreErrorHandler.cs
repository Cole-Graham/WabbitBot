using System;
using System.Threading.Tasks;

namespace WabbitBot.Common.ErrorHandling;

/// <summary>
/// Interface for handling errors within the Core project and coordinating with the GlobalErrorHandler
/// for cross-project error handling.
/// </summary>
public interface ICoreErrorHandler
{
    /// <summary>
    /// Handles an error that occurred in the Core project.
    /// </summary>
    /// <param name="ex">The exception that occurred</param>
    /// <param name="context">Optional context information about where the error occurred</param>
    Task HandleErrorAsync(Exception ex, string? context = null);

    /// <summary>
    /// Initializes the Core error handling system.
    /// </summary>
    Task InitializeAsync();
}
