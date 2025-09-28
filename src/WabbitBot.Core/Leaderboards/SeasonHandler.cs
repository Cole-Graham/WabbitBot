using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.ErrorService;
using WabbitBot.Common.Models;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Configuration;

namespace WabbitBot.Core.Leaderboards;

/// <summary>
/// Handles season-related events and coordinates season operations
/// </summary>
public partial class SeasonHandler : CoreHandler
{
    private readonly ICoreEventBus _eventBus;
    private readonly DatabaseService<Season> _seasonData;
    private readonly DatabaseService<Team> _teamData;
    private readonly IErrorService _errorService;

    public static SeasonHandler Instance { get; } = new(CoreEventBus.Instance, new WabbitBot.Common.ErrorService.ErrorService()); // TODO: Inject properly

    public SeasonHandler(ICoreEventBus eventBus, IErrorService errorService) : base(eventBus)
    {
        _eventBus = eventBus;
        _errorService = errorService;
        _seasonData = new DatabaseService<Season>();
        _teamData = new DatabaseService<Team>();
    }

    public override Task InitializeAsync()
    {
        // Register auto-generated event subscriptions
        // RegisterEventSubscriptions();
        return Task.CompletedTask;
    }

    // NOTE: The handlers for SeasonCreatedEvent and SeasonArchivedEvent have been removed
    // as they were CRUD events. The business logic now directly calls the database
    // and then publishes a more meaningful business logic event if needed.

    /// <summary>
    /// Handles season ended events
    /// </summary>
    public async Task HandleSeasonEndedAsync(SeasonEndedEvent evt)
    {
        try
        {
            // This handler might be used to kick off post-season logic,
            // like calculating final rankings or archiving data.
            // The event itself is the business signal; we avoid database lookups here.
            Console.WriteLine($"Season ended: {evt.SeasonId}. Triggering post-season processing.");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            await _errorService.CaptureAsync(ex, "Error handling season ended event", nameof(HandleSeasonEndedAsync));
        }
    }

    /// <summary>
    /// Handles season rating decay applied events
    /// </summary>
    public async Task HandleSeasonRatingDecayAppliedAsync(SeasonRatingDecayAppliedEvent evt)
    {
        try
        {
            Console.WriteLine($"Rating decay applied to season: {evt.SeasonId}");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling season rating decay event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles team registration for season events
    /// </summary>
    public async Task HandleTeamRegisteredForSeasonAsync(TeamRegisteredForSeasonEvent evt)
    {
        try
        {
            Console.WriteLine($"Team {evt.TeamId} registered for season {evt.SeasonId}");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling team registered for season event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles team result added events
    /// </summary>
    public async Task HandleTeamResultAddedAsync(TeamResultAddedEvent evt)
    {
        try
        {
            // Convert string back to TeamSize enum
            if (!Enum.TryParse<TeamSize>(evt.TeamSize, out var TeamSize))
            {
                Console.WriteLine($"Invalid TeamSize string: {evt.TeamSize}");
                return;
            }

            Console.WriteLine($"Team result added - Season: {evt.SeasonId}, Team: {evt.TeamId}, TeamSize: {TeamSize}, RatingChange: {evt.RatingChange}, Win: {evt.IsWin}");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling team result added event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles season validation events
    /// </summary>
    public async Task HandleSeasonValidationAsync(SeasonValidationEvent evt)
    {
        try
        {
            // Fetch the season object for validation
            var season = await _seasonData.GetByIdAsync(evt.SeasonId, DatabaseComponent.Repository);

            if (season != null)
            {
                // Perform validation checks
                if (season.Data!.StartDate >= season.Data!.EndDate)
                {
                    evt.IsValid = false;
                    evt.ValidationMessage = "Season start date must be before end date";
                }
                else if (season.Data!.StartDate < DateTime.UtcNow.Date && season.Data!.IsActive)
                {
                    evt.IsValid = false;
                    evt.ValidationMessage = "Active season cannot start in the past";
                }
                else
                {
                    evt.IsValid = true;
                    evt.ValidationMessage = "Season is valid";
                }

                Console.WriteLine($"Season validation: {evt.SeasonId} - {evt.ValidationMessage}");
            }
            else
            {
                evt.IsValid = false;
                evt.ValidationMessage = "Season not found";
            }

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            evt.IsValid = false;
            evt.ValidationMessage = $"Validation error: {ex.Message}";
            await _errorService.CaptureAsync(ex, "Error handling season validation event", nameof(HandleSeasonValidationAsync));
        }
    }

    /// <summary>
    /// Handles season teams updated events
    /// </summary>
    public async Task HandleSeasonTeamsUpdatedAsync(SeasonTeamsUpdatedEvent evt)
    {
        try
        {
            Console.WriteLine($"Season teams updated: {evt.SeasonId} - {evt.ParticipatingTeamIds.Count} teams");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling season teams updated event: {ex.Message}");
        }
    }

    #region SeasonRatingService Handlers

    /// <summary>
    /// Handles team rating updated events from SeasonRatingService
    /// </summary>
    public async Task HandleTeamRatingUpdatedAsync(TeamRatingUpdatedEvent evt)
    {
        try
        {
            Console.WriteLine($"Team rating updated: {evt.TeamId} ({evt.TeamSize}) - forwarding to LeaderboardHandler");

            // Forward event to Global Event Bus for Discord integration
            // LeaderboardHandler will handle the actual leaderboard refresh
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            await _errorService.CaptureAsync(ex, "Error handling team rating updated event", nameof(HandleTeamRatingUpdatedAsync));
        }
    }

    /// <summary>
    /// Handles team rating change events from cross-vertical slice communication
    /// </summary>
    public async Task HandleApplyTeamRatingChangeAsync(ApplyTeamRatingChangeEvent evt)
    {
        try
        {
            // TODO: Event Handlers should not perform complex queries like this.
            // This logic should be in a Core class and triggered by a different event,
            // or the active season ID should be included in the event payload.
            var activeSeason = (await _seasonData.GetAllAsync(DatabaseComponent.Repository))
                .Data!.FirstOrDefault(s => s.IsActive && s.TeamSize == evt.TeamSize);

            if (activeSeason == null)
            {
                Console.WriteLine($"No active season found for {evt.TeamSize} - cannot apply rating change");
                return;
            }

            // Find the team in the season
            if (!activeSeason.ParticipatingTeams.ContainsKey(evt.TeamId.ToString()))
            {
                Console.WriteLine($"Team {evt.TeamId} not found in active season - cannot apply rating change");
                return;
            }

            // Load the actual team from the database
            var team = await _teamData.GetByIdAsync(evt.TeamId, DatabaseComponent.Repository);
            if (team == null)
            {
                Console.WriteLine($"Team {evt.TeamId} not found in database - cannot apply rating change");
                return;
            }

            // Ensure the team has stats for this game size
            if (!team.Data.Stats.ContainsKey(evt.TeamSize))
            {
                var initialRating = ConfigurationProvider.GetSection<ScrimmageOptions>(
                ScrimmageOptions.SectionName).InitialRating;
                
                team.Data.Stats[evt.TeamSize] = new Stats
                {
                    TeamId = evt.TeamId,
                    TeamSize = evt.TeamSize,
                    InitialRating = initialRating,
                    CurrentRating = initialRating,
                    HighestRating = initialRating,
                    LastUpdated = DateTime.UtcNow
                };
            }

            // Apply the rating change
            var stats = team.Data.Stats[evt.TeamSize];
            var oldRating = stats.CurrentRating;
            stats.CurrentRating += evt.RatingChange;
            stats.LastUpdated = DateTime.UtcNow;

            // Save the updated team
            await _teamData.UpdateAsync(team.Data, DatabaseComponent.Repository);

            // Publish TeamRatingUpdatedEvent to trigger leaderboard refresh
            await _eventBus.PublishAsync(new TeamRatingUpdatedEvent(evt.TeamId, evt.TeamSize.ToString()));

            Console.WriteLine($"Applied rating change to team {evt.TeamId}: {oldRating} -> {stats.CurrentRating} ({evt.RatingChange}) - {evt.Reason}");
        }
        catch (Exception ex)
        {
            await _errorService.CaptureAsync(ex, "Error handling apply team rating change event", nameof(HandleApplyTeamRatingChangeAsync));
        }
    }

    #endregion
}
