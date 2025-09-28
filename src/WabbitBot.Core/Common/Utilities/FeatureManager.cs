using System;
using System.Threading.Tasks;

namespace WabbitBot.Core.Common.Utilities
{
    public class FeatureManager
    {
        private readonly SchemaVersionTracker _schemaTracker;

        public FeatureManager(SchemaVersionTracker schemaTracker)
        {
            _schemaTracker = schemaTracker;
        }

        public async Task<bool> IsNewLeaderboardEnabledAsync()
        {
            var appVersion = ApplicationInfo.CurrentVersion;
            var schemaVersionString = await _schemaTracker.GetCurrentSchemaVersionAsync();
            var schemaVersion = new Version(schemaVersionString.Split('-')[1]); // Assumes "001-1.0" format

            return appVersion >= new Version("1.2.0") &&
                   schemaVersion >= new Version("2.0");
        }

        public async Task<bool> UseLegacyStatsFormatAsync()
        {
            var schemaVersionString = await _schemaTracker.GetCurrentSchemaVersionAsync();
            var schemaVersion = new Version(schemaVersionString.Split('-')[1]); // Assumes "001-1.0" format

            return schemaVersion < new Version("2.0");
        }

        public async Task<bool> IsAdvancedReportingEnabledAsync()
        {
            var appVersion = ApplicationInfo.CurrentVersion;
            var schemaVersionString = await _schemaTracker.GetCurrentSchemaVersionAsync();
            var schemaVersion = new Version(schemaVersionString.Split('-')[1]); // Assumes "001-1.0" format

            return appVersion >= new Version("1.3.0") &&
                   schemaVersion >= new Version("3.0");
        }
    }
}
