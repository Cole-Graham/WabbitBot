using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Scrimmages.Data.Interface;
using WabbitBot.Core.Scrimmages;

namespace WabbitBot.Core.Scrimmages.Data
{
    public class ScrimmageRepository : BaseRepository<Scrimmage>, IScrimmageRepository
    {
        private static readonly string[] ColumnNames = new[]
        {
            "Id", "Team1Id", "Team2Id", "Team1RosterIds", "Team2RosterIds", "GameSize",
            "StartedAt", "CompletedAt", "WinnerId", "Status", "Team1Rating", "Team2Rating",
            "Team1RatingChange", "Team2RatingChange", "Team1Confidence", "Team2Confidence",
            "ChallengeExpiresAt", "IsAccepted", "BestOf", "CreatedAt", "UpdatedAt"
        };

        public ScrimmageRepository(IDatabaseConnection connection)
            : base(connection, "Scrimmages", ColumnNames, "Id")
        {
        }

        public async Task<Scrimmage?> GetScrimmageAsync(string scrimmageId)
        {
            return await GetByIdAsync(scrimmageId);
        }

        public async Task<IEnumerable<Scrimmage>> GetScrimmagesByTeamAsync(string teamId)
        {
            var sql = $"SELECT * FROM {_tableName} WHERE Team1Id = @TeamId OR Team2Id = @TeamId ORDER BY CreatedAt DESC";
            return await QueryAsync(sql, new { TeamId = teamId });
        }

        public async Task<IEnumerable<Scrimmage>> GetScrimmagesByStatusAsync(ScrimmageStatus status)
        {
            var sql = $"SELECT * FROM {_tableName} WHERE Status = @Status ORDER BY CreatedAt DESC";
            return await QueryAsync(sql, new { Status = (int)status });
        }

        public async Task<IEnumerable<Scrimmage>> GetRecentScrimmagesAsync(int count)
        {
            var sql = $"SELECT * FROM {_tableName} ORDER BY CreatedAt DESC LIMIT @Count";
            return await QueryAsync(sql, new { Count = count });
        }

        protected override Scrimmage MapEntity(IDataReader reader)
        {
            return new Scrimmage
            {
                Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
                Team1Id = reader.GetString(reader.GetOrdinal("Team1Id")),
                Team2Id = reader.GetString(reader.GetOrdinal("Team2Id")),
                Team1RosterIds = JsonUtil.Deserialize<List<string>>(reader.GetString(reader.GetOrdinal("Team1RosterIds"))) ?? new List<string>(),
                Team2RosterIds = JsonUtil.Deserialize<List<string>>(reader.GetString(reader.GetOrdinal("Team2RosterIds"))) ?? new List<string>(),
                GameSize = (GameSize)reader.GetInt32(reader.GetOrdinal("GameSize")),
                StartedAt = reader.IsDBNull(reader.GetOrdinal("StartedAt")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("StartedAt"))),
                CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("CompletedAt"))),
                WinnerId = reader.IsDBNull(reader.GetOrdinal("WinnerId")) ? null : reader.GetString(reader.GetOrdinal("WinnerId")),
                Status = (ScrimmageStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                Team1Rating = reader.GetInt32(reader.GetOrdinal("Team1Rating")),
                Team2Rating = reader.GetInt32(reader.GetOrdinal("Team2Rating")),
                Team1RatingChange = reader.GetDouble(reader.GetOrdinal("Team1RatingChange")),
                Team2RatingChange = reader.GetDouble(reader.GetOrdinal("Team2RatingChange")),
                Team1Confidence = reader.GetDouble(reader.GetOrdinal("Team1Confidence")),
                Team2Confidence = reader.GetDouble(reader.GetOrdinal("Team2Confidence")),
                ChallengeExpiresAt = reader.IsDBNull(reader.GetOrdinal("ChallengeExpiresAt")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("ChallengeExpiresAt"))),
                IsAccepted = reader.GetInt32(reader.GetOrdinal("IsAccepted")) != 0,
                BestOf = reader.GetInt32(reader.GetOrdinal("BestOf")),
                CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))),
                UpdatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("UpdatedAt")))
            };
        }

        protected override object BuildParameters(Scrimmage entity)
        {
            return new
            {
                Id = entity.Id.ToString(),
                entity.Team1Id,
                entity.Team2Id,
                Team1RosterIds = JsonUtil.Serialize(entity.Team1RosterIds),
                Team2RosterIds = JsonUtil.Serialize(entity.Team2RosterIds),
                GameSize = (int)entity.GameSize,
                StartedAt = entity.StartedAt?.ToString("O"),
                CompletedAt = entity.CompletedAt?.ToString("O"),
                entity.WinnerId,
                Status = (int)entity.Status,
                entity.Team1Rating,
                entity.Team2Rating,
                entity.Team1RatingChange,
                entity.Team2RatingChange,
                entity.Team1Confidence,
                entity.Team2Confidence,
                ChallengeExpiresAt = entity.ChallengeExpiresAt?.ToString("O"),
                IsAccepted = entity.IsAccepted ? 1 : 0,
                entity.BestOf,
                entity.CreatedAt,
                entity.UpdatedAt
            };
        }
    }
}
