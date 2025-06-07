using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Schema.Interface;
using WabbitBot.Common.Data.Utilities;

namespace WabbitBot.Common.Data.Schema.Migrations
{
    public class CreateScrimmagesTable : IMigration
    {
        public int Order => MigrationOrder.CreateScrimmages;
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
                        GameSize INTEGER NOT NULL,
                        CreatedAt DATETIME NOT NULL,
                        StartedAt DATETIME NULL,
                        CompletedAt DATETIME NULL,
                        WinnerId TEXT NULL,
                        Status INTEGER NOT NULL,
                        Team1Rating INTEGER NOT NULL,
                        Team2Rating INTEGER NOT NULL,
                        RatingChange INTEGER NOT NULL,
                        RatingMultiplier REAL NOT NULL,
                        ChallengeExpiresAt DATETIME NULL,
                        IsAccepted BOOLEAN NOT NULL,
                        BestOf INTEGER NOT NULL DEFAULT 1,
                        CreatedAt DATETIME NOT NULL,
                        UpdatedAt DATETIME NOT NULL
                    )", new { }, transaction);

                // Create indexes for faster lookups
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_scrimmages_team1id ON Scrimmages(Team1Id)", new { }, transaction);
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_scrimmages_team2id ON Scrimmages(Team2Id)", new { }, transaction);
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_scrimmages_status ON Scrimmages(Status)", new { }, transaction);

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