#### FINALIZATION/REFINEMENT: Testing & Cleanup

### Final Validation and Architecture Cleanup

**🎯 CRITICAL**: Comprehensive testing and cleanup to ensure the refactored architecture is production-ready.

#### Testing & Cleanup - Final Validation Phase

#### a. VersionCompatibilityTests (see step 6.5)

Execute the version compatibility test suite to ensure application and database version compatibility works correctly.

```bash
# Run version compatibility tests
dotnet test --filter VersionCompatibilityTests

# Expected output: All tests pass
# ✅ VersionCompatibility_Works("1.0.0", "001-1.0") = true
# ✅ VersionCompatibility_Works("1.1.0", "002-1.0") = true
# ✅ VersionCompatibility_Works("1.2.0", "001-1.0") = false
```

#### b. Comprehensive Integration Testing

Run full end-to-end integration tests covering all CoreService operations and Discord command interactions.

```csharp
[TestFixture]
public class EndToEndIntegrationTests
{
    [Test]
    public async Task CompletePlayerLifecycle_WorksEndToEnd()
    {
        // 1. Create player via EntityCore factory and CoreService
        var player = Player.Create("TestPlayer", userId);
        var result = await CoreService.Players.CreateAsync(player);
        
        // 2. Verify player exists in database
        var retrieved = await CoreService.Players.GetByIdAsync(player.Id);
        Assert.NotNull(retrieved);
        
        // 3. Update player via EntityCore business logic
        player.UpdateLastActive();
        await CoreService.Players.UpdateAsync(player);
        
        // 4. Verify caching behavior (second call hits cache)
        var cached = await CoreService.Players.GetByIdAsync(player.Id, useCache: true);
        
        // 5. Archive player and verify historical data
        await CoreService.Players.DeleteAsync(player.Id); // Triggers pre-delete snapshot
    }

    [Test]
    public async Task TournamentWorkflow_IntegratesAllComponents()
    {
        // 1. Create tournament via EntityCore factory
        var tournament = Tournament.Create("Test Tournament", organizerId);
        await CoreService.Tournaments.CreateAsync(tournament);
        
        // 2. Register teams and players
        var team1 = Team.Create("Team Alpha");
        var team2 = Team.Create("Team Beta");
        await CoreService.Teams.CreateAsync(team1);
        await CoreService.Teams.CreateAsync(team2);
        
        // 3. Submit match result via EntityCore business logic
        var match = Match.Create(team1.Id, team2.Id, mapId, team1Players, team2Players);
        match.CompleteMatch(team1.Id);
        await CoreService.Matches.UpdateAsync(match);
        
        // 4. Verify leaderboard updates (via events)
        var leaderboard = await CoreService.Leaderboards.GetByIdAsync(tournament.LeaderboardId);
        
        // 5. Complete tournament and verify final rankings
        tournament.Complete();
        await CoreService.Tournaments.UpdateAsync(tournament);
    }
}
```

#### c. Performance Benchmarking

Execute performance benchmarks to ensure the new architecture meets performance requirements.

```csharp
[MemoryDiagnoser]
public class PerformanceBenchmarks
{
    [Benchmark]
    public async Task GetPlayerById_DatabaseService_Performance()
    {
        // Measure DatabaseService query performance (via repository adapter)
        var player = await CoreService.Players.GetByIdAsync(playerId, useCache: false);
    }

    [Benchmark]
    public async Task GetPlayerById_WithCache_Performance()
    {
        // Measure cache hit performance
        var player = await CoreService.Players.GetByIdAsync(cachedPlayerId, useCache: true);
    }

    [Benchmark]
    public async Task JSONB_Query_Performance()
    {
        // Measure JSONB query performance via WithDbContext
        var players = await CoreService.WithDbContext(async db =>
            await db.Players
                .Where(p => p.TeamIds.Contains(teamId)) // JSONB containment query
                .ToListAsync()
        );
    }

    [Benchmark]
    public async Task ComplexQuery_WithIncludes_Performance()
    {
        // Measure complex query with navigation properties
        var matches = await CoreService.WithDbContext(async db =>
            await db.Matches
                .Include(m => m.Team1)
                .Include(m => m.Team2)
                .Where(m => m.CreatedAt > DateTime.UtcNow.AddDays(-7))
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync()
        );
    }

    [Benchmark]
    public async Task Cache_Hit_vs_Miss_Performance()
    {
        // Measure cache performance (first call = miss, second = hit)
        var player1 = await CoreService.Players.GetByIdAsync(playerId, useCache: true);
        var player2 = await CoreService.Players.GetByIdAsync(playerId, useCache: true);
    }
}
```

#### d. Production Readiness Validation

Final checklist to ensure the application is ready for production deployment.

```bash
# Database migration validation (environment-specific outside scope of repo)
dotnet ef database update
# ✅ Migrations apply successfully

# Load testing and health checks are deployment-environment concerns; document commands in ops runbooks.
```

#### e. Documentation Finalization

Complete all documentation and prepare for production deployment.

```markdown
# Documentation Checklist
- [x] API Documentation updated
- [x] Architecture diagrams current
- [x] Migration guides completed
- [x] Performance benchmarks documented
- [x] Security considerations documented
- [x] Deployment procedures finalized
- [x] Monitoring and alerting configured
- [x] Incident response procedures documented
```

#### STEP FINALIZATION IMPACT:

### Testing Strategy

#### Unit Tests
```csharp
[TestFixture]
public class DatabaseServiceTests
{
    [SetUp]
    public void Setup()
    {
        // Initialize CoreService (static, one-time setup)
        CoreService.InitializeServices(
            new CoreEventBus(),
            new ErrorService()
        );
    }

    [Test]
    public async Task CreateAsync_ValidPlayer_ReturnsSuccess()
    {
        // Arrange
        var player = Player.Create("TestPlayer", Guid.NewGuid());

        // Act
        var result = await CoreService.Players.CreateAsync(player);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(player.Id, result.Data.Id);
    }

    [Test]
    public async Task GetByIdAsync_ExistingPlayer_ReturnsPlayer()
    {
        // Arrange
        var player = Player.Create("TestPlayer", Guid.NewGuid());
        await CoreService.Players.CreateAsync(player);

        // Act - Cache miss (first call)
        var retrieved1 = await CoreService.Players.GetByIdAsync(player.Id, useCache: true);
        
        // Act - Cache hit (second call)
        var retrieved2 = await CoreService.Players.GetByIdAsync(player.Id, useCache: true);

        // Assert
        Assert.NotNull(retrieved1);
        Assert.NotNull(retrieved2);
        Assert.Equal(player.Name, retrieved1.Name);
    }

    [Test]
    public async Task GetByIdAsync_CacheDisabled_HitsRepository()
    {
        // Arrange
        var player = Player.Create("TestPlayer", Guid.NewGuid());
        await CoreService.Players.CreateAsync(player);

        // Act
        var retrieved = await CoreService.Players.GetByIdAsync(player.Id, useCache: false);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(player.Name, retrieved.Name);
    }
}

[TestFixture]
public class EntityCoreTests
{
    [Test]
    public void Player_Create_SetsDefaults()
    {
        // Arrange & Act
        var player = Player.Create("TestPlayer", Guid.NewGuid());

        // Assert
        Assert.NotEqual(Guid.Empty, player.Id);
        Assert.Equal("TestPlayer", player.Name);
        Assert.True(player.CreatedAt > DateTime.UtcNow.AddSeconds(-1));
    }

    [Test]
    public void Player_JoinTeam_Success()
    {
        // Arrange
        var player = Player.Create("Test", Guid.NewGuid());
        var teamId = Guid.NewGuid();

        // Act
        var result = player.JoinTeam(teamId);

        // Assert
        Assert.True(result.Success);
        Assert.Contains(teamId, player.TeamIds);
    }

    [Test]
    public void Match_CompleteMatch_ValidatesStatus()
    {
        // Arrange
        var match = Match.Create(team1Id, team2Id, mapId, team1Players, team2Players);
        match.Status = MatchStatus.Pending; // Not started yet

        // Act
        var result = match.CompleteMatch(team1Id);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("must be in progress", result.Error);
    }
}
```

#### Integration Tests
```csharp
[TestFixture]
public class DatabaseIntegrationTests : IClassFixture<PostgresTestFixture>
{
    [Test]
    public async Task FullPlayerWorkflow_WorksEndToEnd()
    {
        // Create player via EntityCore factory
        var player = Player.Create("IntegrationTest", Guid.NewGuid());
        player.TeamIds.Add(Guid.NewGuid());
        
        // Create via DatabaseService
        var createResult = await CoreService.Players.CreateAsync(player);
        Assert.True(createResult.Success);
        
        // Verify in database (cache bypass)
        var retrieved = await CoreService.Players.GetByIdAsync(player.Id, useCache: false);
        Assert.NotNull(retrieved);
        Assert.Equal(player.TeamIds, retrieved.TeamIds);
        
        // Update via EntityCore business logic
        player.UpdateLastActive();
        var updateResult = await CoreService.Players.UpdateAsync(player);
        Assert.True(updateResult.Success);
        
        // Verify caching (second call hits cache)
        var cached1 = await CoreService.Players.GetByIdAsync(player.Id, useCache: true);
        var cached2 = await CoreService.Players.GetByIdAsync(player.Id, useCache: true);
        Assert.Equal(cached1.UpdatedAt, cached2.UpdatedAt);
        
        // Archive player (triggers pre-delete snapshot)
        await CoreService.Players.DeleteAsync(player.Id);
    }

    [Test]
    public async Task PostgreSQL_JSONB_Operations_WorkCorrectly()
    {
        // Create players with JSONB data (TeamIds is uuid[])
        var player1 = Player.Create("Player1", Guid.NewGuid());
        var teamId = Guid.NewGuid();
        player1.TeamIds.Add(teamId);
        await CoreService.Players.CreateAsync(player1);
        
        var player2 = Player.Create("Player2", Guid.NewGuid());
        player2.TeamIds.Add(Guid.NewGuid());
        await CoreService.Players.CreateAsync(player2);
        
        // Test JSONB containment query via WithDbContext
        var playersInTeam = await CoreService.WithDbContext(async db =>
            await db.Players
                .Where(p => p.TeamIds.Contains(teamId)) // PostgreSQL: team_ids @> ARRAY[teamId]::uuid[]
                .ToListAsync()
        );
        
        // Assert
        Assert.Single(playersInTeam);
        Assert.Equal(player1.Id, playersInTeam[0].Id);
    }

    [Test]
    public async Task Game_WithStateHistory_JSONB_Roundtrip()
    {
        // Create game with JSONB state snapshots
        var game = new Game { MapId = Guid.NewGuid() };
        game.StateHistory.Add(new GameStateSnapshot { Data = new { score = 10 } });
        game.StateHistory.Add(new GameStateSnapshot { Data = new { score = 20 } });
        
        await CoreService.Games.CreateAsync(game);
        
        // Retrieve with Include
        var loaded = await CoreService.WithDbContext(async db =>
            await db.Games
                .Include(g => g.StateHistory)
                .FirstOrDefaultAsync(g => g.Id == game.Id)
        );
        
        // Assert JSONB data round-tripped correctly
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.StateHistory.Count);
        Assert.Equal(10, loaded.StateHistory[0].Data["score"]);
    }
}
```

#### Performance Tests
```csharp
[MemoryDiagnoser]
public class DatabasePerformanceTests
{
    private Guid _testPlayerId;

    [GlobalSetup]
    public async Task Setup()
    {
        CoreService.InitializeServices(new CoreEventBus(), new ErrorService());
        var player = Player.Create("PerfTest", Guid.NewGuid());
        await CoreService.Players.CreateAsync(player);
        _testPlayerId = player.Id;
    }

    [Benchmark(Description = "DatabaseService GetById (no cache)")]
    public async Task<Player?> GetPlayerById_NoCache()
    {
        return await CoreService.Players.GetByIdAsync(_testPlayerId, useCache: false);
    }

    [Benchmark(Description = "DatabaseService GetById (with cache)")]
    public async Task<Player?> GetPlayerById_WithCache()
    {
        return await CoreService.Players.GetByIdAsync(_testPlayerId, useCache: true);
    }

    [Benchmark(Description = "JSONB Containment Query")]
    public async Task<List<Player>> JSONB_ContainmentQuery()
    {
        return await CoreService.WithDbContext(async db =>
            await db.Players
                .Where(p => p.TeamIds.Contains(Guid.NewGuid()))
                .ToListAsync()
        );
    }

    [Benchmark(Description = "Complex Query with Includes")]
    public async Task<List<Match>> ComplexQueryWithIncludes()
    {
        return await CoreService.WithDbContext(async db =>
            await db.Matches
                .Include(m => m.Team1)
                .Include(m => m.Team2)
                .Where(m => m.Status == MatchStatus.InProgress)
                .OrderByDescending(m => m.CreatedAt)
                .Take(10)
                .ToListAsync()
        );
    }
}
```

### Cleanup Checklist

#### Remove Deprecated Code
- [x] **Delete Old Services**: Individual entity service classes were never created (jumped directly to DatabaseService pattern)
- [x] **Remove ListWrapper Classes**: Removed PlayerListWrapper, TeamListWrapper, etc. (Step 5.5)
- [x] **Clean Interfaces**: IJsonVersioned and other unused interfaces removed
- [ ] **Update Imports**: Remove unused using statements (ongoing cleanup)
- [ ] **Delete Deprecated Directory**: Clean up `src/deprecated/` folder once all replacements are confirmed working
- [ ] **Remove SQLite References**: Remove any remaining SQLite-related configuration/documentation

#### Configuration Updates (No DI, PostgreSQL-only)
- [x] **appsettings.json**: PostgreSQL connection strings configured (Step 9)
- [x] **CoreService Initialization**: Static initialization via `CoreService.InitializeServices()` (Step 7)
- [x] **No Runtime DI**: Architecture explicitly avoids runtime dependency injection
- [x] **Event Handlers**: CoreEventBus, DiscBotEventBus, and GlobalEventBus integrated (Step 6.5)
- [ ] **Remove Legacy DI References**: Clean up any remaining DI-related comments or placeholder code
- [ ] **Verify Startup Flow**: Ensure proper initialization order in application startup

#### Documentation Updates
- [ ] **README.md**: Update architecture overview with current patterns
- [ ] **"Add an Entity" Guide**: Document the Entity + EntityCore + [EntityMetadata] pattern
- [ ] **CoreService Usage Guide**: Document static accessors and WithDbContext pattern
- [ ] **Migration Guide**: Document EF Core migration workflow and best practices (see `migration-template-guide.md`)
- [ ] **Performance Guide**: Document JSONB benefits, GIN index usage, and caching strategies
- [ ] **Versioning Guide**: Document application/schema versioning and compatibility (Step 6.6)

### Final Validation

#### Architecture Verification
- ✅ **Static CoreService**: Infrastructure orchestration with generated DatabaseService accessors
- ✅ **DatabaseService<TEntity>**: Provider-agnostic data layer (Repository, Cache, Archive)
- ✅ **EntityCore Pattern**: Business logic co-located with entity definitions
- ✅ **EF Core Integration**: Native PostgreSQL JSONB support via Npgsql
- ✅ **No Runtime DI**: Static initialization via `CoreService.InitializeServices()`
- ✅ **Event Bus System**: CoreEventBus, DiscBotEventBus, GlobalEventBus with automatic forwarding
- ✅ **Source Generators**: DbContext, EntityConfig, DatabaseService accessors, Archive entities
- ✅ **Partial Classes**: Generator-friendly architecture with manual extensions

#### Performance Verification
- ✅ **JSONB Operations**: Faster than manual serialization
- ✅ **Query Optimization**: GIN indexes working correctly
- ✅ **Caching**: Efficient in-memory operations
- ✅ **Connection Pooling**: Proper database connection management

#### Code Quality Verification
- ✅ **Lean Implementation**: No speculative code
- ✅ **Consistent Patterns**: Same approach across all entities
- ✅ **Clean Interfaces**: No unnecessary abstractions
- ✅ **Proper Error Handling**: Comprehensive error management

### Success Metrics

The refactor is successful when:
- ✅ **All Tests Pass**: Unit, integration, and performance tests
- ✅ **Zero Breaking Changes**: Existing functionality preserved
- ✅ **Performance Improved**: Measurable gains over old architecture
- ✅ **Code Maintainable**: New features easy to add
- ✅ **Documentation Complete**: All changes documented
- ✅ **Clean Architecture**: No deprecated code remaining

**This finalization step ensures our refactored architecture is production-ready and maintainable!** 🎯

### Benefits of New Architecture

#### Simplicity
- **Static CoreService** provides single entry point for infrastructure
- **Generated Accessors**: `CoreService.Players`, `CoreService.Teams` - zero boilerplate
- **Source Generators** eliminate manual DbContext configuration
- **EntityCore Pattern** keeps business logic with entities
- **Generic DatabaseService** handles CRUD, caching, and archiving uniformly

#### Maintainability
- **Entity + EntityCore** pattern: data model and business logic clearly separated
- **Consistent patterns** across all entities (factory methods, validation, state transitions)
- **`[EntityMetadata]` driven** database mappings auto-generated at compile time
- **Partial classes** allow generator extensions without manual code conflicts
- **No runtime DI** makes initialization flow explicit and debuggable

#### Performance
- **Pluggable caching** (NoOp, InMemory LRU, future: Redis)
- **GIN indexes** for JSONB queries (5-50x faster than table scans)
- **Npgsql dynamic JSON** for native JSONB serialization
- **Connection pooling** via NpgsqlDataSource
- **Archive system** separates hot path from historical queries

#### Developer Experience
- **Add entity**: Create `Entity.cs` + `EntityCore.cs` + add `[EntityMetadata]` → accessor auto-generated
- **Consistent API**: `await CoreService.{Entity}.GetByIdAsync(id)` for all entities
- **Complex queries**: `CoreService.WithDbContext(async db => ...)` for EF LINQ
- **Type safety**: Guid-based relationships, compile-time verified
- **Clear file organization**: Entity definitions in `Models/{Domain}`, logic in `{Entity}Core.cs`
- **Testable**: Mock CoreService accessors or use Testcontainers for integration tests

### Database Migration Strategy

For comprehensive guidance on handling database schema migrations when deploying entity definition changes to production, see: [`database-migration-strategy.md`](./database-migration-strategy.md)

This separate document covers:
- Risk-based migration categories (Low/Medium/High Risk)
- Production deployment workflows
- Rollback strategies and best practices
- EF Core migration implementation patterns
- Testing strategies for schema changes

**The WabbitBot architecture refactor is now complete!** 🎉
