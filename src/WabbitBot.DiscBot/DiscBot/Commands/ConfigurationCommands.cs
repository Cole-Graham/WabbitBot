using System.Text.Json;
using WabbitBot.Common.Models;
using WabbitBot.Common.Data.Utilities;

namespace WabbitBot.DiscBot.DiscBot.Commands;

/// <summary>
/// Pure business logic for configuration commands - no Discord dependencies
/// </summary>
public class ConfigurationCommands
{
    private static readonly string ConfigFile = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "config.json");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    #region Business Logic Methods

    public ConfigurationResult GetConfiguration()
    {
        try
        {
            var fullPath = Path.GetFullPath(ConfigFile);
            if (!File.Exists(fullPath))
            {
                return new ConfigurationResult
                {
                    Success = false,
                    ErrorMessage = $"Configuration file not found at: {fullPath}"
                };
            }

            var jsonString = File.ReadAllText(fullPath);
            var config = JsonSerializer.Deserialize<BotConfiguration>(jsonString, JsonOptions);

            if (config == null)
            {
                return new ConfigurationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to deserialize configuration"
                };
            }

            return new ConfigurationResult
            {
                Success = true,
                Configuration = config
            };
        }
        catch (Exception ex)
        {
            return new ConfigurationResult
            {
                Success = false,
                ErrorMessage = $"Error reading configuration: {ex.Message}"
            };
        }
    }

    public async Task<ConfigurationResult> SetServerIdAsync(ulong serverId)
    {
        var result = GetConfiguration();
        if (!result.Success)
        {
            return result;
        }

        var config = result.Configuration!;
        var newConfig = config with { ServerId = serverId };

        return await SaveConfigurationAsync(newConfig);
    }

    public async Task<ConfigurationResult> SetChannelAsync(string channelType, ulong channelId)
    {
        var result = GetConfiguration();
        if (!result.Success)
        {
            return result;
        }

        var config = result.Configuration!;
        var newChannels = config.Channels with { };

        switch (channelType.ToLowerInvariant())
        {
            case "bot":
                newChannels = newChannels with { BotChannel = channelId };
                break;
            case "replay":
                newChannels = newChannels with { ReplayChannel = channelId };
                break;
            case "deck":
                newChannels = newChannels with { DeckChannel = channelId };
                break;
            case "signup":
                newChannels = newChannels with { SignupChannel = channelId };
                break;
            case "standings":
                newChannels = newChannels with { StandingsChannel = channelId };
                break;
            case "scrimmage":
                newChannels = newChannels with { ScrimmageChannel = channelId };
                break;
            default:
                return new ConfigurationResult
                {
                    Success = false,
                    ErrorMessage = $"Unknown channel type: {channelType}. Valid types: bot, replay, deck, signup, standings, scrimmage"
                };
        }

        var newConfig = config with { Channels = newChannels };
        return await SaveConfigurationAsync(newConfig);
    }

    public async Task<ConfigurationResult> SetRoleAsync(string roleType, ulong roleId)
    {
        var result = GetConfiguration();
        if (!result.Success)
        {
            return result;
        }

        var config = result.Configuration!;
        var newRoles = config.Roles with { };

        switch (roleType.ToLowerInvariant())
        {
            case "whitelisted":
                newRoles = newRoles with { Whitelisted = roleId };
                break;
            case "admin":
                newRoles = newRoles with { Admin = roleId };
                break;
            case "moderator":
                newRoles = newRoles with { Moderator = roleId };
                break;
            default:
                return new ConfigurationResult
                {
                    Success = false,
                    ErrorMessage = $"Unknown role type: {roleType}. Valid types: whitelisted, admin, moderator"
                };
        }

        var newConfig = config with { Roles = newRoles };
        return await SaveConfigurationAsync(newConfig);
    }

    public ConfigurationResult ExportConfiguration()
    {
        var result = GetConfiguration();
        if (!result.Success)
        {
            return result;
        }

        try
        {
            var json = JsonSerializer.Serialize(result.Configuration, JsonOptions);
            return new ConfigurationResult
            {
                Success = true,
                Message = json
            };
        }
        catch (Exception ex)
        {
            return new ConfigurationResult
            {
                Success = false,
                ErrorMessage = $"Error exporting configuration: {ex.Message}"
            };
        }
    }

    public async Task<ConfigurationResult> ImportConfigurationAsync(string json)
    {
        try
        {
            var config = JsonSerializer.Deserialize<BotConfiguration>(json, JsonOptions);
            if (config == null)
            {
                return new ConfigurationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to deserialize configuration JSON"
                };
            }

            return await SaveConfigurationAsync(config);
        }
        catch (Exception ex)
        {
            return new ConfigurationResult
            {
                Success = false,
                ErrorMessage = $"Error importing configuration: {ex.Message}"
            };
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task<ConfigurationResult> SaveConfigurationAsync(BotConfiguration config)
    {
        try
        {
            var fullPath = Path.GetFullPath(ConfigFile);
            var directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var jsonString = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(fullPath, jsonString);

            return new ConfigurationResult
            {
                Success = true,
                Message = "Configuration saved successfully",
                Configuration = config
            };
        }
        catch (Exception ex)
        {
            return new ConfigurationResult
            {
                Success = false,
                ErrorMessage = $"Error saving configuration: {ex.Message}"
            };
        }
    }

    #endregion

    #region Result Classes

    public class ConfigurationResult
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
        public string? ErrorMessage { get; init; }
        public BotConfiguration? Configuration { get; init; }
    }

    #endregion
}