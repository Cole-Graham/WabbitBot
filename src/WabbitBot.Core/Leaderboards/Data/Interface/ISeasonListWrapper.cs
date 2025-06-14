using System;
using System.Collections.Generic;
using WabbitBot.Common.Models;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Models.Interface;

namespace WabbitBot.Core.Leaderboards.Data.Interface
{
    /// <summary>
    /// Defines the contract for managing season collections.
    /// This interface provides thread-safe operations for managing seasons
    /// and their team statistics across different game sizes.
    /// </summary>
    public interface ISeasonListWrapper : IBaseEntity
    {
        /// <summary>
        /// Gets the last time the season collection was updated.
        /// </summary>
        DateTime LastUpdated { get; }

        /// <summary>
        /// Gets or sets whether to include inactive seasons in queries.
        /// </summary>
        bool IncludeInactive { get; set; }

        /// <summary>
        /// Gets or sets the game size filter for queries.
        /// </summary>
        GameSize? FilterByGameSize { get; set; }

        /// <summary>
        /// Gets a read-only dictionary of all seasons.
        /// </summary>
        IReadOnlyDictionary<string, Season> Seasons { get; }

        /// <summary>
        /// Adds a season to the collection.
        /// </summary>
        void AddSeason(Season season);

        /// <summary>
        /// Attempts to get a season by its ID.
        /// </summary>
        bool TryGetSeason(string seasonId, out Season? season);

        /// <summary>
        /// Removes a season from the collection.
        /// </summary>
        bool RemoveSeason(string seasonId);

        /// <summary>
        /// Gets the currently active season.
        /// </summary>
        Season? GetActiveSeason();

        /// <summary>
        /// Gets seasons within a specific date range.
        /// </summary>
        IEnumerable<Season> GetSeasonsByDateRange(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets seasons for a specific game size.
        /// </summary>
        IEnumerable<Season> GetSeasonsByGameSize(GameSize gameSize);

        /// <summary>
        /// Gets team statistics for a specific team and game size.
        /// </summary>
        IEnumerable<SeasonTeamStats> GetTeamStats(string teamId, GameSize gameSize);

        /// <summary>
        /// Gets filtered seasons based on current filter settings.
        /// </summary>
        IEnumerable<Season> GetFilteredSeasons();

        /// <summary>
        /// Gets the top teams for a specific game size.
        /// </summary>
        IEnumerable<SeasonTeamStats> GetTopTeams(GameSize gameSize, int count = 10);

        /// <summary>
        /// Gets teams ordered by win rate for a specific game size.
        /// </summary>
        IEnumerable<SeasonTeamStats> GetTeamsByWinRate(GameSize gameSize, int minMatches = 5);
    }
}