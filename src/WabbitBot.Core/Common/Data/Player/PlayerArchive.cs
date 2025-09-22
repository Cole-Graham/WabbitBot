using System;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Data.Interface;

namespace WabbitBot.Core.Common.Data
{
    public class PlayerArchive : Archive<Player>, IPlayerArchive
    {
        private static readonly string[] Columns = new[]
        {
            "Id", "Name", "CreatedAt", "LastActive",
            "TeamIds", "PreviousUserIds",
            "UpdatedAt", "ArchivedAt"
        };

        public PlayerArchive(IDatabaseConnection connection)
            : base(connection, "ArchivedPlayers", Columns, dateColumn: "ArchivedAt")
        {
        }

        protected override Player MapEntity(IDataReader reader)
        {
            var id = reader.GetString(reader.GetOrdinal("Id"));
            var teamIds = JsonUtil.Deserialize<string[]>(reader.GetString(reader.GetOrdinal("TeamIds")))?.ToList() ?? new();
            var previousUserIds = JsonUtil.Deserialize<string[]>(reader.GetString(reader.GetOrdinal("PreviousUserIds")))?.ToList() ?? new();

            return new Player
            {
                Id = Guid.Parse(id),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                LastActive = reader.GetDateTime(reader.GetOrdinal("LastActive")),
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

        public async Task<IEnumerable<Player>> GetArchivedPlayersByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await GetByDateRangeAsync(startDate, endDate);
        }

        public async Task<IEnumerable<Player>> GetArchivedPlayersByTeamIdAsync(string teamId)
        {
            var sql = "SELECT * FROM ArchivedPlayers WHERE JSON_CONTAINS(TeamIds, @TeamIdPattern) ORDER BY ArchivedAt DESC";
            return await QueryAsync(sql, new { TeamIdPattern = $"\"{teamId}\"" });
        }

        public async Task<IEnumerable<Player>> GetArchivedPlayersByInactivityPeriodAsync(TimeSpan inactivityPeriod)
        {
            var cutoffDate = DateTime.UtcNow - inactivityPeriod;
            var sql = "SELECT * FROM ArchivedPlayers WHERE LastActive <= @CutoffDate ORDER BY LastActive ASC";
            return await QueryAsync(sql, new { CutoffDate = cutoffDate });
        }
    }
}