using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Core.Matches;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Matches.Data
{
    public class MatchArchive : BaseArchive<Match>
    {
        private static readonly string[] Columns = new[]
        {
            "Id", "Team1Id", "Team2Id", "Team1PlayerIds", "Team2PlayerIds",
            "GameSize", "CreatedAt", "StartedAt", "CompletedAt", "WinnerId",
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
                GameSize = (GameSize)reader.GetInt32(reader.GetOrdinal("GameSize")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                StartedAt = reader.IsDBNull(reader.GetOrdinal("StartedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("StartedAt")),
                CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                WinnerId = reader.IsDBNull(reader.GetOrdinal("WinnerId")) ? null : reader.GetString(reader.GetOrdinal("WinnerId")),
                Status = (MatchStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                Stage = (MatchStage)reader.GetInt32(reader.GetOrdinal("Stage")),
                ParentId = reader.IsDBNull(reader.GetOrdinal("ParentId")) ? null : reader.GetString(reader.GetOrdinal("ParentId")),
                ParentType = reader.IsDBNull(reader.GetOrdinal("ParentType")) ? null : reader.GetString(reader.GetOrdinal("ParentType")),
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
                GameSize = (int)entity.GameSize,
                entity.CreatedAt,
                entity.StartedAt,
                entity.CompletedAt,
                entity.WinnerId,
                Status = (int)entity.Status,
                Stage = (int)entity.Stage,
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
    }
}