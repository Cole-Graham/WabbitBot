using System;
using System.Threading;
using System.Threading.Tasks;
using WabbitBot.Core.Common.Database;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.Core.Common.Utilities
{
    /// <summary>
    /// Background service to monitor version compatibility and alert on drift.
    /// Runs periodically to ensure application and database schema remain compatible.
    /// </summary>
    public class VersionMonitor
    {
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Starts the version monitoring service.
        /// Runs continuously until cancellation is requested.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to stop the monitor</param>
        public async Task StartAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckVersionDriftAsync();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        ex,
                        "Error checking version compatibility",
                        nameof(CheckVersionDriftAsync)
                    );

                    // Wait a shorter interval before retrying after an error
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        /// <summary>
        /// Checks for version drift between application and database schema.
        /// Logs an error if incompatibility is detected.
        /// </summary>
        private async Task CheckVersionDriftAsync()
        {
            await using var dbContext = WabbitBotDbContextProvider.CreateDbContext();
            var schemaTracker = new SchemaVersionTracker(dbContext);
            var appVersion = ApplicationInfo.VersionString;
            var schemaVersion = await schemaTracker.GetCurrentSchemaVersionAsync();

            if (!ApplicationInfo.IsCompatibleWithSchema(schemaVersion))
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    new IncompatibleVersionException(
                        $"Version drift detected: App {appVersion} vs Schema {schemaVersion}"
                    ),
                    $"Version drift detected: App {appVersion} vs Schema {schemaVersion}",
                    nameof(CheckVersionDriftAsync)
                );
            }
            // Version OK - no action needed (logging would be too verbose for this check)
        }
    }
}
