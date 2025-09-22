using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Leaderboards.Data.Interface
{
    /// <summary>
    /// Defines the contract for leaderboard data access operations.
    /// This interface is part of the core domain model and should be implemented
    /// by infrastructure-specific repositories (e.g., SQL, NoSQL).
    /// </summary>
    public interface ILeaderboardRepository : IRepository<Leaderboard>
    {
        /// <summary>
        /// Retrieves a leaderboard by its ID.
        /// </summary>
        Task<Leaderboard?> GetLeaderboardAsync(string leaderboardId);

        /// <summary>
        /// Retrieves leaderboards by game size.
        /// </summary>
        Task<IEnumerable<Leaderboard>> GetLeaderboardsByEvenTeamFormatAsync(EvenTeamFormat evenTeamFormat);

        /// <summary>
        /// Retrieves the top rankings for a specific game size.
        /// </summary>
        Task<IEnumerable<LeaderboardItem>> GetTopRankingsAsync(EvenTeamFormat evenTeamFormat, int count = 10);

        /// <summary>
        /// Retrieves team rankings for a specific team and game size.
        /// </summary>
        Task<IEnumerable<LeaderboardItem>> GetRankingsByTeamIdAsync(string teamId, EvenTeamFormat evenTeamFormat);

        /// <summary>
        /// Retrieves player rankings for a specific player and game size.
        /// </summary>
        Task<IEnumerable<LeaderboardItem>> GetRankingsByDateRangeAsync(DateTime startDate, DateTime endDate, EvenTeamFormat evenTeamFormat);
    }
}
