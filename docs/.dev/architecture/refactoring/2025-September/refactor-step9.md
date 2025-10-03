#### Step 9: Configuration and PostgreSQL Setup

### Complete PostgreSQL Integration and Configuration

**🎯 CRITICAL**: Finalize PostgreSQL configuration, JSON strategy, and database schema setup for production deployment.

#### 9a. PostgreSQL JSON Strategy - Use Npgsql Library

### Current Issue
❌ **Current approach**: Using `System.Text.Json` + manual JSON handling
❌ **Problem**: Missing out on PostgreSQL's native JSON performance

### Recommended Solution
✅ **Use Npgsql** - PostgreSQL's official .NET driver with JSON support

#### Why Npgsql?
- Native PostgreSQL JSONB support
- Automatic JSON serialization/deserialization
- Better performance than manual JSON handling
- LINQ support for JSON queries
- Type-safe JSON operations

#### Migration Steps:
1. Add Npgsql package to project
2. Replace manual JSON utilities with Npgsql's JSON features
3. Use Npgsql's `Jsonb` type for JSON columns
4. Leverage Npgsql's LINQ provider for JSON queries

#### Example Npgsql JSON Usage:
```csharp
// The DatabaseService<TEntity> will handle the underlying DbContext operations.
public partial class DatabaseService<Player>
{
    // Custom query method in the Repository partial class
    public async Task<IEnumerable<Player>> GetPlayersInTeam(Guid teamId)
    {
        return await _dbContext.Players
            .Where(p => p.TeamIds.Contains(teamId)) // Native JSON query with Guid
            .ToListAsync();
    }
}
```

#### Benefits:
- 🚀 **Better Performance**: Native JSON operations
- 🔒 **Type Safety**: Strongly-typed JSON handling
- 🛠️ **Rich Features**: LINQ support, automatic mapping
- 📈 **Scalability**: Optimized for PostgreSQL JSONB

#### 9b. Configuration Management (PostgreSQL Only)

#### appsettings.json Structure
```json
{
  "Bot": {
    "Database": {
      "Provider": "PostgreSQL",
      "ConnectionString": "Host=localhost;Database=wabbitbot;Username=...;Password=..."
    },
    "Maps": {
      "ThumbnailsDirectory": "data/maps/thumbnails",
      "DefaultThumbnail": "default.jpg",
      "SupportedExtensions": [".jpg", ".jpeg", ".png", ".gif", ".webp"],
      "MaxThumbnailSizeKB": 2048
    }
  },
  "Cache": {
    "DefaultExpiryMinutes": 60,
    "MaxSize": 1000
  }
}
```

#### 9c. Database Schema Changes (Generated, Guid-first)

#### Unified Table Structure
All entity tables follow the same pattern:

```sql
-- Schema is owned by generated DbContext; example illustrative only
CREATE TABLE players (
    id UUID PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    last_active TIMESTAMP NOT NULL,
    is_archived BOOLEAN NOT NULL DEFAULT FALSE,
    archived_at TIMESTAMP NULL,
    team_ids JSONB,
    previous_user_ids JSONB,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL
);
```

#### Indexing Strategy (including JSONB GIN)
```sql
-- Standard indexes for all entities
CREATE INDEX idx_players_name ON players(Name);
CREATE INDEX idx_players_created_at ON players(CreatedAt);
CREATE INDEX idx_players_is_archived ON players(IsArchived);

-- JSON indexes for complex queries
CREATE INDEX idx_players_team_ids ON players USING GIN (TeamIdsJson);
CREATE INDEX idx_players_previous_user_ids ON players USING GIN (PreviousUserIdsJson);
```

#### 9d. Guid-Based Entity Relationships (Post Step 6.1)

#### ✅ **DECISION MADE**: Entity properties use `Guid` for relationships

**From Step 6.1**: All child entity references to parent entities now use `Guid` properties instead of `string` properties.

```csharp
// ✅ CORRECT: Entity relationships use Guid properties
public class Match : Entity
{
    public Guid Team1Id { get; set; }    // Foreign key to Team.Id (UUID)
    public Guid Team2Id { get; set; }    // Foreign key to Team.Id (UUID)
    public Guid? WinnerId { get; set; }  // Foreign key to Team.Id (UUID)
    public List<Guid> Team1PlayerIds { get; set; } = new(); // Player IDs
    public List<Guid> Team2PlayerIds { get; set; } = new(); // Player IDs
}

public class Player : Entity
{
    public List<Guid> TeamIds { get; set; } = new(); // Foreign keys to Team.Id
}
```

#### Benefits of Guid Properties:
- **Type Safety**: Compiler prevents mixing entity types
- **Performance**: Direct UUID comparisons in database
- **Data Integrity**: Foreign key constraints work properly
- **Debugging**: Clear which entity type is referenced

#### When to Convert to Strings:
```csharp
// Only convert to strings when needed for external systems
public class DiscordService
{
    public async Task SendMatchResultAsync(Match match)
    {
        // Convert Guids to strings for Discord API
        var embed = new DiscordEmbedBuilder()
            .AddField("Winner", match.WinnerId?.ToString() ?? "Draw")
            .AddField("Team 1", match.Team1Id.ToString())
            .AddField("Team 2", match.Team2Id.ToString());
    }
}
```

#### 9e. String Parameters in Business Logic

Strings are still used for **external inputs and API parameters** where type safety isn't guaranteed:

#### External API Integration 📡
```csharp
// Discord commands receive string IDs from users
[Command("get-player")]
public async Task GetPlayerAsync(CommandContext ctx, string playerIdString)
{
    // Parse string to Guid for internal operations
    if (!Guid.TryParse(playerIdString, out var playerId))
    {
        await ctx.RespondAsync("Invalid player ID format");
        return;
    }

    // Use Guid internally for type safety
    var player = await GetByStringIdAsync(playerId.ToString(), DatabaseComponent.Cache);
    // ... rest of logic
}
```

#### Web API Endpoints 🌐
```csharp
// REST API accepts string IDs but converts to Guid
[HttpGet("players/{id}")]
public async Task<IActionResult> GetPlayer(string id)
{
    if (!Guid.TryParse(id, out var playerId))
        return BadRequest("Invalid ID format");

    var player = await GetByStringIdAsync(playerId.ToString(), DatabaseComponent.Cache);
    return Ok(player);
}
```

#### Security and Validation 🔒
```csharp
// Input validation still uses strings for safety
public async Task<Player?> GetPlayerByIdSafeAsync(string playerIdString)
{
    if (string.IsNullOrWhiteSpace(playerIdString))
        return null;

    // Validate format before parsing
    if (!Guid.TryParse(playerIdString, out var playerId))
        return null;

    // Use validated Guid internally
    return await GetByStringIdAsync(playerId.ToString(), DatabaseComponent.Cache);
}
```

#### STEP 9 IMPACT:

### Guid-First Architecture with Strategic String Conversion

#### Core Principle: Guid Properties, String Conversion When Needed

**Entity relationships use Guid properties internally, but convert to strings for external APIs.**

```csharp
// ✅ INTERNAL: Entities use Guid properties for type safety
public class Match : Entity
{
    public Guid Team1Id { get; set; }        // Guid property
    public Guid Team2Id { get; set; }        // Guid property
    public Guid? WinnerId { get; set; }      // Guid property
}

// ✅ EXTERNAL: Convert to strings for APIs and user interfaces
public class MatchDto
{
    public string Team1Id { get; set; }      // String for API
    public string Team2Id { get; set; }      // String for API
    public string? WinnerId { get; set; }    // String for API

    // Conversion methods
    public static MatchDto FromEntity(Match match) => new()
    {
        Team1Id = match.Team1Id.ToString(),
        Team2Id = match.Team2Id.ToString(),
        WinnerId = match.WinnerId?.ToString()
    };
}
```

#### `DatabaseService<TEntity>` (Aligned with Guid Architecture)

The `DatabaseService` itself doesn't need a public interface, as it is instantiated directly. Its public methods serve as the contract.

```csharp
// From: src/WabbitBot.Common/Data/Service/DatabaseService.cs
public partial class DatabaseService<TEntity> where TEntity : Entity
{
    // High-level coordination methods
    public async Task<TEntity?> GetByIdAsync(Guid id, bool useCache = true) { /* ... */ }
    public async Task<Result<TEntity>> CreateAsync(TEntity entity) { /* ... */ }
    public async Task<Result<TEntity>> UpdateAsync(TEntity entity) { /* ... */ }
    public async Task<Result> DeleteAsync(Guid id) { /* ... */ }

    // Methods to directly access a specific component (Repository, Cache, Archive)
    // These might be internal or used for specific scenarios where coordination is bypassed.
    public Task<TEntity?> GetByIdAsync(Guid id, DatabaseComponent component) { /* ... */ }
    // ... other component-specific methods
}
```

#### Direct Usage Pattern - No Thin Wrappers

```csharp
// ✅ CORRECT: Use DatabaseService methods directly from a business logic service
public class MatchLogicService // Example of a class in CoreService partials
{
    private readonly DatabaseService<Match> _matchData;
    private readonly DatabaseService<Team> _teamData;

    public MatchLogicService(DatabaseService<Match> matchData, DatabaseService<Team> teamData)
    {
        _matchData = matchData;
        _teamData = teamData;
    }

    public async Task<MatchResultDto?> ProcessMatchResultAsync(Guid matchId, Guid winnerTeamId)
    {
        // Direct usage - no wrapper methods needed
        var match = await _matchData.GetByIdAsync(matchId);
        if (match == null) return null;

        // Update with Guid properties
        match.WinnerId = winnerTeamId;
        match.Status = MatchStatus.Completed;

        var updateResult = await _matchData.UpdateAsync(match);
        if (!updateResult.Success) return null;

        // Convert to DTO for external consumption
        return MatchResultDto.FromEntity(updateResult.Data!);
    }
}
```

#### When to Create Service Methods (Business Logic Only)

```csharp
public class TournamentLogicService
{
    private readonly DatabaseService<Tournament> _tournamentData;

    // ✅ GOOD: Real business logic with complex operations
    public async Task<TournamentBracketDto> GenerateBracketAsync(Guid tournamentId)
    {
        var tournament = await _tournamentData.GetByIdAsync(tournamentId);
        if (tournament == null) throw new NotFoundException();

        // Complex business logic: matchmaking, seeding, bracket generation
        var matches = await GenerateMatchesForTournamentAsync(tournament);
        var bracket = BuildBracketStructure(matches);

        // Update tournament with bracket data
        tournament.BracketData = JsonSerializer.Serialize(bracket);
        await _tournamentData.UpdateAsync(tournament);

        return TournamentBracketDto.FromTournament(tournament, bracket);
    }

    // ❌ BAD: Thin wrapper - DON'T CREATE THIS
    // public async Task<Tournament?> GetTournamentAsync(Guid id)
    // {
    //     return await _tournamentData.GetByIdAsync(id);
    // }
}
```

#### When Strings Are Still Essential

Even with Guid properties internally, strings remain crucial for several technical and practical reasons:

##### 🔑 **Cache Keys (Abstracted by `DatabaseService.Cache.cs`)**
```csharp
// DatabaseService automatically handles cache key creation internally.
public async Task<Player?> GetPlayerAsync(Guid playerId)
{
    // The GetByIdAsync method in DatabaseService handles the full cache-first logic.
    // 1. Checks cache using a key derived from the Guid.
    // 2. If miss, gets from repository.
    // 3. If found in repo, stores in cache.
    var player = await _playerData.GetByIdAsync(playerId);
    return player;
}
```

*Note: `DatabaseService.Cache.cs` internally uses strings for cache keys but abstracts this complexity away, allowing Guid-based lookups through the main `DatabaseService` methods.*


##### 🌐 **API and Network Protocols**
```csharp
// REST URLs and some protocols are more efficient with strings
GET /api/players/550e8400-e29b-41d4-a716-446655440000  // URL with string ID

// WebSocket messages benefit from compact string payloads
{
  "action": "updatePlayer",
  "playerId": "550e8400-e29b-41d4-a716-446655440000",  // Compact string
  "updates": { "rating": 1500 }
}
```

##### 🔌 **External System Integration**
```csharp
// Discord API returns string IDs
public async Task SyncDiscordUserAsync(string discordUserId)
{
    // Discord gives us strings - we validate and convert
    if (!ulong.TryParse(discordUserId, out var discordId))
        throw new ArgumentException("Invalid Discord ID format");

    var user = await _userData.GetByDiscordIdAsync(discordId); // Assumes custom method
    if (user == null) return;

    // Use the user's PlayerId (Guid) for further internal operations
    var player = await _playerData.GetByIdAsync(user.PlayerId);
}
```

##### 🔒 **Security and Input Validation**
```csharp
// Input validation is safer with strings - prevents injection attacks
public async Task<IActionResult> UpdatePlayer(string id, PlayerUpdateDto update)
{
    // 1. Validate string format first (prevents malicious input)
    if (!Guid.TryParse(id, out var playerId))
        return BadRequest("Invalid player ID format");

    // 2. Additional validation (length, characters, etc.)
    if (id.Length != 36 || !Guid.TryParse(id, out _))
        return BadRequest("Malformed GUID");

    // 3. Only then convert to Guid for type safety
    var player = await _playerData.GetByIdAsync(playerId);
    // ... update logic
}
```

##### 📋 **Debugging and Logging**
```csharp
// Strings are human-readable in logs and debugging
_logger.LogInformation("Processing match result for match {MatchId}", matchIdString);
_logger.LogDebug("Player {PlayerId} joined team {TeamId}",
    playerId.ToString(), teamId.ToString());

// Easier to search logs for specific entities
grep "match:550e8400-e29b-41d4-a716-446655440000" application.log
```

##### ⚡ **Performance Optimizations**
```csharp
// Sometimes you only need the ID reference, not the full object
public async Task<bool> IsPlayerInTournamentAsync(Guid playerId, Guid tournamentId)
{
    // Check membership without loading full objects
    var tournament = await _tournamentData.GetByIdAsync(tournamentId);
    return tournament?.ParticipantPlayerIds.Contains(playerId) ?? false;
}

// Bulk operations with ID collections
public async Task BulkUpdatePlayerRatingsAsync(IEnumerable<string> playerIdStrings)
{
    var playerIds = playerIdStrings.Select(id => Guid.Parse(id)).ToList();
    // Process Guids internally, strings were just for transport
}
```

#### String Handling Strategy

**Guids internally, strings for external interfaces and validation:**

```csharp
// ✅ Discord Command: Accepts string, validates, converts to Guid
[Command("get-match")]
public async Task GetMatchAsync(CommandContext ctx, string matchIdString)
{
    // 1. Validate string input
    if (!Guid.TryParse(matchIdString, out var matchId))
    {
        await ctx.RespondAsync("❌ Invalid match ID format");
        return;
    }

    // 2. Use Guid internally for type safety
    var match = await _matchData.GetByIdAsync(matchId);
    if (match == null)
    {
        await ctx.RespondAsync("❌ Match not found");
        return;
    }

    // 3. Convert back to strings for display
    var embed = new DiscordEmbedBuilder()
        .WithTitle($"Match {match.Id}")
        .AddField("Team 1", match.Team1Id.ToString())
        .AddField("Team 2", match.Team2Id.ToString())
        .AddField("Winner", match.WinnerId?.ToString() ?? "Ongoing");
}

// ✅ Web API: String parameters, Guid internal operations
[HttpGet("matches/{id}")]
public async Task<IActionResult> GetMatch(string id)
{
    if (!Guid.TryParse(id, out var matchId))
        return BadRequest("Invalid match ID format");

    var match = await _matchData.GetByIdAsync(matchId);
    return match != null ? Ok(MatchDto.FromEntity(match)) : NotFound();
}

// ✅ Service Layer: Guids for type safety
public class MatchLogicService
{
    public async Task<MatchResultDto?> ProcessMatchResultAsync(Guid matchId, Guid winnerTeamId)
    {
        // Guids provide compile-time type safety
        var match = await _matchData.GetByIdAsync(matchId);
        if (match == null) return null;

        // Business logic with Guids
        match.WinnerId = winnerTeamId;
        match.Status = MatchStatus.Completed;

        var updateResult = await _matchData.UpdateAsync(match);
        return updateResult.Success ? MatchResultDto.FromEntity(updateResult.Data!) : null;
    }
}
```

### PostgreSQL JSONB Integration

With Guid properties and Npgsql, JSONB operations become type-safe:

```csharp
// ✅ Type-safe JSONB queries with Guids
public async Task<IEnumerable<Player>> GetPlayersInTeamAsync(Guid teamId)
{
    return await _dbContext.Players
        .Where(p => p.TeamIds.Contains(teamId)) // Guid comparison in JSONB
        .ToListAsync();
}

// ✅ Complex JSONB operations maintain type safety
public async Task<IEnumerable<Match>> GetRecentMatchesAsync(Guid playerId)
{
    return await _dbContext.Matches
        .Where(m => m.Team1PlayerIds.Contains(playerId) ||
                   m.Team2PlayerIds.Contains(playerId))
        .Where(m => m.CreatedAt > DateTime.UtcNow.AddDays(-7))
        .OrderByDescending(m => m.CreatedAt)
        .ToListAsync();
}
```

**This updated Step 9 now reflects our actual Guid-based architecture and eliminates the outdated string-vs-Guid debate!** 🎯
