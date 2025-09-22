using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Schema.Interface;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;

namespace WabbitBot.Common.Data.Schema.Migrations
{
    public class CreateScrimmagesTable : IMigration
    {
        public int Order => 1;
        public string Description => "Create initial Scrimmages table";

        public async Task UpAsync(IDatabaseConnection connection)
        {
            var conn = await connection.GetConnectionAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Create Scrimmages table
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE TABLE IF NOT EXISTS Scrimmages (
                        Id TEXT PRIMARY KEY,
                        Team1Id TEXT NOT NULL,
                        Team2Id TEXT NOT NULL,
                        Team1RosterIds TEXT NOT NULL,
                        Team2RosterIds TEXT NOT NULL,
                        EvenTeamFormat INTEGER NOT NULL,
                        StartedAt DATETIME,
                        CompletedAt DATETIME,
                        WinnerId TEXT,
                        Status INTEGER NOT NULL,
                        Team1Rating REAL NOT NULL,
                        Team2Rating REAL NOT NULL,
                        Team1RatingChange REAL NOT NULL,
                        Team2RatingChange REAL NOT NULL,
                        Team1Confidence REAL NOT NULL,
                        Team2Confidence REAL NOT NULL,
                        ChallengeExpiresAt DATETIME,
                        IsAccepted INTEGER NOT NULL,
                        BestOf INTEGER NOT NULL,
                        CreatedAt DATETIME NOT NULL,
                        UpdatedAt DATETIME NOT NULL,
                        SchemaVersion INTEGER NOT NULL DEFAULT 1
                    )", new { }, transaction);

                // Create indexes for faster lookups
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS IX_Scrimmages_Team1Id ON Scrimmages(Team1Id)", new { }, transaction);
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS IX_Scrimmages_Team2Id ON Scrimmages(Team2Id)", new { }, transaction);
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS IX_Scrimmages_Status ON Scrimmages(Status)", new { }, transaction);
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS IX_Scrimmages_CreatedAt ON Scrimmages(CreatedAt)", new { }, transaction);

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
                await QueryUtil.ExecuteNonQueryAsync(conn, "DROP TABLE IF EXISTS Scrimmages", new { }, transaction);
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