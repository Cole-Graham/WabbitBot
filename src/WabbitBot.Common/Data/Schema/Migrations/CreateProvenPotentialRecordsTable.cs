using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Schema.Interface;
using WabbitBot.Common.Data.Utilities;

namespace WabbitBot.Common.Data.Schema
{
    public class CreateProvenPotentialRecordsTable : IMigration
    {
        public int Order => MigrationOrder.CreateProvenPotentialRecords;
        public string Description => "Create ProvenPotentialRecords table for tracking proven potential adjustments";

        public async Task UpAsync(IDatabaseConnection connection)
        {
            var conn = await connection.GetConnectionAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Create ProvenPotentialRecords table
                await QueryUtil.ExecuteNonQueryAsync(
                    conn,
                    @"
                    CREATE TABLE IF NOT EXISTS ProvenPotentialRecords (
                        Id TEXT PRIMARY KEY,
                        OriginalMatchId TEXT NOT NULL,
                        ChallengerId TEXT NOT NULL,
                        OpponentId TEXT NOT NULL,
                        ChallengerRating REAL NOT NULL,
                        OpponentRating REAL NOT NULL,
                        ChallengerConfidence REAL NOT NULL,
                        OpponentConfidence REAL NOT NULL,
                        AppliedThresholds TEXT NOT NULL,
                        ChallengerOriginalRatingChange REAL NOT NULL,
                        OpponentOriginalRatingChange REAL NOT NULL,
                        RatingAdjustment REAL NOT NULL,
                        TeamSize INTEGER NOT NULL,
                        LastCheckedAt DATETIME,
                        IsComplete BOOLEAN NOT NULL,
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
                    CREATE INDEX IF NOT EXISTS idx_proven_potential_challenger ON ProvenPotentialRecords(ChallengerId)",
                    new { },
                    transaction
                );
                await QueryUtil.ExecuteNonQueryAsync(
                    conn,
                    @"
                    CREATE INDEX IF NOT EXISTS idx_proven_potential_opponent ON ProvenPotentialRecords(OpponentId)",
                    new { },
                    transaction
                );
                await QueryUtil.ExecuteNonQueryAsync(
                    conn,
                    @"
                    CREATE INDEX IF NOT EXISTS idx_proven_potential_match ON ProvenPotentialRecords(OriginalMatchId)",
                    new { },
                    transaction
                );
                await QueryUtil.ExecuteNonQueryAsync(
                    conn,
                    @"
                    CREATE INDEX IF NOT EXISTS idx_proven_potential_complete ON ProvenPotentialRecords(IsComplete)",
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
                    "DROP TABLE IF EXISTS ProvenPotentialRecords",
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
