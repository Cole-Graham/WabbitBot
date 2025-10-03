#### Step 8: Entity Migration

### Migrating All Entity Operations to CoreService

**🎯 CRITICAL**: Systematically migrate all entity operations from individual services to the unified CoreService architecture.

#### 8a. Migrate Player Entity Operations

Move all Player-related operations from individual services to CoreService.Player methods.

```csharp
// Before: Individual PlayerService
public class PlayerService
{
    public async Task<Player?> GetPlayerByIdAsync(Guid playerId)
    {
        // Old implementation with manual SQL
    }

    public async Task<Result<Player>> CreatePlayerAsync(Player player)
    {
        // Old implementation
    }
}

// After: CoreService.Player operations
public partial class CoreService
{
    #region Player Business Logic

    // ✅ CORRECT: Call the unified DatabaseService directly.
    public async Task<Player?> GetPlayerAsync(Guid playerId)
    {
        return await _playerData.GetByIdAsync(playerId);
    }

    // Only create specific methods when there's unique business logic
    public async Task<Result<Player>> CreatePlayerWithValidationAsync(Player player)
    {
        // Business logic validation specific to player creation
        var validationResult = await ValidatePlayerAsync(player);
        if (!validationResult.Success) return validationResult;

        return await _playerData.CreateAsync(player);
    }

    #endregion
}
```

#### 8b. Migrate User Entity Operations

Move all User-related operations to CoreService.User methods.

```csharp
// CoreService.User.cs
public partial class CoreService
{
    #region User Business Logic

    public async Task<User?> GetUserByDiscordIdAsync(ulong discordId)
    {
        // Example assumes a custom query method in the repository partial
        return await _userData.GetByDiscordIdAsync(discordId);
    }

    // Only create specific methods when there's unique business logic
    public async Task<Result<User>> CreateUserWithDiscordValidationAsync(User user)
    {
        // User-specific Discord validation and business logic
        var discordValidation = await ValidateDiscordUserAsync(user);
        if (!discordValidation.Success) return discordValidation;

        return await _userData.CreateAsync(user);
    }

    #endregion
}
```

#### 8c. Migrate Team Entity Operations

Move all Team-related operations to CoreService.Team methods.

```csharp
// CoreService.Team.cs
public partial class CoreService
{
    #region Team Business Logic

    public async Task<Team?> GetTeamByIdAsync(Guid teamId)
    {
        return await _teamData.GetByIdAsync(teamId);
    }

    // Only create specific methods when there's unique business logic
    public async Task<Result<Team>> CreateTeamWithPlayerLimitsAsync(Team team)
    {
        // Team-specific validation (max players, etc.)
        var validation = await ValidateTeamAsync(team);
        if (!validation.Success) return validation;

        return await _teamData.CreateAsync(team);
    }

    #endregion
}
```

#### 8d. Migrate Map Entity Operations

Move all Map-related operations to CoreService.Map methods.

```csharp
// CoreService.Map.cs
public partial class CoreService
{
    #region Map Business Logic

    public async Task<Map?> GetMapByIdAsync(Guid mapId)
    {
        return await _mapData.GetByIdAsync(mapId);
    }

    // Only create specific methods when there's unique business logic
    public async Task<IEnumerable<Map>> GetActiveTournamentMapsAsync()
    {
        // Custom query for tournament-active maps
        return await _mapData.GetActiveTournamentMapsAsync();
    }

    #endregion
}
```

#### 8e. Update All Calling Code (Align with No-DI Policy)

Update all Discord commands and other services to use CoreService instead of individual services.

```csharp
// Before: Discord command using separate services (remove these in favor of CoreService)
public class PlayerCommands : ApplicationCommandModule
{
    private readonly PlayerService _playerService; // Remove
    private readonly TeamService _teamService;     // Remove
}

// After: Use CoreService directly — instantiate or obtain via a centralized provider (no runtime DI)
public class PlayerCommands : ApplicationCommandModule
{
    private readonly CoreService _coreService = new CoreService(new CoreEventBus(), new ErrorService());

    [SlashCommand("profile", "Get player profile")]
    public async Task GetProfileAsync(InteractionContext ctx, string playerName)
    {
        var player = await _coreService.GetPlayerByNameAsync(playerName);
        // ... rest of implementation
    }
}
```

#### STEP 8 IMPACT:

### Migration Strategy

1. **🎯 Start with Player**: Use Player as the template for all other entities
2. **🔄 Gradual Migration**: Migrate one entity at a time to ensure stability
3. **🧪 Test Each Step**: Verify functionality after each entity migration
4. **📋 Update References**: Change all calling code to use CoreService methods
5. **🗑️ Remove Old Services**: Clean up individual entity services after migration

### Player Migration Example

```csharp
// Before: Individual PlayerService
public class PlayerService
{
    public async Task<Player?> GetPlayerByIdAsync(Guid playerId)
    {
        // Old implementation
    }
}

// After: CoreService.Player operations
public partial class CoreService
{
    // Direct usage of unified DatabaseService
    public async Task DoSomethingWithPlayer(Guid playerId)
    {
        var player = await _playerData.GetByIdAsync(playerId);
        // ...
    }

    // Specific business logic only when needed
    public async Task<Player?> GetPlayerByNameAsync(string name)
    {
        // Custom logic here, calling a custom method on DatabaseService
        var player = await _playerData.GetByNameAsync(name);
        return player;
    }
}
```

### Benefits of Unified Entity Management

1. **🎯 Single Source of Truth**: All entity operations in one place
2. **🔄 Consistent Patterns**: Same approach for all entities
3. **🧹 Reduced Complexity**: No duplicate service classes
4. **🚀 Better Performance**: Optimized data access patterns
5. **🛠️ Easier Maintenance**: Changes affect all entities consistently

### Migration Checklist

- [ ] **Player Entity**: Complete migration and testing
- [ ] **User Entity**: Migrate operations and update references
- [ ] **Team Entity**: Migrate operations and update references
- [ ] **Map Entity**: Migrate operations and update references
- [ ] **Update Discord Commands**: Change service references to CoreService
- [ ] **Update Event Handlers**: Migrate to CoreService event subscriptions
- [ ] **Remove Old Services**: Delete individual entity service classes
- [ ] **Update Tests**: Modify unit tests to use CoreService

**This entity migration completes the transition to our unified CoreService architecture!** 🎯
