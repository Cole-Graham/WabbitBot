using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Services.DiscBot;

namespace WabbitBot.DiscBot.DSharpPlus.Commands;

/// <summary>
/// Discord slash commands for scrimmage management.
/// Translates Discord interactions into events via DiscBotService.PublishAsync.
/// Uses DSharpPlus.Commands only (not CommandsNext or SlashCommands).
/// </summary>
[Command("scrimmage")]
[Description("Scrimmage management commands")]
public partial class ScrimmageCommands
{
    /// <summary>
    /// Challenge another team to a scrimmage.
    /// Publishes ScrimmageChallengeRequested to Global event bus.
    /// </summary>
    [Command("challenge")]
    [Description("Challenge another team to a scrimmage")]
    public async Task ChallengeAsync(
        CommandContext ctx,
        [Description("Your team name")] string challengerTeam,
        [Description("Opponent team name")] string opponentTeam)
    {
        await ctx.DeferResponseAsync();

        try
        {
            // Light validation only; Core will validate teams exist and are compatible
            if (string.IsNullOrWhiteSpace(challengerTeam) || string.IsNullOrWhiteSpace(opponentTeam))
            {
                await ctx.EditResponseAsync("Both team names must be provided.");
                return;
            }

            if (string.Equals(challengerTeam, opponentTeam, StringComparison.OrdinalIgnoreCase))
            {
                await ctx.EditResponseAsync("You cannot challenge your own team.");
                return;
            }

            // Publish ScrimmageChallengeRequested (Global) to Core
            await DiscBotService.PublishAsync(new ScrimmageChallengeRequested(
                challengerTeam,
                opponentTeam,
                ctx.User.Id,
                ctx.Channel.Id));

            await ctx.EditResponseAsync($"Challenge request from **{challengerTeam}** to **{opponentTeam}** has been submitted. Core will validate and create the challenge.");
        }
        catch (Exception ex)
        {
            await DiscBotService.ErrorHandler.CaptureAsync(
                ex,
                "Failed to process scrimmage challenge command",
                nameof(ChallengeAsync));
            await ctx.EditResponseAsync("An error occurred while processing your challenge. Please try again.");
        }
    }
}

