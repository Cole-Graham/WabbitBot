using WabbitBot.Core.Scrimmages;

namespace WabbitBot.DiscBot.DiscBot.Events;

/// <summary>
/// Interface for Discord-specific scrimmage operations
/// </summary>
public interface IDiscordScrimmageOperations
{
    /// <summary>
    /// Updates the scrimmage message with new status
    /// </summary>
    Task UpdateScrimmageMessageAsync(string scrimmageId, string status, string actionBy);

    /// <summary>
    /// Notifies team members about scrimmage status changes
    /// </summary>
    Task NotifyTeamMembersAsync(string scrimmageId, string status);

    /// <summary>
    /// Updates thread status (e.g., pin important messages, update thread name)
    /// </summary>
    Task UpdateThreadStatusAsync(string scrimmageId, string status);

    /// <summary>
    /// Archives a thread with optional delay
    /// </summary>
    Task ArchiveThreadAsync(string scrimmageId, string reason, int delayMinutes = 0);

    /// <summary>
    /// Creates a final results embed for completed scrimmages
    /// </summary>
    Task CreateFinalResultsEmbedAsync(ScrimmageCompletedEvent @event);

    /// <summary>
    /// Creates private threads for each team in a match when scrimmage is accepted
    /// </summary>
    Task<(ulong channelId, ulong team1ThreadId, ulong team2ThreadId)> CreateMatchThreadsAsync(string matchId, string team1Name, string team2Name, string TeamSize, List<ulong> team1MemberIds, List<ulong> team2MemberIds);
}
