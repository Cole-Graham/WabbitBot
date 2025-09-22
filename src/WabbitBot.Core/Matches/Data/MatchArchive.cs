using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Core.Matches;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Matches.Data.Interface;

namespace WabbitBot.Core.Matches.Data
{
    public class MatchArchive : Archive<Match>, IMatchArchive
    {
        private static readonly string[] Columns = new[]
        {
            "Id", "Team1Id", "Team2Id", "Team1PlayerIds", "Team2PlayerIds",
            "EvenTeamFormat", "CreatedAt", "StartedAt", "CompletedAt", "WinnerId",
            "Status", "Stage", "ParentId", "ParentType", "BestOf",
            "CreatedAt", "UpdatedAt", "ArchivedAt"
        };

        public MatchArchive(IDatabaseConnection connection)
            : base(connection, "ArchivedMatches", Columns, dateColumn: "ArchivedAt")
        {
        }

        protected override Match MapEntity(IDataReader reader)
        {
            return new Match
            {
                Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
                Team1Id = reader.GetString(reader.GetOrdinal("Team1Id")),
                Team2Id = reader.GetString(reader.GetOrdinal("Team2Id")),
                Team1PlayerIds = reader.GetString(reader.GetOrdinal("Team1PlayerIds")).Split(',').ToList(),
                Team2PlayerIds = reader.GetString(reader.GetOrdinal("Team2PlayerIds")).Split(',').ToList(),
                EvenTeamFormat = (EvenTeamFormat)reader.GetInt32(reader.GetOrdinal("EvenTeamFormat")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                StartedAt = reader.IsDBNull(reader.GetOrdinal("StartedAt")) ? null : reader.GetDateTime(
                    reader.GetOrdinal("StartedAt")),
                CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt")) ? null : reader.GetDateTime(
                    reader.GetOrdinal("CompletedAt")),
                WinnerId = reader.IsDBNull(reader.GetOrdinal("WinnerId")) ? null : reader.GetString(
                    reader.GetOrdinal("WinnerId")),
                ParentId = reader.IsDBNull(reader.GetOrdinal("ParentId")) ? null : reader.GetString(
                    reader.GetOrdinal("ParentId")),
                ParentType = reader.IsDBNull(reader.GetOrdinal("ParentType")) ? null : reader.GetString(
                    reader.GetOrdinal("ParentType")),
                BestOf = reader.GetInt32(reader.GetOrdinal("BestOf"))
            };
        }

        protected override object BuildParameters(Match entity)
        {
            return new
            {
                Id = entity.Id.ToString(),
                entity.Team1Id,
                entity.Team2Id,
                Team1PlayerIds = string.Join(",", entity.Team1PlayerIds),
                Team2PlayerIds = string.Join(",", entity.Team2PlayerIds),
                EvenTeamFormat = (int)entity.EvenTeamFormat,
                entity.CreatedAt,
                entity.StartedAt,
                entity.CompletedAt,
                entity.WinnerId,
                entity.ParentId,
                entity.ParentType,
                entity.BestOf,
                UpdatedAt = DateTime.UtcNow,
                ArchivedAt = DateTime.UtcNow
            };
        }

        public async Task<IEnumerable<Match>> GetTeamHistoryAsync(string teamId, int limit = 10)
        {
            const string sql = @"
                SELECT * FROM ArchivedMatches 
                WHERE (Team1Id = @TeamId OR Team2Id = @TeamId)
                ORDER BY ArchivedAt DESC
                LIMIT @Limit";

            return await QueryAsync(sql, new { TeamId = teamId, Limit = limit });
        }

        public async Task<IEnumerable<Match>> GetMatchesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            const string sql = @"
                SELECT * FROM ArchivedMatches 
                WHERE ArchivedAt >= @StartDate AND ArchivedAt <= @EndDate
                ORDER BY ArchivedAt DESC";

            return await QueryAsync(sql, new { StartDate = startDate, EndDate = endDate });
        }

        public async Task<IEnumerable<Match>> GetMatchesByEvenTeamFormatAsync(EvenTeamFormat evenTeamFormat)
        {
            const string sql = @"
                SELECT * FROM ArchivedMatches 
                WHERE EvenTeamFormat = @EvenTeamFormat
                ORDER BY ArchivedAt DESC";

            return await QueryAsync(sql, new { EvenTeamFormat = (int)evenTeamFormat });
        }

        public async Task<IEnumerable<Match>> GetMatchesByTournamentIdAsync(string tournamentId)
        {
            const string sql = @"
                SELECT * FROM ArchivedMatches 
                WHERE ParentId = @TournamentId AND ParentType = 'Tournament'
                ORDER BY ArchivedAt DESC";

            return await QueryAsync(sql, new { TournamentId = tournamentId });
        }
    }
}