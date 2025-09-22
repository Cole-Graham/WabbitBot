using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Events;
using WabbitBot.Core.Common.Services;
using WabbitBot.Common.Attributes;

namespace WabbitBot.Core.Common.Handlers;

/// <summary>
/// Handles game-related events and coordinates game operations
/// </summary>
[GenerateEventSubscriptions(EnableMetrics = true, EnableErrorHandling = true, EnableLogging = true)]
public partial class GameHandler : CoreHandler
{
    private readonly ICoreEventBus _eventBus;

    public static GameHandler Instance { get; } = new();

    private GameHandler() : base(CoreEventBus.Instance)
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
    /// Handles game creation events
    /// </summary>
    public async Task HandleGameCreatedAsync(GameCreatedEvent evt)
    {
        try
        {
            // Log game creation
            Console.WriteLine($"Game created: {evt.GameId}");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling game created event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles game update events
    /// </summary>
    public async Task HandleGameUpdatedAsync(GameUpdatedEvent evt)
    {
        try
        {
            // Log game update
            Console.WriteLine($"Game updated: {evt.GameId}");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling game updated event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles game archival events
    /// </summary>
    public async Task HandleGameArchivedAsync(GameArchivedEvent evt)
    {
        try
        {
            // Log game archival
            Console.WriteLine($"Game archived: {evt.GameId}");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling game archived event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles game deletion events
    /// </summary>
    public async Task HandleGameDeletedAsync(GameDeletedEvent evt)
    {
        try
        {
            // Log game deletion
            Console.WriteLine($"Game deleted: {evt.GameId}");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling game deleted event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles game completion events
    /// </summary>
    public async Task HandleGameCompletedAsync(GameCompletedEvent evt)
    {
        try
        {
            // Log game completion
            Console.WriteLine($"Game completed: {evt.GameId} - Winner: {evt.WinnerId}");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling game completed event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles game creation requests from other services (e.g., MatchService)
    /// </summary>
    public async Task HandleGameCreationRequestedAsync(GameCreationRequestedEvent evt)
    {
        try
        {
            // Create a GameService instance to handle the business logic
            var gameService = new GameService();

            // Create the game using the existing business logic
            var result = await gameService.CreateGameAsync(
                evt.MatchId,
                evt.MapId,
                evt.EvenTeamFormat,
                evt.Team1PlayerIds,
                evt.Team2PlayerIds,
                evt.GameNumber
            );

            if (result.Success && result.Data is not null)
            {
                // Game creation was successful, the CreateGameAsync method already publishes GameCreatedEvent
                // No additional action needed here
            }
            else
            {
                // Log the failure - the error handling is already done in CreateGameAsync
                Console.WriteLine($"Failed to create game: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling game creation request: {ex.Message}");
        }
    }
}
