using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;
using System.ComponentModel;
using WabbitBot.Core.Common.Configuration;
using WabbitBot.DiscBot.DSharpPlus.Embeds;
using WabbitBot.DiscBot.DSharpPlus.Generated;

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
            $"Server ID set to {ctx.Guild.Id} successfully" :
            (result.ErrorMessage ?? "Failed to set server ID"));
    }

    [Command("channel")]
    [Description("Set a channel ID")]
    public async Task SetChannelAsync(
        CommandContext ctx,
        [Description("Channel type (bot, replay, deck, signup, standings, scrimmage)")]
        [SlashChoiceProvider(typeof(ChannelTypeChoiceProvider))]
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
            $"{channelType} channel set successfully" :
            (result.ErrorMessage ?? $"Failed to set {channelType} channel"));
    }

    [Command("role")]
    [Description("Set a role ID")]
    public async Task SetRoleAsync(
        CommandContext ctx,
        [Description("Role type (whitelisted, admin, moderator)")]
        [SlashChoiceProvider(typeof(RoleTypeChoiceProvider))]
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
            $"{roleType} role set successfully" :
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

        var config = result.Data!;
        var embed = new ConfigurationEmbed();
        embed.SetConfiguration(config);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed.ToEmbedBuilder()));
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
        await writer.WriteAsync(result.Data!);
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
                "Configuration imported successfully" :
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
public class ChannelTypeChoiceProvider : global::DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers.IChoiceProvider
{
    public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter)
    {
        return await Task.FromResult(new[]
        {
            new DiscordApplicationCommandOptionChoice("Bot Channel", "bot"),
            new DiscordApplicationCommandOptionChoice("Replay Channel", "replay"),
            new DiscordApplicationCommandOptionChoice("Deck Channel", "deck"),
            new DiscordApplicationCommandOptionChoice("Signup Channel", "signup"),
            new DiscordApplicationCommandOptionChoice("Standings Channel", "standings"),
            new DiscordApplicationCommandOptionChoice("Scrimmage Channel", "scrimmage"),
        });
    }
}

/// <summary>
/// Choice provider for role types
/// </summary>
public class RoleTypeChoiceProvider : global::DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers.IChoiceProvider
{
    public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter)
    {
        return await Task.FromResult(new[]
        {
            new DiscordApplicationCommandOptionChoice("Whitelisted Role", "whitelisted"),
            new DiscordApplicationCommandOptionChoice("Admin Role", "admin"),
            new DiscordApplicationCommandOptionChoice("Moderator Role", "moderator"),
        });
    }
}