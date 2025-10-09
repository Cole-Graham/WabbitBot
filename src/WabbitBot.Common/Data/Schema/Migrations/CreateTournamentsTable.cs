using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Schema.Interface;
using WabbitBot.Common.Data.Utilities;

namespace WabbitBot.Common.Data.Schema.Migrations
{
    public class CreateTournamentsTable : IMigration
    {
        public int Order => MigrationOrder.CreateTournaments;
        public string Description => "Create initial Tournaments table";

        public async Task UpAsync(IDatabaseConnection connection)
        {
            var conn = await connection.GetConnectionAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Create Tournaments table
                await QueryUtil.ExecuteNonQueryAsync(
                    conn,
                    @"
                    CREATE TABLE IF NOT EXISTS Tournaments (
                        Id TEXT PRIMARY KEY,
                        Name TEXT NOT NULL,
                        Description TEXT NOT NULL,
                        TeamSize INTEGER NOT NULL,
                        StartDate DATETIME NOT NULL,
                        EndDate DATETIME NULL,
                        Status INTEGER NOT NULL,
                        MaxParticipants INTEGER NOT NULL,
                        BestOf INTEGER NOT NULL DEFAULT 1,
                        CreatedAt DATETIME NOT NULL,
                        UpdatedAt DATETIME NOT NULL,
                        SchemaVersion INTEGER NOT NULL DEFAULT 1
                    )",
                    new { },
                    transaction
                );

                // Create indexes for faster lookups
                await QueryUtil.ExecuteNonQueryAsync(
                    conn,
                    @"
                    CREATE INDEX IF NOT EXISTS idx_tournaments_status ON Tournaments(Status)",
                    new { },
                    transaction
                );
                await QueryUtil.ExecuteNonQueryAsync(
                    conn,
                    @"
                    CREATE INDEX IF NOT EXISTS idx_tournaments_startdate ON Tournaments(StartDate)",
                    new { },
                    transaction
                );

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
                await QueryUtil.ExecuteNonQueryAsync(conn, "DROP TABLE IF EXISTS Tournaments", new { }, transaction);
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
