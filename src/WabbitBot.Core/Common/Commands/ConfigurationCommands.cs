using System.Text.Json;
using Microsoft.Extensions.Configuration;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Common.ErrorService;
using WabbitBot.Common.Events.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Events;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.Core.Common.Commands;

/// <summary>
/// Pure business logic for configuration commands - no Discord dependencies
/// </summary>
[WabbitCommand("Config")]
public partial class ConfigurationCommands
{
    private static readonly string AppSettingsFile = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    #region Business Logic Methods

    public Result<BotOptions> GetConfiguration()
    {
        try
        {
            var fullPath = Path.GetFullPath(AppSettingsFile);
            if (!File.Exists(fullPath))
            {
                return Result<BotOptions>.Failure($"Configuration file not found at: {fullPath}");
            }

            var jsonString = File.ReadAllText(fullPath);
            var config = JsonSerializer.Deserialize<BotOptions>(jsonString, JsonOptions);

            if (config == null)
            {
                return Result<BotOptions>.Failure("Failed to deserialize configuration");
            }

            return Result<BotOptions>.CreateSuccess(config);
        }
        catch (Exception ex)
        {
            return Result<BotOptions>.Failure($"Error reading configuration: {ex.Message}");
        }
    }

    public async Task<Result<BotOptions>> SetServerIdAsync(ulong serverId)
    {
        var result = GetConfiguration();
        if (!result.Success)
        {
            return result;
        }

        var config = result.Data!;
        var previousServerId = config.ServerId;
        config.ServerId = serverId;

        var saveResult = await SaveConfigurationAsync(config);

        if (saveResult.Success)
        {
            // Publish specific server ID set event (primitive-only payloads)
            await PublishServerIdSetAsync(serverId, previousServerId?.ToString() ?? "null");
        }

        return saveResult;
    }

    public async Task<Result<BotOptions>> SetChannelAsync(string channelType, ulong channelId)
    {
        var result = GetConfiguration();
        if (!result.Success)
        {
            return result;
        }

        var config = result.Data!;
        ulong? previousChannelId = null;

        switch (channelType.ToLowerInvariant())
        {
            case "mashina":
                previousChannelId = config.Channels.MashinaChannel;
                config.Channels.MashinaChannel = channelId;
                break;
            case "replay":
                previousChannelId = config.Channels.ReplayChannel;
                config.Channels.ReplayChannel = channelId;
                break;
            case "deck":
                previousChannelId = config.Channels.DeckChannel;
                config.Channels.DeckChannel = channelId;
                break;
            case "signup":
                previousChannelId = config.Channels.SignupChannel;
                config.Channels.SignupChannel = channelId;
                break;
            case "standings":
                previousChannelId = config.Channels.StandingsChannel;
                config.Channels.StandingsChannel = channelId;
                break;
            case "scrimmage":
                previousChannelId = config.Channels.ScrimmageChannel;
                config.Channels.ScrimmageChannel = channelId;
                break;
            case "challengefeed":
                previousChannelId = config.Channels.ChallengeFeedChannel;
                config.Channels.ChallengeFeedChannel = channelId;
                break;
            default:
                return Result<BotOptions>.Failure(
                    $"Unknown channel type: {channelType}. Valid types: bot, replay, deck, signup, standings, scrimmage"
                );
        }

        var saveResult = await SaveConfigurationAsync(config);

        if (saveResult.Success)
        {
            // Publish specific channel configured event (primitive-only payloads)
            await PublishChannelConfiguredAsync(channelType, channelId, previousChannelId);
        }

        return saveResult;
    }

    public async Task<Result<BotOptions>> SetRoleAsync(string roleType, ulong roleId)
    {
        var result = GetConfiguration();
        if (!result.Success)
        {
            return result;
        }

        var config = result.Data!;
        ulong? previousRoleId = null;

        switch (roleType.ToLowerInvariant())
        {
            case "whitelisted":
                previousRoleId = config.Roles.Whitelisted;
                config.Roles.Whitelisted = roleId;
                break;
            case "admin":
                previousRoleId = config.Roles.Admin;
                config.Roles.Admin = roleId;
                break;
            case "moderator":
                previousRoleId = config.Roles.Moderator;
                config.Roles.Moderator = roleId;
                break;
            default:
                return Result<BotOptions>.Failure(
                    $"Unknown role type: {roleType}. Valid types: whitelisted, admin, moderator"
                );
        }

        var saveResult = await SaveConfigurationAsync(config);

        if (saveResult.Success)
        {
            // Publish specific role configured event (primitive-only payloads)
            await PublishRoleConfiguredAsync(roleType, roleId, previousRoleId);
        }

        return saveResult;
    }

    public async Task<Result<BotOptions>> SetThreadInactivityThresholdAsync(int minutes)
    {
        if (minutes < 1 || minutes > 1440) // 1 minute to 24 hours
        {
            return Result<BotOptions>.Failure(
                "Thread inactivity threshold must be between 1 and 1440 minutes (24 hours)"
            );
        }

        var result = GetConfiguration();
        if (!result.Success)
        {
            return result;
        }

        var config = result.Data!;
        var previousThreshold = config.Threads.InactivityThresholdMinutes;
        config.Threads.InactivityThresholdMinutes = minutes;

        var saveResult = await SaveConfigurationAsync(config);

        if (saveResult.Success)
        {
            // Publish thread inactivity threshold configured event
            await PublishThreadInactivityThresholdConfiguredAsync(minutes, previousThreshold);
        }

        return saveResult;
    }

    public Result<string> ExportConfiguration()
    {
        var result = GetConfiguration();
        if (!result.Success)
        {
            return Result<string>.Failure(result.ErrorMessage!);
        }

        try
        {
            var json = JsonSerializer.Serialize(result.Data, JsonOptions);
            return Result<string>.CreateSuccess(json);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Error exporting configuration: {ex.Message}");
        }
    }

    public async Task<Result<BotOptions>> ImportConfigurationAsync(string json)
    {
        try
        {
            var config = JsonSerializer.Deserialize<BotOptions>(json, JsonOptions);
            if (config == null)
            {
                return Result<BotOptions>.Failure("Failed to deserialize configuration JSON");
            }

            return await SaveConfigurationAsync(config);
        }
        catch (Exception ex)
        {
            return Result<BotOptions>.Failure($"Error importing configuration: {ex.Message}");
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task<Result<BotOptions>> SaveConfigurationAsync(BotOptions config)
    {
        try
        {
            // Create backup before saving
            var backupPath = ConfigurationService.Persistence.CreateBackup();
            if (backupPath != null)
            {
                await WabbitBot.Core.Common.Services.CoreService.ErrorHandler.HandleAsync(
                    new ErrorContext(
                        $"Configuration backup created: {backupPath}",
                        ErrorSeverity.Information,
                        nameof(SaveConfigurationAsync)
                    ),
                    ErrorComponent.Logging
                );
            }

            // Save configuration using the persistence service
            var success = await ConfigurationService.Persistence.SaveConfigurationAsync(config);
            if (!success)
            {
                return Result<BotOptions>.Failure("Failed to save configuration to appsettings.json");
            }

            // Publish configuration changed event (primitive + simple types)
            await PublishConfigurationChangedAsync("Configuration saved");

            return Result<BotOptions>.CreateSuccess(config, "Configuration saved successfully");
        }
        catch (Exception ex)
        {
            return Result<BotOptions>.Failure($"Error saving configuration: {ex.Message}");
        }
    }

    #endregion
}
