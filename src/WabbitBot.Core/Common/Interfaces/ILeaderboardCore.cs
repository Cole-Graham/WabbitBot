using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;

namespace WabbitBot.Core.Common.Interfaces
{
    /// <summary>
    /// Interface for leaderboard-related core operations
    /// </summary>
    public interface ILeaderboardCore : ICore
    {
        /// <summary>
        /// Refreshes the leaderboard for a specific game size from Season data
        /// </summary>
        Task RefreshLeaderboardAsync();

        /// <summary>
        /// Manually triggers a leaderboard refresh for all game sizes
        /// </summary>
        Task RefreshAllLeaderboardsAsync();
    }
}
