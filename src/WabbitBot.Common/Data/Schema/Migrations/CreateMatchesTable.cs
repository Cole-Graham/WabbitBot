using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Schema.Interface;
using WabbitBot.Common.Data.Utilities;

namespace WabbitBot.Common.Data.Schema.Migrations
{
    public class CreateMatchesTable : IMigration
    {
        public int Order => MigrationOrder.CreateMatches;
        public string Description => "Create initial Matches table";

        public async Task UpAsync(IDatabaseConnection connection)
        {
            var conn = await connection.GetConnectionAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Create Matches table
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE TABLE IF NOT EXISTS Matches (
                        Id TEXT PRIMARY KEY,
                        Team1Id TEXT NOT NULL,
                        Team2Id TEXT NOT NULL,
                        Team1PlayerIds TEXT NOT NULL,
                        Team2PlayerIds TEXT NOT NULL,
                        GameSize INTEGER NOT NULL,
                        CreatedAt DATETIME NOT NULL,
                        StartedAt DATETIME NULL,
                        CompletedAt DATETIME NULL,
                        WinnerId TEXT NULL,
                        Status INTEGER NOT NULL,
                        Stage INTEGER NOT NULL,
                        ParentId TEXT NULL,
                        ParentType TEXT NULL,
                        BestOf INTEGER NOT NULL DEFAULT 1,
                        PlayToCompletion BOOLEAN NOT NULL DEFAULT 0,
                        UpdatedAt DATETIME NOT NULL
                    )", new { }, transaction);

                // Create indexes for faster lookups
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_matches_team1id ON Matches(Team1Id)", new { }, transaction);
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_matches_team2id ON Matches(Team2Id)", new { }, transaction);
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_matches_parent ON Matches(ParentId, ParentType)", new { }, transaction);

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
                await QueryUtil.ExecuteNonQueryAsync(conn, "DROP TABLE IF EXISTS Matches", new { }, transaction);
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