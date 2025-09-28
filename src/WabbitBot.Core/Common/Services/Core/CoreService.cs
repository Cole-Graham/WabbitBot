using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Common.Database;
using WabbitBot.Common.ErrorService;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Services
{
    /// <summary>
    /// Main CoreService that handles all core entity operations
    /// Uses service locator pattern instead of dependency injection
    /// </summary>
    public static partial class CoreService
    {
        private static Lazy<ICoreEventBus>? _lazyEventBus;
        private static Lazy<IErrorService>? _lazyErrorHandler;
        private static Lazy<IDbContextFactory<WabbitBotDbContext>>? _lazyDbContextFactory;

        // Static service instances accessible across all projects
        public static ICoreEventBus EventBus => _lazyEventBus!.Value;
        public static IErrorService ErrorHandler => _lazyErrorHandler!.Value;
        public static IDbContextFactory<WabbitBotDbContext> DbContextFactory => _lazyDbContextFactory!.Value;

        // Initialization method called once at startup
        public static void InitializeServices(
            ICoreEventBus eventBus,
            IErrorService errorHandler,
            IDbContextFactory<WabbitBotDbContext> dbContextFactory)
        {
            _lazyEventBus = new Lazy<ICoreEventBus>(
                () => eventBus ?? throw new ArgumentNullException(
                    nameof(eventBus)), LazyThreadSafetyMode.ExecutionAndPublication);

            _lazyErrorHandler = new Lazy<IErrorService>(
                () => errorHandler ?? throw new ArgumentNullException(
                    nameof(errorHandler)), LazyThreadSafetyMode.ExecutionAndPublication);

            _lazyDbContextFactory = new Lazy<IDbContextFactory<WabbitBotDbContext>>(
                () => dbContextFactory ?? throw new ArgumentNullException(
                    nameof(dbContextFactory)), LazyThreadSafetyMode.ExecutionAndPublication);
        }

        // Testability Hooks
        internal static void SetTestServices(
            ICoreEventBus eventBus,
            IErrorService errorHandler)
        {
            _lazyEventBus = new Lazy<ICoreEventBus>(
                () => eventBus, LazyThreadSafetyMode.ExecutionAndPublication);
            _lazyErrorHandler = new Lazy<IErrorService>(
                () => errorHandler, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        internal static void SetTestDbContextFactory(
            IDbContextFactory<WabbitBotDbContext> dbContextFactory)
        {
            _lazyDbContextFactory = new Lazy<IDbContextFactory<WabbitBotDbContext>>(
                () => dbContextFactory, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        // Legacy instance properties removed, as all access will now be through the static properties
        // and managed initialization.

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