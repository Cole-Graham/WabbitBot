using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Core.Scrimmages;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Scrimmages.Data
{
    public class ScrimmageArchive : BaseArchive<Scrimmage>
    {
        private static readonly string[] Columns = new[]
        {
            "Id", "Team1Id", "Team2Id", "Team1RosterIds", "Team2RosterIds",
            "GameSize", "CreatedAt", "StartedAt", "CompletedAt", "WinnerId",
            "Status", "Team1Rating", "Team2Rating", "RatingChange", "RatingMultiplier",
            "ChallengeExpiresAt", "IsAccepted", "BestOf", "UpdatedAt", "ArchivedAt"
        };

        public ScrimmageArchive(IDatabaseConnection connection)
            : base(connection, "ArchivedScrimmages", Columns, dateColumn: "ArchivedAt")
        {
        }

        protected override Scrimmage MapEntity(IDataReader reader)
        {
            return new Scrimmage
            {
                Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
                Team1Id = reader.GetString(reader.GetOrdinal("Team1Id")),
                Team2Id = reader.GetString(reader.GetOrdinal("Team2Id")),
                Team1RosterIds = reader.GetString(reader.GetOrdinal("Team1RosterIds")).Split(',').ToList(),
                Team2RosterIds = reader.GetString(reader.GetOrdinal("Team2RosterIds")).Split(',').ToList(),
                GameSize = (GameSize)reader.GetInt32(reader.GetOrdinal("GameSize")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                StartedAt = reader.IsDBNull(reader.GetOrdinal("StartedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("StartedAt")),
                CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                WinnerId = reader.IsDBNull(reader.GetOrdinal("WinnerId")) ? null : reader.GetString(reader.GetOrdinal("WinnerId")),
                Status = (ScrimmageStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                Team1Rating = reader.GetInt32(reader.GetOrdinal("Team1Rating")),
                Team2Rating = reader.GetInt32(reader.GetOrdinal("Team2Rating")),
                RatingChange = reader.GetInt32(reader.GetOrdinal("RatingChange")),
                RatingMultiplier = reader.GetDouble(reader.GetOrdinal("RatingMultiplier")),
                ChallengeExpiresAt = reader.IsDBNull(reader.GetOrdinal("ChallengeExpiresAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ChallengeExpiresAt")),
                IsAccepted = reader.GetBoolean(reader.GetOrdinal("IsAccepted")),
                BestOf = reader.GetInt32(reader.GetOrdinal("BestOf")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }

        protected override object BuildParameters(Scrimmage entity)
        {
            return new
            {
                Id = entity.Id.ToString(),
                entity.Team1Id,
                entity.Team2Id,
                Team1RosterIds = string.Join(",", entity.Team1RosterIds),
                Team2RosterIds = string.Join(",", entity.Team2RosterIds),
                GameSize = (int)entity.GameSize,
                entity.CreatedAt,
                entity.StartedAt,
                entity.CompletedAt,
                entity.WinnerId,
                Status = (int)entity.Status,
                entity.Team1Rating,
                entity.Team2Rating,
                entity.RatingChange,
                entity.RatingMultiplier,
                entity.ChallengeExpiresAt,
                entity.IsAccepted,
                entity.BestOf,
                entity.UpdatedAt,
                ArchivedAt = DateTime.UtcNow
            };
        }

        public async Task<IEnumerable<Scrimmage>> GetTeamHistoryAsync(string teamId, int limit = 10)
        {
            const string sql = @"
                SELECT * FROM ArchivedScrimmages 
                WHERE (Team1Id = @TeamId OR Team2Id = @TeamId)
                ORDER BY ArchivedAt DESC
                LIMIT @Limit";

            return await QueryAsync(sql, new { TeamId = teamId, Limit = limit });
        }

        public async Task<IEnumerable<Scrimmage>> GetByStatusAsync(ScrimmageStatus status)
        {
            const string sql = @"
                SELECT * FROM ArchivedScrimmages 
                WHERE Status = @Status
                ORDER BY ArchivedAt DESC";

            return await QueryAsync(sql, new { Status = (int)status });
        }

        public async Task<IEnumerable<Scrimmage>> GetByGameSizeAsync(GameSize gameSize)
        {
            const string sql = @"
                SELECT * FROM ArchivedScrimmages 
                WHERE GameSize = @GameSize
                ORDER BY ArchivedAt DESC";

            return await QueryAsync(sql, new { GameSize = (int)gameSize });
        }
    }
}
