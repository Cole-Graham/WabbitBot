using System.Text.Json;
using WabbitBot.Common.Models;
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
                Console.WriteLine($"Error saving configuration: {ex.Message}");
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
                    Console.WriteLine("Failed to load current configuration");
                    return false;
                }

                // Update only the maps section
                currentConfig.Maps = mapsConfig;

                // Save the updated configuration
                return await SaveConfigurationAsync(currentConfig);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving maps configuration: {ex.Message}");
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
                    Console.WriteLine($"Configuration file not found at: {fullPath}");
                    return null;
                }

                var jsonString = await File.ReadAllTextAsync(fullPath);
                return JsonSerializer.Deserialize<BotOptions>(jsonString, JsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
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
                Console.WriteLine($"Error creating configuration backup: {ex.Message}");
                return null;
            }
        }
    }
}
