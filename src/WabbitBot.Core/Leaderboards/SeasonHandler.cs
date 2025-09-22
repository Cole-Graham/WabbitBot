using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Core.Leaderboards.Data.Interface;
using WabbitBot.Common.Attributes;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Data;
using WabbitBot.Core.Scrimmages.ScrimmageRating;

namespace WabbitBot.Core.Leaderboards;

/// <summary>
/// Handles season-related events and coordinates season operations
/// </summary>
[GenerateEventSubscriptions(EnableMetrics = true, EnableErrorHandling = true, EnableLogging = true)]
public partial class SeasonHandler : CoreHandler
{
    private readonly ICoreEventBus _eventBus;
    private ISeasonRepository SeasonRepository => WabbitBot.Core.Common.Data.DataServiceManager.SeasonRepository;
    private ISeasonCache SeasonCache => WabbitBot.Core.Common.Data.DataServiceManager.SeasonCache;

    public static SeasonHandler Instance { get; } = new();

    private SeasonHandler() : base(CoreEventBus.Instance)
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
    /// Handles season archived events
    /// </summary>
    public async Task HandleSeasonArchivedAsync(SeasonArchivedEvent evt)
    {
        try
        {
            Console.WriteLine($"Season archived: {evt.SeasonId}");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling season archived event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles season creation events
    /// </summary>
    public async Task HandleSeasonCreatedAsync(SeasonCreatedEvent evt)
    {
        try
        {
            Console.WriteLine($"Season created: {evt.SeasonId}");

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling season created event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles season ended events
    /// </summary>
    public async Task HandleSeasonEndedAsync(SeasonEndedEvent evt)
    {
        try
        {
            // Fetch the season object
            var season = await SeasonCache.GetAsync(evt.SeasonId) ??
                        await SeasonRepository.GetByIdAsync(evt.SeasonId);

            if (season != null)
            {
                Console.WriteLine($"Season ended: {season.Id} at {season.EndDate}");
            }
            else
            {
                Console.WriteLine($"Season ended: {evt.SeasonId} (season object not found)");
            }

            // Forward event to Global Event Bus for Discord integration
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling season ended event: {ex.Message}");
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
            // Convert string back to EvenTeamFormat enum
            if (!Enum.TryParse<EvenTeamFormat>(evt.EvenTeamFormat, out var evenTeamFormat))
            {
                Console.WriteLine($"Invalid EvenTeamFormat string: {evt.EvenTeamFormat}");
                return;
            }

            Console.WriteLine($"Team result added - Season: {evt.SeasonId}, Team: {evt.TeamId}, EvenTeamFormat: {evenTeamFormat}, RatingChange: {evt.RatingChange}, Win: {evt.IsWin}");

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
            var season = await SeasonCache.GetAsync(evt.SeasonId) ??
                        await SeasonRepository.GetByIdAsync(evt.SeasonId);

            if (season != null)
            {
                // Perform validation checks
                if (season.StartDate >= season.EndDate)
                {
                    evt.IsValid = false;
                    evt.ValidationMessage = "Season start date must be before end date";
                }
                else if (season.StartDate < DateTime.UtcNow.Date && season.IsActive)
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
            Console.WriteLine($"Error handling season validation event: {ex.Message}");
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
            // Convert string back to EvenTeamFormat enum
            if (!Enum.TryParse<EvenTeamFormat>(evt.EvenTeamFormat, out var evenTeamFormat))
            {
                Console.WriteLine($"Invalid EvenTeamFormat string in TeamRatingUpdatedEvent: {evt.EvenTeamFormat}");
                return;
            }

            Console.WriteLine($"Team rating updated: {evt.TeamId} ({evenTeamFormat}) - forwarding to LeaderboardHandler");

            // Forward event to Global Event Bus for Discord integration
            // LeaderboardHandler will handle the actual leaderboard refresh
            await _eventBus.PublishAsync(evt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling team rating updated event: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles team rating change events from cross-vertical slice communication
    /// </summary>
    public async Task HandleApplyTeamRatingChangeAsync(ApplyTeamRatingChangeEvent evt)
    {
        try
        {
            // Get the active season for this game size
            var activeSeason = await GetActiveSeasonAsync(evt.EvenTeamFormat);
            if (activeSeason == null)
            {
                Console.WriteLine($"No active season found for {evt.EvenTeamFormat} - cannot apply rating change");
                return;
            }

            // Find the team in the season
            if (!activeSeason.ParticipatingTeams.ContainsKey(evt.TeamId))
            {
                Console.WriteLine($"Team {evt.TeamId} not found in active season - cannot apply rating change");
                return;
            }

            // Load the actual team from the database
            var team = await WabbitBot.Core.Common.Data.DataServiceManager.TeamRepository.GetByIdAsync(evt.TeamId);
            if (team == null)
            {
                Console.WriteLine($"Team {evt.TeamId} not found in database - cannot apply rating change");
                return;
            }

            // Ensure the team has stats for this game size
            if (!team.Stats.ContainsKey(evt.EvenTeamFormat))
            {
                team.Stats[evt.EvenTeamFormat] = new Stats
                {
                    TeamId = evt.TeamId,
                    EvenTeamFormat = evt.EvenTeamFormat,
                    InitialRating = Leaderboard.InitialRating,
                    CurrentRating = Leaderboard.InitialRating,
                    HighestRating = Leaderboard.InitialRating,
                    LastUpdated = DateTime.UtcNow
                };
            }

            // Apply the rating change
            var stats = team.Stats[evt.EvenTeamFormat];
            var oldRating = stats.CurrentRating;
            stats.CurrentRating += evt.RatingChange;
            stats.LastUpdated = DateTime.UtcNow;

            // Save the updated team
            await WabbitBot.Core.Common.Data.DataServiceManager.TeamRepository.UpdateAsync(team);

            // Update the season cache (ParticipatingTeams references don't change)
            await SeasonCache.SetActiveSeasonAsync(activeSeason);

            // Publish TeamRatingUpdatedEvent to trigger leaderboard refresh
            await _eventBus.PublishAsync(new TeamRatingUpdatedEvent(evt.TeamId, evt.EvenTeamFormat.ToString()));

            Console.WriteLine($"Applied rating change to team {evt.TeamId}: {oldRating} -> {stats.CurrentRating} ({evt.RatingChange}) - {evt.Reason}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling apply team rating change event: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the active season for the specified game size.
    /// </summary>
    private async Task<Season?> GetActiveSeasonAsync(EvenTeamFormat evenTeamFormat)
    {
        // Try cache first
        var cachedSeason = await SeasonCache.GetActiveSeasonAsync(evenTeamFormat);
        if (cachedSeason != null)
        {
            return cachedSeason;
        }

        // Try database
        var dbSeason = await SeasonRepository.GetActiveSeasonAsync(evenTeamFormat);
        if (dbSeason != null)
        {
            await SeasonCache.SetActiveSeasonAsync(dbSeason);
            return dbSeason;
        }

        // No active season found
        return null;
    }

    #endregion
}
