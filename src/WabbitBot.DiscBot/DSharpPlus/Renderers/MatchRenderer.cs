using DSharpPlus;
using DSharpPlus.Entities;
using WabbitBot.Common.Models;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Services.DiscBot;
using WabbitBot.DiscBot.DSharpPlus;

namespace WabbitBot.DiscBot.DSharpPlus.Renderers;

/// <summary>
/// Renderer for match-related Discord operations (threads, containers).
/// Accepts concrete Discord parameters and performs rendering logic.
/// Does NOT subscribe to events - that's the Handler's job.
/// </summary>
public static class MatchRenderer
{
    /// <summary>
    /// Renders a match thread in the specified channel.
    /// </summary>
    /// <param name="client">Discord client</param>
    /// <param name="channel">Channel to create thread in</param>
    /// <param name="matchId">Match ID</param>
    /// <returns>Result indicating success/failure</returns>
    public static async Task<Result> RenderMatchThreadAsync(
        DiscordClient client,
        DiscordChannel channel,
        Guid matchId)
    {
        try
        {
            // Create thread
            var threadName = $"Match {matchId:N}"; // Use short GUID format
            var thread = await channel.CreateThreadAsync(
                threadName,
                DiscordAutoArchiveDuration.Day,
                DiscordChannelType.PublicThread);

            // Publish confirmation that thread was created (Event for cross-boundary communication)
            await DiscBotService.PublishAsync(new MatchThreadCreated(
                matchId,
                thread.Id));

            return Result.CreateSuccess("Match thread created");
        }
        catch (Exception ex)
        {
            await DiscBotService.ErrorHandler.CaptureAsync(
                ex,
                $"Failed to render match thread for {matchId}",
                nameof(RenderMatchThreadAsync));
            return Result.Failure($"Failed to create match thread: {ex.Message}");
        }
    }

    /// <summary>
    /// Renders a match container in the specified thread.
    /// </summary>
    /// <param name="client">Discord client</param>
    /// <param name="thread">Thread channel to post container in</param>
    /// <param name="matchId">Match ID</param>
    /// <returns>Result indicating success/failure</returns>
    public static async Task<Result> RenderMatchContainerAsync(
        DiscordClient client,
        DiscordChannel thread,
        Guid matchId)
    {
        try
        {
            // TODO: Step 3h - Use POCO visual models and factories for embed/container creation
            // For now, create a simple placeholder container

            // Create match container with buttons
            var startButton = new DiscordButtonComponent(
                DiscordButtonStyle.Primary,
                $"start_match_{matchId}",
                "Start Match");

            var cancelButton = new DiscordButtonComponent(
                DiscordButtonStyle.Danger,
                $"cancel_match_{matchId}",
                "Cancel Match");

            var text = $"**Match Container**\n\nMatch ID: `{matchId}`\n\nWaiting for players to complete setup...";

            var components = new List<DiscordComponent> { startButton, cancelButton, new DiscordTextDisplayComponent(text) };

            var container = new DiscordContainerComponent(components);

            var message = await thread.SendMessageAsync(new DiscordMessageBuilder()
                .AddContainerComponent(container));

            // Publish MatchProvisioned (Global) to confirm Discord UI is ready
            await DiscBotService.PublishAsync(new MatchProvisioned(
                matchId,
                thread.Id));

            return Result.CreateSuccess("Match container created");
        }
        catch (Exception ex)
        {
            await DiscBotService.ErrorHandler.CaptureAsync(
                ex,
                $"Failed to render match container for {matchId} in thread {thread.Id}",
                nameof(RenderMatchContainerAsync));
            return Result.Failure($"Failed to create match container: {ex.Message}");
        }
    }

    #region Map Ban DM Rendering

    /// <summary>
    /// Renders a map ban DM for the specified user.
    /// </summary>
    /// <param name="client">Discord client</param>
    /// <param name="user">User to send DM to</param>
    /// <param name="matchId">Match ID</param>
    /// <returns>Result indicating success/failure</returns>
    public static async Task<Result> RenderMapBanDmStartAsync(
        DiscordClient client,
        DiscordUser user,
        Guid matchId)
    {
        try
        {
            // TODO: Get available maps from Core via request-response pattern
            // For now, using placeholder maps
            var availableMaps = new[] { "Echeneis", "Glittering Lagoon", "Silent Sanctum", "Thornwood" };

            // Create dropdown for map selection
            var dropdown = new DiscordSelectComponent(
                $"select_mapban_{matchId}",
                "Select maps to ban",
                availableMaps.Select(map => new DiscordSelectComponentOption(map, map)).ToList(),
                minOptions: 1,
                maxOptions: 2);

            var text = $"**Map Ban Selection**\n\nMatch ID: `{matchId}`\n\nPlease select maps you want to ban (1-2 maps):";
            var components = new List<DiscordComponent> { new DiscordTextDisplayComponent(text), dropdown };
            var container = new DiscordContainerComponent(components);

            var dmChannel = await user.CreateDmChannelAsync();
            await dmChannel.SendMessageAsync(new DiscordMessageBuilder()
                .AddContainerComponent(container));

            return Result.CreateSuccess("Map ban DM sent");
        }
        catch (Exception ex)
        {
            await DiscBotService.ErrorHandler.CaptureAsync(
                ex,
                $"Failed to render map ban DM for match {matchId} to user {user.Id}",
                nameof(RenderMapBanDmStartAsync));
            return Result.Failure($"Failed to send map ban DM: {ex.Message}");
        }
    }

    #endregion
}

