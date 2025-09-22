using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Events;
using WabbitBot.Core.Common.Data.Interface;
using WabbitBot.Common.Attributes;

namespace WabbitBot.Core.Common.Handlers;

/// <summary>
/// Handles team-related events and coordinates team operations
/// </summary>
[GenerateEventSubscriptions(EnableMetrics = true, EnableErrorHandling = true, EnableLogging = true)]
public partial class TeamHandler : CoreHandler
{
    private readonly ICoreEventBus _eventBus;
    private ITeamRepository TeamRepository => WabbitBot.Core.Common.Data.DataServiceManager.TeamRepository;
    private ITeamCache TeamCache => WabbitBot.Core.Common.Data.DataServiceManager.TeamCache;

    public static TeamHandler Instance { get; } = new();

    private TeamHandler() : base(CoreEventBus.Instance)
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
    /// Handles team creation events
    /// </summary>
    public async Task HandleTeamCreatedAsync(TeamCreatedEvent evt)
    {
        try
        {
            // Fetch the team object from cache/repository
            var team = await TeamCache.GetAsync(evt.TeamId) ??
                      await TeamRepository.GetByIdAsync(evt.TeamId);

            if (team != null)
            {
                // Log team creation
                Console.WriteLine($"Team created: {team.Name} (ID: {team.Id})");
            }
            else
            {
                Console.WriteLine($"Team created: {evt.TeamId} (team object not found)");
            }

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling team created event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles team update events
    /// </summary>
    public async Task HandleTeamUpdatedAsync(TeamUpdatedEvent evt)
    {
        try
        {
            // Fetch the team object from cache/repository
            var team = await TeamCache.GetAsync(evt.TeamId) ??
                      await TeamRepository.GetByIdAsync(evt.TeamId);

            if (team != null)
            {
                // Log team update
                Console.WriteLine($"Team updated: {team.Name} (ID: {team.Id})");
            }
            else
            {
                Console.WriteLine($"Team updated: {evt.TeamId} (team object not found)");
            }

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling team updated event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles team archival events
    /// </summary>
    public async Task HandleTeamArchivedAsync(TeamArchivedEvent evt)
    {
        try
        {
            // Fetch the team object from cache/repository
            var team = await TeamCache.GetAsync(evt.TeamId) ??
                      await TeamRepository.GetByIdAsync(evt.TeamId);

            if (team != null)
            {
                // Log team archival
                Console.WriteLine($"Team archived: {team.Name} (ID: {team.Id})");
            }
            else
            {
                Console.WriteLine($"Team archived: {evt.TeamId} (team object not found)");
            }

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling team archived event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles team member addition events
    /// </summary>
    public async Task HandleTeamMemberAddedAsync(TeamMemberAddedEvent evt)
    {
        try
        {
            // Fetch the team object from cache/repository
            var team = await TeamCache.GetAsync(evt.TeamId) ??
                      await TeamRepository.GetByIdAsync(evt.TeamId);

            if (team != null)
            {
                // Log member addition
                Console.WriteLine($"Member added to team {team.Name}: {evt.PlayerId} as {evt.Role}");
            }
            else
            {
                Console.WriteLine($"Member added to team {evt.TeamId}: {evt.PlayerId} as {evt.Role} (team object not found)");
            }

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling team member added event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles team member removal events
    /// </summary>
    public async Task HandleTeamMemberRemovedAsync(TeamMemberRemovedEvent evt)
    {
        try
        {
            // Fetch the team object from cache/repository
            var team = await TeamCache.GetAsync(evt.TeamId) ??
                      await TeamRepository.GetByIdAsync(evt.TeamId);

            if (team != null)
            {
                // Log member removal
                Console.WriteLine($"Member removed from team {team.Name}: {evt.PlayerId}");
            }
            else
            {
                Console.WriteLine($"Member removed from team {evt.TeamId}: {evt.PlayerId} (team object not found)");
            }

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling team member removed event: {ex.Message}");
        }
    }
}
