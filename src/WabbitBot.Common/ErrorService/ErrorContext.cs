namespace WabbitBot.Common.ErrorService;

/// <summary>
/// Represents the immutable context of an error that has occurred.
/// </summary>
public sealed class ErrorContext
{
    /// <summary>
    /// The exception that was thrown, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// A custom message providing more context about the error.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The severity level of the error.
    /// </summary>
    public ErrorSeverity Severity { get; }

    /// <summary>
    /// A unique identifier to correlate logs and traces for this error.
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// The name of the operation that was being performed when the error occurred.
    /// </summary>
    public string OperationName { get; }

    /// <summary>
    /// Optional metadata providing additional context.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; }

    public ErrorContext(
        string message,
        ErrorSeverity severity,
        string operationName,
        Exception? exception = null,
        IReadOnlyDictionary<string, object>? metadata = null
    )
    {
        Message = message;
        Severity = severity;
        OperationName = operationName;
        Exception = exception;
        Metadata = metadata;
        CorrelationId = Guid.NewGuid();
    }
}

/// <summary>
/// Defines the severity levels for an error.
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// A critical error that requires immediate attention. The application may be in an unstable state.
    /// </summary>
    Critical,

    /// <summary>
    /// A significant error that has impacted a user or a system, but the application can continue.
    /// </summary>
    Error,

    /// <summary>
    /// A potential problem that does not prevent the current operation from completing but should be investigated.
    /// </summary>
    Warning,

    /// <summary>
    /// Informational message about a notable event that is not an error.
    /// </summary>
    Information,
}
