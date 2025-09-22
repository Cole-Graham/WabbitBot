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
    public class UserArchive : Archive<User>, IUserArchive
    {
        private static readonly string[] Columns = new[]
        {
            "Id", "DiscordId", "Username", "CreatedAt", "LastActive",
            "PlayerId", "UpdatedAt", "ArchivedAt"
        };

        public UserArchive(IDatabaseConnection connection)
            : base(connection, "ArchivedUsers", Columns, dateColumn: "ArchivedAt")
        {
        }

        protected override User MapEntity(IDataReader reader)
        {
            var id = reader.GetString(reader.GetOrdinal("Id"));
            var discordId = reader.GetString(reader.GetOrdinal("DiscordId"));
            var playerId = reader.IsDBNull(reader.GetOrdinal("PlayerId")) ? null : reader.GetString(reader.GetOrdinal("PlayerId"));

            return new User
            {
                Id = Guid.Parse(id),
                DiscordId = discordId,
                Username = reader.GetString(reader.GetOrdinal("Username")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                LastActive = reader.GetDateTime(reader.GetOrdinal("LastActive")),
                PlayerId = playerId,
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }

        protected override Dictionary<string, object> BuildParameters(User entity)
        {
            return new Dictionary<string, object>
            {
                ["Id"] = entity.Id.ToString(),
                ["DiscordId"] = entity.DiscordId,
                ["Username"] = entity.Username,
                ["CreatedAt"] = entity.CreatedAt,
                ["LastActive"] = entity.LastActive,
                ["PlayerId"] = entity.PlayerId ?? (object)DBNull.Value,
                ["UpdatedAt"] = entity.UpdatedAt,
                ["ArchivedAt"] = DateTime.UtcNow
            };
        }

        public async Task<IEnumerable<User>> GetArchivedUsersByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var query = $"SELECT * FROM ArchivedUsers WHERE ArchivedAt >= @startDate AND ArchivedAt <= @endDate ORDER BY ArchivedAt DESC";
            return await QueryAsync(query, new Dictionary<string, object>
            {
                ["startDate"] = startDate,
                ["endDate"] = endDate
            });
        }

        public async Task<IEnumerable<User>> GetArchivedUsersByPlayerIdAsync(string playerId)
        {
            var query = $"SELECT * FROM ArchivedUsers WHERE PlayerId = @playerId ORDER BY ArchivedAt DESC";
            return await QueryAsync(query, new Dictionary<string, object>
            {
                ["playerId"] = playerId
            });
        }

        public async Task<IEnumerable<User>> GetArchivedUsersByInactivityPeriodAsync(TimeSpan inactivityPeriod)
        {
            var cutoffDate = DateTime.UtcNow - inactivityPeriod;
            var query = $"SELECT * FROM ArchivedUsers WHERE LastActive <= @cutoffDate ORDER BY LastActive ASC";
            return await QueryAsync(query, new Dictionary<string, object>
            {
                ["cutoffDate"] = cutoffDate
            });
        }
    }
}
