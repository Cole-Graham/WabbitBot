using System;
using System.Linq;
using WabbitBot.Common.Data; // RepositoryAdapterRegistry
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Database;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Models.Leaderboard;
using WabbitBot.Core.Common.Models.Scrimmage;
using WabbitBot.Core.Common.Models.Tournament;

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
            await using var context = WabbitBotDbContextProvider.CreateDbContext();
            await work(context);
        }

        /// <summary>
        /// Executes work within a safely managed DbContext scope for fire-and-forget operations
        /// </summary>
        public static async Task WithDbContext(Action<WabbitBotDbContext> work)
        {
            await using var context = WabbitBotDbContextProvider.CreateDbContext();
            work(context);
        }

        /// <summary>
        /// Executes work within a safely managed DbContext scope and returns a result
        /// </summary>
        public static async Task<T> WithDbContext<T>(Func<WabbitBotDbContext, Task<T>> work)
        {
            await using var context = WabbitBotDbContextProvider.CreateDbContext();
            return await work(context);
        }

        /// <summary>
        /// Executes work within a safely managed DbContext scope with standardized error handling
        /// </summary>
        public static async Task<Result> TryWithDbContext(Func<WabbitBotDbContext, Task> work, string operationName)
        {
            try
            {
                await using var context = WabbitBotDbContextProvider.CreateDbContext();
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
        public static async Task<Result<T>> TryWithDbContext<T>(
            Func<WabbitBotDbContext, Task<T>> work,
            string operationName
        )
        {
            try
            {
                await using var context = WabbitBotDbContextProvider.CreateDbContext();
                var result = await work(context);
                return Result<T>.CreateSuccess(result);
            }
            catch (Exception ex)
            {
                await ErrorHandler.CaptureAsync(ex, $"Database operation failed: {operationName}", operationName);
                return Result<T>.Failure($"An error occurred during {operationName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers EF repository adapters for known entities.
        /// Adapters use WabbitBotDbContextProvider directly (no DI abstraction needed).
        /// </summary>
        public static void RegisterRepositoryAdapters()
        {
            // Core entities
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Player>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Team>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Map>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Game>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Match>());

            // Scrimmage entities
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<ScrimmageChallenge>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Scrimmage>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<ScrimmageStateSnapshot>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<ProvenPotentialRecord>());

            // Team-related entities
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<TeamRoster>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<TeamMember>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<ScrimmageTeamStats>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<TournamentTeamStats>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<TeamVarietyStats>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<TeamOpponentEncounter>());

            // Match-related entities
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<MatchStateSnapshot>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<GameStateSnapshot>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Replay>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<ReplayPlayer>());

            // User entities
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<MashinaUser>());

            // Division entities
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Division>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<DivisionStats>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<DivisionMapStats>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<DivisionLearningCurve>());

            // Leaderboard entities
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<ScrimmageLeaderboard>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<TournamentLeaderboard>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<ScrimmageLeaderboardItem>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<TournamentLeaderboardItem>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Season>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<SeasonConfig>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<RatingPercentileBreakpoints>());

            // Tournament entities
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Tournament>());
            RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<TournamentStateSnapshot>());
        }

        /// <summary>
        /// Manually defined stub for cache provider registration. Intentionally empty by default; rely on
        /// DatabaseService fallback provider when no per-entity provider is registered. Generators may later
        /// emit per-entity registrations based on EntityMetadata.EmitCacheRegistration.
        /// </summary>
        public static void RegisterCacheProviders()
        {
            // No explicit registrations; defaults apply
            RegisterCacheProviders_Generated();
        }

        static partial void RegisterCacheProviders_Generated();

        /// <summary>
        /// Manually defined stub for archive provider registration. Intentionally empty by default; rely on
        /// DatabaseService fallback provider when no per-entity provider is registered. Generators may later
        /// emit per-entity registrations based on archive options.
        /// </summary>
        public static void RegisterArchiveProviders()
        {
            // Manual registrations (if any) go here. Generator-provided registrations come next.
            RegisterArchiveProviders_Generated();
        }

        static partial void RegisterArchiveProviders_Generated();

        /// <summary>
        /// Purges archive snapshots older than the provided cutoff for all registered archive providers.
        /// Intended to be scheduled periodically (e.g., daily).
        /// </summary>
        public static async Task<Result> RunArchiveRetentionAsync(TimeSpan retentionWindow)
        {
            try
            {
                var cutoff = DateTime.UtcNow - retentionWindow;
                // Iterate providers; since registry keys are entity types, we need to invoke generic Purge across sets.
                var entityTypes = ArchiveProviderRegistry.GetRegisteredEntityTypes();
                foreach (var entityType in entityTypes)
                {
                    // Build DbSet for archive type to get distinct EntityId values
                    await using var db = WabbitBotDbContextProvider.CreateDbContext();
                    var archiveType = entityType.Assembly.GetType(entityType.FullName + "Archive");
                    if (archiveType is null)
                        continue;
                    var entityIdProp = archiveType.GetProperty("EntityId");
                    if (entityIdProp is null)
                        continue;
                    var archivedAtProp = archiveType.GetProperty("ArchivedAt");
                    if (archivedAtProp is null)
                        continue;

                    var setGeneric = typeof(Microsoft.EntityFrameworkCore.DbContext)
                        .GetMethods()
                        .First(m => m.Name == "Set" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0)
                        .MakeGenericMethod(archiveType)
                        .Invoke(db, null)!;

                    var list = new List<object>();
                    foreach (var item in (System.Collections.IEnumerable)setGeneric)
                    {
                        list.Add(item);
                    }
                    var entityIds = list.Select(x => (Guid)entityIdProp.GetValue(x)!).Distinct().ToList();

                    // Resolve provider instance from registry via reflection of generic method
                    var getProviderMethod = typeof(ArchiveProviderRegistry)
                        .GetMethod("GetProvider")!
                        .MakeGenericMethod(entityType);
                    var provider = getProviderMethod.Invoke(null, Array.Empty<object>());
                    if (provider is null)
                        continue;
                    var purgeMethod = provider.GetType().GetMethod("PurgeAsync");
                    if (purgeMethod is null)
                        continue;

                    foreach (var id in entityIds)
                    {
                        await (Task)purgeMethod.Invoke(provider, new object?[] { id, cutoff })!;
                    }
                }
                return Result.CreateSuccess();
            }
            catch (Exception ex)
            {
                await ErrorHandler.CaptureAsync(ex, "Archive retention job failed", nameof(RunArchiveRetentionAsync));
                return Result.Failure($"Archive retention job failed: {ex.Message}");
            }
        }
    }
}
