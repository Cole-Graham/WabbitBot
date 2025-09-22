using System.Threading.Tasks;

namespace WabbitBot.DiscBot.DiscBot.Events
{
    /// <summary>
    /// Interface for Discord-specific match operations
    /// </summary>
    public interface IMatchDiscordOperations
    {
        /// <summary>
        /// Updates a match with Discord thread information
        /// </summary>
        Task UpdateMatchDiscordInfoAsync(string matchId, ulong channelId, ulong team1ThreadId, ulong team2ThreadId);
    }
}
