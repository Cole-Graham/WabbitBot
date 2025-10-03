using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Services.DiscBot;

namespace WabbitBot.DiscBot.App;

/// <summary>
/// This app is library-agnostic and communicates only via events.
/// </summary>
[EventGenerator(GenerateSubscribers = true, DefaultBus = EventBusType.DiscBot, TriggerMode = "OptIn")]
public partial class MatchApp : IMatchApp
{
    /// <summary>
    /// Initializes the app by subscribing to relevant events.
    /// This will be replaced by generated code once EventGenerator is implemented.
    /// </summary>
    public void Initialize()
    {
        DiscBotService.EventBus.Subscribe<MatchThreadCreated>(OnMatchThreadCreatedAsync);
    }

    /// <summary>
    /// Handles match provisioning requests from Core.
    /// Publishes DiscBot-local events to create threads and containers.
    /// NOTE: This will be auto-subscribed to MatchProvisioningRequested (Global) once EventGenerator is implemented in step 6.
    /// The MatchProvisioningRequested event is owned by Core and will be generated/copied to DiscBot by source generators.
    /// </summary>
    public async Task HandleMatchProvisioningRequestedAsync(Guid matchId, Guid scrimmageId)
    {
        // App layer requests thread creation; Renderer will perform the actual Discord API call
        await DiscBotService.PublishAsync(new MatchThreadCreateRequested(matchId, scrimmageId));
    }

    /// <summary>
    /// Handles thread creation confirmation to request container creation.
    /// </summary>
    public async Task OnMatchThreadCreatedAsync(MatchThreadCreated evt)
    {
        // Request match container creation now that thread exists
        await DiscBotService.PublishAsync(new MatchContainerRequested(evt.MatchId, evt.ThreadId));
    }

    #region Map Ban
    public async Task StartMapBanDMsAsync(Guid matchId, ulong team1DiscordId, ulong team2DiscordId)
    {
        await DiscBotService.PublishAsync(new MapBanDmStartRequested(matchId, team1DiscordId));
        await DiscBotService.PublishAsync(new MapBanDmStartRequested(matchId, team2DiscordId));
    }

    public async Task OnTeamMapBanSelectedAsync(Guid matchId, ulong teamId, string[] selections)
    {
        await DiscBotService.PublishAsync(new MapBanDmUpdateRequested(matchId, teamId, selections));
    }

    public async Task OnTeamMapBanConfirmedAsync(Guid matchId, ulong teamId, string[] selections)
    {
        await DiscBotService.PublishAsync(new MapBanDmConfirmRequested(matchId, teamId, selections));
    }

    /// <summary>
    /// Handles player map ban selection (provisional).
    /// Updates the DM preview with the selected bans.
    /// </summary>
    public async Task OnPlayerMapBanSelectedAsync(Guid matchId, ulong playerId, string[] selections)
    {
        // Publish event to update DM preview; Renderer will update the Discord message
        await DiscBotService.PublishAsync(new MapBanDmUpdateRequested(matchId, playerId, selections));
    }

    /// <summary>
    /// Handles player map ban confirmation (final).
    /// Locks the DM UI and prepares for next phase.
    /// </summary>
    public async Task OnPlayerMapBanConfirmedAsync(Guid matchId, ulong playerId, string[] selections)
    {
        // Publish event to lock DM UI; Renderer will disable components
        await DiscBotService.PublishAsync(new MapBanDmConfirmRequested(matchId, playerId, selections));

        // TODO: Check if both players have confirmed, then trigger next phase (deck submission)
        // This will be coordinated via a flow orchestrator or match state tracker
    }
    #endregion
}

