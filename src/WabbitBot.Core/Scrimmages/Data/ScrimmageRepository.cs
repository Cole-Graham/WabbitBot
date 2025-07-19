using System.Data;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Scrimmages;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Scrimmages.Data
{
    /// <summary>
    /// Implementation of IScrimmageRepository that provides data access operations for scrimmages.
    /// </summary>
    public class ScrimmageRepository : BaseRepository<Scrimmage>, IScrimmageRepository
    {
        private const string TableName = "Scrimmages";
        private static readonly string[] ColumnNames = new[]
        {
            "Id", "Team1Id", "Team2Id", "Team1RosterIds", "Team2RosterIds",
            "GameSize", "CreatedAt", "StartedAt", "CompletedAt", "WinnerId",
            "Status", "Team1Rating", "Team2Rating", "RatingChange", "RatingMultiplier",
            "Team1Confidence", "Team2Confidence", "ChallengeExpiresAt", "IsAccepted", "BestOf"
        };

        public ScrimmageRepository(IDatabaseConnection connection)
            : base(connection, TableName, ColumnNames, "Id")
        {
        }

        public async Task<Scrimmage?> GetScrimmageAsync(string scrimmageId)
        {
            return await GetByIdAsync(scrimmageId);
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
                Team1Confidence = reader.GetDouble(reader.GetOrdinal("Team1Confidence")),
                Team2Confidence = reader.GetDouble(reader.GetOrdinal("Team2Confidence")),
                ChallengeExpiresAt = reader.IsDBNull(reader.GetOrdinal("ChallengeExpiresAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ChallengeExpiresAt")),
                IsAccepted = reader.GetBoolean(reader.GetOrdinal("IsAccepted")),
                BestOf = reader.GetInt32(reader.GetOrdinal("BestOf"))
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
                entity.Team1Confidence,
                entity.Team2Confidence,
                entity.ChallengeExpiresAt,
                entity.IsAccepted,
                entity.BestOf
            };
        }

        public async Task<IEnumerable<Scrimmage>> GetScrimmagesByTeamAsync(string teamId)
        {
            const string sql = @"
                SELECT * FROM Scrimmages 
                WHERE Team1Id = @TeamId OR Team2Id = @TeamId 
                ORDER BY CreatedAt DESC";

            return await QueryAsync(sql, new { TeamId = teamId });
        }

        public async Task<IEnumerable<Scrimmage>> GetScrimmagesByStatusAsync(ScrimmageStatus status)
        {
            const string sql = @"
                SELECT * FROM Scrimmages 
                WHERE Status = @Status 
                ORDER BY CreatedAt DESC";

            return await QueryAsync(sql, new { Status = status });
        }

        public async Task<IEnumerable<Scrimmage>> GetRecentScrimmagesAsync(int count)
        {
            const string sql = @"
                SELECT * FROM Scrimmages 
                ORDER BY CreatedAt DESC 
                LIMIT @Count";

            return await QueryAsync(sql, new { Count = count });
        }
    }
}
