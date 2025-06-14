using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using WabbitBot.Common.Models;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Leaderboards.Data
{
    public class SeasonListWrapper : BaseEntity
    {
        private readonly ConcurrentDictionary<string, Season> _seasons = new();
        public DateTime LastUpdated { get; set; }
        public bool IncludeInactive { get; set; }
        public GameSize? FilterByGameSize { get; set; }

        public IReadOnlyDictionary<string, Season> Seasons => _seasons;

        public SeasonListWrapper()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
            IncludeInactive = false;
        }

        public void AddSeason(Season season)
        {
            _seasons.TryAdd(season.Id.ToString(), season);
            LastUpdated = DateTime.UtcNow;
        }

        public bool TryGetSeason(string seasonId, out Season? season)
        {
            return _seasons.TryGetValue(seasonId, out season);
        }

        public bool RemoveSeason(string seasonId)
        {
            var result = _seasons.TryRemove(seasonId, out _);
            if (result)
            {
                LastUpdated = DateTime.UtcNow;
            }
            return result;
        }

        public Season? GetActiveSeason()
        {
            return _seasons.Values.FirstOrDefault(s => s.IsActive);
        }

        public IEnumerable<Season> GetSeasonsByDateRange(DateTime startDate, DateTime endDate)
        {
            return _seasons.Values
                .Where(s => s.StartDate >= startDate && s.EndDate <= endDate)
                .OrderBy(s => s.StartDate);
        }

        public IEnumerable<Season> GetSeasonsByGameSize(GameSize gameSize)
        {
            return _seasons.Values
                .Where(s => s.TeamStats.ContainsKey(gameSize))
                .OrderBy(s => s.StartDate);
        }

        public IEnumerable<SeasonTeamStats> GetTeamStats(string teamId, GameSize gameSize)
        {
            return _seasons.Values
                .Where(s => s.TeamStats.ContainsKey(gameSize) &&
                           s.TeamStats[gameSize].ContainsKey(teamId))
                .Select(s => s.TeamStats[gameSize][teamId])
                .OrderByDescending(s => s.CurrentRating);
        }

        public IEnumerable<Season> GetFilteredSeasons()
        {
            var query = _seasons.Values.AsEnumerable();

            if (!IncludeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            if (FilterByGameSize.HasValue)
            {
                query = query.Where(s => s.TeamStats.ContainsKey(FilterByGameSize.Value));
            }

            return query.OrderBy(s => s.StartDate);
        }

        public IEnumerable<SeasonTeamStats> GetTopTeams(GameSize gameSize, int count = 10)
        {
            return _seasons.Values
                .Where(s => s.TeamStats.ContainsKey(gameSize))
                .SelectMany(s => s.TeamStats[gameSize].Values)
                .OrderByDescending(s => s.CurrentRating)
                .Take(count);
        }

        public IEnumerable<SeasonTeamStats> GetTeamsByWinRate(GameSize gameSize, int minMatches = 5)
        {
            return _seasons.Values
                .Where(s => s.TeamStats.ContainsKey(gameSize))
                .SelectMany(s => s.TeamStats[gameSize].Values)
                .Where(s => s.MatchesCount >= minMatches)
                .OrderByDescending(s => s.WinRate);
        }
    }
}