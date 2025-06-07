using WabbitBot.Common.Events;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Common.BotCore;

public interface ICoreErrorHandler
{
    Task HandleError(Exception ex);
    Task Initialize();
}

public class CoreErrorHandler : ICoreErrorHandler
{
    private readonly ICoreEventBus _coreEventBus;
    private readonly IGlobalEventBus _globalEventBus;

    public CoreErrorHandler(ICoreEventBus coreEventBus, IGlobalEventBus globalEventBus)
    {
        _coreEventBus = coreEventBus;
        _globalEventBus = globalEventBus;
    }

    public Task HandleError(Exception ex)
    {
        // Log the error at core level
        Console.Error.WriteLine($"Core error: {ex.Message}");

        // Publish error event for core handlers
        return _coreEventBus.PublishAsync(new CoreStartupFailedEvent(ex));
        // The CoreEventBus will forward this to GlobalEventBus as configured
    }

    public Task Initialize()
    {
        // Set up any core-specific error handlers

        // Signal that core error handling is ready
        return _coreEventBus.PublishAsync(new CoreErrorHandlingReadyEvent());
    }
}