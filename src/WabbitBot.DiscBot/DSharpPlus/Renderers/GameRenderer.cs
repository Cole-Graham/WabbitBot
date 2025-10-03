using DSharpPlus;
using DSharpPlus.Entities;
using WabbitBot.Common.Models;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Services.DiscBot;

namespace WabbitBot.DiscBot.DSharpPlus.Renderers;

/// <summary>
/// Renderer for per-game containers and deck submission DMs.
/// Accepts concrete Discord parameters and performs rendering logic.
/// Does NOT subscribe to events - that's the Handler's job.
/// </summary>
public static class GameRenderer
{
    /// <summary>
    /// Renders a game container in the specified thread.
    /// </summary>
    /// <param name="client">Discord client</param>
    /// <param name="thread">Thread channel to post container in</param>
    /// <param name="matchId">Match ID</param>
    /// <param name="gameNumber">Game number</param>
    /// <param name="chosenMap">Chosen map for this game</param>
    /// <returns>Result indicating success/failure</returns>
    public static async Task<Result> RenderGameContainerAsync(
        DiscordClient client,
        DiscordChannel thread,
        Guid matchId,
        int gameNumber,
        string chosenMap)
    {
        try
        {
            // TODO: Step 3h - Use POCO visual models and factories for game container creation
            // The container should include:
            // - Game number
            // - Selected map
            // - Instructions for playing and submitting replay
            // - Replay upload button/attachment

            // Example of what the final implementation would look like:
            var uploadButton = new DiscordButtonComponent(
                DiscordButtonStyle.Primary,
                $"upload_replay_{matchId}_{gameNumber}",
                "Upload Replay");

            await thread.SendMessageAsync(new DiscordMessageBuilder()
                .WithContent($"**Game {gameNumber}**\n\nMap: **{chosenMap}**\n\nPlease play the game and upload the replay when finished.")
                .AddActionRowComponent(uploadButton));

            return Result.CreateSuccess("Game container created");
        }
        catch (Exception ex)
        {
            await DiscBotService.ErrorHandler.CaptureAsync(
                ex,
                $"Failed to render game container for match {matchId}, game {gameNumber}",
                nameof(RenderGameContainerAsync));
            return Result.Failure($"Failed to create game container: {ex.Message}");
        }
    }

    /// <summary>
    /// Renders a deck submission DM for the specified user.
    /// </summary>
    /// <param name="client">Discord client</param>
    /// <param name="user">User to send DM to</param>
    /// <param name="matchId">Match ID</param>
    /// <param name="gameNumber">Game number</param>
    /// <returns>Result indicating success/failure</returns>
    public static async Task<Result> RenderDeckDmStartAsync(
        DiscordClient client,
        DiscordUser user,
        Guid matchId,
        int gameNumber)
    {
        try
        {
            // Create button to open deck submission modal
            var submitButton = new DiscordButtonComponent(
                DiscordButtonStyle.Primary,
                $"open_deck_modal_{matchId}_{gameNumber}",
                "Submit Deck Code");

            var dmChannel = await user.CreateDmChannelAsync();
            await dmChannel.SendMessageAsync(new DiscordMessageBuilder()
                .WithContent($"**Deck Code Submission**\n\nMatch ID: `{matchId}` | Game {gameNumber}\n\nClick the button below to submit your deck code:")
                .AddActionRowComponent(submitButton));

            // Note: The modal will be shown when the button is clicked
            // The interaction handler will present the modal via ShowModalAsync

            return Result.CreateSuccess("Deck DM sent");
        }
        catch (Exception ex)
        {
            await DiscBotService.ErrorHandler.CaptureAsync(
                ex,
                $"Failed to render deck DM for match {matchId}, game {gameNumber} to user {user.Id}",
                nameof(RenderDeckDmStartAsync));
            return Result.Failure($"Failed to send deck DM: {ex.Message}");
        }
    }
}

