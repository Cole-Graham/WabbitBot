namespace WabbitBot.Common.ErrorService;

/// <summary>
/// Defines the contract for a unified error handling service.
/// </summary>
public interface IErrorService
{
    /// <summary>
    /// Handles an error by invoking the specified component's logic.
    /// </summary>
    /// <param name="context">The context of the error.</param>
    /// <param name="component">The error handling component to invoke.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleAsync(ErrorContext context, ErrorComponent component);

    /// <summary>
    /// Captures an exception and creates an <see cref="ErrorContext"/> to be handled.
    /// This is a convenience method for simple error handling.
    /// </summary>
    /// <param name="exception">The exception to capture.</param>
    /// <param name="message">A custom message providing additional context.</param>
    /// <param name="operationName">The name of the operation being performed.</param>
    /// <param name="severity">The severity of the error.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CaptureAsync(
        Exception exception,
        string message,
        string operationName,
        ErrorSeverity severity = ErrorSeverity.Error);
}
