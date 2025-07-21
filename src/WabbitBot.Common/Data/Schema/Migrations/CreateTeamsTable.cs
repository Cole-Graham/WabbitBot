using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Schema.Interface;
using WabbitBot.Common.Data.Utilities;

namespace WabbitBot.Common.Data.Schema
{
    public class CreateTeamsTable : IMigration
    {
        public int Order => MigrationOrder.CreateTeams;
        public string Description => "Create initial Teams table";

        public async Task UpAsync(IDatabaseConnection connection)
        {
            var conn = await connection.GetConnectionAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Create Teams table
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE TABLE IF NOT EXISTS Teams (
                        Id TEXT PRIMARY KEY,
                        Name TEXT NOT NULL,
                        TeamCaptainId TEXT NOT NULL,
                        TeamSize INTEGER NOT NULL,
                        MaxRosterSize INTEGER NOT NULL,
                        Roster TEXT NOT NULL,
                        LastActive DATETIME NOT NULL,
                        Tag TEXT,
                        IsArchived BOOLEAN NOT NULL,
                        ArchivedAt DATETIME,
                        CreatedAt DATETIME NOT NULL,
                        UpdatedAt DATETIME NOT NULL
                    )", new { }, transaction);

                // Create indexes for faster lookups
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_teams_name ON Teams(Name)", new { }, transaction);
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_teams_captain ON Teams(TeamCaptainId)", new { }, transaction);
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_teams_last_active ON Teams(LastActive)", new { }, transaction);

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
                await QueryUtil.ExecuteNonQueryAsync(conn, "DROP TABLE IF EXISTS Teams", new { }, transaction);
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