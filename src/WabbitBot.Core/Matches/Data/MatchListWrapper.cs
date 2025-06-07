using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using WabbitBot.Common.Models;
using WabbitBot.Core.Matches;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Matches.Data
{
    public class MatchListWrapper : BaseEntity
    {
        private readonly ConcurrentDictionary<string, Match> _matches = new();
        public DateTime LastUpdated { get; set; }

        public IReadOnlyDictionary<string, Match> Matches => _matches;

        public MatchListWrapper()
        {
            LastUpdated = DateTime.UtcNow;
        }

        public void AddMatch(Match match)
        {
            _matches.TryAdd(match.Id.ToString(), match);
            LastUpdated = DateTime.UtcNow;
        }

        public bool TryGetMatch(string matchId, out Match? match)
        {
            return _matches.TryGetValue(matchId, out match);
        }

        public bool RemoveMatch(string matchId)
        {
            var result = _matches.TryRemove(matchId, out _);
            if (result)
            {
                LastUpdated = DateTime.UtcNow;
            }
            return result;
        }

        public IEnumerable<Match> GetMatchesByStatus(MatchStatus status)
        {
            return _matches.Values.Where(m => m.Status == status);
        }

        public IEnumerable<Match> GetMatchesByTeam(string teamId)
        {
            return _matches.Values.Where(m =>
                m.Team1Id == teamId || m.Team2Id == teamId);
        }
    }
}