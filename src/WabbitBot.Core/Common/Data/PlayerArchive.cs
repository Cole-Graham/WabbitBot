using System;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data
{
    public class PlayerArchive : BaseArchive<Player>
    {
        private static readonly string[] Columns = new[]
        {
            "Id", "Name", "CreatedAt", "LastActive",
            "Stats", "TeamIds", "PreviousUserIds",
            "UpdatedAt", "ArchivedAt"
        };

        public PlayerArchive(IDatabaseConnection connection)
            : base(connection, "ArchivedPlayers", Columns, dateColumn: "ArchivedAt")
        {
        }

        protected override Player MapEntity(IDataReader reader)
        {
            var id = reader.GetString(reader.GetOrdinal("Id"));
            var stats = JsonUtil.Deserialize<Dictionary<GameSize, PlayerStats>>(reader.GetString(reader.GetOrdinal("Stats"))) ?? new();
            var teamIds = JsonUtil.Deserialize<string[]>(reader.GetString(reader.GetOrdinal("TeamIds")))?.ToList() ?? new();
            var previousUserIds = JsonUtil.Deserialize<string[]>(reader.GetString(reader.GetOrdinal("PreviousUserIds")))?.ToList() ?? new();

            return new Player
            {
                Id = Guid.Parse(id),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                LastActive = reader.GetDateTime(reader.GetOrdinal("LastActive")),
                Stats = stats,
                TeamIds = teamIds,
                PreviousUserIds = previousUserIds,
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }

        protected override object BuildParameters(Player entity)
        {
            return new
            {
                Id = entity.Id.ToString(),
                entity.Name,
                entity.CreatedAt,
                entity.LastActive,
                Stats = JsonUtil.Serialize(entity.Stats),
                TeamIds = JsonUtil.Serialize(entity.TeamIds),
                PreviousUserIds = JsonUtil.Serialize(entity.PreviousUserIds),
                entity.UpdatedAt,
                ArchivedAt = DateTime.UtcNow
            };
        }

        public async Task<IEnumerable<Player>> GetPlayersByTeamIdAsync(string teamId)
        {
            const string sql = @"
                SELECT * FROM ArchivedPlayers 
                WHERE TeamIds LIKE @TeamIdPattern 
                ORDER BY ArchivedAt DESC";

            return await QueryAsync(sql, new { TeamIdPattern = $"%{teamId}%" });
        }

        public async Task<IEnumerable<Player>> GetPlayersByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await GetByDateRangeAsync(startDate, endDate);
        }
    }
}