using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;
using System.ComponentModel;
using WabbitBot.DiscBot.DiscBot.Commands;
using WabbitBot.Core.Common.Models;
using WabbitBot.DiscBot.DSharpPlus.Attributes;

namespace WabbitBot.DiscBot.DSharpPlus.Commands;

/// <summary>
/// Discord integration for scrimmage commands - handles Discord-specific code and calls business logic
/// </summary>
[Command("Scrimmage")]
[Description("Scrimmage management commands")]
public partial class ScrimmageCommandsDiscord
{
    private static readonly ScrimmageCommands ScrimmageCommands = new();

    #region Discord Command Handlers

    [Command("challenge")]
    [Description("Challenge another team to a scrimmage")]
    [RequireTeamCorePlayer("challengerTeam")]
    public async Task ChallengeAsync(
        CommandContext ctx,
        [Description("Game size")]
        [SlashChoiceProvider(typeof(GameSizeChoiceProvider))]
        string size,
        [Description("Your team name")]
        [SlashAutoCompleteProvider(typeof(DynamicTeamAutoCompleteProvider))]
        string challengerTeam,
        [Description("Opponent team name")]
        [SlashAutoCompleteProvider(typeof(DynamicTeamAutoCompleteProvider))]
        string opponent)
    {
        await ctx.DeferResponseAsync();

        // Parse game size
        if (!Helpers.TryParseGameSize(size, out var gameSize))
        {
            await ctx.EditResponseAsync($"Invalid game size: {size}");
            return;
        }

        // Call business logic - the business logic validates teams exist and have the same game size
        var result = await ScrimmageCommands.ChallengeAsync(challengerTeam, opponent, gameSize);

        if (!result.Success)
        {
            await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to create scrimmage challenge");
            return;
        }

        var embed = new DiscordEmbedBuilder()
            .WithTitle("🎯 Scrimmage Challenge Created")
            .WithDescription(result.Message ?? "Scrimmage challenge created")
            .WithColor(DiscordColor.Green)
            .AddField("Challenger", challengerTeam, true)
            .AddField("Opponent", opponent, true)
            .AddField("Game Size", size, true)
            .AddField("Challenge ID", result.Scrimmage?.Id.ToString() ?? "Unknown", true)
            .AddField("Expires", result.Scrimmage?.ChallengeExpiresAt?.ToString("g") ?? "Unknown", true)
            .WithFooter("The opponent team can accept or decline this challenge");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    #endregion


}