using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.ErrorService;
using WabbitBot.Common.Models;


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

            _lazyEventBus = new Lazy<IDiscBotEventBus>(
                () => eventBus, LazyThreadSafetyMode.ExecutionAndPublication);
            _lazyErrorHandler = new Lazy<IErrorService>(
                () => errorHandler, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        // Testability Hooks
        internal static void SetTestServices(
            IDiscBotEventBus eventBus,
            IErrorService errorHandler)
        {
            _lazyEventBus = new Lazy<IDiscBotEventBus>(
                () => eventBus, LazyThreadSafetyMode.ExecutionAndPublication);
            _lazyErrorHandler = new Lazy<IErrorService>(
                () => errorHandler, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Publishes an event with basic null validation
        /// </summary>
        public static Task PublishAsync<TEvent>(TEvent evt) where TEvent : class, IEvent
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