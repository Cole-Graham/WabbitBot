using System;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data
{
    public class PlayerWithUserDetails
    {
        public required Player Player { get; set; }
        public string? DiscordId { get; set; }
        public string? Username { get; set; }
        public string? Nickname { get; set; }
    }

    public class PlayerRepository : BaseJsonRepository<Player>
    {
        private static readonly string[] Columns = new[]
        {
            "Id", "Name", "CreatedAt", "LastActive",
            "Stats", "TeamIds", "PreviousUserIds",
            "IsArchived", "ArchivedAt",
            "CreatedAt", "UpdatedAt"
        };

        public PlayerRepository(IDatabaseConnection connection)
            : base(connection, "Players", Columns)
        {
        }

        protected override Player CreateEntity()
        {
            return new Player();
        }

        public async Task<PlayerWithUserDetails?> GetPlayerWithUserDetailsAsync(string playerId)
        {
            const string sql = @"
                SELECT 
                    p.*,
                    u.DiscordId,
                    u.Username,
                    u.Nickname
                FROM Players p
                LEFT JOIN Users u ON u.CurrentPlayerId = p.Id
                WHERE p.Id = @PlayerId
                AND p.IsArchived = 0";

            var conn = await _connection.GetConnectionAsync();
            var results = await QueryUtil.QueryAsync(
                conn,
                sql,
                MapPlayerWithUserDetails,
                new { PlayerId = playerId }
            );
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<PlayerWithUserDetails>> GetPlayersWithUserDetailsByTeamIdAsync(string teamId)
        {
            const string sql = @"
                SELECT 
                    p.*,
                    u.DiscordId,
                    u.Username,
                    u.Nickname
                FROM Players p
                LEFT JOIN Users u ON u.CurrentPlayerId = p.Id
                WHERE p.TeamIds LIKE @TeamIdPattern
                AND p.IsArchived = 0";

            var conn = await _connection.GetConnectionAsync();
            return await QueryUtil.QueryAsync(
                conn,
                sql,
                MapPlayerWithUserDetails,
                new { TeamIdPattern = $"%{teamId}%" }
            );
        }

        private PlayerWithUserDetails MapPlayerWithUserDetails(IDataReader reader)
        {
            var player = MapEntity(reader);
            return new PlayerWithUserDetails
            {
                Player = player,
                DiscordId = reader.IsDBNull(reader.GetOrdinal("DiscordId")) ? null : reader.GetString(reader.GetOrdinal("DiscordId")),
                Username = reader.IsDBNull(reader.GetOrdinal("Username")) ? null : reader.GetString(reader.GetOrdinal("Username")),
                Nickname = reader.IsDBNull(reader.GetOrdinal("Nickname")) ? null : reader.GetString(reader.GetOrdinal("Nickname")),
            };
        }

        public async Task<Player?> GetByNameAsync(string name)
        {
            const string sql = "SELECT * FROM Players WHERE Name = @Name AND IsArchived = 0";
            var results = await QueryAsync(sql, new { Name = name });
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<Player>> GetInactivePlayersAsync(TimeSpan inactivityThreshold)
        {
            var cutoffDate = DateTime.UtcNow.Subtract(inactivityThreshold);
            const string sql = @"
                SELECT * FROM Players 
                WHERE LastActive < @CutoffDate 
                AND IsArchived = 0";
            return await QueryAsync(sql, new { CutoffDate = cutoffDate });
        }

        public async Task UpdateLastActiveAsync(string playerId)
        {
            const string sql = @"
                UPDATE Players 
                SET LastActive = @LastActive, UpdatedAt = @UpdatedAt 
                WHERE Id = @Id AND IsArchived = 0";

            await QueryUtil.ExecuteNonQueryAsync(
                await _connection.GetConnectionAsync(),
                sql,
                new
                {
                    Id = playerId,
                    LastActive = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
        }

        public async Task<IEnumerable<Player>> GetPlayersByTeamIdAsync(string teamId)
        {
            const string sql = @"
                SELECT * FROM Players 
                WHERE TeamIds LIKE @TeamIdPattern 
                AND IsArchived = 0";

            return await QueryAsync(sql, new { TeamIdPattern = $"%{teamId}%" });
        }

        public async Task ArchivePlayerAsync(string playerId)
        {
            const string sql = @"
                UPDATE Players 
                SET IsArchived = 1, 
                    ArchivedAt = @ArchivedAt, 
                    UpdatedAt = @UpdatedAt 
                WHERE Id = @Id AND IsArchived = 0";

            await QueryUtil.ExecuteNonQueryAsync(
                await _connection.GetConnectionAsync(),
                sql,
                new
                {
                    Id = playerId,
                    ArchivedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
        }

        public async Task UnarchivePlayerAsync(string playerId)
        {
            const string sql = @"
                UPDATE Players 
                SET IsArchived = 0, 
                    ArchivedAt = NULL, 
                    UpdatedAt = @UpdatedAt 
                WHERE Id = @Id AND IsArchived = 1";

            await QueryUtil.ExecuteNonQueryAsync(
                await _connection.GetConnectionAsync(),
                sql,
                new
                {
                    Id = playerId,
                    UpdatedAt = DateTime.UtcNow
                }
            );
        }

        public async Task<IEnumerable<Player>> GetArchivedPlayersAsync()
        {
            const string sql = "SELECT * FROM Players WHERE IsArchived = 1";
            return await QueryAsync(sql);
        }

        public async Task<IEnumerable<Player>> GetArchivedPlayersByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            const string sql = @"
                SELECT * FROM Players 
                WHERE IsArchived = 1 
                AND ArchivedAt BETWEEN @StartDate AND @EndDate
                ORDER BY ArchivedAt DESC";

            return await QueryAsync(sql, new { StartDate = startDate, EndDate = endDate });
        }
    }
}