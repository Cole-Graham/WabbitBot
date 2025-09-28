using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Schema.Interface;
using WabbitBot.Common.Data.Utilities;

namespace WabbitBot.Common.Data.Schema.Migrations
{
    public class CreateLeaderboardsTable : IMigration
    {
        public int Order => MigrationOrder.CreateLeaderboards;
        public string Description => "Create initial Leaderboards table";

        public async Task UpAsync(IDatabaseConnection connection)
        {
            var conn = await connection.GetConnectionAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Create Leaderboards table
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE TABLE IF NOT EXISTS Leaderboards (
                        Id TEXT PRIMARY KEY,
                        SeasonId TEXT NOT NULL,
                        TeamSize INTEGER NOT NULL,
                        InitialRating REAL NOT NULL,
                        KFactor INTEGER NOT NULL,
                        CreatedAt DATETIME NOT NULL,
                        UpdatedAt DATETIME NOT NULL,
                        SchemaVersion INTEGER NOT NULL DEFAULT 1
                    )", new { }, transaction);

                // Create indexes for faster lookups
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_leaderboards_gamesize ON Leaderboards(TeamSize)", new { }, transaction);

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
                await QueryUtil.ExecuteNonQueryAsync(conn, "DROP TABLE IF EXISTS Leaderboards", new { }, transaction);
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