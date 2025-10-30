using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.ErrorService;
using WabbitBot.Common.Events.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.DiscBot.App.Commands;
using WabbitBot.DiscBot.App.Services.DiscBot;

namespace WabbitBot.DiscBot.App
{
    /// <summary>
    /// Bootstrap class for initializing the DiscBot Discord client and wiring up all components.
    /// Called from Host Program.cs to set up the Discord client without exposing DSharpPlus to Core.
    /// </summary>
    public static class DiscBotBootstrap
    {
        private static DiscordClient? _client;
        private static bool _isInitialized;

        /// <summary>
        /// Initializes the DiscBot event bus and services from Host.
        /// Must be called before StartDiscordClientAsync.
        /// </summary>
        /// <param name="discBotEventBus">The DiscBot event bus instance</param>
        /// <param name="errorService">The shared error service instance</param>
        public static async Task InitializeServicesAsync(IDiscBotEventBus discBotEventBus, IErrorService errorService)
        {
            ArgumentNullException.ThrowIfNull(discBotEventBus);
            ArgumentNullException.ThrowIfNull(errorService);

            // Initialize DiscBotService static internals
            DiscBotService.Initialize(discBotEventBus, errorService);

            // Initialize handlers (subscribe to events)
            Handlers.GameHandler.Initialize();
            Handlers.ScrimmageHandler.Initialize();

            // Start background batch processor for game container updates
            Handlers.GameHandler.StartBatchProcessor();

            // Initialize temp storage for attachment downloads
            DiscBotService.TempStorage.Initialize();

            // Start periodic cleanup of temp files (15 min interval, 1 hour retention)
            _ = DiscBotService.TempStorage.StartPeriodicCleanup(
                interval: TimeSpan.FromMinutes(15),
                maxAge: TimeSpan.FromHours(1)
            );

            // Start private-thread container cleanup (1 min sweep, 5 min inactivity)
            _ = DiscBotService.ThreadContainers.StartCleanupLoop(
                sweepInterval: TimeSpan.FromMinutes(1),
                inactivity: TimeSpan.FromMinutes(5)
            );

            // Initialize the event bus
            await discBotEventBus.InitializeAsync();
        }

        /// <summary>
        /// Builds and starts the DiscordClient with DSharpPlus 5.0.
        /// Registers commands and sets up interaction handlers.
        /// </summary>
        /// <param name="config">The bot configuration service</param>
        /// <param name="environment">The current environment (Development, Production, etc.)</param>
        public static async Task StartDiscordClientAsync(IBotConfigurationService config, string environment)
        {
            ArgumentNullException.ThrowIfNull(config);

            if (_isInitialized)
            {
                throw new InvalidOperationException("Discord client has already been initialized");
            }

            try
            {
                // Build Discord client using DSharpPlus 5.0 with Commands only
                _client = DiscordClientBuilder
                    .CreateDefault(
                        config.GetToken(),
                        DiscordIntents.MessageContents
                            | DiscordIntents.DirectMessages
                            | DiscordIntents.GuildMessages
                            | DiscordIntents.Guilds
                            | DiscordIntents.GuildMembers,
                        null // No service collection - we avoid DI
                    )
                    .UseCommands(
                        (services, commands) =>
                        {
                            // Configure slash commands only (DSharpPlus.Commands)
                            commands.AddProcessor<SlashCommandProcessor>();

                            // Register command classes
                            // For development: Register to specific guild for instant updates
                            // For production: Register globally (takes ~1 hour to update)
                            var debugGuildId = config.GetDebugGuildId();
                            var isDevelopment = string.Equals(
                                environment,
                                "Development",
                                StringComparison.OrdinalIgnoreCase
                            );

                            if (debugGuildId.HasValue)
                            {
                                // Guild-specific registration for development (instant updates)
                                commands.AddCommands<ScrimmageCommands>(debugGuildId.Value);
                                commands.AddCommands<UserCommands>(debugGuildId.Value);
                                commands.AddCommands<TeamCommands>(debugGuildId.Value);
                                commands.AddCommands<ConfigCommands>(debugGuildId.Value);
                                commands.AddCommands<AdminCommands>(debugGuildId.Value);
                                Console.WriteLine(
                                    $"🔧 [{environment.ToUpperInvariant()}] Commands registered to guild"
                                        + $" {debugGuildId.Value} for instant updates"
                                );
                            }
                            else
                            {
                                // Global registration for production (takes up to 1 hour to update)
                                commands.AddCommands<ScrimmageCommands>();
                                commands.AddCommands<UserCommands>();
                                commands.AddCommands<TeamCommands>();
                                commands.AddCommands<ConfigCommands>();
                                commands.AddCommands<AdminCommands>();
                                if (isDevelopment)
                                {
                                    Console.WriteLine(
                                        "⚠️  [DEVELOPMENT] Commands registered globally - updates may take up to"
                                            + " 1 hour. Set Bot:DebugGuildId for instant updates."
                                    );
                                }
                                else
                                {
                                    Console.WriteLine(
                                        $"🌍 [{environment.ToUpperInvariant()}] Commands registered globally"
                                    );
                                }
                            }
                        }
                    )
                    .ConfigureEventHandlers(b =>
                    {
                        // Handle socket errors
                        b.HandleSocketClosed(
                            (client, args) =>
                            {
                                var error = new Exception(
                                    $"Socket closed with code {args.CloseCode}: {args.CloseMessage}"
                                );
                                return DiscBotService.ErrorHandler.CaptureAsync(
                                    error,
                                    "Discord socket closed",
                                    nameof(DiscBotBootstrap)
                                );
                            }
                        );

                        // Handle zombied connection
                        b.HandleZombied(
                            (client, args) =>
                            {
                                var error = new Exception("Discord connection zombied");
                                return DiscBotService.ErrorHandler.CaptureAsync(
                                    error,
                                    "Discord connection zombied",
                                    nameof(DiscBotBootstrap)
                                );
                            }
                        );

                        // Handle component interactions (buttons, selects, etc.)
                        b.HandleComponentInteractionCreated(
                            async (client, args) =>
                            {
                                await HandleComponentInteractionAsync(client, args);
                            }
                        );

                        // Handle modal submissions
                        b.HandleModalSubmitted(
                            async (client, args) =>
                            {
                                await HandleModalSubmitAsync(client, args);
                            }
                        );

                        // Handle message creation for reply-based add flow
                        b.HandleMessageCreated(
                            async (client, args) =>
                            {
                                try
                                {
                                    if (args.Message.ReferencedMessage is null)
                                    {
                                        return;
                                    }
                                    var replied = args.Message.ReferencedMessage;

                                    // Retrieve TeamApp state via reflection to avoid direct dependency
                                    var state = TeamApp.TeamApp_GetStateSafe(replied.Id);
                                    if (
                                        state is null
                                        || !state.AwaitingAddInput
                                        || state.SelectedTeamId is null
                                        || state.SelectedRosterGroup is null
                                    )
                                    {
                                        return;
                                    }

                                    // Parse mention/ID
                                    var text = args.Message.Content?.Trim() ?? string.Empty;
                                    if (string.IsNullOrWhiteSpace(text))
                                    {
                                        return;
                                    }
                                    if (!TeamApp.TeamApp_TryParseDiscordId(text, out var discordUserId))
                                    {
                                        await args.Message.RespondAsync(
                                            "Provide a valid mention (<@...>) or numeric ID."
                                        );
                                        return;
                                    }

                                    // Delegate to TeamApp helper to add
                                    var result = await TeamApp.TeamApp_TryAddPlayerFromDiscordIdAsync(
                                        replied,
                                        state,
                                        discordUserId
                                    );
                                    if (!result.Success)
                                    {
                                        await args.Message.RespondAsync(result.ErrorMessage ?? "Failed to add player.");
                                        return;
                                    }

                                    await args.Message.RespondAsync("✅ Player added.");
                                }
                                catch (Exception ex)
                                {
                                    await DiscBotService.ErrorHandler.CaptureAsync(
                                        ex,
                                        "Failed to handle add-player reply",
                                        nameof(DiscBotBootstrap)
                                    );
                                }
                            }
                        );
                    })
                    .Build();

                // Register the client with the provider for other services to use
                DiscordClientProvider.SetClient(_client);

                // Connect to Discord
                await _client.ConnectAsync();

                // Load persisted thread tracking into memory
                await DiscBotService.ThreadContainers.LoadPersistedAsync();

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to initialize Discord client",
                    nameof(StartDiscordClientAsync)
                );
                throw;
            }
        }

        /// <summary>
        /// Handles component interactions (buttons, dropdowns) by publishing DiscBot-local events.
        /// Uses the hybrid pattern: returns Result for immediate feedback and publishes events for cross-boundary communication.
        /// </summary>
        private static async Task HandleComponentInteractionAsync(
            DiscordClient client,
            global::DSharpPlus.EventArgs.ComponentInteractionCreatedEventArgs args
        )
        {
            try
            {
                // Parse the custom ID to determine the interaction type
                var customId = args.Interaction.Data.CustomId;

                // Delegate to appropriate handler based on custom ID pattern
                // Handlers return Result for immediate feedback
                Result? result = null;

                // Check if it's a select menu or button based on component type
                var isSelectMenu = args.Interaction.Data.ComponentType == DiscordComponentType.StringSelect;

                // Route scrimmage-related interactions
                if (
                    customId.StartsWith("accept_challenge_", StringComparison.Ordinal)
                    || customId.StartsWith("decline_challenge_", StringComparison.Ordinal)
                    || customId.StartsWith("cancel_challenge_", StringComparison.Ordinal)
                    || customId.StartsWith("challenge_issue_", StringComparison.Ordinal)
                    || customId.StartsWith("challenge_cancel_", StringComparison.Ordinal)
                )
                {
                    result = await ScrimmageApp.ProcessButtonInteractionAsync(client, args);
                }
                else if (
                    isSelectMenu
                    && (
                        customId.StartsWith("challenge_", StringComparison.Ordinal)
                        || customId.StartsWith("select_teammates_", StringComparison.Ordinal)
                    )
                )
                {
                    result = await Handlers.ScrimmageHandler.HandleSelectMenuInteractionAsync(client, args);
                }
                // Route team roles editor interactions
                else if (isSelectMenu && customId.StartsWith("team_roles_", StringComparison.Ordinal))
                {
                    result = await TeamApp.ProcessSelectMenuInteractionAsync(client, args);
                }
                else if (
                    customId.StartsWith("team_roles_", StringComparison.Ordinal)
                    || customId.StartsWith("confirm_delete_team_", StringComparison.Ordinal)
                    || customId.StartsWith("confirm_delete_roster_", StringComparison.Ordinal)
                    || customId.Equals("cancel_destructive", StringComparison.Ordinal)
                )
                {
                    result = await TeamApp.ProcessButtonInteractionAsync(client, args);
                }
                else if (customId.StartsWith("match_", StringComparison.Ordinal))
                {
                    result = await MatchApp.ProcessButtonInteractionAsync(client, args);
                }
                else if (customId.StartsWith("game_", StringComparison.Ordinal))
                {
                    result = await GameApp.ProcessButtonInteractionAsync(client, args);
                }
                // Add more handlers as needed

                // Log failures (success is typically logged by the handler itself)
                if (result is not null && !result.Success)
                {
                    await DiscBotService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException(result.ErrorMessage ?? "Unknown error"),
                        $"Handler failed for interaction: {customId}",
                        nameof(HandleComponentInteractionAsync)
                    );
                }
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle component interaction: {args.Interaction.Data.CustomId}",
                    nameof(HandleComponentInteractionAsync)
                );
            }
        }

        /// <summary>
        /// Handles modal submissions by publishing DiscBot-local events.
        /// </summary>
        private static async Task HandleModalSubmitAsync(
            DiscordClient client,
            global::DSharpPlus.EventArgs.ModalSubmittedEventArgs args
        )
        {
            try
            {
                var customId = args.Interaction.Data.CustomId;

                Result? result = null;

                // Delegate to appropriate handler based on custom ID pattern
                if (
                    customId.StartsWith("submit_deck_", StringComparison.Ordinal)
                    || customId.StartsWith("submit_replay_", StringComparison.Ordinal)
                )
                {
                    result = await GameApp.ProcessModalSubmitAsync(client, args);
                }
                else if (customId.StartsWith("team_roles_admin_add_modal", StringComparison.Ordinal))
                {
                    result = await TeamApp.ProcessModalSubmitAsync(client, args);
                }

                // Log failures (success is typically logged by the handler itself)
                if (result is not null && !result.Success)
                {
                    await DiscBotService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException(result.ErrorMessage ?? "Unknown error"),
                        $"Modal submission failed: {customId}",
                        nameof(HandleModalSubmitAsync)
                    );
                }
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle modal submission: {args.Interaction.Data.CustomId}",
                    nameof(HandleModalSubmitAsync)
                );
            }
        }

        /// <summary>
        /// Stops and disposes the Discord client.
        /// </summary>
        public static async Task StopAsync()
        {
            // Stop the batch processor first to ensure no pending updates
            Handlers.GameHandler.StopBatchProcessor();

            if (_client is not null)
            {
                try
                {
                    await _client.DisconnectAsync();
                    _client.Dispose();
                }
                catch (Exception ex)
                {
                    await DiscBotService.ErrorHandler.CaptureAsync(
                        ex,
                        "Failed to stop Discord client",
                        nameof(StopAsync)
                    );
                }
            }
        }
    }
}
