using System;
using System.Threading.Tasks;
using WabbitBot.DiscBot.DiscBot.Base;
using WabbitBot.DiscBot.DiscBot.Events;
using WabbitBot.Common.Events;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.DiscBot.DiscBot.ErrorHandling;

/// <summary>
/// Discord error handler implementation - provides basic error handling framework
/// </summary>
public class DiscordErrorHandler : DiscordBaseHandler
{
    private static DiscordErrorHandler? _instance;
    private static readonly object _instanceLock = new();

    public static DiscordErrorHandler Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_instanceLock)
                {
                    _instance ??= new DiscordErrorHandler();
                }
            }
            return _instance;
        }
    }

    // Private constructor for singleton
    private DiscordErrorHandler() : base(DiscordEventBus.Instance)
    {
    }

    public async Task HandleErrorAsync(Exception ex, string? context = null)
    {
        try
        {
            // Log the error
            LogError(ex, context);

            // Publish error event
            await PublishErrorEventAsync(ex, "Discord");
        }
        catch (Exception handlerEx)
        {
            // Last resort - just log to console
            Console.Error.WriteLine($"[DiscordErrorHandler] CRITICAL: Error handler failed: {handlerEx.Message}");
        }
    }

    public override Task InitializeAsync()
    {
        try
        {
            // Set up any Discord-specific error handlers
            Console.WriteLine("[DiscordErrorHandler] Initializing placeholder error handler...");

            // TODO: Set up Discord-specific error handling
            // - Handle Discord API errors
            // - Handle command execution errors
            // - Handle embed/UI errors

            Console.WriteLine("[DiscordErrorHandler] Placeholder error handler initialized");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[DiscordErrorHandler] Failed to initialize: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Logs an error with timestamp and context
    /// </summary>
    private void LogError(Exception ex, string? context = null)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var contextInfo = string.IsNullOrEmpty(context) ? "" : $" [{context}]";

        Console.Error.WriteLine($"[DiscordErrorHandler]{contextInfo} {timestamp} - {ex.GetType().Name}: {ex.Message}");

        if (ex.InnerException != null)
        {
            Console.Error.WriteLine($"  Inner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        }
    }

    /// <summary>
    /// Publishes an error event to the global event bus
    /// </summary>
    private async Task PublishErrorEventAsync(Exception ex, string component = "Discord")
    {
        if (EventBus != null)
        {
            try
            {
                await EventBus.PublishAsync(new CriticalStartupErrorEvent(ex, component));
            }
            catch (Exception publishEx)
            {
                Console.Error.WriteLine($"[DiscordErrorHandler] Failed to publish error event: {publishEx.Message}");
            }
        }
    }

    // Convenience method for synchronous error handling
    public async Task HandleError(Exception ex)
    {
        await HandleErrorAsync(ex);
    }

    // Convenience method for synchronous initialization
    public async Task Initialize()
    {
        await InitializeAsync();
    }
}

/// <summary>
/// Minimal implementation of IGlobalEventBus for error handling fallback
/// </summary>
internal class MinimalGlobalEventBus : IGlobalEventBus
{
    public Task PublishAsync<TEvent>(TEvent @event) where TEvent : class
    {
        // Just log that we can't publish - this is a fallback scenario
        Console.WriteLine($"[MinimalGlobalEventBus] Cannot publish {typeof(TEvent).Name} - GlobalEventBus not available");
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
}
