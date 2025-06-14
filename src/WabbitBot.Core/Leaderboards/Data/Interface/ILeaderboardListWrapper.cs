using System;
using System.Collections.Generic;
using WabbitBot.Common.Models;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Models.Interface;

namespace WabbitBot.Core.Leaderboards.Data.Interface
{
    /// <summary>
    /// Defines the contract for managing leaderboard collections.
    /// This interface provides thread-safe operations for managing leaderboards
    /// and their rankings across different game sizes.
    /// </summary>
    public interface ILeaderboardListWrapper : IBaseEntity
    {
        /// <summary>
        /// Gets the last time the leaderboard collection was updated.
        /// </summary>
        DateTime LastUpdated { get; }

        /// <summary>
        /// Gets or sets whether to include inactive leaderboards in queries.
        /// </summary>
        bool IncludeInactive { get; set; }

        /// <summary>
        /// Gets or sets the game size filter for queries.
        /// </summary>
        GameSize? FilterByGameSize { get; set; }

        /// <summary>
        /// Gets a read-only dictionary of all leaderboards.
        /// </summary>
        IReadOnlyDictionary<string, Leaderboard> Leaderboards { get; }

        /// <summary>
        /// Adds a leaderboard to the collection.
        /// </summary>
        void AddLeaderboard(Leaderboard leaderboard);

        /// <summary>
        /// Attempts to get a leaderboard by its ID.
        /// </summary>
        bool TryGetLeaderboard(string leaderboardId, out Leaderboard? leaderboard);

        /// <summary>
        /// Removes a leaderboard from the collection.
        /// </summary>
        bool RemoveLeaderboard(string leaderboardId);

        /// <summary>
        /// Gets all leaderboards for a specific game size.
        /// </summary>
        IEnumerable<Leaderboard> GetLeaderboardsByGameSize(GameSize gameSize);

        /// <summary>
        /// Gets the top rankings for a specific game size.
        /// </summary>
        IEnumerable<LeaderboardEntry> GetTopRankings(GameSize gameSize, int count = 10);

        /// <summary>
        /// Gets team rankings for a specific team and game size.
        /// </summary>
        IEnumerable<LeaderboardEntry> GetTeamRankings(string teamId, GameSize gameSize);

        /// <summary>
        /// Gets player rankings for a specific player and game size.
        /// </summary>
        IEnumerable<LeaderboardEntry> GetPlayerRankings(string playerId, GameSize gameSize);

        /// <summary>
        /// Gets filtered rankings based on specified criteria.
        /// </summary>
        IEnumerable<LeaderboardEntry> GetFilteredRankings(GameSize gameSize, bool teamsOnly = false, bool playersOnly = false);
    }
}