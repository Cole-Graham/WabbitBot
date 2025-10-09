using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Schema.Interface;
using WabbitBot.Common.Data.Utilities;

namespace WabbitBot.Common.Data.Schema.Migrations
{
    public class CreateTournamentStateSnapshotsTable : IMigration
    {
        public int Order => MigrationOrder.CreateTournamentStateSnapshots;
        public string Description => "Create TournamentStateSnapshots table for event sourcing";

        public async Task UpAsync(IDatabaseConnection connection)
        {
            var conn = await connection.GetConnectionAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Create TournamentStateSnapshots table
                await QueryUtil.ExecuteNonQueryAsync(
                    conn,
                    @"
                    CREATE TABLE IF NOT EXISTS TournamentStateSnapshots (
                        Id TEXT PRIMARY KEY,
                        CreatedAt DATETIME NOT NULL,
                        UpdatedAt DATETIME NOT NULL,
                        SchemaVersion INTEGER NOT NULL DEFAULT 1,
                        -- Tournament reference
                        TournamentId TEXT NOT NULL,
                        Timestamp DATETIME NOT NULL,
                        UserId TEXT NOT NULL,
                        PlayerName TEXT NOT NULL,
                        AdditionalData TEXT NULL, -- JSON serialized
                        -- Tournament lifecycle properties
                        RegistrationOpenedAt DATETIME NULL,
                        StartedAt DATETIME NULL,
                        CompletedAt DATETIME NULL,
                        CancelledAt DATETIME NULL,
                        -- Tournament configuration properties
                        Name TEXT NOT NULL,
                        Description TEXT NOT NULL,
                        StartDate DATETIME NOT NULL,
                        MaxParticipants INTEGER NOT NULL,
                        -- Tournament status properties
                        WinnerTeamId TEXT NULL,
                        CancelledByUserId TEXT NULL,
                        CancellationReason TEXT NULL,
                        -- Tournament progression properties
                        RegisteredTeamIds TEXT NULL, -- JSON serialized list
                        ParticipantTeamIds TEXT NULL, -- JSON serialized list
                        ActiveMatchIds TEXT NULL, -- JSON serialized list
                        CompletedMatchIds TEXT NULL, -- JSON serialized list
                        AllMatchIds TEXT NULL, -- JSON serialized list
                        FinalRankings TEXT NULL, -- JSON serialized list
                        CurrentParticipantCount INTEGER NOT NULL DEFAULT 0,
                        CurrentRound INTEGER NOT NULL DEFAULT 1,
                        FOREIGN KEY (TournamentId) REFERENCES Tournaments(Id) ON DELETE CASCADE
                    )",
                    new { },
                    transaction
                );

                // Create indexes for faster lookups
                await QueryUtil.ExecuteNonQueryAsync(
                    conn,
                    @"
                    CREATE INDEX IF NOT EXISTS idx_tournamentstatesnapshots_tournamentid ON TournamentStateSnapshots(TournamentId)",
                    new { },
                    transaction
                );
                await QueryUtil.ExecuteNonQueryAsync(
                    conn,
                    @"
                    CREATE INDEX IF NOT EXISTS idx_tournamentstatesnapshots_timestamp ON TournamentStateSnapshots(Timestamp)",
                    new { },
                    transaction
                );
                await QueryUtil.ExecuteNonQueryAsync(
                    conn,
                    @"
                    CREATE INDEX IF NOT EXISTS idx_tournamentstatesnapshots_userid ON TournamentStateSnapshots(UserId)",
                    new { },
                    transaction
                );
                await QueryUtil.ExecuteNonQueryAsync(
                    conn,
                    @"
                    CREATE INDEX IF NOT EXISTS idx_tournamentstatesnapshots_createdat ON TournamentStateSnapshots(CreatedAt)",
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
                await QueryUtil.ExecuteNonQueryAsync(
                    conn,
                    "DROP TABLE IF EXISTS TournamentStateSnapshots",
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
    }
}
