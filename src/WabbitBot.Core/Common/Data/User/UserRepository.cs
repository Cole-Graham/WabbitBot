using System;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Events;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Data.Interface;

namespace WabbitBot.Core.Common.Data
{
    public class UserRepository : JsonRepository<User>, IUserRepository
    {
        private static readonly string[] Columns = new[]
        {
            "Id", "DiscordId", "Username", "Nickname", "AvatarUrl",
            "JoinedAt", "LastActive", "CurrentPlayerId",
            "CreatedAt", "UpdatedAt"
        };

        public UserRepository(IDatabaseConnection connection)
            : base(connection, "Users", Columns)
        {
            // Subscribe to player archive check events
            CoreEventBus.Instance.Subscribe<PlayerArchiveCheckEvent>(HandlePlayerArchiveCheck);
        }

        private async Task HandlePlayerArchiveCheck(PlayerArchiveCheckEvent @event)
        {
            // Check if any active users are linked to this player
            const string sql = @"
                SELECT COUNT(*) FROM Users 
                WHERE CurrentPlayerId = @PlayerId 
                AND LastActive > @CutoffDate";

            var cutoffDate = DateTime.UtcNow.AddDays(-30); // Consider users inactive after 30 days
            var count = await QueryUtil.ExecuteScalarAsync<int>(
                await _connection.GetConnectionAsync(),
                sql,
                new { PlayerId = @event.PlayerId, CutoffDate = cutoffDate }
            );
            @event.HasActiveUsers = count > 0;
        }

        protected override User CreateEntity()
        {
            return new User();
        }

        public async Task<User?> GetByDiscordIdAsync(ulong discordId)
        {
            const string sql = "SELECT * FROM Users WHERE DiscordId = @DiscordId";
            var results = await QueryAsync(sql, new { DiscordId = discordId.ToString() });
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<User>> GetInactiveUsersAsync(TimeSpan inactivityThreshold)
        {
            var cutoffDate = DateTime.UtcNow.Subtract(inactivityThreshold);
            const string sql = "SELECT * FROM Users WHERE LastActive < @CutoffDate";
            return await QueryAsync(sql, new { CutoffDate = cutoffDate });
        }

        public async Task UpdateLastActiveAsync(string userId)
        {
            const string sql = @"
                UPDATE Users 
                SET LastActive = @LastActive, UpdatedAt = @UpdatedAt 
                WHERE Id = @Id";

            await QueryUtil.ExecuteNonQueryAsync(
                await _connection.GetConnectionAsync(),
                sql,
                new
                {
                    Id = userId,
                    LastActive = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentNullException(nameof(username));
            }

            const string sql = "SELECT * FROM Users WHERE Username = @Username";
            var results = await QueryAsync(sql, new { Username = username });
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<User>> GetUsersByPlayerIdAsync(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                throw new ArgumentNullException(nameof(playerId));
            }

            const string sql = "SELECT * FROM Users WHERE CurrentPlayerId = @PlayerId";
            return await QueryAsync(sql, new { PlayerId = playerId });
        }

        public async Task ArchiveUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            const string sql = @"
                UPDATE Users 
                SET IsArchived = 1, ArchivedAt = @ArchivedAt, UpdatedAt = @UpdatedAt 
                WHERE Id = @Id";

            await QueryUtil.ExecuteNonQueryAsync(
                await _connection.GetConnectionAsync(),
                sql,
                new
                {
                    Id = userId,
                    ArchivedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
        }

        public async Task UnarchiveUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            const string sql = @"
                UPDATE Users 
                SET IsArchived = 0, ArchivedAt = NULL, UpdatedAt = @UpdatedAt 
                WHERE Id = @Id";

            await QueryUtil.ExecuteNonQueryAsync(
                await _connection.GetConnectionAsync(),
                sql,
                new
                {
                    Id = userId,
                    UpdatedAt = DateTime.UtcNow
                }
            );
        }

        public async Task<IEnumerable<User>> GetArchivedUsersAsync()
        {
            const string sql = "SELECT * FROM Users WHERE IsArchived = 1 ORDER BY ArchivedAt DESC";
            return await QueryAsync(sql);
        }

        public async Task<IEnumerable<User>> GetArchivedUsersByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            const string sql = @"
                SELECT * FROM Users 
                WHERE IsArchived = 1 
                AND ArchivedAt BETWEEN @StartDate AND @EndDate 
                ORDER BY ArchivedAt DESC";

            return await QueryAsync(sql, new { StartDate = startDate, EndDate = endDate });
        }
    }
}
