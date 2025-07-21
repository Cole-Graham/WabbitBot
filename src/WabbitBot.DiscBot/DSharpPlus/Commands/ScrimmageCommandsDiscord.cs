using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using WabbitBot.Common.Attributes;
using WabbitBot.DiscBot.DiscBot.Commands;
using WabbitBot.Core.Common.Models;

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
    public async Task ChallengeAsync(
        CommandContext ctx,
        [Description("Game size (1v1, 2v2, 3v3, 4v4)")]
        [ChoiceProvider(typeof(GameSizeChoiceProvider))]
        string size,
        [Description("Opponent team name or ID")]
        string opponent)
    {
        await ctx.DeferResponseAsync();

        // Parse game size
        if (!TryParseGameSize(size, out var gameSize))
        {
            await ctx.EditResponseAsync($"Invalid game size: {size}. Valid sizes: 1v1, 2v2, 3v3, 4v4");
            return;
        }

        // TODO: Get challenger team ID from the user's context
        // For now, we'll use a placeholder - this would typically come from the user's team membership
        var challengerTeamId = "placeholder_challenger_team_id";

        // TODO: Resolve opponent team ID from the opponent parameter
        // This could be a team name, team ID, or mention
        var opponentTeamId = opponent; // For now, assume it's already an ID

        // Call business logic
        var result = await ScrimmageCommands.ChallengeAsync(challengerTeamId, opponentTeamId, gameSize);

        if (!result.Success)
        {
            await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to create scrimmage challenge");
            return;
        }

        var embed = new DiscordEmbedBuilder()
            .WithTitle("🎯 Scrimmage Challenge Created")
            .WithDescription(result.Message ?? "Scrimmage challenge created")
            .WithColor(DiscordColor.Green)
            .AddField("Challenger", $"<@{challengerTeamId}>", true)
            .AddField("Opponent", $"<@{opponentTeamId}>", true)
            .AddField("Game Size", size, true)
            .AddField("Challenge ID", result.Scrimmage?.Id.ToString() ?? "Unknown", true)
            .AddField("Expires", result.Scrimmage?.ChallengeExpiresAt?.ToString("g") ?? "Unknown", true)
            .WithFooter("The opponent team can accept or decline this challenge");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    #endregion

    #region Private Helper Methods

    private static bool TryParseGameSize(string size, out GameSize gameSize)
    {
        gameSize = size.ToLowerInvariant() switch
        {
            "1v1" => GameSize.OneVOne,
            "2v2" => GameSize.TwoVTwo,
            "3v3" => GameSize.ThreeVThree,
            "4v4" => GameSize.FourVFour,
            _ => GameSize.OneVOne
        };

        return size.ToLowerInvariant() is "1v1" or "2v2" or "3v3" or "4v4";
    }

    #endregion
}

/// <summary>
/// Choice provider for game sizes
/// </summary>
public class GameSizeChoiceProvider : IChoiceProvider
{
    public IEnumerable<CommandChoice> GetChoices()
    {
        return new[]
        {
            new CommandChoice("1v1", "1v1"),
            new CommandChoice("2v2", "2v2"),
            new CommandChoice("3v3", "3v3"),
            new CommandChoice("4v4", "4v4"),
        };
    }
}