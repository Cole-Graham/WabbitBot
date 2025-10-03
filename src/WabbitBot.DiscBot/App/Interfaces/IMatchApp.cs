namespace WabbitBot.DiscBot.App;

/// <summary>
/// Interface for match provisioning app operations.
/// Handles creation of Discord threads and containers for matches.
/// </summary>
public interface IMatchApp : IDiscBotApp
{
    /// <summary>
    /// Handles match provisioning requests from Core.
    /// Publishes DiscBot-local events to create threads and containers.
    /// </summary>
    Task HandleMatchProvisioningRequestedAsync(Guid matchId, Guid scrimmageId);

    #region Map Ban
    /// <summary>
    /// Starts the map ban DM flow for teams in a match.
    /// </summary>
    Task StartMapBanDMsAsync(Guid matchId, ulong team1DiscordId, ulong team2DiscordId);

    /// <summary>
    /// Handles team map ban selection (provisional).
    /// </summary>
    Task OnTeamMapBanSelectedAsync(Guid matchId, ulong teamId, string[] selections);

    /// <summary>
    /// Handles team map ban confirmation (final).
    /// </summary>
    Task OnTeamMapBanConfirmedAsync(Guid matchId, ulong teamId, string[] selections);
    #endregion

}

