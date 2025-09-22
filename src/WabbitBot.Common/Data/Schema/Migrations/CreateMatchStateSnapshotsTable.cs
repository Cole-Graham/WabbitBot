using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Schema.Interface;
using WabbitBot.Common.Data.Utilities;

namespace WabbitBot.Common.Data.Schema.Migrations
{
    public class CreateMatchStateSnapshotsTable : IMigration
    {
        public int Order => MigrationOrder.CreateMatchStateSnapshots;
        public string Description => "Create MatchStateSnapshots table for event sourcing";

        public async Task UpAsync(IDatabaseConnection connection)
        {
            var conn = await connection.GetConnectionAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Create MatchStateSnapshots table
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE TABLE IF NOT EXISTS MatchStateSnapshots (
                        Id TEXT PRIMARY KEY,
                        CreatedAt DATETIME NOT NULL,
                        UpdatedAt DATETIME NOT NULL,
                        SchemaVersion INTEGER NOT NULL DEFAULT 1,
                        -- Match reference
                        MatchId TEXT NOT NULL,
                        Timestamp DATETIME NOT NULL,
                        UserId TEXT NOT NULL,
                        PlayerName TEXT NOT NULL,
                        AdditionalData TEXT NULL, -- JSON serialized
                        -- Match lifecycle properties
                        StartedAt DATETIME NULL,
                        CompletedAt DATETIME NULL,
                        CancelledAt DATETIME NULL,
                        ForfeitedAt DATETIME NULL,
                        -- Match status properties
                        WinnerId TEXT NULL,
                        CancelledByUserId TEXT NULL,
                        ForfeitedByUserId TEXT NULL,
                        ForfeitedTeamId TEXT NULL,
                        CancellationReason TEXT NULL,
                        ForfeitReason TEXT NULL,
                        -- Game progression properties
                        CurrentGameNumber INTEGER NOT NULL DEFAULT 1,
                        CurrentMapId TEXT NULL,
                        -- Final match results
                        FinalScore TEXT NULL,
                        -- Map ban state properties
                        AvailableMaps TEXT NULL, -- JSON serialized list
                        Team1MapBans TEXT NULL, -- JSON serialized list
                        Team2MapBans TEXT NULL, -- JSON serialized list
                        Team1BansSubmitted BOOLEAN NOT NULL DEFAULT 0,
                        Team2BansSubmitted BOOLEAN NOT NULL DEFAULT 0,
                        Team1BansConfirmed BOOLEAN NOT NULL DEFAULT 0,
                        Team2BansConfirmed BOOLEAN NOT NULL DEFAULT 0,
                        FinalMapPool TEXT NULL, -- JSON serialized list
                        FOREIGN KEY (MatchId) REFERENCES Matches(Id) ON DELETE CASCADE
                    )", new { }, transaction);

                // Create indexes for faster lookups
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_matchstatesnapshots_matchid ON MatchStateSnapshots(MatchId)", new { }, transaction);
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_matchstatesnapshots_timestamp ON MatchStateSnapshots(Timestamp)", new { }, transaction);
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_matchstatesnapshots_userid ON MatchStateSnapshots(UserId)", new { }, transaction);
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_matchstatesnapshots_createdat ON MatchStateSnapshots(CreatedAt)", new { }, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task DownAsync(IDatabaseConnection connection)
        {
            var conn = await connection.GetConnectionAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                await QueryUtil.ExecuteNonQueryAsync(conn, "DROP TABLE IF EXISTS MatchStateSnapshots", new { }, transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
