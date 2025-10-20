using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Events;
using WabbitBot.Common.Events.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Services;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Interfaces;
using WabbitBot.DiscBot.App.Services.DiscBot;

namespace WabbitBot.DiscBot.App
{
    /// <summary>
    /// Handles game flow: per-game containers, deck submission, map selection, and game lifecycle.
    /// This app is library-agnostic and communicates only via events.
    /// </summary>
    public partial class GameApp : IGameApp
    {
        // Rate limiting for refresh button: tracks last refresh time per game container
        // Each game container (one per team/thread) gets a 5-second cooldown
        private static readonly Dictionary<string, DateTime> _lastRefreshTimes = new();
        private static readonly TimeSpan _refreshCooldown = TimeSpan.FromSeconds(5);
        private static readonly object _refreshLock = new();

        /// <summary>
        /// Cleans up old rate limit entries to prevent unbounded memory growth.
        /// Removes entries older than 1 minute.
        /// </summary>
        private static void CleanupOldRateLimitEntries()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-1);
            var keysToRemove = _lastRefreshTimes.Where(kvp => kvp.Value < cutoff).Select(kvp => kvp.Key).ToList();

            foreach (var key in keysToRemove)
            {
                _lastRefreshTimes.Remove(key);
            }
        }

        /// <summary>
        /// Handles button interactions.
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
                // Deck submission modal button
                if (customId.StartsWith("open_deck_modal_", StringComparison.Ordinal))
                {
                    return await ProcessOpenDeckModalAsync(interaction, customId);
                }

                // Deck confirmation button
                if (customId.StartsWith("confirm_deck_", StringComparison.Ordinal))
                {
                    return await ProcessDeckConfirmAsync(interaction, customId);
                }

                // Deck revise button
                if (customId.StartsWith("revise_deck_", StringComparison.Ordinal))
                {
                    return await ProcessDeckReviseAsync(interaction, customId);
                }

                // Replay submission modal button
                if (customId.StartsWith("open_replay_modal_", StringComparison.Ordinal))
                {
                    return await ProcessOpenReplayModalAsync(interaction, customId);
                }

                // Refresh opponent status button
                if (customId.StartsWith("refresh_opponent_", StringComparison.Ordinal))
                {
                    return await ProcessRefreshOpponentAsync(interaction, customId);
                }

                // Forfeit game button
                if (customId.StartsWith("forfeit_game_", StringComparison.Ordinal))
                {
                    return await ProcessForfeitGameRequestAsync(interaction, customId);
                }

                // Confirm forfeit game button
                if (customId.StartsWith("confirm_forfeit_game_", StringComparison.Ordinal))
                {
                    return await ProcessConfirmForfeitGameAsync(interaction, customId);
                }

                // Cancel forfeit game button
                if (customId.StartsWith("cancel_forfeit_game_", StringComparison.Ordinal))
                {
                    return await ProcessCancelForfeitGameAsync(interaction, customId);
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
                // Placeholder, add select menu interactions here
                return Result.CreateSuccess("No select menu handlers registered");
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
        /// Publishes events for cross-boundary communication.
        /// </summary>
        public static async Task<Result> ProcessModalSubmitAsync(DiscordClient client, ModalSubmittedEventArgs args)
        {
            var interaction = args.Interaction;
            var customId = interaction.Data.CustomId;

            try
            {
                // Deck code submission modal
                if (customId.StartsWith("submit_deck_", StringComparison.Ordinal))
                {
                    return await ProcessDeckSubmissionAsync(interaction, customId);
                }

                // Replay file submission modal
                if (customId.StartsWith("submit_replay_", StringComparison.Ordinal))
                {
                    return await ProcessReplaySubmissionAsync(interaction, customId);
                }

                return Result.Failure($"Unknown custom ID: {customId}");
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

        /// <summary>
        /// Handles button click to open replay submission modal.
        /// Shows a modal with file upload component for .rpl3 replay files.
        /// </summary>
        private static async Task<Result> ProcessOpenReplayModalAsync(DiscordInteraction interaction, string customId)
        {
            // Parse game ID from custom ID: "open_replay_modal_{gameId}"
            var parts = customId.Replace("open_replay_modal_", "", StringComparison.Ordinal).Split('_');
            if (parts.Length < 1 || !Guid.TryParse(parts[0], out var gameId))
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Invalid game information.").AsEphemeral()
                );
                return Result.Failure("Invalid game information");
            }

            try
            {
                // Build modal with file upload component
                var fileUploadComponent = new DiscordFileUploadComponent(
                    customId: "replay_file",
                    minValues: 1,
                    maxValues: 1,
                    isRequired: true
                );

                var modal = new DiscordModalBuilder()
                    .WithTitle("Submit Replay File")
                    .WithCustomId($"submit_replay_{gameId}")
                    .AddFileUpload(fileUploadComponent, "Replay File", "Upload your .rpl3 replay file");

                await interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
                return Result.CreateSuccess("Modal shown");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to show replay modal for game {gameId}",
                    nameof(ProcessOpenReplayModalAsync)
                );
                return Result.Failure($"Failed to show replay modal: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles replay file submission from modal.
        /// Downloads, parses, and stores the replay file, then updates game container.
        /// </summary>
        private static async Task<Result> ProcessReplaySubmissionAsync(DiscordInteraction interaction, string customId)
        {
            // Parse game ID from custom ID: "submit_replay_{gameId}"
            var parts = customId.Replace("submit_replay_", "", StringComparison.Ordinal).Split('_');
            if (parts.Length < 1 || !Guid.TryParse(parts[0], out var gameId))
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Invalid game information.").AsEphemeral()
                );
                return Result.Failure("Invalid game information");
            }

            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            try
            {
                // Get file attachments from modal submission - files are sent as resolved attachments
                var resolvedFiles = interaction.Data.Resolved?.Attachments;
                if (resolvedFiles is null || !resolvedFiles.Any())
                {
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder().WithContent("No replay file attached. Please try again.")
                    );
                    return Result.Failure("No replay file attached");
                }

                var attachment = resolvedFiles.First().Value;

                // Validate file type
                if (
                    attachment?.FileName is null
                    || !attachment.FileName.EndsWith(".rpl3", StringComparison.OrdinalIgnoreCase)
                )
                {
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder().WithContent(
                            "Invalid file type. Only .rpl3 replay files are accepted."
                        )
                    );
                    return Result.Failure("Invalid file type");
                }

                // Download the file
                using var httpClient = new HttpClient();
                var fileBytes = await httpClient.GetByteArrayAsync(attachment.Url);

                // Get the match ID for this game
                var gameResult = await Core.Common.Services.CoreService.Games.GetByIdAsync(
                    gameId,
                    Common.Data.Interfaces.DatabaseComponent.Repository
                );
                if (!gameResult.Success || gameResult.Data is null)
                {
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder().WithContent("Failed to retrieve game information.")
                    );
                    return Result.Failure("Game not found");
                }

                var matchId = gameResult.Data.MatchId;

                // Parse replay using ReplayCore
                var parseResult = Core.Common.Models.Common.ReplayCore.Parser.ParseReplayFile(
                    fileBytes,
                    gameId,
                    matchId,
                    null,
                    attachment.FileName
                );

                if (!parseResult.Success || parseResult.Data is null)
                {
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder().WithContent(
                            $"Failed to parse replay file: {parseResult.ErrorMessage}"
                        )
                    );
                    return Result.Failure($"Failed to parse replay: {parseResult.ErrorMessage}");
                }

                var replay = parseResult.Data;

                // Get player name for zip filename
                var playerResult = await Core.Common.Services.CoreService.WithDbContext(async db =>
                {
                    return await db
                        .Players.Where(p => p.MashinaUser != null && p.MashinaUser.DiscordUserId == interaction.User.Id)
                        .FirstOrDefaultAsync();
                });

                if (playerResult is null)
                {
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder().WithContent(
                            "Could not find your player account. Please contact an administrator."
                        )
                    );
                    return Result.Failure("Player not found");
                }

                // Get game details for zip filename
                var game = gameResult.Data;
                var mapName = game.Map?.Name ?? "Unknown";
                var divisionName = game.Team1Division?.Name ?? "Unknown";
                var playerName = playerResult.MashinaUser?.DiscordUsername ?? "Unknown";
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");

                // Create a compressed zip file containing the single replay
                // This saves disk space and makes downloads faster
                var zipFileName = $"{playerName}-Game{game.GameNumber}-{divisionName}-{mapName}-{timestamp}.zip";

                // Sanitize filename
                zipFileName = string.Join("_", zipFileName.Split(Path.GetInvalidFileNameChars()));

                // Create temp zip file
                var tempDir = Path.Combine(Path.GetTempPath(), "WabbitBot", "ReplaySubmissions");
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }
                var tempZipPath = Path.Combine(tempDir, zipFileName);

                try
                {
                    // Create zip with the replay file
                    using (
                        var zipArchive = System.IO.Compression.ZipFile.Open(
                            tempZipPath,
                            System.IO.Compression.ZipArchiveMode.Create
                        )
                    )
                    {
                        var entry = zipArchive.CreateEntry(attachment.FileName);
                        using var entryStream = entry.Open();
                        await entryStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                    }

                    // Read the zip file and save it to the replay storage
                    var zipBytes = await File.ReadAllBytesAsync(tempZipPath);
                    var savedFilePath = await CoreService.FileSystem.SaveReplayFileAsync(zipBytes, zipFileName);

                    if (savedFilePath is null)
                    {
                        await interaction.EditOriginalResponseAsync(
                            new DiscordWebhookBuilder().WithContent("Failed to store replay file.")
                        );
                        return Result.Failure("Failed to store replay file");
                    }

                    // Update replay with file path
                    replay.FilePath = savedFilePath;
                }
                finally
                {
                    // Clean up temp file
                    if (File.Exists(tempZipPath))
                    {
                        try
                        {
                            File.Delete(tempZipPath);
                        }
                        catch
                        {
                            // Ignore cleanup errors
                        }
                    }
                }

                // Save to database
                var saveResult = await Core.Common.Services.CoreService.Replays.CreateAsync(
                    replay,
                    Common.Data.Interfaces.DatabaseComponent.Repository
                );

                if (!saveResult.Success || saveResult.Data is null)
                {
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder().WithContent(
                            $"Failed to save replay data: {saveResult.ErrorMessage}"
                        )
                    );
                    return Result.Failure($"Failed to save replay: {saveResult.ErrorMessage}");
                }

                // Publish PlayerReplaySubmitted event with replay ID
                await PublishPlayerReplaySubmittedAsync(gameId, playerResult.Id, replay.Id);

                await interaction.EditOriginalResponseAsync(
                    new DiscordWebhookBuilder().WithContent("✅ Replay file submitted successfully!")
                );

                return Result.CreateSuccess("Replay submitted");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to process replay submission for game {gameId}",
                    nameof(ProcessReplaySubmissionAsync)
                );

                try
                {
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder().WithContent(
                            "An error occurred while processing your replay. Please try again."
                        )
                    );
                }
                catch
                {
                    // Ignore if response edit fails
                }

                return Result.Failure($"Failed to process replay submission: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles deck code modal submission from the game embed.
        /// Extracts deck code from modal, parses it, and publishes event to update game state.
        /// </summary>
        private static async Task<Result> ProcessDeckSubmissionAsync(DiscordInteraction interaction, string customId)
        {
            try
            {
                // Parse game ID from custom ID: "submit_deck_{gameId}"
                var gameIdStr = customId.Replace("submit_deck_", "", StringComparison.Ordinal);
                if (!Guid.TryParse(gameIdStr, out var gameId))
                {
                    await interaction.CreateResponseAsync(
                        DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent("Invalid game information.").AsEphemeral()
                    );
                    return Result.Failure("Invalid game information");
                }

                // Extract deck code from modal submission
                var deckCodeComponent =
                    interaction.Data.Components?.FirstOrDefault(c => c.CustomId == "deck_code")
                    as DiscordTextInputComponent;
                var deckCode = deckCodeComponent?.Value?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(deckCode))
                {
                    await interaction.CreateResponseAsync(
                        DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent("Deck code cannot be empty.").AsEphemeral()
                    );
                    return Result.Failure("Deck code cannot be empty");
                }

                await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

                // Parse deck code to extract division information
                var deckParseResult = Core.Common.Utilities.DeckParser.DecodeDeckString(deckCode);
                if (!deckParseResult.Success || deckParseResult.Data is null)
                {
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder().WithContent(
                            $"❌ Invalid deck code: {deckParseResult.ErrorMessage ?? "Unable to parse deck"}"
                        )
                    );
                    return Result.Failure($"Invalid deck code: {deckParseResult.ErrorMessage}");
                }

                var deck = deckParseResult.Data;
                var divisionId = deck.Division.Id;
                var divisionName = deck.Division.Descriptor; // May be null if no lookup service provided
                if (divisionName is null)
                {
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder().WithContent(
                            $"Could not find the division name. Error: {deckParseResult.ErrorMessage}"
                        )
                    );
                    return Result.Failure($"Could not find the division name. Error: {deckParseResult.ErrorMessage}");
                }

                // Get player ID from MashinaUser using Discord User ID
                var playerResult = await Core.Common.Services.CoreService.WithDbContext(async db =>
                {
                    return await db
                        .Players.Where(p => p.MashinaUser != null && p.MashinaUser.DiscordUserId == interaction.User.Id)
                        .FirstOrDefaultAsync();
                });

                if (playerResult is null)
                {
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder().WithContent("Could not find player entity.")
                    );
                    return Result.Failure("Could not find player entity.");
                }

                await PublishPlayerDeckSubmittedAsync(gameId, playerResult.Id, deckCode, divisionId, divisionName);

                await interaction.EditOriginalResponseAsync(
                    new DiscordWebhookBuilder().WithContent("✅ Deck code submitted successfully!")
                );

                return Result.CreateSuccess("Deck submitted");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to process deck submission",
                    nameof(ProcessDeckSubmissionAsync)
                );

                try
                {
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder().WithContent(
                            "An error occurred while processing your deck code. Please try again."
                        )
                    );
                }
                catch
                {
                    // Ignore if response edit fails
                }

                return Result.Failure($"Failed to process deck submission: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles deck confirmation button from the game embed.
        /// Marks the deck as confirmed in the game state.
        /// </summary>
        private static async Task<Result> ProcessDeckConfirmAsync(DiscordInteraction interaction, string customId)
        {
            try
            {
                // Parse game ID from custom ID: "confirm_deck_{gameId}"
                var gameIdStr = customId.Replace("confirm_deck_", "", StringComparison.Ordinal);
                if (!Guid.TryParse(gameIdStr, out var gameId))
                {
                    await interaction.CreateResponseAsync(
                        DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent("Invalid game information.").AsEphemeral()
                    );
                    return Result.Failure("Invalid game information");
                }

                await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

                // Get player ID from MashinaUser using Discord User ID
                var playerResult = await Core.Common.Services.CoreService.WithDbContext(async db =>
                {
                    return await db
                        .Players.Where(p => p.MashinaUser != null && p.MashinaUser.DiscordUserId == interaction.User.Id)
                        .FirstOrDefaultAsync();
                });

                if (playerResult is null)
                {
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder().WithContent(
                            "Could not find your player account. Please contact an administrator."
                        )
                    );
                    return Result.Failure("Player not found");
                }

                // Publish event to mark deck as confirmed in GameStateSnapshot
                await DiscBotService.PublishAsync(new Common.Events.Core.PlayerDeckConfirmed(gameId, playerResult.Id));

                await interaction.EditOriginalResponseAsync(
                    new DiscordWebhookBuilder().WithContent("✅ Deck confirmed!")
                );

                return Result.CreateSuccess("Deck confirmed");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to process deck confirmation",
                    nameof(ProcessDeckConfirmAsync)
                );

                try
                {
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder().WithContent(
                            "An error occurred while confirming your deck. Please try again."
                        )
                    );
                }
                catch
                {
                    // Ignore if response edit fails
                }

                return Result.Failure($"Failed to process deck confirmation: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles button click to open deck submission modal.
        /// Shows a modal with text input for deck code.
        /// </summary>
        private static async Task<Result> ProcessOpenDeckModalAsync(DiscordInteraction interaction, string customId)
        {
            // Parse game ID from custom ID: "open_deck_modal_{gameId}"
            var gameIdStr = customId.Replace("open_deck_modal_", "", StringComparison.Ordinal);
            if (!Guid.TryParse(gameIdStr, out var gameId))
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Invalid game information.").AsEphemeral()
                );
                return Result.Failure("Invalid game information");
            }

            try
            {
                // Build modal with text input for deck code
                var deckCodeInput = new DiscordTextInputComponent(
                    customId: "deck_code",
                    placeholder: "Paste your deck code here",
                    value: null,
                    required: true,
                    style: DiscordTextInputStyle.Paragraph,
                    min_length: 10,
                    max_length: 2000
                );

                var modal = new DiscordModalBuilder()
                    .WithTitle("Submit Deck Code")
                    .WithCustomId($"submit_deck_{gameId}")
                    .AddTextInput(deckCodeInput, "Deck Code");

                await interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
                return Result.CreateSuccess("Deck modal shown");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to show deck modal for game {gameId}",
                    nameof(ProcessOpenDeckModalAsync)
                );
                return Result.Failure($"Failed to show deck modal: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles deck revise button click from the game embed.
        /// Resets deck confirmation state to allow re-submission.
        /// </summary>
        private static async Task<Result> ProcessDeckReviseAsync(DiscordInteraction interaction, string customId)
        {
            try
            {
                // Parse game ID from custom ID: "revise_deck_{gameId}"
                var gameIdStr = customId.Replace("revise_deck_", "", StringComparison.Ordinal);
                if (!Guid.TryParse(gameIdStr, out var gameId))
                {
                    await interaction.CreateResponseAsync(
                        DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent("Invalid game information.").AsEphemeral()
                    );
                    return Result.Failure("Invalid game information");
                }

                await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

                // Get player ID from MashinaUser using Discord User ID
                var playerResult = await Core.Common.Services.CoreService.WithDbContext(async db =>
                {
                    return await db
                        .Players.Where(p => p.MashinaUser != null && p.MashinaUser.DiscordUserId == interaction.User.Id)
                        .FirstOrDefaultAsync();
                });

                if (playerResult is null)
                {
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder().WithContent(
                            "Could not find your player account. Please contact an administrator."
                        )
                    );
                    return Result.Failure("Player not found");
                }

                // Publish event to reset deck confirmation in GameStateSnapshot
                await DiscBotService.PublishAsync(new Common.Events.Core.PlayerDeckRevised(gameId, playerResult.Id));

                await interaction.EditOriginalResponseAsync(
                    new DiscordWebhookBuilder().WithContent("✅ Deck revision initiated. Please submit a new deck code.")
                );

                return Result.CreateSuccess("Deck revision initiated");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to process deck revision",
                    nameof(ProcessDeckReviseAsync)
                );

                try
                {
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder().WithContent(
                            "An error occurred while revising your deck. Please try again."
                        )
                    );
                }
                catch
                {
                    // Ignore if response edit fails
                }

                return Result.Failure($"Failed to process deck revision: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles refresh opponent status button click from the game embed.
        /// Triggers a re-render of the game embed with updated opponent team status.
        /// Rate limited to once per 5 seconds per game container.
        /// </summary>
        private static async Task<Result> ProcessRefreshOpponentAsync(DiscordInteraction interaction, string customId)
        {
            // Parse game ID from custom ID: "refresh_opponent_{gameId}"
            var gameIdStr = customId.Replace("refresh_opponent_", "", StringComparison.Ordinal);
            if (!Guid.TryParse(gameIdStr, out var gameId))
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Invalid game information.").AsEphemeral()
                );
                return Result.Failure("Invalid game information");
            }

            // Check rate limit for this game container
            // Use the channel/thread ID to differentiate between the two team containers
            // Each team has their own thread, so this effectively rate limits per team
            var rateLimitKey = $"{gameIdStr}_{interaction.ChannelId}";
            int? rateLimitSeconds = null;

            lock (_refreshLock)
            {
                // Periodically clean up old entries (1 in 20 chance)
                if (Random.Shared.Next(20) == 0)
                {
                    CleanupOldRateLimitEntries();
                }

                if (_lastRefreshTimes.TryGetValue(rateLimitKey, out var lastRefresh))
                {
                    var timeSinceLastRefresh = DateTime.UtcNow - lastRefresh;
                    if (timeSinceLastRefresh < _refreshCooldown)
                    {
                        rateLimitSeconds = (int)Math.Ceiling((_refreshCooldown - timeSinceLastRefresh).TotalSeconds);
                    }
                }

                // Only update if not rate limited
                if (rateLimitSeconds is null)
                {
                    _lastRefreshTimes[rateLimitKey] = DateTime.UtcNow;
                }
            }

            // Handle rate limit outside the lock
            if (rateLimitSeconds.HasValue)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent(
                            $"⏳ Please wait **{rateLimitSeconds.Value} second{(rateLimitSeconds.Value != 1 ? "s" : "")}** before refreshing again."
                        )
                        .AsEphemeral()
                );
                return Result.Failure("Rate limited");
            }

            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            // Trigger a re-render of the game container to show updated opponent status
            var refreshResult = await Renderers.GameRenderer.RefreshGameContainerAsync(gameId);

            if (!refreshResult.Success)
            {
                await interaction.CreateFollowupMessageAsync(
                    new DiscordFollowupMessageBuilder()
                        .WithContent($"⚠️ Failed to refresh: {refreshResult.Message}")
                        .AsEphemeral()
                );
                return Result.Failure(refreshResult.Message ?? "Failed to refresh game container");
            }

            return Result.CreateSuccess("Refresh triggered");
        }

        #region Forfeit Game
        private static async Task<Result> ProcessForfeitGameRequestAsync(
            DiscordInteraction interaction,
            string customId
        )
        {
            // Parse game ID from custom ID: "forfeit_game_{gameId}"
            var gameIdStr = customId.Replace("forfeit_game_", "", StringComparison.Ordinal);
            if (!Guid.TryParse(gameIdStr, out var gameId))
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Invalid game ID.").AsEphemeral()
                );
                return Result.Failure("Invalid game ID");
            }

            // Get game to verify it exists and is in progress
            var getGame = await CoreService.Games.GetByIdAsync(gameId, DatabaseComponent.Repository);
            if (!getGame.Success || getGame.Data is null)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Game not found.").AsEphemeral()
                );
                return Result.Failure("Game not found");
            }

            var game = getGame.Data;
            var currentStatus = MatchCore.Accessors.GetCurrentStatus(game);

            if (currentStatus != GameStatus.InProgress && currentStatus != GameStatus.Created)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("You can only forfeit games that are in progress or not yet started.")
                        .AsEphemeral()
                );
                return Result.Failure("Game is not in progress");
            }

            // Verify user is in this game
            var player = await CoreService.WithDbContext(async db =>
                await db.Players.FirstOrDefaultAsync(p => p.MashinaUser.DiscordUserId == interaction.User.Id)
            );

            if (player is null)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("Player not found. You must be registered to forfeit.")
                        .AsEphemeral()
                );
                return Result.Failure("Player not found");
            }

            bool isOnTeam = game.Team1PlayerIds.Contains(player.Id) || game.Team2PlayerIds.Contains(player.Id);

            if (!isOnTeam)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("You are not playing in this game and cannot forfeit for your team.")
                        .AsEphemeral()
                );
                return Result.Failure("User not in game");
            }

            // Determine which team the player is on
            var teamId = game.Team1PlayerIds.Contains(player.Id) ? game.Match.Team1Id : game.Match.Team2Id;

            // Show confirmation dialog with confirm/cancel buttons
            var confirmButton = new DiscordButtonComponent(
                DiscordButtonStyle.Danger,
                $"confirm_forfeit_game_{gameId}_{teamId}",
                "Confirm Forfeit"
            );
            var cancelButton = new DiscordButtonComponent(
                DiscordButtonStyle.Secondary,
                $"cancel_forfeit_game_{gameId}_{teamId}",
                "Cancel"
            );

            var actionRow = new DiscordActionRowComponent([confirmButton, cancelButton]);
            var components = new List<DiscordComponent> { actionRow };
            var container = new DiscordContainerComponent(components);

            await interaction.CreateResponseAsync(
                DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent(
                        "⚠️ **Are you sure you want to forfeit this game?**\n\n"
                            + "Forfeiting a game will forfeit the entire match and result in a loss for your team.\n"
                            + "This action cannot be undone."
                    )
                    .AsEphemeral()
            );

            // Send the buttons as a follow-up message with container
            await interaction.CreateFollowupMessageAsync(
                new DiscordFollowupMessageBuilder().AddContainerComponent(container).AsEphemeral()
            );

            return Result.CreateSuccess("Forfeit confirmation shown");
        }

        private static async Task<Result> ProcessConfirmForfeitGameAsync(
            DiscordInteraction interaction,
            string customId
        )
        {
            // Parse game ID and team ID from custom ID: "confirm_forfeit_game_{gameId}_{teamId}"
            var parts = customId.Replace("confirm_forfeit_game_", "", StringComparison.Ordinal).Split('_');
            if (
                parts.Length != 2
                || !Guid.TryParse(parts[0], out var gameId)
                || !Guid.TryParse(parts[1], out var forfeitingTeamId)
            )
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Invalid game or team ID.").AsEphemeral()
                );
                return Result.Failure("Invalid game or team ID");
            }

            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            // Get game and match
            var getGame = await CoreService.Games.GetByIdAsync(gameId, DatabaseComponent.Repository);
            if (!getGame.Success || getGame.Data is null)
            {
                return Result.Failure("Game not found");
            }

            var game = getGame.Data;
            var match = game.Match;

            // Determine winner (the team that didn't forfeit)
            var winnerTeamId = forfeitingTeamId == match.Team1Id ? match.Team2Id : match.Team1Id;

            // TODO: Call Core logic to forfeit the game (which forfeits the entire match)
            // This should:
            // 1. Update game state to Forfeited
            // 2. Update match state to Forfeited
            // 3. Set winner for both game and match
            // 4. Publish MatchForfeited event
            // For now, we'll just show a placeholder message

            try
            {
                await interaction.EditOriginalResponseAsync(
                    new DiscordWebhookBuilder().WithContent(
                        "✅ **Game and match forfeited.**\n\n"
                            + "The game and match have been forfeited, and the opposing team has been awarded the win."
                    )
                );
            }
            catch
            {
                // Ignore if we can't edit the response
            }

            return Result.CreateSuccess("Game forfeited");
        }

        private static async Task<Result> ProcessCancelForfeitGameAsync(DiscordInteraction interaction, string customId)
        {
            // No need to parse IDs, just acknowledge the cancellation
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            try
            {
                await interaction.EditOriginalResponseAsync(
                    new DiscordWebhookBuilder().WithContent("Forfeit cancelled.")
                );
            }
            catch
            {
                // Ignore if we can't edit the response
            }

            return Result.CreateSuccess("Forfeit cancelled");
        }
        #endregion
    }
}
