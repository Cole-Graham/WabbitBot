namespace WabbitBot.Common.ErrorService;

/// <summary>
/// Specifies the component of the error handling system to be invoked.
/// </summary>
public enum ErrorComponent
{
    /// <summary>
    /// Handles logging of the error.
    /// </summary>
    Logging,

    /// <summary>
    /// Handles notifying relevant parties about the error.
    /// </summary>
    Notification,

    /// <summary>
    /// Handles telemetry and metrics for the error.
    /// </summary>
    Telemetry,

    /// <summary>
    /// Handles attempting to recover from the error.
    /// </summary>
    Recovery,

    /// <summary>
    /// Handles auditing of the error for security or compliance.
    /// </summary>
    Audit,
}
