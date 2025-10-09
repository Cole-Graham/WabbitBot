using System.Threading.Tasks;

namespace WabbitBot.Common.ErrorService;

/// <summary>
/// Provides a unified service for handling errors across the application.
/// This class is implemented using partial methods to separate concerns.
/// </summary>
public partial class ErrorService : IErrorService
{
    // In a real implementation, you would inject dependencies like loggers,
    // notification services, etc., here.
    // For now, we will keep it simple.

    public ErrorService()
    {
        // Initialize dependencies here.
    }

    /// <inheritdoc/>
    public async Task HandleAsync(ErrorContext context, ErrorComponent component)
    {
        switch (component)
        {
            case ErrorComponent.Logging:
                await LogAsync(context);
                break;
            case ErrorComponent.Notification:
                await NotifyAsync(context);
                break;
            case ErrorComponent.Recovery:
                await RecoverAsync(context);
                break;
            case ErrorComponent.Telemetry:
            case ErrorComponent.Audit:
                // Not implemented in this example, but would be handled here.
                await Task.CompletedTask;
                break;
            default:
                throw new ArgumentException($"Unsupported error component: {component}", nameof(component));
        }
    }

    /// <inheritdoc/>
    public async Task CaptureAsync(
        Exception exception,
        string message,
        string operationName,
        ErrorSeverity severity = ErrorSeverity.Error
    )
    {
        var context = new ErrorContext(message, severity, operationName, exception);

        // By default, captured exceptions are logged.
        // More complex logic could decide which components to invoke based on severity, etc.
        await HandleAsync(context, ErrorComponent.Logging);
    }

    // Partial method declarations. The implementations will be in the partial class files.
    private partial Task LogAsync(ErrorContext context);

    private partial Task NotifyAsync(ErrorContext context);

    private partial Task RecoverAsync(ErrorContext context);
}
