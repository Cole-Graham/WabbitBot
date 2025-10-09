using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Services;
using WabbitBot.DiscBot.App;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Services.DiscBot;
using WabbitBot.Common.Attributes;
using WabbitBot.Core.Scrimmages;
using WabbitBot.Core.Common.Models.Scrimmage;
using WabbitBot.Common.Events.Core;

/// <summary>
/// Handles button and component interactions for scrimmage flows.
/// Publishes DiscBot-local interaction events to the event bus.
/// </summary>
namespace WabbitBot.DiscBot.App.Handlers
{
    /// <summary>
    /// Handles button and component interactions for scrimmage flows.
    /// Publishes DiscBot-local interaction events to the event bus.
    /// </summary>
    public static partial class ScrimmageHandler
    {
        public static async Task<Result> HandleChallengeAcceptedAsync(ChallengeAccepted evt)
        {
            // Get scrimmage channel from config
            var scrimmageChannelId = ConfigurationProvider
                .GetSection<ChannelsOptions>(ChannelsOptions.SectionName).ScrimmageChannel;
            if (scrimmageChannelId is null)
            {
                return Result.Failure("Scrimmage channel not found");
            }

            // Update scrimmage to accepted
            var challengeId = evt.ChallengeId;
            var challengeResult = await CoreService.ScrimmageChallenges.GetByIdAsync(challengeId, DatabaseComponent.Repository);
            if (!challengeResult.Success)
            {
                return Result.Failure("Failed to get challenge");
            }
            var challenge = challengeResult.Data;
            if (challenge == null)
            {
                return Result.Failure("Challenge not found");
            }
            challenge.ChallengeStatus = ScrimmageChallengeStatus.Accepted;
            var updateResult = await CoreService.ScrimmageChallenges.UpdateAsync(challenge, DatabaseComponent.Repository);
            if (!updateResult.Success)
            {
                return Result.Failure("Failed to update scrimmage challenge");
            }

            // Commented out pending generator publishers
            // await PublishMatchProvisioningRequestedAsync(scrimmageChannelId.Value, scrimmage.Match.Id, scrimmage.Id);

            return Result.CreateSuccess("Scrimmage challenge accepted");
        }

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
                // Accept challenge button
                if (customId.StartsWith("accept_challenge_", StringComparison.Ordinal))
                {
                    return await HandleAcceptChallengeButtonAsync(interaction, customId);
                }

                // Decline challenge button
                if (customId.StartsWith("decline_challenge_", StringComparison.Ordinal))
                {
                    return await HandleDeclineChallengeButtonAsync(interaction, customId);
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
        /// </summary>
        public static async Task<Result> HandleSelectMenuInteractionAsync(DiscordClient client, ComponentInteractionCreatedEventArgs args)
        {
            var interaction = args.Interaction;
            var customId = interaction.Data.CustomId;

            try
            {
                // Placeholder, add select menu interactions here
                return Result.CreateSuccess("No select menu handlers registered");
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

        private static async Task<Result> HandleAcceptChallengeButtonAsync(DiscordInteraction interaction, string customId)
        {
            // Parse challenge ID from custom ID: "accept_challenge_{challengeId}"
            var challengeIdStr = customId.Replace("accept_challenge_", "", StringComparison.Ordinal);
            if (!Guid.TryParse(challengeIdStr, out var challengeId))
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("Invalid challenge ID.")
                        .AsEphemeral());
                return Result.Failure("Invalid challenge ID");
            }

            // Acknowledge interaction
            await interaction.CreateResponseAsync(
                DiscordInteractionResponseType.DeferredMessageUpdate);

            // Publish ScrimmageAccepted (Global) to Core
            // await PublishChallengeAcceptedAsync(challengeId, interaction.User.Id);

            return Result.CreateSuccess("Challenge accepted");
        }

        private static async Task<Result> HandleDeclineChallengeButtonAsync(DiscordInteraction interaction, string customId)
        {
            var challengeIdStr = customId.Replace("decline_challenge_", "", StringComparison.Ordinal);
            if (!Guid.TryParse(challengeIdStr, out var challengeId))
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("Invalid challenge ID.")
                        .AsEphemeral());
                return Result.Failure("Invalid challenge ID");
            }

            await interaction.CreateResponseAsync(
                DiscordInteractionResponseType.DeferredMessageUpdate);

            // Publish ScrimmageDeclined (Global) to Core
            // await PublishChallengeDeclinedAsync(challengeId, interaction.User.Id);

            return Result.CreateSuccess("Challenge declined");
        }
    }
}