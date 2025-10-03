using System.Text.Json;
using WabbitBot.Common.Configuration;

namespace WabbitBot.Core.Common.Services;

/// <summary>
/// Main service for configuration management operations
/// </summary>
public class ConfigurationService
{
    /// <summary>
    /// Nested class containing the original ConfigurationPersistenceService functionality
    /// </summary>
    public static class Persistence
    {
        private static readonly string AppSettingsFile = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Saves the complete bot configuration to appsettings.json
        /// </summary>
        public static async Task<bool> SaveConfigurationAsync(BotOptions config)
        {
            try
            {
                var fullPath = Path.GetFullPath(AppSettingsFile);
                var directory = Path.GetDirectoryName(fullPath);

                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var jsonString = JsonSerializer.Serialize(config, JsonOptions);
                await File.WriteAllTextAsync(fullPath, jsonString);
                return true;
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(ex, "Error saving configuration", nameof(SaveConfigurationAsync));
                return false;
            }
        }

        /// <summary>
        /// Saves only the maps configuration section to appsettings.json
        /// </summary>
        public static async Task<bool> SaveMapsConfigurationAsync(MapsOptions mapsConfig)
        {
            try
            {
                // Load current configuration
                var currentConfig = await LoadConfigurationAsync();
                if (currentConfig == null)
                {
                    await CoreService.ErrorHandler.HandleAsync(new WabbitBot.Common.ErrorService.ErrorContext("Failed to load current configuration", WabbitBot.Common.ErrorService.ErrorSeverity.Warning, nameof(SaveMapsConfigurationAsync)), WabbitBot.Common.ErrorService.ErrorComponent.Logging);
                    return false;
                }

                // Update only the maps section
                currentConfig.Maps = mapsConfig;

                // Save the updated configuration
                return await SaveConfigurationAsync(currentConfig);
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(ex, "Error saving maps configuration", nameof(SaveMapsConfigurationAsync));
                return false;
            }
        }

        /// <summary>
        /// Loads the current configuration from appsettings.json
        /// </summary>
        public static async Task<BotOptions?> LoadConfigurationAsync()
        {
            try
            {
                var fullPath = Path.GetFullPath(AppSettingsFile);
                if (!File.Exists(fullPath))
                {
                    await CoreService.ErrorHandler.HandleAsync(new WabbitBot.Common.ErrorService.ErrorContext($"Configuration file not found at: {fullPath}", WabbitBot.Common.ErrorService.ErrorSeverity.Warning, nameof(LoadConfigurationAsync)), WabbitBot.Common.ErrorService.ErrorComponent.Logging);
                    return null;
                }

                var jsonString = await File.ReadAllTextAsync(fullPath);
                return JsonSerializer.Deserialize<BotOptions>(jsonString, JsonOptions);
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(ex, "Error loading configuration", nameof(LoadConfigurationAsync));
                return null;
            }
        }

        /// <summary>
        /// Creates a backup of the current configuration file
        /// </summary>
        public static string? CreateBackup()
        {
            try
            {
                var fullPath = Path.GetFullPath(AppSettingsFile);
                if (!File.Exists(fullPath))
                {
                    return null;
                }

                var backupPath = $"{fullPath}.backup.{DateTime.UtcNow:yyyyMMddHHmmss}";
                File.Copy(fullPath, backupPath);
                return backupPath;
            }
            catch (Exception ex)
            {
                CoreService.ErrorHandler.CaptureAsync(ex, "Error creating configuration backup", nameof(CreateBackup)).GetAwaiter().GetResult();
                return null;
            }
        }
    }
}
