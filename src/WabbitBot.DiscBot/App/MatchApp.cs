using System.Security.Cryptography.X509Certificates;
using DSharpPlus;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Services;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Interfaces;
using WabbitBot.DiscBot.App.Services.DiscBot;

namespace WabbitBot.DiscBot.App
{
    public partial class MatchApp : IMatchApp
    {
        #region Modals
        /// <summary>
        /// Handles button interactions (accept/decline challenge, confirm selections).
        /// Returns Result indicating success/failure for immediate feedback.
        /// Publishes events for cross-boundary communication.
        /// </summary>
        public static async Task<Result> ProcessButtonInteractionAsync(
            DiscordClient client,
            ComponentInteractionCreatedEventArgs args
        )
        {
            var interaction = args.Interaction;
            var customId = interaction.Data.CustomId;

            try
            {
                // Map ban confirmation button
                if (customId.StartsWith("confirm_mapban_", StringComparison.Ordinal))
                {
                    return await ConfirmMapBanAsync(interaction, customId);
                }

                return Result.Failure($"Unknown custom ID: {customId}");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle button interaction: {customId}",
                    nameof(ProcessButtonInteractionAsync)
                );

                // Try to respond with error - may fail if response was already sent
                try
                {
                    await interaction.CreateResponseAsync(
                        DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("An error occurred while processing your interaction. Please try again.")
                            .AsEphemeral()
                    );
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
        public static async Task<Result> ProcessSelectMenuInteractionAsync(
            DiscordClient client,
            ComponentInteractionCreatedEventArgs args
        )
        {
            var interaction = args.Interaction;
            var customId = interaction.Data.CustomId;

            try
            {
                // Map ban selection dropdown
                if (customId.StartsWith("select_mapban_", StringComparison.Ordinal))
                {
                    return await ProcessMapBanSelectionAsync(interaction, customId);
                }

                return Result.Failure($"Unknown custom ID: {customId}");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle select menu interaction: {customId}",
                    nameof(ProcessSelectMenuInteractionAsync)
                );

                // Try to respond with error - may fail if response was already sent
                try
                {
                    await interaction.CreateResponseAsync(
                        DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("An error occurred while processing your selection. Please try again.")
                            .AsEphemeral()
                    );
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
        public static async Task<Result> ProcessModalSubmitAsync(DiscordClient client, ModalSubmittedEventArgs args)
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
                    nameof(ProcessModalSubmitAsync)
                );

                // Try to respond with error - may fail if response was already sent
                try
                {
                    await interaction.CreateResponseAsync(
                        DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("An error occurred while processing your submission. Please try again.")
                            .AsEphemeral()
                    );
                }
                catch
                {
                    // Response was already sent, ignore
                }

                return Result.Failure($"Failed to handle modal submission: {ex.Message}");
            }
        }
        #endregion

        #region Map Bans
        private static async Task<Result> ProcessMapBanSelectionAsync(DiscordInteraction interaction, string customId)
        {
            // Parse match ID from custom ID: "select_mapban_{matchId}"
            var matchIdStr = customId.Replace("select_mapban_", "", StringComparison.Ordinal);
            if (!Guid.TryParse(matchIdStr, out var matchId))
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Invalid match ID.").AsEphemeral()
                );
                return Result.Failure("Invalid match ID");
            }

            // Get selected values from dropdown
            var selections = interaction.Data.Values.ToArray();

            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            // Publish PlayerMapBanSelected (DiscBot-local) for App to handle
            await DiscBotService.PublishAsync(new PlayerMapBanSelected(matchId, interaction.User.Id, selections));

            return Result.CreateSuccess("Map ban selection recorded");
        }

        private static async Task<Result> ConfirmMapBanAsync(DiscordInteraction interaction, string customId)
        {
            // Parse match ID from custom ID: "confirm_mapban_{matchId}"
            var matchIdStr = customId.Replace("confirm_mapban_", "", StringComparison.Ordinal);
            if (!Guid.TryParse(matchIdStr, out var matchId))
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Invalid match ID.").AsEphemeral()
                );
                return Result.Failure("Invalid match ID");
            }

            // TODO: Retrieve current selections from DM message state or cache
            var selections = Array.Empty<string>(); // Placeholder

            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            // Publish PlayerMapBanConfirmed (DiscBot-local) for App to handle
            await DiscBotService.PublishAsync(new PlayerMapBanConfirmed(matchId, interaction.User.Id, selections));

            return Result.CreateSuccess("Map ban confirmed");
        }

        /// <summary>
        /// Handles map ban DM update requests by extracting Discord context and calling the Renderer.
        /// </summary>
        private static async Task<Result> ProcessMapBanUpdateAsync(MapBanDmUpdateRequested evt)
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
                    nameof(ProcessMapBanUpdateAsync)
                );
                return Result.Failure($"Failed to update map ban DM: {ex.Message}");
            }
        }
        #endregion
    }
}
