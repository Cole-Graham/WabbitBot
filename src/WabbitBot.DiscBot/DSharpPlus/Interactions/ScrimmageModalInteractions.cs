using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Threading.Tasks;
using WabbitBot.Core.Scrimmages;
using WabbitBot.DiscBot.DiscBot.ErrorHandling;
using WabbitBot.DiscBot.DSharpPlus.Embeds;
using WabbitBot.Core.Common.BotCore;

namespace WabbitBot.DiscBot.DSharpPlus.Interactions;

/// <summary>
/// Handles modal interactions for scrimmage matches (map bans, deck submissions, game results)
/// </summary>
public class ScrimmageModalInteractions
{
    private static readonly ScrimmageCommands ScrimmageCommands = new();

    /// <summary>
    /// Handles dropdown selection events (replacing modal interactions)
    /// </summary>
    public static async Task HandleDropdownSelectionAsync(DiscordClient client, ComponentInteractionCreatedEventArgs e)
    {
        try
        {
            var customId = e.Interaction.Data.CustomId;

            if (customId.StartsWith("map_ban_"))
            {
                await HandleMapBansSelectionAsync(e);
            }
            else if (customId.StartsWith("game_winner_"))
            {
                await HandleGameResultSelectionAsync(e);
            }
        }
        catch (Exception ex)
        {
            await DiscordErrorHandler.Instance.HandleError(ex);

            // Send ephemeral error message to user
            await e.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("An error occurred while processing your request.")
                    .AsEphemeral());
        }
    }

    /// <summary>
    /// Handles map bans dropdown selection
    /// </summary>
    private static async Task HandleMapBansSelectionAsync(ComponentInteractionCreatedEventArgs e)
    {
        var customId = e.Interaction.Data.CustomId;
        var scrimmageId = ExtractScrimmageId(customId, "map_ban_");
        var teamId = ExtractTeamId(customId);

        // Extract selected map bans from dropdown values
        var selectedValues = e.Interaction.Data.Values;
        var mapBans = selectedValues.ToList();

        if (mapBans.Count == 0)
        {
            await e.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("Please select at least one map to ban.")
                    .AsEphemeral());
            return;
        }

        // Call business logic
        var result = await ScrimmageCommands.SubmitMapBansAsync(scrimmageId, teamId, mapBans);

        if (!result.Success)
        {
            await e.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent(result.ErrorMessage ?? "Failed to submit map bans")
                    .AsEphemeral());
            return;
        }

        // Send success response
        await e.Interaction.CreateResponseAsync(
            DiscordInteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent($"Map bans submitted successfully! Banned: {string.Join(", ", mapBans)}")
                .AsEphemeral());

        // TODO: Update the scrimmage embed with new map ban information
        // This would trigger an event to update the embed in the thread
    }

    /// <summary>
    /// Handles game result dropdown selection
    /// </summary>
    private static async Task HandleGameResultSelectionAsync(ComponentInteractionCreatedEventArgs e)
    {
        var customId = e.Interaction.Data.CustomId;
        var parts = customId.Split('_');
        var scrimmageId = parts[2];
        var gameNumber = int.Parse(parts[3]);

        // Extract winner from dropdown selection
        var selectedValues = e.Interaction.Data.Values;
        var winnerId = selectedValues.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(winnerId))
        {
            await e.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("Please select a winner.")
                    .AsEphemeral());
            return;
        }

        // Call business logic
        var result = await ScrimmageCommands.ReportGameResultAsync(scrimmageId, winnerId);

        if (!result.Success)
        {
            await e.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent(result.ErrorMessage ?? "Failed to report game result")
                    .AsEphemeral());
            return;
        }

        // Send success response
        await e.Interaction.CreateResponseAsync(
            DiscordInteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent($"Game {gameNumber} result reported successfully! Winner: {winnerId}")
                .AsEphemeral());

        // TODO: Update the scrimmage embed with game result and check if match is complete
    }

    #region Helper Methods

    /// <summary>
    /// Extracts scrimmage ID from custom ID
    /// </summary>
    private static string ExtractScrimmageId(string customId, string prefix)
    {
        var idPart = customId.Substring(prefix.Length);
        var parts = idPart.Split('_');
        return parts[0];
    }

    /// <summary>
    /// Extracts team ID from custom ID
    /// </summary>
    private static string ExtractTeamId(string customId)
    {
        var parts = customId.Split('_');
        return parts.Length > 2 ? parts[2] : string.Empty;
    }

    #endregion
}
