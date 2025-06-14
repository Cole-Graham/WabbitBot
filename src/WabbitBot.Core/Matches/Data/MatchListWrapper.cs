using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using WabbitBot.Common.Models;
using WabbitBot.Core.Matches;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;

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
            return _matches.Values.Where(m => m.Team1Id == teamId || m.Team2Id == teamId);
        }

        public IEnumerable<Match> GetMatchesByGameSize(GameSize gameSize)
        {
            return _matches.Values.Where(m => m.GameSize == gameSize);
        }

        public IEnumerable<Match> GetMatchesByParent(string parentId, string parentType)
        {
            return _matches.Values.Where(m => m.ParentId == parentId && m.ParentType == parentType);
        }

        public IEnumerable<Match> GetMatchesByDateRange(DateTime startDate, DateTime endDate)
        {
            return _matches.Values.Where(m => m.CreatedAt >= startDate && m.CreatedAt <= endDate);
        }

        public IEnumerable<Match> GetMatchesByPlayer(string playerId)
        {
            return _matches.Values.Where(m =>
                m.Team1PlayerIds.Contains(playerId) || m.Team2PlayerIds.Contains(playerId));
        }

        public IEnumerable<Match> GetActiveMatches()
        {
            return _matches.Values.Where(m => m.Status == MatchStatus.InProgress);
        }

        public IEnumerable<Match> GetCompletedMatches()
        {
            return _matches.Values.Where(m => m.Status == MatchStatus.Completed);
        }

        public IEnumerable<Match> GetTournamentMatches(string tournamentId)
        {
            return _matches.Values.Where(m => m.ParentId == tournamentId && m.ParentType == "Tournament");
        }

        public IEnumerable<Match> GetScrimmageMatches(string scrimmageId)
        {
            return _matches.Values.Where(m => m.ParentId == scrimmageId && m.ParentType == "Scrimmage");
        }

        private static Match MapMatch(System.Data.IDataReader reader)
        {
            return new Match
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                Team1Id = reader.GetString(reader.GetOrdinal("Team1Id")),
                Team2Id = reader.GetString(reader.GetOrdinal("Team2Id")),
                Team1PlayerIds = reader.GetString(reader.GetOrdinal("Team1PlayerIds")).Split(',').ToList(),
                Team2PlayerIds = reader.GetString(reader.GetOrdinal("Team2PlayerIds")).Split(',').ToList(),
                GameSize = (GameSize)reader.GetInt32(reader.GetOrdinal("GameSize")),
                StartedAt = reader.GetDateTime(reader.GetOrdinal("StartedAt")),
                CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                WinnerId = reader.IsDBNull(reader.GetOrdinal("WinnerId"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("WinnerId")),
                Status = (MatchStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                Stage = (MatchStage)reader.GetInt32(reader.GetOrdinal("Stage")),
                ParentId = reader.IsDBNull(reader.GetOrdinal("ParentId"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("ParentId")),
                ParentType = reader.IsDBNull(reader.GetOrdinal("ParentType"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("ParentType")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }
    }
}