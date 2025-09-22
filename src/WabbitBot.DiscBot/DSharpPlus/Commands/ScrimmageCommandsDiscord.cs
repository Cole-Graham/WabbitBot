using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;
using System.ComponentModel;
using WabbitBot.Core.Scrimmages;
using WabbitBot.Core.Common.Models;
using WabbitBot.DiscBot.DSharpPlus.Attributes;
using WabbitBot.DiscBot.DSharpPlus.Embeds;

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
        [SlashChoiceProvider(typeof(EvenTeamFormatChoiceProvider))]
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
        if (!Game.Validation.TryParseEvenTeamFormat(size, out var evenTeamFormat))
        {
            await ctx.EditResponseAsync($"Invalid game size: {size}");
            return;
        }

        // Call business logic - the business logic validates teams exist and have the same game size
        var result = await ScrimmageCommands.ChallengeAsync(challengerTeam, opponent, evenTeamFormat);

        if (!result.Success)
        {
            await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to create scrimmage challenge");
            return;
        }

        // Create and post the challenge embed first
        var challengeEmbed = new ScrimmageEmbed();
        challengeEmbed.SetScrimmage(result.Scrimmage!, 1, result.ChallengerTeamName, result.OpponentTeamName);

        var embedBuilder = challengeEmbed.ToEmbedBuilder();

        // Add accept/decline buttons
        var acceptButton = new DiscordButtonComponent(DiscordButtonStyle.Success, $"accept_challenge_{result.Scrimmage!.Id}", "Accept Challenge");
        var declineButton = new DiscordButtonComponent(DiscordButtonStyle.Danger, $"decline_challenge_{result.Scrimmage!.Id}", "Decline Challenge");

        // Send message and create thread from it
        var message = await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder()
            .AddEmbed(embedBuilder)
            .AddActionRowComponent(acceptButton, declineButton));

        // Create thread from the message
        var threadName = $"{result.ChallengerTeamName} vs {result.OpponentTeamName}";
        var thread = await message.CreateThreadAsync(
            name: threadName,
            archiveAfter: DiscordAutoArchiveDuration.Day
        );

        await ctx.EditResponseAsync($"Challenge created! Thread: {thread.Mention}");
    }


    #endregion


}