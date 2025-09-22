using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Events;
using WabbitBot.Core.Common.Data.Interface;
using WabbitBot.Common.Attributes;

namespace WabbitBot.Core.Common.Handlers;

/// <summary>
/// Handles user-related events and coordinates user operations
/// </summary>
[GenerateEventSubscriptions(EnableMetrics = true, EnableErrorHandling = true, EnableLogging = true)]
public partial class UserHandler : CoreHandler
{
    private readonly ICoreEventBus _eventBus;
    private IUserRepository UserRepository => WabbitBot.Core.Common.Data.DataServiceManager.UserRepository;
    private IUserCache UserCache => WabbitBot.Core.Common.Data.DataServiceManager.UserCache;

    public static UserHandler Instance { get; } = new();

    private UserHandler() : base(CoreEventBus.Instance)
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
    /// Handles user creation events
    /// </summary>
    public async Task HandleUserCreatedAsync(UserCreatedEvent evt)
    {
        try
        {
            // Fetch the user object from cache/repository
            var user = await UserCache.GetAsync(evt.UserId) ??
                      await UserRepository.GetByIdAsync(evt.UserId);

            if (user != null)
            {
                // Log user creation
                Console.WriteLine($"User created: {user.Id} ({user.Username})");
            }
            else
            {
                Console.WriteLine($"User created: {evt.UserId} (user object not found)");
            }

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling user created event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles user update events
    /// </summary>
    public async Task HandleUserUpdatedAsync(UserUpdatedEvent evt)
    {
        try
        {
            // Fetch the user object from cache/repository
            var user = await UserCache.GetAsync(evt.UserId) ??
                      await UserRepository.GetByIdAsync(evt.UserId);

            if (user != null)
            {
                // Log user update
                Console.WriteLine($"User updated: {user.Id} ({user.Username})");
            }
            else
            {
                Console.WriteLine($"User updated: {evt.UserId} (user object not found)");
            }

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling user updated event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles user archival events
    /// </summary>
    public async Task HandleUserArchivedAsync(UserArchivedEvent evt)
    {
        try
        {
            // Fetch the user object from cache/repository
            var user = await UserCache.GetAsync(evt.UserId) ??
                      await UserRepository.GetByIdAsync(evt.UserId);

            if (user != null)
            {
                // Log user archival
                Console.WriteLine($"User archived: {user.Id} ({user.Username})");
            }
            else
            {
                Console.WriteLine($"User archived: {evt.UserId} (user object not found)");
            }

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling user archived event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles user deletion events
    /// </summary>
    public async Task HandleUserDeletedAsync(UserDeletedEvent evt)
    {
        try
        {
            // For deleted users, we might not be able to fetch from cache/repository
            // Log user deletion with just the ID
            Console.WriteLine($"User deleted: {evt.UserId}");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling user deleted event: {ex.Message}");
        }
    }
}
