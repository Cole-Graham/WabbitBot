using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Leaderboards.Data
{
    public class SeasonArchive : BaseArchive<Season>
    {
        private static readonly string[] Columns = new[]
        {
            "Id", "Name", "StartDate", "EndDate", "IsActive",
            "TeamStats", "Config", "CreatedAt", "UpdatedAt", "ArchivedAt"
        };

        public SeasonArchive(IDatabaseConnection connection)
            : base(connection, "ArchivedSeasons", Columns, dateColumn: "ArchivedAt")
        {
        }

        protected override Season MapEntity(IDataReader reader)
        {
            var teamStats = JsonUtil.Deserialize<Dictionary<GameSize, Dictionary<string, SeasonTeamStats>>>(
                reader.GetString(reader.GetOrdinal("TeamStats"))) ?? new();
            var config = JsonUtil.Deserialize<SeasonConfig>(
                reader.GetString(reader.GetOrdinal("Config"))) ?? new();

            return new Season
            {
                Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                StartDate = reader.GetDateTime(reader.GetOrdinal("StartDate")),
                EndDate = reader.GetDateTime(reader.GetOrdinal("EndDate")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                TeamStats = teamStats,
                Config = config,
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }

        protected override object BuildParameters(Season entity)
        {
            return new
            {
                Id = entity.Id.ToString(),
                entity.Name,
                entity.StartDate,
                entity.EndDate,
                entity.IsActive,
                TeamStats = JsonUtil.Serialize(entity.TeamStats),
                Config = JsonUtil.Serialize(entity.Config),
                entity.CreatedAt,
                entity.UpdatedAt,
                ArchivedAt = DateTime.UtcNow
            };
        }

        public override async Task<IEnumerable<Season>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            const string sql = @"
                SELECT * FROM ArchivedSeasons 
                WHERE StartDate >= @StartDate 
                AND EndDate <= @EndDate
                ORDER BY ArchivedAt DESC";

            return await QueryAsync(sql, new { StartDate = startDate, EndDate = endDate });
        }

        public async Task<IEnumerable<Season>> GetByTeamIdAsync(string teamId)
        {
            const string sql = @"
                SELECT * FROM ArchivedSeasons 
                WHERE TeamStats LIKE @TeamIdPattern
                ORDER BY ArchivedAt DESC";

            return await QueryAsync(sql, new { TeamIdPattern = $"%{teamId}%" });
        }
    }
}
