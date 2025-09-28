using System;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Database;

namespace WabbitBot.Core.Common.Services
{
    /// <summary>
    /// Database service coordination for CoreService
    /// Provides unified DatabaseService instances for all entities
    /// </summary>
    public static partial class CoreService
    {
        // Static lazy accessors for DatabaseService instances




        /// <summary>
        /// Executes work within a safely managed DbContext scope
        /// </summary>
        public static async Task WithDbContext(Func<WabbitBotDbContext, Task> work)
        {
            await using var context = await DbContextFactory.CreateDbContextAsync();
            await work(context);
        }

        /// <summary>
        /// Executes work within a safely managed DbContext scope for fire-and-forget operations
        /// </summary>
        public static async Task WithDbContext(Action<WabbitBotDbContext> work)
        {
            await using var context = await DbContextFactory.CreateDbContextAsync();
            work(context);
        }

        /// <summary>
        /// Executes work within a safely managed DbContext scope and returns a result
        /// </summary>
        public static async Task<T> WithDbContext<T>(Func<WabbitBotDbContext, Task<T>> work)
        {
            await using var context = await DbContextFactory.CreateDbContextAsync();
            return await work(context);
        }

        /// <summary>
        /// Executes work within a safely managed DbContext scope with standardized error handling
        /// </summary>
        public static async Task<Result> TryWithDbContext(Func<WabbitBotDbContext, Task> work, string operationName)
        {
            try
            {
                await using var context = await DbContextFactory.CreateDbContextAsync();
                await work(context);
                return Result.CreateSuccess();
            }
            catch (Exception ex)
            {
                await ErrorHandler.CaptureAsync(ex, $"Database operation failed: {operationName}", operationName);
                return Result.Failure($"An error occurred during {operationName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes work within a safely managed DbContext scope with standardized error handling and returns a result
        /// </summary>
        public static async Task<Result<T>> TryWithDbContext<T>(Func<WabbitBotDbContext, Task<T>> work, string operationName)
        {
            try
            {
                await using var context = await DbContextFactory.CreateDbContextAsync();
                var result = await work(context);
                return Result<T>.CreateSuccess(result);
            }
            catch (Exception ex)
            {
                await ErrorHandler.CaptureAsync(ex, $"Database operation failed: {operationName}", operationName);
                return Result<T>.Failure($"An error occurred during {operationName}: {ex.Message}");
            }
        }
    }
}
