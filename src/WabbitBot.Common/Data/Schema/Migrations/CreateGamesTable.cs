using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Schema.Interface;
using WabbitBot.Common.Data.Utilities;

namespace WabbitBot.Common.Data.Schema.Migrations
{
    public class CreateGamesTable : IMigration
    {
        public int Order => MigrationOrder.CreateGames;
        public string Description => "Create initial Games table";

        public async Task UpAsync(IDatabaseConnection connection)
        {
            var conn = await connection.GetConnectionAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Create Games table
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE TABLE IF NOT EXISTS Games (
                        Id TEXT PRIMARY KEY,
                        MatchId TEXT NOT NULL,
                        MapId TEXT NOT NULL,
                        GameSize INTEGER NOT NULL,
                        Team1PlayerIds TEXT NOT NULL,
                        Team2PlayerIds TEXT NOT NULL,
                        WinnerId TEXT NULL,
                        StartedAt DATETIME NOT NULL,
                        CompletedAt DATETIME NULL,
                        Status INTEGER NOT NULL,
                        GameNumber INTEGER NOT NULL,
                        CreatedAt DATETIME NOT NULL,
                        UpdatedAt DATETIME NOT NULL,
                        FOREIGN KEY (MatchId) REFERENCES Matches(Id)
                    )", new { }, transaction);

                // Create index on MatchId for faster lookups
                await QueryUtil.ExecuteNonQueryAsync(conn, @"
                    CREATE INDEX IF NOT EXISTS idx_games_matchid ON Games(MatchId)", new { }, transaction);

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
                await QueryUtil.ExecuteNonQueryAsync(conn, "DROP TABLE IF EXISTS Games", new { }, transaction);
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