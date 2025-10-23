using DSharpPlus;
using DSharpPlus.Entities;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.ErrorService;
using WabbitBot.Common.Events.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.DiscBot.App;

namespace WabbitBot.DiscBot.App.Services.DiscBot
{
    /// <summary>
    /// Main DiscBotService that handles all DiscBot entity operations
    /// Uses service locator pattern instead of dependency injection
    /// </summary>
    public static partial class DiscBotService
    {
        private static Lazy<IDiscBotEventBus>? _lazyEventBus;
        private static Lazy<IErrorService>? _lazyErrorHandler;

        // Static service instances accessible across all projects
        public static IDiscBotEventBus EventBus => _lazyEventBus!.Value;
        public static IErrorService ErrorHandler => _lazyErrorHandler!.Value;
        public static WabbitBot.Core.Common.Services.FileSystemService FileSystem =>
            WabbitBot.Core.Common.Services.CoreService.FileSystem;

        /// <summary>
        /// Gets the Discord client instance
        /// </summary>
        public static DiscordClient Client => DiscordClientProvider.GetClient();

        /// <summary>
        /// Scrimmage channel ID cached from configuration (validated to exist in Discord)
        /// </summary>
        private static ulong? _scrimmageChannelId;

        /// <summary>
        /// Challenge feed channel ID cached from configuration (validated to exist in Discord)
        /// </summary>
        private static ulong? _challengeFeedChannelId;

        /// <summary>
        /// Gets the scrimmage channel (throws with helpful message if not configured)
        /// </summary>
        public static DiscordChannel ScrimmageChannel
        {
            get
            {
                if (_scrimmageChannelId is null)
                {
                    throw new InvalidOperationException(
                        "Scrimmage channel not configured. Please configure it using the bot setup commands."
                    );
                }

                try
                {
                    return Client.GetChannelAsync(_scrimmageChannelId.Value).Result;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to access scrimmage channel {_scrimmageChannelId}. Channel may have been deleted"
                            + " or bot lacks permissions.",
                        ex
                    );
                }
            }
        }

        /// <summary>
        /// Gets the challenge feed channel (throws with helpful message if not configured)
        /// </summary>
        public static DiscordChannel ChallengeFeedChannel
        {
            get
            {
                if (_challengeFeedChannelId is null)
                {
                    throw new InvalidOperationException(
                        "Challenge feed channel not configured. Please configure it using the bot setup commands."
                    );
                }

                try
                {
                    return Client.GetChannelAsync(_challengeFeedChannelId.Value).Result;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to access challenge feed channel {_challengeFeedChannelId}. Channel may have been deleted"
                            + " or bot lacks permissions.",
                        ex
                    );
                }
            }
        }

        /// <summary>
        /// Initializes or refreshes the cached channel configuration values
        /// Fails gracefully if configuration is invalid or missing
        /// Call this at startup and whenever channel configuration changes
        /// </summary>
        public static void RefreshChannelConfiguration()
        {
            try
            {
                var configService = ConfigurationProvider.GetConfigurationService();
                var channelsConfig = configService.GetSection<ChannelsOptions>("Bot:Channels");
                _scrimmageChannelId = channelsConfig.ScrimmageChannel;
                _challengeFeedChannelId = channelsConfig.ChallengeFeedChannel;
            }
            catch (InvalidOperationException ex)
                when (ex.Message.Contains("Configuration service has not been initialized"))
            {
                _scrimmageChannelId = null;
                LogConfigurationError(
                    "Configuration service not initialized. This should be called after"
                        + " ConfigurationProvider.Initialize().",
                    ex
                );
            }
            catch (Exception ex)
                when (ex.Message.Contains("Bot:Channels") || ex.GetType().Name.Contains("Configuration"))
            {
                _scrimmageChannelId = null;
                LogConfigurationError(
                    "Bot:Channels section not found or invalid in configuration. Please check appsettings.json.",
                    ex
                );
            }
            catch (Exception ex)
            {
                _scrimmageChannelId = null;
                LogConfigurationError(
                    "Unexpected error loading scrimmage channel configuration. Please check configuration format.",
                    ex
                );
            }
        }

        private static void LogConfigurationError(string message, Exception ex)
        {
            // Fire and forget - if logging fails, it's a systemic issue beyond our control
            _ = ErrorHandler.CaptureAsync(ex, message, nameof(RefreshChannelConfiguration));
        }

        /// <summary>
        /// Initializes DiscBotService with required dependencies.
        /// Should be called once during application startup from Core Program.cs.
        /// </summary>
        /// <param name="eventBus">The DiscBot event bus instance</param>
        /// <param name="errorHandler">The error service instance</param>
        public static void Initialize(IDiscBotEventBus eventBus, IErrorService errorHandler)
        {
            ArgumentNullException.ThrowIfNull(eventBus);
            ArgumentNullException.ThrowIfNull(errorHandler);

            _lazyEventBus = new Lazy<IDiscBotEventBus>(() => eventBus, LazyThreadSafetyMode.ExecutionAndPublication);
            _lazyErrorHandler = new Lazy<IErrorService>(
                () => errorHandler,
                LazyThreadSafetyMode.ExecutionAndPublication
            );

            // Initialize cached configuration values
            RefreshChannelConfiguration();
        }

        // Testability Hooks
        internal static void SetTestServices(IDiscBotEventBus eventBus, IErrorService errorHandler)
        {
            _lazyEventBus = new Lazy<IDiscBotEventBus>(() => eventBus, LazyThreadSafetyMode.ExecutionAndPublication);
            _lazyErrorHandler = new Lazy<IErrorService>(
                () => errorHandler,
                LazyThreadSafetyMode.ExecutionAndPublication
            );
        }

        /// <summary>
        /// Publishes an event with basic null validation
        /// </summary>
        public static Task PublishAsync<TEvent>(TEvent evt)
            where TEvent : class, IEvent
        {
            ArgumentNullException.ThrowIfNull(evt);
            return EventBus.PublishAsync(evt).AsTask();
        }

        /// <summary>
        /// Executes an operation with standardized error handling
        /// </summary>
        public static async Task<Result> TryAsync(Func<Task> op, string operationName)
        {
            try
            {
                await op();
                return Result.CreateSuccess();
            }
            catch (Exception ex)
            {
                await ErrorHandler.CaptureAsync(ex, $"Operation failed: {operationName}", operationName);
                return Result.Failure($"An error occurred during {operationName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes a synchronous operation with standardized error handling
        /// </summary>
        public static Task<Result> TrySync(Action op, string operationName)
        {
            try
            {
                op();
                return Task.FromResult(Result.CreateSuccess());
            }
            catch (Exception ex)
            {
                ErrorHandler.CaptureAsync(ex, $"Operation failed: {operationName}", operationName);
                return Task.FromResult(Result.Failure($"An error occurred during {operationName}: {ex.Message}"));
            }
        }

        /// <summary>
        /// Executes an operation with standardized error handling and returns a result
        /// </summary>
        public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> op, string operationName)
        {
            try
            {
                var result = await op();
                return Result<T>.CreateSuccess(result);
            }
            catch (Exception ex)
            {
                await ErrorHandler.CaptureAsync(ex, $"Operation failed: {operationName}", operationName);
                return Result<T>.Failure($"An error occurred during {operationName}: {ex.Message}");
            }
        }
    }
}
