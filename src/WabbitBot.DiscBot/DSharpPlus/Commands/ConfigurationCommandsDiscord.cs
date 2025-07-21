using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using WabbitBot.Common.Attributes;
using WabbitBot.DiscBot.DiscBot.Commands;

namespace WabbitBot.DiscBot.DSharpPlus.Commands;

/// <summary>
/// Discord integration for configuration commands - handles Discord-specific code and calls business logic
/// </summary>
[Command("Config")]
[Description("Configuration management commands")]
[RequirePermissions(DiscordPermission.Administrator)]
public partial class ConfigurationCommandsDiscord
{
    private static readonly ConfigurationCommands ConfigurationCommands = new();

    #region Discord Command Handlers

    [Command("server")]
    [Description("Set the server ID to the current server")]
    public async Task SetServerIdAsync(CommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        if (ctx.Guild is null)
        {
            await ctx.EditResponseAsync("This command can only be used in a server.");
            return;
        }

        // Call business logic with the current guild ID
        var result = await ConfigurationCommands.SetServerIdAsync(ctx.Guild.Id);

        await ctx.EditResponseAsync(result.Success ?
            (result.Message ?? $"Server ID set to {ctx.Guild.Id} successfully") :
            (result.ErrorMessage ?? "Failed to set server ID"));
    }

    [Command("channel")]
    [Description("Set a channel ID")]
    public async Task SetChannelAsync(
        CommandContext ctx,
        [Description("Channel type (bot, replay, deck, signup, standings, scrimmage)")]
        [ChoiceProvider(typeof(ChannelTypeChoiceProvider))]
        string channelType,
        [Description("Channel ID")] ulong channelId)
    {
        await ctx.DeferResponseAsync();

        // Validate that the channel exists in the current guild
        if (ctx.Guild?.Channels.Values.FirstOrDefault(c => c.Id == channelId) is not DiscordChannel channel)
        {
            await ctx.EditResponseAsync($"Channel with ID {channelId} not found in this server.");
            return;
        }

        // Call business logic
        var result = await ConfigurationCommands.SetChannelAsync(channelType, channelId);

        await ctx.EditResponseAsync(result.Success ?
            (result.Message ?? $"{channelType} channel set successfully") :
            (result.ErrorMessage ?? $"Failed to set {channelType} channel"));
    }

    [Command("role")]
    [Description("Set a role ID")]
    public async Task SetRoleAsync(
        CommandContext ctx,
        [Description("Role type (whitelisted, admin, moderator)")]
        [ChoiceProvider(typeof(RoleTypeChoiceProvider))]
        string roleType,
        [Description("Role ID")] ulong roleId)
    {
        await ctx.DeferResponseAsync();

        // Validate that the role exists in the current guild
        if (ctx.Guild?.Roles.Values.FirstOrDefault(r => r.Id == roleId) is not DiscordRole role)
        {
            await ctx.EditResponseAsync($"Role with ID {roleId} not found in this server.");
            return;
        }

        // Call business logic
        var result = await ConfigurationCommands.SetRoleAsync(roleType, roleId);

        await ctx.EditResponseAsync(result.Success ?
            (result.Message ?? $"{roleType} role set successfully") :
            (result.ErrorMessage ?? $"Failed to set {roleType} role"));
    }

    [Command("show")]
    [Description("Show current configuration")]
    public async Task ShowConfigAsync(CommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        // Call business logic
        var result = ConfigurationCommands.GetConfiguration();

        if (!result.Success)
        {
            await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to get configuration");
            return;
        }

        var config = result.Configuration!;
        var embed = new DiscordEmbedBuilder()
            .WithTitle("Bot Configuration")
            .WithColor(DiscordColor.Blue)
            .AddField("Server ID", config.ServerId?.ToString() ?? "Not set", true)
            .AddField("Bot Channel", config.Channels.BotChannel?.ToString() ?? "Not set", true)
            .AddField("Replay Channel", config.Channels.ReplayChannel?.ToString() ?? "Not set", true)
            .AddField("Deck Channel", config.Channels.DeckChannel?.ToString() ?? "Not set", true)
            .AddField("Signup Channel", config.Channels.SignupChannel?.ToString() ?? "Not set", true)
            .AddField("Standings Channel", config.Channels.StandingsChannel?.ToString() ?? "Not set", true)
            .AddField("Scrimmage Channel", config.Channels.ScrimmageChannel?.ToString() ?? "Not set", true)
            .AddField("Whitelisted Role", config.Roles.Whitelisted?.ToString() ?? "Not set", true)
            .AddField("Admin Role", config.Roles.Admin?.ToString() ?? "Not set", true)
            .AddField("Moderator Role", config.Roles.Moderator?.ToString() ?? "Not set", true);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    [Command("export")]
    [Description("Export current configuration")]
    public async Task ExportConfigAsync(CommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        // Call business logic
        var result = ConfigurationCommands.ExportConfiguration();

        if (!result.Success)
        {
            await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to export configuration");
            return;
        }

        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(result.Message!);
        await writer.FlushAsync();
        stream.Position = 0;

        var message = new DiscordWebhookBuilder()
            .WithContent("Current configuration:")
            .AddFile("config.json", stream);

        await ctx.EditResponseAsync(message);
    }

    [Command("import")]
    [Description("Import configuration from a JSON file")]
    public async Task ImportConfigAsync(
        CommandContext ctx,
        [Description("The config.json file to import")]
        DiscordAttachment attachment)
    {
        await ctx.DeferResponseAsync();

        try
        {
            using var client = new HttpClient();
            var json = await client.GetStringAsync(attachment.Url);

            // Call business logic
            var result = await ConfigurationCommands.ImportConfigurationAsync(json);

            await ctx.EditResponseAsync(result.Success ?
                (result.Message ?? "Configuration imported successfully") :
                (result.ErrorMessage ?? "Failed to import configuration"));
        }
        catch (Exception ex)
        {
            await ctx.EditResponseAsync($"Error importing configuration: {ex.Message}");
        }
    }

    #endregion
}

/// <summary>
/// Choice provider for channel types
/// </summary>
public class ChannelTypeChoiceProvider : IChoiceProvider
{
    public IEnumerable<CommandChoice> GetChoices()
    {
        return new[]
        {
            new CommandChoice("Bot Channel", "bot"),
            new CommandChoice("Replay Channel", "replay"),
            new CommandChoice("Deck Channel", "deck"),
            new CommandChoice("Signup Channel", "signup"),
            new CommandChoice("Standings Channel", "standings"),
            new CommandChoice("Scrimmage Channel", "scrimmage"),
        };
    }
}

/// <summary>
/// Choice provider for role types
/// </summary>
public class RoleTypeChoiceProvider : IChoiceProvider
{
    public IEnumerable<CommandChoice> GetChoices()
    {
        return new[]
        {
            new CommandChoice("Whitelisted Role", "whitelisted"),
            new CommandChoice("Admin Role", "admin"),
            new CommandChoice("Moderator Role", "moderator"),
        };
    }
}