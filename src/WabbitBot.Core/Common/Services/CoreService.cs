using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Events.Interfaces;
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
        private static FileSystemService? _fileSystemService;

        // Static service instances accessible across all projects
        public static ICoreEventBus EventBus => _lazyEventBus!.Value;
        public static IErrorService ErrorHandler => _lazyErrorHandler!.Value;

        /// <summary>
        /// Gets the shared FileSystemService instance.
        /// Must be initialized via InitializeFileSystemService before use.
        /// </summary>
        public static FileSystemService FileSystem => _fileSystemService ?? throw new InvalidOperationException("FileSystemService has not been initialized");

        // Note: DbContext access is now handled via static WabbitBotDbContextProvider
        // instead of DI-injected factory. See WabbitBotDbContextProviderAdapter for
        // IDbContextFactory compatibility where needed (e.g., EfRepositoryAdapter).

        /// <summary>
        /// Initializes the FileSystemService with explicit dependencies.
        /// Should be called once during application startup.
        /// </summary>
        /// <param name="eventBus">The Core event bus instance</param>
        /// <param name="errorHandler">The error service instance</param>
        public static void InitializeFileSystemService(ICoreEventBus eventBus, IErrorService errorHandler)
        {
            ArgumentNullException.ThrowIfNull(eventBus);
            ArgumentNullException.ThrowIfNull(errorHandler);

            if (_fileSystemService is not null)
            {
                throw new InvalidOperationException("FileSystemService has already been initialized");
            }

            _fileSystemService = new FileSystemService(eventBus, errorHandler);
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

        // Test hook for DbContext removed - tests should use WabbitBotDbContextProvider.Initialize()
        // or mock WabbitBotDbContextProviderAdapter for unit tests.

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