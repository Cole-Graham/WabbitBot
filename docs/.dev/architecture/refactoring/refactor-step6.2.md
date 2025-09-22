#### Step 6.2: Code Organization and Cleanup ✅ COMPLETED

**Clean up and reorganize configuration classes** following the entity grouping patterns established in DbContext and migrations. This improves maintainability and consistency across the codebase.

### **🎯 OBJECTIVES:**

1. **Reorganize ConfigureIndexes Method** - Sort regions alphabetically, remove deprecated sections
2. **Reorganize EntityConfigurations.cs** - Group by entity families like DbContext files
3. **Reorganize EntityConfigTests.cs** - Match the new EntityConfigurations.cs structure

### **📋 DETAILED TASKS:**

#### 6.2a. Reorganize ConfigureIndexes Method Regions

**Current State:** Regions are not in alphabetical order, contains deprecated Stats section.

**Target State:** Clean alphabetical organization matching migration file patterns.

```csharp
// ❌ BEFORE: Random order with deprecated Stats
#region Game
// ... game indexes
#endregion

#region Stats  // ❌ DEPRECATED - remove entirely
// ... stats indexes (no longer used)
#endregion

#region Scrimmage
// ... scrimmage indexes
#endregion

// ✅ AFTER: Alphabetical order, no deprecated sections
#region Game
// ... game indexes
#endregion

#region Leaderboard
// ... leaderboard indexes
#endregion

#region Map
// ... map indexes
#endregion

#region Match
// ... match indexes
#endregion

#region Scrimmage
// ... scrimmage indexes
#endregion

#region Tournament
// ... tournament indexes
#endregion

#region User
// ... user indexes
#endregion
```

**Specific Changes:**
- Remove entire `#region Stats` section (Stats now stored as JSONB)
- Reorder regions: Game → Leaderboard → Map → Match → Scrimmage → Tournament → User
- Ensure each region matches its corresponding migration file structure

#### 6.2b. Reorganize EntityConfigurations.cs by Entity Groups

**Current State:** Mixed organization with some grouping but inconsistent.

**Target State:** Consistent entity grouping like DbContext partial files.

```csharp
// ❌ BEFORE: Mixed organization
// Common/Core entities
private static readonly Lazy<PlayerDbConfig> _playerDbConfig = new(() => new PlayerDbConfig());
private static readonly Lazy<UserDbConfig> _userDbConfig = new(() => new UserDbConfig());

// Additional entities (Stats, SeasonConfig, SeasonGroup, LeaderboardItem)
private static readonly Lazy<StatsDbConfig> _statsDbConfig = new(() => new StatsDbConfig());

// Vertical slice entities
private static readonly Lazy<ScrimmageDbConfig> _scrimmageDbConfig = new(() => new ScrimmageDbConfig());

// ✅ AFTER: Entity-grouped organization
#region Core Entities
private static readonly Lazy<PlayerDbConfig> _playerDbConfig = new(() => new PlayerDbConfig());
private static readonly Lazy<UserDbConfig> _userDbConfig = new(() => new UserDbConfig());
private static readonly Lazy<TeamDbConfig> _teamDbConfig = new(() => new TeamDbConfig());
#endregion

#region Game Entities
private static readonly Lazy<GameDbConfig> _gameDbConfig = new(() => new GameDbConfig());
private static readonly Lazy<GameStateSnapshotDbConfig> _gameStateSnapshotDbConfig = new(() => new GameStateSnapshotDbConfig());
private static readonly Lazy<MapConfig> _mapDbConfig = new(() => new MapConfig());
#endregion

#region Leaderboard Entities
private static readonly Lazy<LeaderboardDbConfig> _leaderboardDbConfig = new(() => new LeaderboardDbConfig());
private static readonly Lazy<LeaderboardItemDbConfig> _leaderboardItemDbConfig = new(() => new LeaderboardItemDbConfig());
private static readonly Lazy<SeasonDbConfig> _seasonDbConfig = new(() => new SeasonDbConfig());
private static readonly Lazy<SeasonConfigDbConfig> _seasonConfigDbConfig = new(() => new SeasonConfigDbConfig());
private static readonly Lazy<SeasonGroupDbConfig> _seasonGroupDbConfig = new(() => new SeasonGroupDbConfig());
#endregion

#region Match Entities
private static readonly Lazy<MatchDbConfig> _matchDbConfig = new(() => new MatchDbConfig());
private static readonly Lazy<MatchStateSnapshotDbConfig> _matchStateSnapshotDbConfig = new(() => new MatchStateSnapshotDbConfig());
#endregion

#region Scrimmage Entities
private static readonly Lazy<ScrimmageDbConfig> _scrimmageDbConfig = new(() => new ScrimmageDbConfig());
private static readonly Lazy<ProvenPotentialRecordConfig> _provenPotentialRecordDbConfig = new(() => new ProvenPotentialRecordConfig());
#endregion

#region Tournament Entities
private static readonly Lazy<TournamentDbConfig> _tournamentDbConfig = new(() => new TournamentDbConfig());
private static readonly Lazy<TournamentStateSnapshotDbConfig> _tournamentStateSnapshotDbConfig = new(() => new TournamentStateSnapshotDbConfig());
#endregion
```

**Remove Deprecated:**
- `StatsDbConfig` (Stats now JSONB)
- Any other deprecated configurations

#### 6.2c. Reorganize EntityConfigTests.cs to Match New Structure

**Update test organization** to match the new EntityConfigurations.cs grouping.

```csharp
// Group tests by entity family to match new configuration structure

#region Core Entity Tests
[Fact]
public void PlayerDbConfig_ShouldHaveCorrectSettings() { /* ... */ }

[Fact]
public void UserDbConfig_ShouldHaveCorrectSettings() { /* ... */ }

[Fact]
public void TeamDbConfig_ShouldHaveCorrectSettings() { /* ... */ }
#endregion

#region Game Entity Tests
[Fact]
public void GameDbConfig_ShouldHaveCorrectSettings() { /* ... */ }

[Fact]
public void GameStateSnapshotDbConfig_ShouldHaveCorrectSettings() { /* ... */ }

[Fact]
public void MapConfig_ShouldHaveCorrectSettings() { /* ... */ }
#endregion

// ... etc for each entity group
```

### **✅ SUCCESS CRITERIA:**

- **ConfigureIndexes method** regions in strict alphabetical order
- **No deprecated sections** (Stats removed)
- **EntityConfigurations.cs** grouped by entity families with clear regions
- **EntityConfigTests.cs** reorganized to match configuration groupings
- **All tests pass** after reorganization
- **Consistent naming** across all configuration files

### **🔗 DEPENDENCIES:**

- **Requires:** Step 6.1 (Entity relocation) - entities must be in Common/Models first
- **Enables:** Future configuration additions will follow consistent patterns

### **📈 IMPACT:**

**Before:** Inconsistent organization, deprecated code, hard to maintain
**After:** Clean alphabetical organization, no deprecated code, easy to maintain

**This step establishes consistent patterns** for all future configuration additions and makes the codebase much more maintainable! 🎯
