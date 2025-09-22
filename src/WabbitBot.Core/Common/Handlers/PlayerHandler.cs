using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Events;
using WabbitBot.Core.Common.Data.Interface;
using WabbitBot.Common.Attributes;

namespace WabbitBot.Core.Common.Handlers;

/// <summary>
/// Handles player-related events and coordinates player operations
/// </summary>
[GenerateEventSubscriptions(EnableMetrics = true, EnableErrorHandling = true, EnableLogging = true)]
public partial class PlayerHandler : CoreHandler
{
    private readonly ICoreEventBus _eventBus;
    private IPlayerRepository PlayerRepository => WabbitBot.Core.Common.Data.DataServiceManager.PlayerRepository;
    private IPlayerCache PlayerCache => WabbitBot.Core.Common.Data.DataServiceManager.PlayerCache;

    public static PlayerHandler Instance { get; } = new();

    private PlayerHandler() : base(CoreEventBus.Instance)
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
    /// Handles player archive check events
    /// </summary>
    public async Task HandlePlayerArchiveCheckAsync(PlayerArchiveCheckEvent evt)
    {
        try
        {
            // Log archive check
            Console.WriteLine($"Archive check for player: {evt.PlayerId}");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling player archive check event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles player archival events
    /// </summary>
    public async Task HandlePlayerArchivedAsync(PlayerArchivedEvent evt)
    {
        try
        {
            // Fetch the player object from cache/repository
            var player = await PlayerCache.GetAsync(evt.PlayerId) ??
                        await PlayerRepository.GetByIdAsync(evt.PlayerId);

            if (player != null)
            {
                // Log player archival
                Console.WriteLine($"Player archived: {player.Id} at {DateTime.UtcNow}");
            }
            else
            {
                Console.WriteLine($"Player archived: {evt.PlayerId} (player object not found)");
            }

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling player archived event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles player unarchival events
    /// </summary>
    public async Task HandlePlayerUnarchivedAsync(PlayerUnarchivedEvent evt)
    {
        try
        {
            // Log player unarchival
            Console.WriteLine($"Player unarchived: {evt.PlayerId} at {evt.UnarchivedAt}");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling player unarchived event: {ex.Message}");
        }
    }
}
