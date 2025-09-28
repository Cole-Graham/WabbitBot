##### Prompt:

I guess you should know how my database is implemented. The application itself is made up four projects: Common.csproj, Core.csproj (what we've been discussing), DiscBot.csproj (platform code for Discord integration), and SourceGenerators.
 
The DatabaseService works like this:
 

```csharp
// src\WabbitBot.Common\Data\Interfaces\IDatabaseService.cs
namespace WabbitBot.Common.Data.Interfaces
{
    /// <summary>
    /// Interface for database services that handle CRUD operations for entities
    /// Supports different database components (Repository, Cache, Archive)
    /// </summary>
    public interface IDatabaseService<TEntity> where TEntity : Entity
    {
        // Core CRUD operations
        Task<Result<TEntity>> CreateAsync(TEntity entity, DatabaseComponent component);
        Task<Result<TEntity>> UpdateAsync(TEntity entity, DatabaseComponent component);
        Task<Result<TEntity>> DeleteAsync(object id, DatabaseComponent component);
        Task<bool> ExistsAsync(object id, DatabaseComponent component);

        // Query operations
        Task<TEntity?> GetByIdAsync(object id, DatabaseComponent component);
        Task<TEntity?> GetByStringIdAsync(string id, DatabaseComponent component);
        Task<TEntity?> GetByNameAsync(string name, DatabaseComponent component);
        Task<IEnumerable<TEntity>> GetAllAsync(DatabaseComponent component);
        Task<IEnumerable<TEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, DatabaseComponent component);
        Task<IEnumerable<TEntity>> QueryAsync(string whereClause, object? parameters, DatabaseComponent component);
    }

    /// <summary>
    /// Enumeration of database component types
    /// </summary>
    public enum DatabaseComponent
    {
        Repository,
        Cache,
        Archive
    }
}

// src\WabbitBot.Common\Data\Service\DatabaseService.cs
namespace WabbitBot.Common.Data.Service;

/// <summary>
/// Unified database service that handles repository, cache, and archive operations for a single entity type.
/// Uses partial classes for clean organization of different operation types.
/// Implements the same IDatabaseService interface as individual services for compatibility.
/// </summary>
public partial class DatabaseService<TEntity> : IDatabaseService<TEntity> where TEntity : Entity
{
    /// <summary>
    /// Creates a new DatabaseService with default configuration
    /// </summary>
    public DatabaseService()
    {
        // Initialize with default settings
        // These would be overridden by entity-specific factory methods
        InitializeRepository("entities", new[] { "Id", "Name", "CreatedAt", "UpdatedAt" });
        InitializeCache(1000, TimeSpan.FromHours(1));
        InitializeArchive("entities_archive", new[] { "Id", "Name", "CreatedAt", "UpdatedAt", "ArchivedAt" });
    }

    /// <summary>
    /// Creates a new DatabaseService with custom configuration
    /// </summary>
    public DatabaseService(
        string tableName,
        IEnumerable<string> columns,
        string archiveTableName,
        IEnumerable<string> archiveColumns,
        int cacheMaxSize = 1000,
        TimeSpan? cacheDefaultExpiry = null,
        string idColumn = "Id")
    {
        InitializeRepository(tableName, columns, idColumn);
        InitializeCache(cacheMaxSize, cacheDefaultExpiry);
        InitializeArchive(archiveTableName, archiveColumns);
    }

    #region IDatabaseService Implementation

    public async Task<Result<TEntity>> CreateAsync(TEntity entity, DatabaseComponent component)
    {
        Result<TEntity>? result;
        switch (component)
        {
            case DatabaseComponent.Repository:
                result = await CreateInRepositoryAsync(entity);
                break;
            case DatabaseComponent.Cache:
                result = await CreateInCacheAsync(entity);
                break;
            case DatabaseComponent.Archive:
                result = await CreateInArchiveAsync(entity);
                break;
            default:
                throw new ArgumentException($"Unsupported component: {component}", nameof(component));
        }

        if (result is null)
        {
            throw new ArgumentException($"Create operation for component '{component}' returned null.", nameof(component));
        }

        return result;
    }

    public async Task<Result<TEntity>> UpdateAsync(TEntity entity, DatabaseComponent component)
    {
        Result<TEntity>? result;
        switch (component)
        {
            case DatabaseComponent.Repository:
                result = await UpdateInRepositoryAsync(entity);
                if (result.Success)
                {
                    await UpdateInCacheAsync(result.Data!);
                }
                break;
            case DatabaseComponent.Cache:
                result = await UpdateInCacheAsync(entity);
                break;
            default:
                throw new ArgumentException($"Unsupported component for update: {component}", nameof(component));
        }

        if (result is null)
        {
            throw new ArgumentException($"Update operation for component '{component}' returned null.", nameof(component));
        }

        return result;
    }

    public async Task<Result<TEntity>> DeleteAsync(object id, DatabaseComponent component)
    {
        Result<TEntity>? result;
        switch (component)
        {
            case DatabaseComponent.Repository:
                result = await DeleteFromRepositoryAsync(id);
                if (result.Success)
                {
                    await DeleteFromCacheAsync(id);
                }
                break;
            case DatabaseComponent.Cache:
                result = await DeleteFromCacheAsync(id);
                break;
            default:
                throw new ArgumentException($"Unsupported component for delete: {component}", nameof(component));
        }

        if (result is null)
        {
            throw new ArgumentException($"Delete operation for component '{component}' returned null.", nameof(component));
        }

        return result;
    }

    public async Task<bool> ExistsAsync(object id, DatabaseComponent component)
    {
        switch (component)
        {
            case DatabaseComponent.Repository:
                if (await ExistsInCacheAsync(id))
                {
                    return true;
                }
                return await ExistsInRepositoryAsync(id);
            case DatabaseComponent.Cache:
                return await ExistsInCacheAsync(id);
            default:
                throw new ArgumentException($"Unsupported component for exists: {component}", nameof(component));
        }
    }

    public async Task<TEntity?> GetByIdAsync(object id, DatabaseComponent component)
    {
        TEntity? entity;
        switch (component)
        {
            case DatabaseComponent.Repository:
                entity = await GetByIdFromCacheAsync(id);
                if (entity is not null)
                {
                    return entity;
                }
                entity = await GetByIdFromRepositoryAsync(id);
                if (entity is null)
                {
                    throw new ArgumentException(
                        $"GetById for '{component}' returned null.",
                        nameof(component)
                    );
                }
                return entity;

            case DatabaseComponent.Cache:
                entity = await GetByIdFromCacheAsync(id);
                if (entity is null)
                {
                    throw new ArgumentException(
                        $"GetById for '{component}' returned null.",
                        nameof(component)
                    );
                }
                return entity;

            default:
                throw new ArgumentException(
                    $"Unsupported component for GetById: {component}",
                    nameof(component)
                );
        }
    }

    public async Task<TEntity?> GetByStringIdAsync(string id, DatabaseComponent component)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return null;
        }

        TEntity? entity;
        switch (component)
        {
            case DatabaseComponent.Repository:
                entity = await GetByIdFromCacheAsync(guid);
                if (entity is not null)
                {
                    return entity;
                }
                entity = await GetByIdFromRepositoryAsync(guid);
                if (entity is null)
                {
                    throw new ArgumentException($"GetByStringId for component '{component}' returned null from repository.", nameof(component));
                }
                return entity;
            case DatabaseComponent.Cache:
                entity = await GetByIdFromCacheAsync(guid);
                if (entity is null)
                {
                    throw new ArgumentException($"GetByStringId for component '{component}' returned null from cache.", nameof(component));
                }
                return entity;
            default:
                throw new ArgumentException($"Unsupported component for GetByStringId: {component}", nameof(component));
        }
    }

    public async Task<TEntity?> GetByNameAsync(string name, DatabaseComponent component)
    {
        TEntity? entity;
        switch (component)
        {
            case DatabaseComponent.Repository:
                entity = await GetByNameFromCacheAsync(name);
                if (entity is not null)
                {
                    return entity;
                }
                entity = await GetByNameFromRepositoryAsync(name);
                if (entity is null)
                {
                    throw new ArgumentException($"GetByName for component '{component}' returned null from repository.", nameof(component));
                }
                return entity;
            case DatabaseComponent.Cache:
                entity = await GetByNameFromCacheAsync(name);
                if (entity is null)
                {
                    throw new ArgumentException($"GetByName for component '{component}' returned null from cache.", nameof(component));
                }
                return entity;
            default:
                throw new ArgumentException($"Unsupported component for GetByName: {component}", nameof(component));
        }
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync(DatabaseComponent component)
    {
        IEnumerable<TEntity>? result;
        switch (component)
        {
            case DatabaseComponent.Repository:
                result = await GetAllFromRepositoryAsync();
                break;
            case DatabaseComponent.Cache:
                result = await GetAllFromCacheAsync();
                break;
            default:
                throw new ArgumentException($"Unsupported component for GetAll: {component}", nameof(component));
        }

        if (result is null)
        {
            throw new ArgumentException($"GetAll for component '{component}' returned null.", nameof(component));
        }
        return result;
    }

    public async Task<IEnumerable<TEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, DatabaseComponent component)
    {
        IEnumerable<TEntity>? result;
        switch (component)
        {
            case DatabaseComponent.Repository:
                result = await GetByDateRangeFromRepositoryAsync(startDate, endDate);
                break;
            default:
                throw new ArgumentException($"Unsupported component for GetByDateRange: {component}", nameof(component));
        }

        if (result is null)
        {
            throw new ArgumentException($"GetByDateRange for component '{component}' returned null.", nameof(component));
        }
        return result;
    }

    public async Task<IEnumerable<TEntity>> QueryAsync(string whereClause, object? parameters, DatabaseComponent component)
    {
        IEnumerable<TEntity>? result;
        switch (component)
        {
            case DatabaseComponent.Repository:
                result = await QueryFromRepositoryAsync(whereClause, parameters);
                break;
            default:
                throw new ArgumentException($"Unsupported component for Query: {component}", nameof(component));
        }

        if (result is null)
        {
            throw new ArgumentException($"Query for component '{component}' returned null.", nameof(component));
        }
        return result;
    }

    #endregion

    // Implementation methods are defined in partial classes:
    // - DatabaseService.Repository.cs: Repository operations
    // - DatabaseService.Cache.cs: Cache operations
    // - DatabaseService.Archive.cs: Archive operations
}

// src\WabbitBot.Core\Common\Services\Core\CoreService.cs
namespace WabbitBot.Core.Common.Services
{
    /// <summary>
    /// Main CoreService that handles all core entity operations
    /// Uses service locator pattern instead of dependency injection
    /// </summary>
    public partial class CoreService
    {
        // Static service instances accessible across all projects
        public static ICoreEventBus? EventBus { get; private set; }
        public static IErrorService? ErrorHandler { get; private set; }
        public static WabbitBotDbContext? DbContext { get; private set; }

        // Initialization method called once at startup
        public static void InitializeServices(
            ICoreEventBus eventBus,
            IErrorService errorHandler,
            WabbitBotDbContext dbContext)
        {
            EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            ErrorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        // Legacy instance properties for backward compatibility during transition
        protected ICoreEventBus InstanceEventBus => EventBus ?? throw new InvalidOperationException("CoreService not initialized. Call InitializeServices() first.");
        protected IErrorService InstanceErrorHandler => ErrorHandler ?? throw new InvalidOperationException("CoreService not initialized. Call InitializeServices() first.");
        protected WabbitBotDbContext InstanceDbContext => DbContext ?? throw new InvalidOperationException("CoreService not initialized. Call InitializeServices() first.");

        public virtual Task InitializeAsync()
        {
            // Placeholder for async initialization
            return Task.CompletedTask;
        }
    }
}

// src\WabbitBot.Core\Common\Services\Core\CoreService.Database.cs
// This file is outdated, the DatabaseService instances are now defined in each EntityCore class so they are only being created when/where they are actually needed.
using System;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Services
{
    /// <summary>
    /// Database service coordination for CoreService
    /// Provides unified DatabaseService instances for all entities
    /// </summary>
    public partial class CoreService
    {
        // DatabaseService instances for each entity type
        // Set to nullable because compiler can't guarantee InitalizeDatabaseServices()
        // will be called before these are used. Which also means we changed Database Service Properties
        // to use null-forgiving operators (yikes!). We should probably find a better solution
        // for this.
        private DatabaseService<Player>? _playerData;
        private DatabaseService<Team>? _teamData;
        private DatabaseService<Game>? _gameData;
        private DatabaseService<User>? _userData;
        private DatabaseService<Map>? _mapData;
        private DatabaseService<ProvenPotentialRecord>? _ppRecordData;

        // Vertical slice database services (to be added later)
        // private readonly DatabaseService<Match> _matchData;
        // private readonly DatabaseService<Scrimmage> _scrimmageData;
        // private readonly DatabaseService<Tournament> _tournamentData;
        // private readonly DatabaseService<Leaderboard> _leaderboardData;
        // private readonly DatabaseService<Season> _seasonData;

        /// <summary>
        /// Initializes database services for common entities
        /// </summary>
        private void InitializeDatabaseServices()
        {
            // Initialize DatabaseService instances for common entities
            _playerData = new DatabaseService<Player>(
                tableName: "players",
                columns: new[] { "Id", "Name", "DisplayName", "Rating", "TeamIds", "CreatedAt", "UpdatedAt" },
                archiveTableName: "players_archive",
                archiveColumns: new[] { "Id", "Name", "DisplayName", "Rating", "TeamIds", "CreatedAt", "UpdatedAt", "ArchivedAt" }
            );

            _teamData = new DatabaseService<Team>(
                tableName: "teams",
                columns: new[] { "Id", "Name", "TeamCaptainId", "TeamMember.PlayerId", "TeamMember.JoinedAt", "Stats", "CreatedAt", "UpdatedAt" },
                archiveTableName: "teams_archive",
                archiveColumns: new[] { "Id", "Name", "TeamCaptainId", "TeamMember.PlayerId", "TeamMember.JoinedAt", "Stats", "CreatedAt", "UpdatedAt", "ArchivedAt" }
            );

            _gameData = new DatabaseService<Game>(
                tableName: "games",
                columns: new[] { "Id", "MatchId", "MapId", "Team1PlayerIds", "Team2PlayerIds", "WinnerId", "GameFormat", "GameStateSnapshot", "CancelledByUserId", "ForfeitedByUserId", "ForfeitedTeamId", "CreatedAt", "UpdatedAt" },
                archiveTableName: "games_archive",
                archiveColumns: new[] { "Id", "MatchId", "MapId", "Team1PlayerIds", "Team2PlayerIds", "WinnerId", "GameFormat", "GameStateSnapshot", "CancelledByUserId", "ForfeitedByUserId", "ForfeitedTeamId", "CreatedAt", "UpdatedAt", "ArchivedAt" }
            );

            _userData = new DatabaseService<User>(
                tableName: "users",
                columns: new[] { "Id", "DiscordUserId", "PlayerId", "Username", "Discriminator", "AvatarUrl", "CreatedAt", "UpdatedAt" },
                archiveTableName: "users_archive",
                archiveColumns: new[] { "Id", "DiscordUserId", "PlayerId", "Username", "Discriminator", "AvatarUrl", "CreatedAt", "UpdatedAt", "ArchivedAt" }
            );

            _mapData = new DatabaseService<Map>(
                tableName: "maps",
                columns: new[] { "Id", "Name", "Description", "IsActive", "Size", "IsInRandomPool", "IsInTournamentPool", "ThumbnailFilename", "CreatedAt", "UpdatedAt" },
                archiveTableName: "maps_archive",
                archiveColumns: new[] { "Id", "Name", "Description", "IsActive", "Size", "IsInRandomPool", "IsInTournamentPool", "ThumbnailFilename", "CreatedAt", "UpdatedAt", "ArchivedAt" }
            );
        }

        #region Database Service Properties

        /// <summary>
        /// Gets the player database service
        /// </summary>
        public DatabaseService<Player> Players => _playerData!;

        /// <summary>
        /// Gets the team database service
        /// </summary>
        public DatabaseService<Team> Teams => _teamData!;

        /// <summary>
        /// Gets the game database service
        /// </summary>
        public DatabaseService<Game> Games => _gameData!;

        /// <summary>
        /// Gets the user database service
        /// </summary>
        public DatabaseService<User> Users => _userData!;

        /// <summary>
        /// Gets the map database service
        /// </summary>
        public DatabaseService<Map> Maps => _mapData!;

        #endregion
    }
}

// Example EntityCore class (Match)
namespace WabbitBot.Core.Matches
{
    [GenerateEventPublisher(EventBusType = EventBusType.Core, EnableValidation = true, EnableTimestamps = true)]
    public partial class MatchCore : CoreService
    {
        private readonly DatabaseService<Match> _matchData = new();
        private readonly DatabaseService<Game> _gameData = new();
        private readonly DatabaseService<Team> _teamData = new();
        private readonly DatabaseService<MatchParticipant> _matchParticipantData = new();
        private readonly DatabaseService<TeamOpponentEncounter> _opponentEncounterData = new();

        // Constructor not needed - services accessed via CoreService static properties

        public enum MatchStatus
        {
            Created,
            InProgress,
            Completed,
            Cancelled,
            Forfeited
        }

        public static class Factory
        {
            // ------------------------ Factory & Initialization ---------------------------
            public static Match CreateMatch(
                TeamSize teamSize,
                int bestOf = 1,
                bool playToCompletion = false)
            {
                var match = new Match
                {
                    TeamSize = teamSize,
                    BestOf = bestOf,
                    PlayToCompletion = playToCompletion,
                };
                // Add initial state to match
                var initialState = CreateMatchStateSnapshot(match.Id);
                match.StateHistory.Add(initialState);
                return match;
            }

            public static MatchStateSnapshot CreateMatchStateSnapshot(Guid matchId)
            {
                return new MatchStateSnapshot
                {
                    MatchId = matchId,
                    Timestamp = DateTime.UtcNow,
                };
            }

            public static MatchStateSnapshot CreateMatchStateSnapshotFromOther(MatchStateSnapshot other)
            {
                // Manual copy to avoid reference issues
                return new MatchStateSnapshot
                {
                    MatchId = other.MatchId,
                    Timestamp = DateTime.UtcNow,
                    UserId = other.UserId,
                    PlayerName = other.PlayerName,
                    AdditionalData = new Dictionary<string, object>(other.AdditionalData),
                    StartedAt = other.StartedAt,
                    CompletedAt = other.CompletedAt,
                    CancelledAt = other.CancelledAt,
                    ForfeitedAt = other.ForfeitedAt,
                    WinnerId = other.WinnerId,
                    CancelledByUserId = other.CancelledByUserId,
                    ForfeitedByUserId = other.ForfeitedByUserId,
                    ForfeitedTeamId = other.ForfeitedTeamId,
                    CancellationReason = other.CancellationReason,
                    ForfeitReason = other.ForfeitReason,
                    CurrentGameNumber = other.CurrentGameNumber,
                    Games = new List<Game>(other.Games),
                    CurrentMapId = other.CurrentMapId,
                    FinalScore = other.FinalScore,
                    FinalGames = new List<Game>(other.FinalGames),
                    AvailableMaps = new List<string>(other.AvailableMaps),
                    Team1MapBans = new List<string>(other.Team1MapBans),
                    Team2MapBans = new List<string>(other.Team2MapBans),
                    Team1BansSubmitted = other.Team1BansSubmitted,
                    Team2BansSubmitted = other.Team2BansSubmitted,
                    Team1BansConfirmed = other.Team1BansConfirmed,
                    Team2BansConfirmed = other.Team2BansConfirmed,
                    FinalMapPool = new List<string>(other.FinalMapPool),
                };
            }


        }

        public static class Accessors
        {
            // ------------------------ State & Logic Accessors ----------------------------
            public static MatchStateSnapshot GetCurrentSnapshot(Match match)
            {
                return match.StateHistory.LastOrDefault() ?? Factory.CreateMatchStateSnapshot(match.Id);
            }

            public static MatchStatus GetCurrentStatus(Match match)
            {
                var snapshot = GetCurrentSnapshot(match);

                if (snapshot.CompletedAt.HasValue)
                    return MatchStatus.Completed;

                if (snapshot.CancelledAt.HasValue)
                    return MatchStatus.Cancelled;

                if (snapshot.ForfeitedAt.HasValue)
                    return MatchStatus.Forfeited;

                if (snapshot.StartedAt.HasValue)
                    return MatchStatus.InProgress;

                return MatchStatus.Created;
            }

            public static bool IsTeamMatch(Match match)
            {
                return match.TeamSize != TeamSize.OneVOne;
            }

            public static string GetRequiredAction(Match match)
            {
                return GetCurrentStatus(match) switch
                {
                    MatchStatus.Created => "Waiting for match to start",
                    MatchStatus.InProgress => "Match in progress",
                    MatchStatus.Completed => "Match completed",
                    MatchStatus.Cancelled => "Match cancelled",
                    MatchStatus.Forfeited => "Match forfeited",
                    _ => "Unknown state",
                };
            }

            public static bool AreMapBansSubmitted(Match match)
            {
                var snapshot = GetCurrentSnapshot(match);
                return snapshot.Team1BansSubmitted && snapshot.Team2BansSubmitted;
            }

            public static bool AreMapBansConfirmed(Match match)
            {
                var snapshot = GetCurrentSnapshot(match);
                return snapshot.Team1BansConfirmed && snapshot.Team2BansConfirmed;
            }

            public static bool IsReadyToStart(Match match)
            {
                return AreMapBansSubmitted(match) && AreMapBansConfirmed(match);
            }
        }


        public static class Validation
        {
            // ------------------------ Validation Logic -----------------------------------
            // No validation needed for TeamSize as it's a simple enum
            // Future: Add business rule validations here
        }

        /// <summary>
        /// Starts a match with full business logic orchestration
        /// </summary>
        public async Task<Result> StartMatchAsync(Guid matchId, Guid team1Id, Guid team2Id, List<Guid> team1PlayerIds, List<Guid> team2PlayerIds)
        {
            try
            {
                var match = await _matchData.GetByIdAsync(matchId, DatabaseComponent.Repository);
                if (match == null)
                    return Result.Failure("Match not found.");

                // Validate match can be started
                if (Accessors.GetCurrentStatus(match) != MatchStatus.Created)
                    return Result.Failure("Match can only be started when in Created state.");

                // Set start time
                match.StartedAt = DateTime.UtcNow;

                // Create match participants
                var team1Participant = new MatchParticipant
                {
                    MatchId = matchId,
                    TeamId = team1Id,
                    TeamNumber = 1,
                    PlayerIds = team1PlayerIds,
                    JoinedAt = DateTime.UtcNow
                };

                var team2Participant = new MatchParticipant
                {
                    MatchId = matchId,
                    TeamId = team2Id,
                    TeamNumber = 2,
                    PlayerIds = team2PlayerIds,
                    JoinedAt = DateTime.UtcNow
                };

                var participant1Result = await _matchParticipantData.CreateAsync(team1Participant, DatabaseComponent.Repository);
                var participant2Result = await _matchParticipantData.CreateAsync(team2Participant, DatabaseComponent.Repository);

                if (!participant1Result.Success || !participant2Result.Success)
                    return Result.Failure("Failed to create match participants.");

                // Create opponent encounters
                var encounter1 = new TeamOpponentEncounter
                {
                    TeamId = team1Id,
                    OpponentId = team2Id,
                    MatchId = matchId,
                    TeamSize = (int)match.TeamSize,
                    EncounteredAt = DateTime.UtcNow,
                    Won = false // Will be updated when match completes
                };

                var encounter2 = new TeamOpponentEncounter
                {
                    TeamId = team2Id,
                    OpponentId = team1Id,
                    MatchId = matchId,
                    TeamSize = (int)match.TeamSize,
                    EncounteredAt = DateTime.UtcNow,
                    Won = false // Will be updated when match completes
                };

                var encounter1Result = await _opponentEncounterData.CreateAsync(encounter1, DatabaseComponent.Repository);
                var encounter2Result = await _opponentEncounterData.CreateAsync(encounter2, DatabaseComponent.Repository);

                if (!encounter1Result.Success || !encounter2Result.Success)
                    return Result.Failure("Failed to create opponent encounters.");

                // TODO: Re-implement map pool logic without MapService dependency
                // match.AvailableMaps = GetDefaultMapPool();

                // Create the first game
                var game = new Game
                {
                    MatchId = match.Id,
                    TeamSize = match.TeamSize,
                    Team1PlayerIds = team1PlayerIds,
                    Team2PlayerIds = team2PlayerIds,
                    GameNumber = 1
                };

                var createGameResult = await _gameData.CreateAsync(game, DatabaseComponent.Repository);
                if (!createGameResult.Success)
                    return Result.Failure("Failed to create the first game for the match.");

                match.Games.Add(createGameResult.Data!);

                await _matchData.UpdateAsync(match, DatabaseComponent.Repository);
                await _matchData.UpdateAsync(match, DatabaseComponent.Cache);

                // Publish event for Discord integration
                await EventBus.PublishAsync(new MatchStartedEvent
                {
                    MatchId = match.Id,
                    StartedAt = match.StartedAt.Value,
                    GameId = createGameResult.Data!.Id
                });

                return Result.CreateSuccess();
            }
            catch (Exception ex)
            {
                await ErrorHandler.CaptureAsync(ex, "Failed to start match", nameof(StartMatchAsync));
                return Result.Failure($"An unexpected error occurred while starting the match: {ex.Message}");
            }
        }

        /// <summary>
        /// Static methods for computed properties and business logic related to MatchParticipant and TeamOpponentEncounter
        /// </summary>
        public static class MatchBusinessLogic
        {
            /// <summary>
            /// Gets the participants for a given team in this match
            /// </summary>
            public static List<MatchParticipant> GetParticipantsForTeam(Match match, Guid teamId)
            {
                return match.Participants.Where(p => p.TeamId == teamId).ToList();
            }

            /// <summary>
            /// Gets the opponent encounters for a given team in this match
            /// </summary>
            public static List<TeamOpponentEncounter> GetOpponentEncountersForTeam(Match match, Guid teamId)
            {
                return match.OpponentEncounters.Where(oe => oe.TeamId == teamId).ToList();
            }

            /// <summary>
            /// Gets all teams participating in this match
            /// </summary>
            public static List<Guid> GetParticipatingTeamIds(Match match)
            {
                return match.Participants.Select(p => p.TeamId).Distinct().ToList();
            }

            /// <summary>
            /// Gets all opponent encounters for this match
            /// </summary>
            public static List<(Guid TeamId, Guid OpponentId)> GetAllOpponentPairs(Match match)
            {
                var pairs = new List<(Guid, Guid)>();
                foreach (var encounter in match.OpponentEncounters)
                {
                    pairs.Add((encounter.TeamId, encounter.OpponentId));
                }
                return pairs.Distinct().ToList();
            }

            /// <summary>
            /// Calculates variety entropy for a team's opponents in this match
            /// </summary>
            public static double CalculateVarietyEntropyForTeam(Match match, Guid teamId)
            {
                var encounters = GetOpponentEncountersForTeam(match, teamId);
                if (!encounters.Any()) return 0.0;

                var totalEncounters = encounters.Count;
                var uniqueOpponents = encounters.Select(e => e.OpponentId).Distinct().Count();

                // Shannon entropy calculation for opponent distribution
                var entropy = 0.0;
                var opponentGroups = encounters.GroupBy(e => e.OpponentId);

                foreach (var group in opponentGroups)
                {
                    var probability = (double)group.Count() / totalEncounters;
                    entropy -= probability * Math.Log(probability);
                }

                // Normalize by log of unique opponents to get value between 0 and 1
                var maxEntropy = Math.Log(uniqueOpponents);
                return maxEntropy == 0 ? 0 : entropy / maxEntropy;
            }

            /// <summary>
            /// Calculates variety bonus based on opponent distribution
            /// </summary>
            public static double CalculateVarietyBonusForTeam(Match match, Guid teamId)
            {
                var encounters = GetOpponentEncountersForTeam(match, teamId);
                var uniqueOpponents = encounters.Select(e => e.OpponentId).Distinct().Count();
                var totalEncounters = encounters.Count;

                // Bonus increases with more unique opponents, but decreases with repeated encounters
                var uniqueBonus = Math.Min(uniqueOpponents * 0.1, 1.0); // Cap at 1.0 for 10+ opponents
                var repeatPenalty = totalEncounters > uniqueOpponents ? (totalEncounters - uniqueOpponents) * 0.05 : 0.0;

                return Math.Max(uniqueBonus - repeatPenalty, 0.0);
            }
        }

        /// <summary>
        /// Completes a match with winner determination and variety statistics updates
        /// </summary>
        public async Task<Result> CompleteMatchAsync(Guid matchId, Guid winnerId)
        {
            try
            {
                var match = await _matchData.GetByIdAsync(matchId, DatabaseComponent.Repository);
                if (match == null)
                    return Result.Failure("Match not found.");

                // Validate match can be completed
                if (Accessors.GetCurrentStatus(match) != MatchStatus.InProgress)
                    return Result.Failure("Match can only be completed when in InProgress state.");

                // Validate winner is a participant
                var winnerParticipant = match.Participants.FirstOrDefault(p => p.TeamId == winnerId);
                if (winnerParticipant == null)
                    return Result.Failure("Winner team is not a participant in this match.");

                // Set completion data
                match.CompletedAt = DateTime.UtcNow;
                match.WinnerId = winnerId;

                // Update match participants with winner information
                foreach (var participant in match.Participants)
                {
                    participant.IsWinner = participant.TeamId == winnerId;
                    participant.UpdatedAt = DateTime.UtcNow;
                    await _matchParticipantData.UpdateAsync(participant, DatabaseComponent.Repository);
                }

                // Update opponent encounters with winner information
                foreach (var encounter in match.OpponentEncounters)
                {
                    encounter.Won = encounter.TeamId == winnerId;
                    encounter.UpdatedAt = DateTime.UtcNow;
                    await _opponentEncounterData.UpdateAsync(encounter, DatabaseComponent.Repository);
                }

                // Update match and save
                await _matchData.UpdateAsync(match, DatabaseComponent.Repository);
                await _matchData.UpdateAsync(match, DatabaseComponent.Cache);

                // Trigger variety statistics updates for participating teams
                await UpdateTeamVarietyStatsAsync(match);

                // Publish match completion event
                await EventBus.PublishAsync(new MatchCompletedEvent
                {
                    MatchId = match.Id,
                    WinnerId = winnerId,
                    CompletedAt = match.CompletedAt.Value
                });

                return Result.CreateSuccess();
            }
            catch (Exception ex)
            {
                await ErrorHandler.CaptureAsync(ex, "Failed to complete match", nameof(CompleteMatchAsync));
                return Result.Failure($"An unexpected error occurred while completing the match: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates variety statistics for all teams that participated in a match
        /// </summary>
        private async Task UpdateTeamVarietyStatsAsync(Match match)
        {
            try
            {
                var teamData = new DatabaseService<Team>();
                var varietyStatsData = new DatabaseService<TeamVarietyStats>();

                foreach (var participant in match.Participants)
                {
                    var team = await teamData.GetByIdAsync(participant.TeamId, DatabaseComponent.Repository);
                    if (team == null) continue;

                    // Get or create variety stats for this team and team size
                    var varietyStats = team.VarietyStats.GetValueOrDefault((TeamSize)match.TeamSize);
                    if (varietyStats == null)
                    {
                        varietyStats = new TeamVarietyStats
                        {
                            TeamId = team.Id,
                            TeamSize = (TeamSize)match.TeamSize,
                            VarietyEntropy = 0.0,
                            VarietyBonus = 0.0,
                            TotalOpponents = 0,
                            UniqueOpponents = 0,
                            LastCalculated = DateTime.UtcNow,
                            LastUpdated = DateTime.UtcNow
                        };

                        var createResult = await varietyStatsData.CreateAsync(varietyStats, DatabaseComponent.Repository);
                        if (createResult.Success)
                        {
                            team.VarietyStats[(TeamSize)match.TeamSize] = varietyStats;
                        }
                    }

                    // Update variety stats using recent encounters
                    var recentEncounters = team.RecentOpponents?
                        .Where(oe => oe.TeamSize == (int)match.TeamSize)
                        .OrderByDescending(oe => oe.EncounteredAt)
                        .Take(100) // Use last 100 encounters for stats calculation
                        .ToList() ?? new List<TeamOpponentEncounter>();

                    Team.StatsBusinessLogic.UpdateVarietyStats(varietyStats, recentEncounters);
                    varietyStats.LastUpdated = DateTime.UtcNow;

                    await varietyStatsData.UpdateAsync(varietyStats, DatabaseComponent.Repository);
                    await teamData.UpdateAsync(team, DatabaseComponent.Repository);
                }
            }
            catch (Exception ex)
            {
                await ErrorHandler.CaptureAsync(ex, "Failed to update team variety stats", nameof(UpdateTeamVarietyStatsAsync));
                // Don't throw - variety stats update failure shouldn't fail match completion
            }
        }

        // TODO: The rest of the business logic methods from MatchService.deprecated.cs need to be migrated here.
        // - CancelMatchAsync
        // - ForfeitMatchAsync
        // - SubmitMapBansAsync
        // - CheckMatchVictoryConditionsAsync
    }
}
```

##### grok-4-fast (beta):

### Strengths of Your Updated Design (With DB Service Context)
Integrating `DatabaseService` and the static locator in `CoreService` is a clever evolution—it keeps your no-DI procedural purity while providing a unified CRUD facade across Repo/Cache/Archive. The component-switching (e.g., fallback from Cache to Repo in `GetByIdAsync`) adds resilience without complexity, and partials for `DatabaseService` (e.g., `.Repository.cs`) maintain modularity. In `MatchCore`, nesting statics like `Factory`, `Accessors`, and `Validation` keeps logic contained and composable (e.g., `Accessors.GetCurrentStatus(match)`), aligning with low coupling. Inline `new DatabaseService<T>()` per core avoids the bloat in `CoreService.Database.cs` (smart deprecation), and primitive event payloads (e.g., `MatchStartedEvent`) stay decoupled. Overall, this scales well for vertical slices: Each core owns its DB orchestration explicitly.

### Potential Issues
With the DB layer exposed, some risks sharpen—mostly around globals, repetition, and error handling—but nothing breaks your goals. Here's a breakdown:

1. **Global Static State in CoreService (Service Locator Pitfalls)**:
   - `CoreService.EventBus`, `ErrorHandler`, `DbContext` as statics enable easy access (e.g., `await EventBus.PublishAsync(...)` in `MatchCore`), fitting no-DI. But globals introduce shared mutable state: In multi-threaded Discord scenarios (e.g., concurrent match starts), uninitialized access throws `InvalidOperationException`, or worse, race conditions if `InitializeServices` runs partially.
   - **Impact**: Medium-high—scalability hit if DiscBot scales to high concurrency; violates modularity if one slice's init affects others.
   - **Mitigation**: Add thread-safe init guards (e.g., `private static readonly object _lock = new();` and `lock(_lock) { if (EventBus == null) { /* init */ } }`). For DbContext, use `IDbContextFactory` (static wrapper) to scope per-operation instances, avoiding long-lived context leaks. No DI exception needed—keep it static but explicit.

2. **Repetitive DatabaseService Instantiation**:
   - Inline `new DatabaseService<Match>()` in `MatchCore` (and again in `UpdateTeamVarietyStatsAsync` for Team/VarietyStats) works, but recreates the service per method/call—re-initializing table/column configs each time. For hot paths (e.g., `StartMatchAsync` called frequently), this wastes CPU/memory without caching the service itself.
   - The commented "yikes" on null-forgiving (`_playerData!`) in old `CoreService.Database.cs` highlights a related smell: Lazy init via properties risks NPEs if `InitializeDatabaseServices` isn't called first.
   - **Impact**: Low-medium—perf drag at scale, but procedural explicitness is a win.
   - **Mitigation**: Add a static factory in Common (e.g., `static class DbServiceFactory { public static DatabaseService<T> CreateFor<T>(string tableName, string[] columns) where T : Entity => new DatabaseService<T>(tableName, columns, ...); }`). In `MatchCore`: `private static readonly DatabaseService<Match> _matchData = DbServiceFactory.CreateFor<Match>("matches", new[] { "Id", ... });`. This centralizes configs (in one place per entity) without globals or inheritance—pure composition.

3. **Error Handling and Result Propagation**:
   - `Result<T>` wrapping (e.g., `if (!participant1Result.Success) return Result.Failure(...)`) is solid for procedural flows, but `GetByIdAsync` throws `ArgumentException` on null (e.g., "returned null from repository")—inconsistent with `Result` pattern. In `CompleteMatchAsync`, exceptions bubble to `ErrorHandler.CaptureAsync` but still return `Result.Failure(ex.Message)`, exposing internals.
   - Archive support is stubbed (e.g., no switch case in many methods), risking future gaps.
   - **Impact**: Medium—debugging leaks (e.g., stack traces in user-facing Results); modularity erodes if errors cross slices.
   - **Mitigation**: Unify to always return `Result` (e.g., refactor `GetByIdAsync` to `Task<Result<TEntity?>>` with `Result.Success(null)` on miss). For errors, log via `ErrorHandler` but return sanitized `Result.Failure("Match not found")`—keep `ex.Message` internal. Enable Archive in switches gradually; use partials for component-specific Results.

4. **Query Limitations and Cross-Slice Fragility**:
   - `QueryAsync(whereClause, parameters)` is flexible (raw SQL-ish), but relies on stringly-typed clauses—prone to injection/NRE if params mismatch (though EF params help). Cross-slice lookups (e.g., Scrimmage querying Match via ID) still hit runtime schema risks, amplified by per-core DB services (no shared projections).
   - Cache fallbacks (e.g., Repo on Cache miss) are great, but `GetAllAsync` lacks it, potentially inconsistent.
   - **Impact**: Medium—scales poorly for complex joins (e.g., Match + Games + Teams in one query).
   - **Mitigation**: Extend `IDatabaseService` with typed queries (e.g., `Task<IEnumerable<TEntity>> QueryBySpecAsync(ISpec<T> spec)`), implemented via static specs in Common (e.g., `MatchByIdSpec : ISpec<Match>`). For consistency, add Cache fallbacks to `GetAllAsync`. This boosts modularity without DI—slices compose specs explicitly.

5. **Business Logic Density in Cores**:
   - `MatchCore` is growing (Factory + Accessors + Validation + async methods + nested `MatchBusinessLogic`), and partials help, but `UpdateTeamVarietyStatsAsync` couples to Team/VarietyStats DB (new services inline)—blurs boundaries if Leaderboard slice owns variety calcs.
   - Entropy/bonus calcs (e.g., Shannon in `CalculateVarietyEntropyForTeam`) are pure but could drift if Team stats evolve.
   - **Impact**: Low—fits verticals, but risks god-core as features expand.
   - **Mitigation**: Extract calcs to static utils in Common (e.g., `static class StatsUtils { public static double CalculateEntropy(...) }`), called from cores. For nesting, your static classes are fine—no subclass urge; partials for methods (e.g., `MatchCore.Variety.cs`) keep it modular.

6. **Testing and Scalability Edges**:
   - No-DI shines for tests (instantiate `MatchCore` directly), but static `EventBus` needs test doubles (e.g., mock via reflection—hacky). Archive/Cache configs hardcoded per entity risk perf tuning per env.
   - **Impact**: Low.
   - **Mitigation**: Static test overrides (e.g., `CoreService.SetTestEventBus(IMockBus)` with internal setter). Configs via consts or appsettings parsed in factory.

### Addressing the Match Incoherency (DB Lens)
With `DatabaseService<Match>` inline in `MatchCore`, the "component" feel strengthens—Match's DB ops (e.g., cascading to Games/Participants) are self-contained, but slices like Scrimmage still need to query it via events/DB. Moving to Common remains ideal:

- **Why?** Central `Common/Match/MatchCore.cs` with static DB factory (e.g., `_matchData = DbServiceFactory.CreateFor<Match>(...)`). Slices import Common, call statics (e.g., `MatchCore.Factory.CreateMatch(...)`), or subscribe to events. Fixes duplication (e.g., no per-slice `new DatabaseService<Match>()`); low coupling preserved.
- **How (No-DI)**: Expose static entry points (e.g., `public static async Task<Result> StartMatchAsync(...)` in MatchCore). For slice tweaks: Static adapters (e.g., `ScrimmageMatchCore.AdaptStart(Match match)` wrapping base).
- **Pros**: One config for Match tables/columns; events from Common reduce cross-DB hits.

Skip "Compete" grouping—DB services would need shared statics, risking globals. Source gen: Still overkill; factory covers repetition.

### Wrapping Up
This DB layer elevates your arch—explicit, component-aware CRUD without DI magic. Focus quick fixes: DbService factory for repetition, Result unification for errors, thread guards for statics. It keeps coupling low, inheritance nil, and modularity high—prime for scaling to more slices.

Diffs in Scrimmage/Tournament Match use (e.g., scrims needing casual maps vs. tourn brackets)? Could inspire adapter statics.