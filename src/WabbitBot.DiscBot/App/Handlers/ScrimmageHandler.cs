using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
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
using WabbitBot.DiscBot.App.Renderers;
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
            Console.WriteLine($"🎯 DEBUG: HandleChallengeCreatedAsync called!");
            Console.WriteLine($"   Event ChallengeId: {evt.ChallengeId}");

            // Add a small delay to ensure the challenge is committed to the database
            await Task.Delay(100);

            // Load challenge - lazy loading will handle navigation properties
            var challenge = await CoreService.ScrimmageChallenges.GetByIdAsync(
                evt.ChallengeId,
                DatabaseComponent.Repository
            );
            Console.WriteLine($"🔍 DEBUG: GetByIdAsync result: Success={challenge.Success}");
            if (!challenge.Success)
            {
                Console.WriteLine($"   Error: {challenge.ErrorMessage}");

                // Debug: Check if challenge exists at all in the database
                var debugCheck = await CoreService.WithDbContext(async db =>
                {
                    var exists = await db.ScrimmageChallenges.AnyAsync(c => c.Id == evt.ChallengeId);
                    return exists;
                });
                Console.WriteLine($"🔍 DEBUG: Challenge exists in database: {debugCheck}");

                await DiscBotService.ErrorHandler.CaptureAsync(
                    new InvalidOperationException("Failed to get challenge"),
                    "Failed to get challenge",
                    nameof(HandleChallengeCreatedAsync)
                );
                return;
            }
            var challengeData = challenge.Data;
            Console.WriteLine($"🔍 DEBUG: Challenge data: {(challengeData != null ? "Found" : "Null")}");
            if (challengeData == null)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    new InvalidOperationException("Challenge not found"),
                    "Challenge not found",
                    nameof(HandleChallengeCreatedAsync)
                );
                return;
            }
            Console.WriteLine($"🔍 DEBUG: Challenge details:");
            Console.WriteLine($"   ID: {challengeData.Id}");
            Console.WriteLine($"   ChallengerTeamId: {challengeData.ChallengerTeamId}");
            Console.WriteLine($"   OpponentTeamId: {challengeData.OpponentTeamId}");

            // Load the teams since navigation properties aren't loaded
            Console.WriteLine($"🔍 DEBUG: Looking up challenger team with ID: {challengeData.ChallengerTeamId}");
            var challengerTeamResult = await CoreService.Teams.GetByIdAsync(
                challengeData.ChallengerTeamId,
                DatabaseComponent.Repository
            );
            Console.WriteLine(
                $"🔍 DEBUG: Challenger team lookup result: Success={challengerTeamResult.Success}, Data={challengerTeamResult.Data?.Name ?? "null"}"
            );
            if (!challengerTeamResult.Success || challengerTeamResult.Data is null)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    new InvalidOperationException("Challenger team not found"),
                    "Challenger team not found",
                    nameof(HandleChallengeCreatedAsync)
                );
                return;
            }

            var opponentTeamResult = await CoreService.Teams.GetByIdAsync(
                challengeData.OpponentTeamId,
                DatabaseComponent.Repository
            );
            if (!opponentTeamResult.Success || opponentTeamResult.Data is null)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    new InvalidOperationException("Opponent team not found"),
                    "Opponent team not found",
                    nameof(HandleChallengeCreatedAsync)
                );
                return;
            }

            var challengerTeam = challengerTeamResult.Data;
            var opponentTeam = opponentTeamResult.Data;

            var containerResult = await ScrimmageRenderer.RenderChallengeContainerAsync(
                challengeData.Id,
                challengeData.TeamSize,
                challengerTeam.Name,
                opponentTeam.Name,
                opponentTeam
            );
            if (!containerResult.Success || containerResult.Data is null)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    new InvalidOperationException("Failed to create challenge container"),
                    "Failed to create challenge container",
                    nameof(HandleChallengeCreatedAsync)
                );
                return;
            }

            // Store the message ID and channel ID in the challenge
            challengeData.ChallengeMessageId = containerResult.Data.ChallengeMessage.Id;
            challengeData.ChallengeChannelId = containerResult.Data.ChallengeChannel.Id;
            var updateResult = await CoreService.ScrimmageChallenges.UpdateAsync(
                challengeData,
                DatabaseComponent.Repository
            );

            if (!updateResult.Success)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    new InvalidOperationException(
                        $"Failed to update challenge with message ID: {updateResult.ErrorMessage}"
                    ),
                    "Failed to update challenge with message ID",
                    nameof(HandleChallengeCreatedAsync)
                );
            }

            return;
        }

        public static async Task HandleChallengeDeclinedAsync(ChallengeDeclined evt)
        {
            // TODO: Implement challenge declined notification
            await Task.CompletedTask;
        }

        /// <summary>
        /// Handles string select dropdown interactions (challenge configuration selections).
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
                // Route to ScrimmageApp for challenge configuration and teammate selection
                if (
                    customId.StartsWith("challenge_opponent_", StringComparison.Ordinal)
                    || customId.StartsWith("challenge_players_", StringComparison.Ordinal)
                    || customId.StartsWith("challenge_bestof_", StringComparison.Ordinal)
                    || customId.StartsWith("select_teammates_", StringComparison.Ordinal)
                )
                {
                    return await ScrimmageApp.ProcessSelectMenuInteractionAsync(client, args);
                }

                return Result.CreateSuccess("No select menu handlers registered for this interaction");
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
            // Placeholder if we need to handle this event
            await Task.CompletedTask;
        }
    }
}
