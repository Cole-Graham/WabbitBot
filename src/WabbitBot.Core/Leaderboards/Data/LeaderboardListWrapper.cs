using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using WabbitBot.Common.Models;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Leaderboards.Data
{
    public class LeaderboardListWrapper : BaseEntity
    {
        private readonly ConcurrentDictionary<string, Leaderboard> _leaderboards = new();
        public DateTime LastUpdated { get; set; }
        public bool IncludeInactive { get; set; }
        public GameSize? FilterByGameSize { get; set; }

        public IReadOnlyDictionary<string, Leaderboard> Leaderboards => _leaderboards;

        public LeaderboardListWrapper()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
            IncludeInactive = false;
        }

        public void AddLeaderboard(Leaderboard leaderboard)
        {
            _leaderboards.TryAdd(leaderboard.Id.ToString(), leaderboard);
            LastUpdated = DateTime.UtcNow;
        }

        public bool TryGetLeaderboard(string leaderboardId, out Leaderboard? leaderboard)
        {
            return _leaderboards.TryGetValue(leaderboardId, out leaderboard);
        }

        public bool RemoveLeaderboard(string leaderboardId)
        {
            var result = _leaderboards.TryRemove(leaderboardId, out _);
            if (result)
            {
                LastUpdated = DateTime.UtcNow;
            }
            return result;
        }

        public IEnumerable<Leaderboard> GetLeaderboardsByGameSize(GameSize gameSize)
        {
            return _leaderboards.Values.Where(l => l.Rankings.ContainsKey(gameSize));
        }

        public IEnumerable<LeaderboardEntry> GetTopRankings(GameSize gameSize, int count = 10)
        {
            return _leaderboards.Values
                .Where(l => l.Rankings.ContainsKey(gameSize))
                .SelectMany(l => l.Rankings[gameSize].Values)
                .OrderByDescending(e => e.Rating)
                .Take(count);
        }

        public IEnumerable<LeaderboardEntry> GetTeamRankings(string teamId, GameSize gameSize)
        {
            return _leaderboards.Values
                .Where(l => l.Rankings.ContainsKey(gameSize))
                .SelectMany(l => l.Rankings[gameSize].Values)
                .Where(e => e.Name == teamId && e.IsTeam)
                .OrderByDescending(e => e.Rating);
        }

        public IEnumerable<LeaderboardEntry> GetPlayerRankings(string playerId, GameSize gameSize)
        {
            return _leaderboards.Values
                .Where(l => l.Rankings.ContainsKey(gameSize))
                .SelectMany(l => l.Rankings[gameSize].Values)
                .Where(e => e.Name == playerId && !e.IsTeam)
                .OrderByDescending(e => e.Rating);
        }

        public IEnumerable<LeaderboardEntry> GetFilteredRankings(GameSize gameSize, bool teamsOnly = false, bool playersOnly = false)
        {
            var query = _leaderboards.Values
                .Where(l => l.Rankings.ContainsKey(gameSize))
                .SelectMany(l => l.Rankings[gameSize].Values);

            if (teamsOnly)
            {
                query = query.Where(e => e.IsTeam);
            }
            else if (playersOnly)
            {
                query = query.Where(e => !e.IsTeam);
            }

            return query.OrderByDescending(e => e.Rating);
        }
    }
}
