using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Events.Core;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Scrimmage;
using WabbitBot.Core.Common.Services;
using WabbitBot.Core.Scrimmages;
using WabbitBot.DiscBot.App;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Services.DiscBot;

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
        public static async Task HandleChallengeCreatedAsync(ChallengeCreated evt)
        {
            var challenge = await CoreService.ScrimmageChallenges.GetByIdAsync(
                evt.ChallengeId,
                DatabaseComponent.Repository
            );
            if (!challenge.Success)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    new InvalidOperationException("Failed to get challenge"),
                    "Failed to get challenge",
                    nameof(HandleChallengeCreatedAsync)
                );
                return;
            }
            var challengeData = challenge.Data;
            if (challengeData == null)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    new InvalidOperationException("Challenge not found"),
                    "Challenge not found",
                    nameof(HandleChallengeCreatedAsync)
                );
                return;
            }
            if (challengeData.ChallengerTeam == null)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    new InvalidOperationException("Challenger team not found"),
                    "Challenger team not found",
                    nameof(HandleChallengeCreatedAsync)
                );
                return;
            }
            if (challengeData.OpponentTeam == null)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    new InvalidOperationException("Opponent team not found"),
                    "Opponent team not found",
                    nameof(HandleChallengeCreatedAsync)
                );
                return;
            }
            var containerResult = await ScrimmageApp.CreateChallengeContainerAsync(
                challengeData.Id,
                challengeData.TeamSize,
                challengeData.ChallengerTeam.Name,
                challengeData.OpponentTeam.Name
            );
            if (!containerResult.Success)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    new InvalidOperationException("Failed to create challenge container"),
                    "Failed to create challenge container",
                    nameof(HandleChallengeCreatedAsync)
                );
                return;
            }
            // var pubResult = await PublishChallengeContainerCreatedAsync(
            //     challengeData.Id,
            //     containerResult.Data!.ChallengeChannel.Id
            // );
            // if (!pubResult.Success)
            // {
            //     await DiscBotService.ErrorHandler.CaptureAsync(
            //         new InvalidOperationException("Failed to publish challenge container created"),
            //         "Failed to publish challenge container created",
            //         nameof(HandleChallengeCreatedAsync)
            //     );
            //     return;
            // }
            return;
        }

        public static async Task HandleChallengeDeclinedAsync(ChallengeDeclined evt)
        {
            // TODO: Implement challenge declined notification
            await Task.CompletedTask;
        }

        public static async Task HandleChallengeCancelledAsync(ChallengeCancelled evt)
        {
            // TODO: Implement challenge cancelled notification
            await Task.CompletedTask;
        }

        public static async Task HandleScrimmageCreatedAsync(ScrimmageCreated evt)
        {
            // TODO: Implement scrimmage created notification
            await Task.CompletedTask;
        }

        public static async Task HandleMatchProvisioningRequestedAsync(MatchProvisioningRequested evt)
        {
            // TODO: Implement match provisioning
            await Task.CompletedTask;
        }

        /// <summary>
        /// Handles string select dropdown interactions (map ban selections).
        /// Returns Result indicating success/failure for immediate feedback.
        /// </summary>
        public static async Task<Result> HandleSelectMenuInteractionAsync(
            DiscordClient client,
            ComponentInteractionCreatedEventArgs args
        )
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
                    nameof(HandleSelectMenuInteractionAsync)
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
                    nameof(HandleModalSubmitAsync)
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

        private static async Task HandleScrimmageMatchCreatedAsync(ScrimmageMatchCreated evt)
        {
            var threadsResult = await ScrimmageApp.CreateScrimmageThreadsAsync(evt.ScrimmageId, evt.MatchId);
            if (!threadsResult.Success)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    new InvalidOperationException("Failed to create scrimmage threads"),
                    "Failed to create scrimmage threads, result is failure",
                    nameof(HandleScrimmageMatchCreatedAsync)
                );
                return;
            }
            if (threadsResult.Data == null)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    new InvalidOperationException("Failed to create scrimmage threads"),
                    "Failed to create scrimmage threads, data is null",
                    nameof(HandleScrimmageMatchCreatedAsync)
                );
                return;
            }
            var pubResult = await PublishScrimmageThreadsCreatedAsync(
                evt.MatchId,
                DiscBotService.ScrimmageChannel.Id,
                threadsResult.Data.ChallengerThread.Id,
                threadsResult.Data.OpponentThread.Id
            );
            if (!pubResult.Success)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    new InvalidOperationException("Failed to publish scrimmage threads created"),
                    "Failed to publish scrimmage threads created, result is failure",
                    nameof(HandleScrimmageMatchCreatedAsync)
                );
                return;
            }
            return;
        }

        private static async Task HandleScrimmageThreadsCreatedAsync(ScrimmageThreadsCreated evt)
        {
            // TODO: Implement scrimmage threads created notification
            await Task.CompletedTask;
        }
    }
}
