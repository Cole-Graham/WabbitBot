using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Events;
using WabbitBot.Common.Attributes;

namespace WabbitBot.Core.Common.Handlers;

/// <summary>
/// Handles map-related events and coordinates map operations
/// </summary>
[GenerateEventSubscriptions(EnableMetrics = true, EnableErrorHandling = true, EnableLogging = true)]
public partial class MapHandler : CoreHandler
{
    private readonly ICoreEventBus _eventBus;

    public static MapHandler Instance { get; } = new();

    private MapHandler() : base(CoreEventBus.Instance)
    {
        _eventBus = CoreEventBus.Instance;
    }

    public override Task InitializeAsync()
    {
        // Register auto-generated event subscriptions
        RegisterEventSubscriptions();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles map save events
    /// </summary>
    public async Task HandleMapsSavedAsync(MapsSavedEvent evt)
    {
        try
        {
            // Log map save
            Console.WriteLine($"Maps saved: {evt.Maps.Count} maps");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling maps saved event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles map export events
    /// </summary>
    public async Task HandleMapsExportedAsync(MapsExportedEvent evt)
    {
        try
        {
            // Log map export
            Console.WriteLine($"Maps exported to: {evt.Path}");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling maps exported event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles map import events
    /// </summary>
    public async Task HandleMapsImportedAsync(MapsImportedEvent evt)
    {
        try
        {
            // Log map import
            Console.WriteLine($"Maps imported: {evt.Maps.Count} maps");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling maps imported event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles map addition events
    /// </summary>
    public async Task HandleMapAddedAsync(MapAddedEvent evt)
    {
        try
        {
            // Log map addition
            Console.WriteLine($"Map added: {evt.Map.Name} ({evt.Map.Size})");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling map added event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles map update events
    /// </summary>
    public async Task HandleMapUpdatedAsync(MapUpdatedEvent evt)
    {
        try
        {
            // Log map update
            Console.WriteLine($"Map updated: {evt.Map.Name} ({evt.Map.Size})");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling map updated event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles map removal events
    /// </summary>
    public async Task HandleMapRemovedAsync(MapRemovedEvent evt)
    {
        try
        {
            // Log map removal
            Console.WriteLine($"Map removed: {evt.Map.Name}");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling map removed event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles map thumbnail update events
    /// </summary>
    public async Task HandleMapThumbnailUpdatedAsync(MapThumbnailUpdatedEvent evt)
    {
        try
        {
            // Log thumbnail update
            Console.WriteLine($"Map thumbnail updated: {evt.Map.Name} -> {evt.NewFilename}");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling map thumbnail updated event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles map thumbnail removal events
    /// </summary>
    public async Task HandleMapThumbnailRemovedAsync(MapThumbnailRemovedEvent evt)
    {
        try
        {
            // Log thumbnail removal
            Console.WriteLine($"Map thumbnail removed: {evt.Map.Name}");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling map thumbnail removed event: {ex.Message}");
        }
    }
}