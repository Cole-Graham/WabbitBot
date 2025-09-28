using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Schema.Interface;
using WabbitBot.Common.Data.Utilities;

namespace WabbitBot.Common.Data.Schema.Migrations
{
    public class CreateSeasonsTable : IMigration
    {
        public int Order => MigrationOrder.CreateSeasons;
        public string Description => "Create initial Seasons table";

        public async Task UpAsync(IDatabaseConnection connection)
        {
            var conn = await connection.GetConnectionAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Create Seasons table
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE TABLE IF NOT EXISTS Seasons (
                        Id TEXT PRIMARY KEY,
                        SeasonGroupId TEXT NOT NULL,
                        TeamSize INTEGER NOT NULL,
                        StartDate DATETIME NOT NULL,
                        EndDate DATETIME NOT NULL,
                        IsActive BOOLEAN NOT NULL,
                        TeamStats TEXT NOT NULL,
                        Config TEXT NOT NULL,
                        CreatedAt DATETIME NOT NULL,
                        UpdatedAt DATETIME NOT NULL,
                        SchemaVersion INTEGER NOT NULL DEFAULT 1
                    )", new { }, transaction);

                // Create indexes for faster lookups
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_seasons_seasongroupid ON Seasons(SeasonGroupId)", new { }, transaction);
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_seasons_gamesize ON Seasons(TeamSize)", new { }, transaction);
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_seasons_startdate ON Seasons(StartDate)", new { }, transaction);
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_seasons_enddate ON Seasons(EndDate)", new { }, transaction);
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_seasons_isactive ON Seasons(IsActive)", new { }, transaction);
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_seasons_isactive_gamesize ON Seasons(IsActive, TeamSize)", new { }, transaction);

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
                await QueryUtil.ExecuteNonQueryAsync(conn, "DROP TABLE IF EXISTS Seasons", new { }, transaction);
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