using DSharpPlus;
using DSharpPlus.Entities;
using WabbitBot.Common.Models;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Services.DiscBot;
using WabbitBot.DiscBot.App.Utilities;

namespace WabbitBot.DiscBot.App.Renderers
{
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
            DiscordChannel channel,
            DiscordThreadChannel team1thread,
            DiscordThreadChannel team2thread,
            Guid matchId,
            int gameNumber,
            string chosenMap)
        {
            try
            {
                // Resolve map thumbnail (CDN-first, fallback to attachment)
                var (cdnUrl, attachmentHint) = await DiscBotService.AssetResolver.ResolveMapThumbnailAsync(chosenMap);

                // Build markdown for thumbnail reference
                string thumbnailMarkdown = cdnUrl is not null
                    ? $"![Map Thumbnail]({cdnUrl})"
                    : attachmentHint is not null
                        ? $"![Map Thumbnail](attachment://{attachmentHint.CanonicalFileName})"
                        : "_(Thumbnail unavailable)_";

                // Compose container content
                var header = $"**Game {gameNumber}**\n\n";
                var body = $"Map: **{chosenMap}**\n{thumbnailMarkdown}\n\n"
                    + "Please play the game and upload the replay when finished.";

                // Build container components
                var uploadButton = new DiscordButtonComponent(
                    DiscordButtonStyle.Primary,
                    $"upload_replay_{matchId}_{gameNumber}",
                    "Upload Replay");

                var components = new List<DiscordComponent>
                {
                    new DiscordTextDisplayComponent(header + body),
                    uploadButton,
                };

                var container = new DiscordContainerComponent(components);

                // Build message with container and optional attachment
                var builder = new DiscordMessageBuilder()
                    .AddContainerComponent(container);

                if (attachmentHint is not null)
                {
                    var filePath = ResolveFilePath(attachmentHint.CanonicalFileName);
                    if (File.Exists(filePath))
                    {
                        using var fileStream = File.OpenRead(filePath);
                        builder.AddFile(attachmentHint.CanonicalFileName, fileStream);
                    }
                    else
                    {
                        await DiscBotService.ErrorHandler.CaptureAsync(
                            new FileNotFoundException($"Attachment file not found: {filePath}"),
                            $"Failed to attach file: {attachmentHint.CanonicalFileName}",
                            nameof(RenderGameContainerAsync));
                    }
                }

                var team1message = await team1thread.SendMessageAsync(builder);
                var team2message = await team2thread.SendMessageAsync(builder);

                // Capture CDN URL when attachment was used
                if (attachmentHint is not null)
                {
                    await CdnCapture.CaptureFromMessageAsync(team1message, attachmentHint.CanonicalFileName);
                }

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
        private static string ResolveFilePath(string canonicalFileName)
        {
            string relativePath;

            if (canonicalFileName.Contains("thumbnail", StringComparison.OrdinalIgnoreCase))
            {
                relativePath = Path.Combine("data", "maps", "thumbnails", canonicalFileName);
            }
            else if (canonicalFileName.Contains("icon", StringComparison.OrdinalIgnoreCase))
            {
                relativePath = Path.Combine("data", "divisions", "icons", canonicalFileName);
            }
            else
            {
                relativePath = Path.Combine("data", canonicalFileName);
            }

            return Path.Combine(AppContext.BaseDirectory, relativePath);
        }
    }
}
