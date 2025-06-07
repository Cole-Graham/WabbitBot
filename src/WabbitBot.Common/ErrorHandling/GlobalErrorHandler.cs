namespace WabbitBot.Common.Events;

public interface IGlobalErrorHandler
{
    Task HandleError(Exception ex);
    Task Initialize();
}

public class GlobalErrorHandler : IGlobalErrorHandler
{
    private readonly IGlobalEventBus _eventBus;

    public GlobalErrorHandler(IGlobalEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public Task HandleError(Exception ex)
    {
        // Log the error
        Console.Error.WriteLine($"Critical error: {ex.Message}");
        Console.Error.WriteLine(ex.StackTrace);

        // Publish error event for handlers across projects
        return _eventBus.PublishAsync(new CriticalStartupErrorEvent(ex));
    }

    public Task Initialize()
    {
        // Set up error handling infrastructure
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                HandleError(ex).GetAwaiter().GetResult();
            }
        };

        // Signal that error handling is ready
        return _eventBus.PublishAsync(new GlobalErrorHandlingReadyEvent());
    }
}