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
            var latest = await _context
                .SchemaMetadata.OrderByDescending(x => x.AppliedAt)
                .Select(x => x.SchemaVersion)
                .FirstOrDefaultAsync();
            return string.IsNullOrWhiteSpace(latest) ? "000-0.0" : latest!;
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

        // Migration-name parsing removed; source of truth is SchemaMetadata.
    }

    public class IncompatibleVersionException : Exception
    {
        public IncompatibleVersionException(string message)
            : base(message) { }
    }
}
