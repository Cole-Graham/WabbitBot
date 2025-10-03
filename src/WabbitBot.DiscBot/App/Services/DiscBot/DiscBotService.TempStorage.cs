namespace WabbitBot.DiscBot.App.Services.DiscBot
{
    public static partial class DiscBotService
    {
        /// <summary>
        /// Manages temporary file storage for DiscBot attachment downloads.
        /// Files are stored temporarily until successfully ingested by Core, then cleaned up.
        /// </summary>
        public static class TempStorage
        {
            private static string _tempDirectory = string.Empty;
            private static bool _isInitialized;
            private static readonly object _lock = new();

            /// <summary>
            /// Initializes the temp directory under app base directory.
            /// </summary>
            /// <param name="subdirectory">Optional subdirectory name (defaults to "data/tmp/discord")</param>
            public static void Initialize(string? subdirectory = null)
            {
                lock (_lock)
                {
                    if (_isInitialized)
                    {
                        return;
                    }

                    subdirectory ??= Path.Combine("data", "tmp", "discord");
                    _tempDirectory = Path.Combine(AppContext.BaseDirectory, subdirectory);

                    // Ensure directory exists
                    if (!Directory.Exists(_tempDirectory))
                    {
                        Directory.CreateDirectory(_tempDirectory);
                    }

                    _isInitialized = true;
                }
            }

            /// <summary>
            /// Gets the temp directory path.
            /// Throws if not initialized.
            /// </summary>
            public static string GetTempDirectory()
            {
                lock (_lock)
                {
                    if (!_isInitialized)
                    {
                        throw new InvalidOperationException(
                            "TempStorage has not been initialized. Call Initialize() first.");
                    }

                    return _tempDirectory;
                }
            }

            /// <summary>
            /// Creates a temporary file path with a unique name.
            /// </summary>
            /// <param name="prefix">Optional prefix for the filename</param>
            /// <param name="extension">Optional file extension (e.g., ".jpg", ".png")</param>
            /// <returns>Full path to a unique temporary file</returns>
            public static string CreateTempFilePath(string? prefix = null, string? extension = null)
            {
                var tempDir = GetTempDirectory();
                var fileName = $"{prefix ?? "temp"}_{Guid.NewGuid():N}{extension ?? string.Empty}";
                return Path.Combine(tempDir, fileName);
            }

            /// <summary>
            /// Deletes a temporary file if it exists.
            /// Safe to call even if the file doesn't exist.
            /// </summary>
            /// <param name="filePath">Full path to the temporary file</param>
            /// <returns>True if the file was deleted, false if it didn't exist</returns>
            public static bool DeleteTempFile(string filePath)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        return true;
                    }
                    return false;
                }
                catch
                {
                    // Silently fail - cleanup is best-effort
                    return false;
                }
            }

            /// <summary>
            /// Cleans up old temporary files that exceed the specified age.
            /// This should be called periodically to prevent temp directory bloat.
            /// </summary>
            /// <param name="maxAge">Maximum age of files to keep (default: 1 hour)</param>
            /// <returns>Number of files deleted</returns>
            public static int CleanupOldFiles(TimeSpan? maxAge = null)
            {
                maxAge ??= TimeSpan.FromHours(1);
                var tempDir = GetTempDirectory();
                var filesDeleted = 0;

                try
                {
                    var cutoffTime = DateTime.UtcNow - maxAge.Value;
                    var files = Directory.GetFiles(tempDir);

                    foreach (var file in files)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            if (fileInfo.LastWriteTimeUtc < cutoffTime)
                            {
                                fileInfo.Delete();
                                filesDeleted++;
                            }
                        }
                        catch
                        {
                            // Skip files we can't delete
                            continue;
                        }
                    }
                }
                catch
                {
                    // If we can't read the directory, just return the count so far
                }

                return filesDeleted;
            }

            /// <summary>
            /// Starts a background cleanup task that runs periodically.
            /// </summary>
            /// <param name="interval">How often to run cleanup (default: 15 minutes)</param>
            /// <param name="maxAge">Maximum age of files to keep (default: 1 hour)</param>
            /// <returns>A task representing the background cleanup loop</returns>
            public static Task StartPeriodicCleanup(TimeSpan? interval = null, TimeSpan? maxAge = null)
            {
                interval ??= TimeSpan.FromMinutes(15);
                maxAge ??= TimeSpan.FromHours(1);

                return Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            await Task.Delay(interval.Value);
                            var deletedCount = CleanupOldFiles(maxAge);

                            // Log if any files were cleaned up
                            if (deletedCount > 0)
                            {
                                await DiscBotService.ErrorHandler.CaptureAsync(
                                    new InvalidOperationException($"Cleaned up {deletedCount} temp files"),
                                    $"Temp storage cleanup removed {deletedCount} old file(s)",
                                    nameof(StartPeriodicCleanup));
                            }
                        }
                        catch (Exception ex)
                        {
                            await DiscBotService.ErrorHandler.CaptureAsync(
                                ex,
                                "Temp storage cleanup failed",
                                nameof(StartPeriodicCleanup));
                        }
                    }
                });
            }
        }
    }
}
