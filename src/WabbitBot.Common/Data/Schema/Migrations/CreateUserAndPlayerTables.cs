using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Schema.Interface;
using WabbitBot.Common.Data.Utilities;

namespace WabbitBot.Common.Data.Schema.Migrations
{
    public class CreateUserAndPlayerTables : IMigration
    {
        public int Order => MigrationOrder.CreatePlayers;
        public string Description => "Create initial user and player tables";

        public async Task UpAsync(IDatabaseConnection connection)
        {
            var conn = await connection.GetConnectionAsync();

            // Create Users table
            const string createUsersTable = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id TEXT PRIMARY KEY,
                    DiscordId TEXT UNIQUE NOT NULL,
                    Username TEXT NOT NULL,
                    Nickname TEXT,
                    AvatarUrl TEXT,
                    JoinedAt DATETIME NOT NULL,
                    LastActive DATETIME NOT NULL,
                    CurrentPlayerId TEXT,
                    CreatedAt DATETIME NOT NULL,
                    UpdatedAt DATETIME NOT NULL,
                    SchemaVersion INTEGER NOT NULL DEFAULT 1
                )";

            await QueryUtil.ExecuteNonQueryAsync(conn, createUsersTable);

            // Create Players table
            const string createPlayersTable = @"
                CREATE TABLE IF NOT EXISTS Players (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    LastActive DATETIME NOT NULL,
                    TeamIds TEXT,
                    PreviousUserIds TEXT,
                    CreatedAt DATETIME NOT NULL,
                    UpdatedAt DATETIME NOT NULL,
                    IsArchived BOOLEAN NOT NULL,
                    ArchivedAt DATETIME,
                    SchemaVersion INTEGER NOT NULL DEFAULT 1
                )";

            await QueryUtil.ExecuteNonQueryAsync(conn, createPlayersTable);

            // Create UserPlayerHistory table
            const string createHistoryTable = @"
                CREATE TABLE IF NOT EXISTS UserPlayerHistory (
                    Id TEXT PRIMARY KEY,
                    UserId TEXT NOT NULL,
                    PlayerId TEXT NOT NULL,
                    LinkedAt DATETIME NOT NULL,
                    UnlinkedAt DATETIME,
                    Reason TEXT,
                    CreatedAt DATETIME NOT NULL,
                    UpdatedAt DATETIME NOT NULL,
                    FOREIGN KEY (UserId) REFERENCES Users(Id),
                    FOREIGN KEY (PlayerId) REFERENCES Players(Id)
                )";

            await QueryUtil.ExecuteNonQueryAsync(conn, createHistoryTable);

            // Create indexes
            const string createIndexes = @"
                CREATE INDEX IF NOT EXISTS idx_users_discord_id ON Users(DiscordId);
                CREATE INDEX IF NOT EXISTS idx_users_current_player ON Users(CurrentPlayerId);
                CREATE INDEX IF NOT EXISTS idx_players_name ON Players(Name);
                CREATE INDEX IF NOT EXISTS idx_players_last_active ON Players(LastActive);
                CREATE INDEX IF NOT EXISTS idx_user_player_history_user ON UserPlayerHistory(UserId);
                CREATE INDEX IF NOT EXISTS idx_user_player_history_player ON UserPlayerHistory(PlayerId);";

            await QueryUtil.ExecuteNonQueryAsync(conn, createIndexes);
        }

        public async Task DownAsync(IDatabaseConnection connection)
        {
            var conn = await connection.GetConnectionAsync();

            // Drop tables in reverse order
            const string dropTables = @"
                DROP TABLE IF EXISTS UserPlayerHistory;
                DROP TABLE IF EXISTS Players;
                DROP TABLE IF EXISTS Users;";

            await QueryUtil.ExecuteNonQueryAsync(conn, dropTables);
        }
    }
}