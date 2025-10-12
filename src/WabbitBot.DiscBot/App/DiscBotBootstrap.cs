using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.ErrorService;
using WabbitBot.Common.Events.Interfaces;
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
            // Handlers.MatchHandler.Initialize();
            // Handlers.GameHandler.Initialize();

            // Initialize temp storage for attachment downloads
            DiscBotService.TempStorage.Initialize();

            // Start periodic cleanup of temp files (15 min interval, 1 hour retention)
            _ = DiscBotService.TempStorage.StartPeriodicCleanup(
                interval: TimeSpan.FromMinutes(15),
                maxAge: TimeSpan.FromHours(1)
            );

            // Initialize the event bus
            await discBotEventBus.InitializeAsync();
        }

        /// <summary>
        /// Builds and starts the DiscordClient with DSharpPlus 5.0.
        /// Registers commands and sets up interaction handlers.
        /// </summary>
        /// <param name="config">The bot configuration service</param>
        public static async Task StartDiscordClientAsync(IBotConfigurationService config)
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
                            commands.AddCommands<ScrimmageCommands>();
                            // TODO: Add other command classes as they are implemented
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
                    })
                    .Build();

                // Register the client with the provider for other services to use
                DiscordClientProvider.SetClient(_client);

                // Connect to Discord
                await _client.ConnectAsync();

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
                Common.Models.Result? result = null;

                if (
                    customId.StartsWith("accept_challenge_", StringComparison.Ordinal)
                    || customId.StartsWith("decline_challenge_", StringComparison.Ordinal)
                )
                {
                    result = await ScrimmageApp.ProcessButtonInteractionAsync(client, args);
                }
                else if (customId.StartsWith("match_", StringComparison.Ordinal))
                {
                    result = await MatchApp.ProcessButtonInteractionAsync(client, args);
                }
                else if (customId.StartsWith("game_", StringComparison.Ordinal))
                {
                    result = await Handlers.GameHandler.ProcessButtonInteractionAsync(client, args);
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

                // Delegate to appropriate handler based on custom ID pattern
                // TODO: Implement modal handlers as needed
                await Task.CompletedTask;
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
