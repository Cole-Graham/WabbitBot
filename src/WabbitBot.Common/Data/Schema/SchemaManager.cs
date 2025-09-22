using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Schema.Interface;
using WabbitBot.Common.Data.Utilities;

namespace WabbitBot.Common.Data.Schema
{
    /// <summary>
    /// Defines the order in which migrations should be applied.
    /// Each constant represents a step in the database's evolution.
    /// The numbers are sequential across all migrations and represent the database version.
    /// </summary>
    public static class MigrationOrder
    {
        public const int CreateGames = 1;
        public const int CreateLeaderboards = 2;
        public const int CreateMaps = 3;
        public const int CreateMatchStateSnapshots = 4;
        public const int CreateMatches = 5;
        public const int CreatePlayers = 6;
        public const int CreateProvenPotentialRecords = 7;
        public const int CreateScrimmages = 8;
        public const int CreateSeasons = 9;
        public const int CreateTeams = 10;
        public const int CreateTournamentStateSnapshots = 11;
        public const int CreateTournaments = 12;
    }

    public class SchemaManager
    {
        private readonly IDatabaseConnection _connection;
        private readonly string _schemaVersionTable = "SchemaVersion";
        private readonly IMigration[] _migrations;

        public SchemaManager(IDatabaseConnection connection, IMigration[] migrations)
        {
            _connection = connection;
            _migrations = migrations;
        }

        public async Task InitializeSchemaAsync()
        {
            await CreateSchemaVersionTableAsync();
            await ApplyMigrationsAsync();
        }

        private async Task CreateSchemaVersionTableAsync()
        {
            var sql = $@"
                CREATE TABLE IF NOT EXISTS {_schemaVersionTable} (
                    Version INTEGER PRIMARY KEY,
                    AppliedAt DATETIME NOT NULL,
                    Description TEXT NOT NULL
                )";

            await QueryUtil.ExecuteNonQueryAsync(
                await _connection.GetConnectionAsync(),
                sql
            );
        }

        private async Task ApplyMigrationsAsync()
        {
            var currentVersion = await GetCurrentVersionAsync();

            foreach (var migration in _migrations)
            {
                if (migration.Order > currentVersion)
                {
                    await migration.UpAsync(_connection);
                    await RecordMigrationAsync(migration);
                }
            }
        }

        private async Task<int> GetCurrentVersionAsync()
        {
            var sql = $"SELECT COALESCE(MAX(Version), 0) FROM {_schemaVersionTable}";
            var result = await QueryUtil.ExecuteScalarAsync<int>(
                await _connection.GetConnectionAsync(),
                sql
            );
            return result;
        }

        private async Task RecordMigrationAsync(IMigration migration)
        {
            var sql = $@"
                INSERT INTO {_schemaVersionTable} (Version, AppliedAt, Description)
                VALUES (@Version, @AppliedAt, @Description)";

            await QueryUtil.ExecuteNonQueryAsync(
                await _connection.GetConnectionAsync(),
                sql,
                new
                {
                    migration.Order,
                    AppliedAt = DateTime.UtcNow,
                    migration.Description
                }
            );
        }
    }
}