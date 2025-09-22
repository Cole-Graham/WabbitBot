using WabbitBot.Common.Events;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.ErrorHandling;

namespace WabbitBot.Core.Common.BotCore;

/// <summary>
/// Core error handler implementation - provides basic error handling framework
/// </summary>
public class CoreErrorHandler : CoreHandler, ICoreErrorHandler
{
    private static CoreErrorHandler? _instance;
    private static readonly object _instanceLock = new();

    public static CoreErrorHandler Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_instanceLock)
                {
                    _instance ??= new CoreErrorHandler();
                }
            }
            return _instance;
        }
    }


    private readonly IGlobalEventBus? _globalEventBus;

    // Private constructor for singleton
    private CoreErrorHandler() : base(GetCoreEventBus())
    {
        _globalEventBus = GetGlobalEventBus();
    }

    private static ICoreEventBus GetCoreEventBus()
    {
        try
        {
            return BotCore.CoreEventBus.Instance;
        }
        catch
        {
            // If CoreEventBus is not available, create a minimal implementation
            // This should rarely happen, but provides a fallback
            return new MinimalCoreEventBus();
        }
    }

    private static IGlobalEventBus? GetGlobalEventBus()
    {
        try
        {
            return GlobalEventBusProvider.GetGlobalEventBus();
        }
        catch
        {
            // Event bus not ready yet - that's okay for placeholder
            return null;
        }
    }

    // Public constructor for dependency injection (if needed later)
    public CoreErrorHandler(ICoreEventBus coreEventBus, IGlobalEventBus? globalEventBus = null) : base(coreEventBus)
    {
        _globalEventBus = globalEventBus;
    }

    public async Task HandleErrorAsync(Exception ex, string? context = null)
    {
        try
        {
            // Log the error
            LogError(ex, context);

            // Publish error event to core event bus
            await PublishCoreErrorEventAsync(ex, "Core");
        }
        catch (Exception handlerEx)
        {
            // Last resort - just log to console
            Console.Error.WriteLine($"[CoreErrorHandler] CRITICAL: Error handler failed: {handlerEx.Message}");
        }
    }

    public override async Task InitializeAsync()
    {
        try
        {
            // Set up any core-specific error handlers
            Console.WriteLine("[CoreErrorHandler] Initializing placeholder error handler...");

            // Signal that core error handling is ready (if event bus is available)
            if (EventBus != null)
            {
                try
                {
                    await EventBus.PublishAsync(new CoreErrorHandlingReadyEvent());
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[CoreErrorHandler] Failed to publish ready event: {ex.Message}");
                }
            }

            Console.WriteLine("[CoreErrorHandler] Placeholder error handler initialized");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[CoreErrorHandler] Failed to initialize: {ex.Message}");
        }
    }

    /// <summary>
    /// Logs an error with timestamp and context
    /// </summary>
    private void LogError(Exception ex, string? context = null)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var contextInfo = string.IsNullOrEmpty(context) ? "" : $" [{context}]";

        Console.Error.WriteLine($"[CoreErrorHandler]{contextInfo} {timestamp} - {ex.GetType().Name}: {ex.Message}");

        if (ex.InnerException != null)
        {
            Console.Error.WriteLine($"  Inner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        }
    }

    /// <summary>
    /// Publishes an error event to the core event bus
    /// </summary>
    private async Task PublishCoreErrorEventAsync(Exception ex, string component = "Core")
    {
        if (EventBus != null)
        {
            try
            {
                await EventBus.PublishAsync(new CoreStartupFailedEvent(ex, component));
            }
            catch (Exception publishEx)
            {
                Console.Error.WriteLine($"[CoreErrorHandler] Failed to publish core error event: {publishEx.Message}");
            }
        }
    }

    /// <summary>
    /// Publishes an error event to the global event bus
    /// </summary>
    private async Task PublishGlobalErrorEventAsync(Exception ex, string component = "Core")
    {
        if (_globalEventBus != null)
        {
            try
            {
                await _globalEventBus.PublishAsync(new CriticalStartupErrorEvent(ex, component));
            }
            catch (Exception publishEx)
            {
                Console.Error.WriteLine($"[CoreErrorHandler] Failed to publish global error event: {publishEx.Message}");
            }
        }
    }

}

/// <summary>
/// Minimal implementation of ICoreEventBus for error handling fallback
/// </summary>
internal class MinimalCoreEventBus : ICoreEventBus
{
    public Task PublishAsync<TEvent>(TEvent @event) where TEvent : class
    {
        // Just log that we can't publish - this is a fallback scenario
        Console.WriteLine($"[MinimalCoreEventBus] Cannot publish {typeof(TEvent).Name} - CoreEventBus not available");
        return Task.CompletedTask;
    }

    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
    {
        // No-op for minimal implementation
    }

    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
    {
        // No-op for minimal implementation
    }

    public Task<TResponse?> RequestAsync<TRequest, TResponse>(TRequest request) where TRequest : class where TResponse : class
    {
        return Task.FromResult<TResponse?>(null);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }
}