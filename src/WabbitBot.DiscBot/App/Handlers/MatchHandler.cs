using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Services;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Events;
using WabbitBot.DiscBot.App.Handlers;
using WabbitBot.DiscBot.App.Renderers;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Services.DiscBot;
using System.Text.RegularExpressions;


/// <summary>
/// Handles button and component interactions for match flows.
/// Publishes DiscBot-local interaction events to the event bus.
/// Also handles match-related "Requested" events and calls appropriate Renderer methods.
/// </summary>
namespace WabbitBot.DiscBot.App.Handlers
{
    /// <summary>
    /// Handles button and component interactions for match flows.
    /// Publishes DiscBot-local interaction events to the event bus.
    /// Also handles match-related "Requested" events and calls appropriate Renderer methods.
    /// </summary>
    public partial class MatchHandler
    {

        /// <summary>
        /// Handles button interactions (accept/decline challenge, confirm selections).
        /// Returns Result indicating success/failure for immediate feedback.
        /// Publishes events for cross-boundary communication.
        /// </summary>
        public static async Task<Result> HandleButtonInteractionAsync(DiscordClient client, ComponentInteractionCreatedEventArgs args)
        {
            var interaction = args.Interaction;
            var customId = interaction.Data.CustomId;

            try
            {
                // Map ban confirmation button
                if (customId.StartsWith("confirm_mapban_", StringComparison.Ordinal))
                {
                    return await HandleMapBanConfirmAsync(interaction, customId);
                }

                return Result.Failure($"Unknown custom ID: {customId}");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle button interaction: {customId}",
                    nameof(HandleButtonInteractionAsync));

                // Try to respond with error - may fail if response was already sent
                try
                {
                    await interaction.CreateResponseAsync(
                        DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("An error occurred while processing your interaction. Please try again.")
                            .AsEphemeral());
                }
                catch
                {
                    // Response was already sent, ignore
                }

                return Result.Failure($"Failed to handle button interaction: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles string select dropdown interactions (map ban selections).
        /// Returns Result indicating success/failure for immediate feedback.
        /// Publishes events for cross-boundary communication.
        /// </summary>
        public static async Task<Result> HandleSelectMenuInteractionAsync(DiscordClient client, ComponentInteractionCreatedEventArgs args)
        {
            var interaction = args.Interaction;
            var customId = interaction.Data.CustomId;

            try
            {
                // Map ban selection dropdown
                if (customId.StartsWith("select_mapban_", StringComparison.Ordinal))
                {
                    return await HandleMapBanSelectionAsync(interaction, customId);
                }

                return Result.Failure($"Unknown custom ID: {customId}");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle select menu interaction: {customId}",
                    nameof(HandleSelectMenuInteractionAsync));

                // Try to respond with error - may fail if response was already sent
                try
                {
                    await interaction.CreateResponseAsync(
                        DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("An error occurred while processing your selection. Please try again.")
                            .AsEphemeral());
                }
                catch
                {
                    // Response was already sent, ignore
                }

                return Result.Failure($"Failed to handle select menu interaction: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles modal submissions (deck code input).
        /// Returns Result indicating success/failure for immediate feedback.
        /// </summary>
        public static async Task<Result> HandleModalSubmitAsync(DiscordClient client, ModalSubmittedEventArgs args)
        {
            var interaction = args.Interaction;
            var customId = interaction.Data.CustomId;

            try
            {
                // Placeholder, add modal interactions here
                return Result.CreateSuccess("No modal handlers registered");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle modal submission: {customId}",
                    nameof(HandleModalSubmitAsync));

                // Try to respond with error - may fail if response was already sent
                try
                {
                    await interaction.CreateResponseAsync(
                        DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("An error occurred while processing your submission. Please try again.")
                            .AsEphemeral());
                }
                catch
                {
                    // Response was already sent, ignore
                }

                return Result.Failure($"Failed to handle modal submission: {ex.Message}");
            }
        }

        private static async Task<Result> HandleScrimmageMatchCreatedAsync(ulong scrimmageChannelId, Guid matchId)
        {
            var pubResult = await PublishScrimThreadCreateRequestedAsync(scrimmageChannelId, matchId);
            if (!pubResult.Success)
            {
                return Result.Failure("Failed to publish match thread create requested");
            }

            return Result.CreateSuccess();
        }

        private static async Task<Result> HandleMapBanSelectionAsync(DiscordInteraction interaction, string customId)
        {
            // Parse match ID from custom ID: "select_mapban_{matchId}"
            var matchIdStr = customId.Replace("select_mapban_", "", StringComparison.Ordinal);
            if (!Guid.TryParse(matchIdStr, out var matchId))
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("Invalid match ID.")
                        .AsEphemeral());
                return Result.Failure("Invalid match ID");
            }

            // Get selected values from dropdown
            var selections = interaction.Data.Values.ToArray();

            await interaction.CreateResponseAsync(
                DiscordInteractionResponseType.DeferredMessageUpdate);

            // Publish PlayerMapBanSelected (DiscBot-local) for App to handle
            await DiscBotService.PublishAsync(new PlayerMapBanSelected(
                matchId,
                interaction.User.Id,
                selections));

            return Result.CreateSuccess("Map ban selection recorded");
        }

        private static async Task<Result> HandleMapBanConfirmAsync(DiscordInteraction interaction, string customId)
        {
            // Parse match ID from custom ID: "confirm_mapban_{matchId}"
            var matchIdStr = customId.Replace("confirm_mapban_", "", StringComparison.Ordinal);
            if (!Guid.TryParse(matchIdStr, out var matchId))
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("Invalid match ID.")
                        .AsEphemeral());
                return Result.Failure("Invalid match ID");
            }

            // TODO: Retrieve current selections from DM message state or cache
            var selections = Array.Empty<string>(); // Placeholder

            await interaction.CreateResponseAsync(
                DiscordInteractionResponseType.DeferredMessageUpdate);

            // Publish PlayerMapBanConfirmed (DiscBot-local) for App to handle
            await DiscBotService.PublishAsync(new PlayerMapBanConfirmed(
                matchId,
                interaction.User.Id,
                selections));

            return Result.CreateSuccess("Map ban confirmed");
        }

        #region Rendering Requests

        /// <summary>
        /// Handles match thread creation requests by extracting Discord context and calling the Renderer.
        /// </summary>
        private static async Task<Result> HandleScrimThreadCreateRequestedAsync(ScrimThreadCreateRequested evt)
        {
            try
            {
                var client = DiscordClientProvider.GetClient();

                // TODO: Get the appropriate channel from configuration
                // For now, using a placeholder - this will be wired in step 5
                var guild = client.Guilds.Values.FirstOrDefault();
                if (guild is null)
                {
                    await DiscBotService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("No guilds available"),
                        "Cannot create match thread - no guilds",
                        nameof(HandleScrimThreadCreateRequestedAsync));
                    return Result.Failure("No guilds available");
                }

                var channel = guild.Channels.Values
                    .FirstOrDefault(c => c.Type == DiscordChannelType.Text);
                if (channel is null)
                {
                    await DiscBotService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("No text channels available"),
                        "Cannot create match thread - no text channels",
                        nameof(HandleScrimThreadCreateRequestedAsync));
                    return Result.Failure("No text channels available");
                }

                // Get the players for the match
                var match = await CoreService.Matches.GetByIdAsync(evt.MatchId, DatabaseComponent.Repository);
                if (!match.Success)
                {
                    return Result.Failure("Match not found");
                }
                var selectedTeam1Players = match.Data!.Team1PlayerIds
                    .Select(id => CoreService.Players
                    .GetByIdAsync(id, DatabaseComponent.Repository).Result.Data!).ToList();
                var selectedTeam2Players = match.Data!.Team2PlayerIds
                    .Select(id => CoreService.Players
                    .GetByIdAsync(id, DatabaseComponent.Repository).Result.Data!).ToList();

                // Call renderer with concrete parameters
                var challengerBuilder = new DiscordMessageBuilder();
                var opponentBuilder = new DiscordMessageBuilder();
                return await Renderers.MatchRenderer
                    .RenderMatchContainerAsync(
                        client,
                        channel,
                        challengerBuilder,
                        opponentBuilder,
                        selectedTeam1Players,
                        selectedTeam2Players,
                        evt.MatchId);
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle match thread create request for {evt.MatchId}",
                    nameof(HandleScrimThreadCreateRequestedAsync));
                return Result.Failure($"Failed to create match thread: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles match container creation requests by extracting Discord context and calling the Renderer.
        /// </summary>
        private static async Task<Result> HandleMatchContainerRequestedAsync(MatchContainerRequested evt)
        {
            try
            {
                var client = DiscordClientProvider.GetClient();
                var channel = await client.GetChannelAsync(evt.ChannelId);
                var team1thread = await client.GetChannelAsync(evt.Team1ThreadId) as DiscordThreadChannel;
                var team2thread = await client.GetChannelAsync(evt.Team2ThreadId) as DiscordThreadChannel;

                if (channel is null)
                {
                    await DiscBotService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException($"Channel {evt.ChannelId} not found"),
                        "Cannot create match container - thread not found",
                        nameof(HandleMatchContainerRequestedAsync));
                    return Result.Failure("Channel not found");
                }
                if (team1thread is null)
                {
                    await DiscBotService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException($"Team 1 thread {evt.Team1ThreadId} not found"),
                        "Cannot create match container - team 1 thread not found",
                        nameof(HandleMatchContainerRequestedAsync));
                    return Result.Failure("Team 1 thread not found");
                }
                if (team2thread is null)
                {
                    await DiscBotService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException($"Team 2 thread {evt.Team2ThreadId} not found"),
                        "Cannot create match container - team 2 thread not found",
                        nameof(HandleMatchContainerRequestedAsync));
                    return Result.Failure("Team 2 thread not found");
                }
                var match = await CoreService.Matches.GetByIdAsync(evt.MatchId, DatabaseComponent.Repository);
                if (!match.Success)
                {
                    return Result.Failure("Match not found");
                }
                var selectedTeam1Players = new List<Player>();
                var selectedTeam2Players = new List<Player>();
                foreach (var playerId in match.Data!.Team1PlayerIds)
                {
                    var player = await CoreService.Players.GetByIdAsync(playerId, DatabaseComponent.Repository);
                    if (!player.Success)
                    {
                        return Result.Failure("Player not found");
                    }
                    selectedTeam1Players.Add(player.Data!);
                }
                foreach (var playerId in match.Data.Team2PlayerIds)
                {
                    var player = await CoreService.Players.GetByIdAsync(playerId, DatabaseComponent.Repository);
                    if (!player.Success)
                    {
                        return Result.Failure("Player not found");
                    }
                    selectedTeam2Players.Add(player.Data!);
                }

                // Call renderer with concrete parameters
                // TODO: Verify we actually want to pass this as a list of players
                var challengerBuilder = new DiscordMessageBuilder();
                var opponentBuilder = new DiscordMessageBuilder();
                return await Renderers.MatchRenderer.RenderMatchContainerAsync(
                    client,
                    channel,
                    challengerBuilder,
                    opponentBuilder,
                    selectedTeam1Players,
                    selectedTeam2Players,
                    evt.MatchId);
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle match container request for {evt.MatchId} " +
                    $"in channel {evt.ChannelId} and threads {evt.Team1ThreadId} and {evt.Team2ThreadId}",
                    nameof(HandleMatchContainerRequestedAsync));
                return Result.Failure($"Failed to create match container: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles map ban DM update requests by extracting Discord context and calling the Renderer.
        /// </summary>
        private static async Task<Result> HandleMapBanDmUpdateRequestedAsync(MapBanDmUpdateRequested evt)
        {
            try
            {
                // TODO: Implement when message tracking is available
                return Result.CreateSuccess("Map ban DM update placeholder");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle map ban DM update request for match {evt.MatchId}, player {evt.PlayerId}",
                    nameof(HandleMapBanDmUpdateRequestedAsync));
                return Result.Failure($"Failed to update map ban DM: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles map ban DM confirm requests by extracting Discord context and calling the Renderer.
        /// </summary>
        private static async Task<Result> HandleMapBanDmConfirmRequestedAsync(MapBanDmConfirmRequested evt)
        {
            try
            {
                // TODO: Implement when message tracking is available
                return Result.CreateSuccess("Map ban DM confirm placeholder");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle map ban DM confirm request for match {evt.MatchId}, player {evt.PlayerId}",
                    nameof(HandleMapBanDmConfirmRequestedAsync));
                return Result.Failure($"Failed to confirm map ban DM: {ex.Message}");
            }
        }

        #endregion
    }
}