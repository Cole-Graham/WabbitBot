using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Core.Common.Database;

namespace WabbitBot.Core.Common.Utilities
{
    public class SchemaVersionTracker
    {
        private readonly WabbitBotDbContext _context;

        public SchemaVersionTracker(WabbitBotDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetCurrentSchemaVersionAsync()
        {
            var migrations = await _context.Database.GetAppliedMigrationsAsync();
            var latestMigration = migrations.OrderByDescending(m => m).FirstOrDefault();

            return ParseMigrationToSchemaVersion(latestMigration);
        }

        public async Task ValidateCompatibilityAsync()
        {
            var appVersion = ApplicationInfo.VersionString;
            var schemaVersion = await GetCurrentSchemaVersionAsync();

            if (!ApplicationInfo.IsCompatibleWithSchema(schemaVersion))
            {
                throw new IncompatibleVersionException(
                    $"Application version {appVersion} is incompatible with database schema version {schemaVersion}."
                );
            }
        }

        private string ParseMigrationToSchemaVersion(string? migrationName)
        {
            if (string.IsNullOrEmpty(migrationName))
                return "000-0.0";

            // TODO: Implement a real parsing logic based on migration naming convention.
            // For now, we'll just return a placeholder.
            // e.g., "20240101120000_AddPlayerStats" -> "001-1.2"
            return "001-1.0"; // Placeholder
        }
    }

    public class IncompatibleVersionException : Exception
    {
        public IncompatibleVersionException(string message)
            : base(message) { }
    }
}
