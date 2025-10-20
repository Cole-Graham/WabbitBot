# Development Database Seed Data

## Overview

The `DevelopmentDataSeeder` provides test data for local development and testing. It automatically seeds the database with predefined teams and players when running in Development environment.

## Location

**File:** `src/WabbitBot.Core/Common/Database/DevelopmentDataSeeder.cs`

## How It Works

The seeder runs automatically during application startup when:
1. The environment is set to `Development`
2. After database migrations have been applied
3. Before other core services are initialized

The seeding process is **idempotent** - it's safe to run multiple times. The seeder checks if data already exists before creating it, so restarting the application won't create duplicates.

## Default Seed Data

### AlphaTeam
- **Team Name:** AlphaTeam
- **Team Tag:** ALPHA
- **Team Type:** Team (supports Duo/Squad)
- **Roster Group:** Duo
- **Players:**
  - Discord User ID: `1348719242882584689` (Captain)
  - Discord User ID: `1348724033306366055`

### BravoTeam
- **Team Name:** BravoTeam
- **Team Tag:** BRAVO
- **Team Type:** Team (supports Duo/Squad)
- **Roster Group:** Duo
- **Players:**
  - Discord User ID: `1348724778906681447` (Captain)
  - Discord User ID: `1348725467422916749`

## What Gets Created

For each team, the seeder creates:

1. **MashinaUser** entities for each Discord user ID
   - Links Discord users to the system
   - Stores Discord username, mention, and other metadata

2. **Player** entities for each user
   - Game-specific player data
   - Links to MashinaUser

3. **Team** entity
   - Team metadata (name, tag, type)
   - First player is set as team captain

4. **TeamRoster** entity
   - Roster configuration for the team size
   - Links players to the team

5. **TeamMember** entities
   - One for each player on the roster
   - First player is marked as captain and team manager
   - All members are set to receive scrimmage pings

## Customizing Seed Data

To add or modify seed data:

1. Edit `src/WabbitBot.Core/Common/Database/DevelopmentDataSeeder.cs`
2. Add new Discord user IDs to existing teams or create new teams
3. Call `SeedTeamAsync` with your custom parameters

### Example: Adding a New Team

```csharp
// In DevelopmentDataSeeder.SeedAsync method
var charlieTeamUserIds = new List<ulong> { 1234567890123456789, 9876543210987654321 };

await SeedTeamAsync(
    context,
    teamName: "CharlieTeam",
    teamTag: "CHARLIE",
    discordUserIds: charlieTeamUserIds,
    teamType: TeamType.Team,
    rosterGroup: TeamSizeRosterGroup.Squad
);
```

## Disabling Seed Data

Seed data only runs in Development environment. To disable it:

1. Set the environment variable: `ASPNETCORE_ENVIRONMENT=Production`
2. Or remove/comment out the seeding call in `Program.cs`

## Console Output

When the seeder runs, you'll see console output like:
```
🌱 Seeding development data...
✅ Created team 'AlphaTeam' with 2 players
✅ Created team 'BravoTeam' with 2 players
✅ Development data seeded successfully!
```

If data already exists:
```
🌱 Seeding development data...
⏭️  Team 'AlphaTeam' already exists, skipping...
⏭️  Team 'BravoTeam' already exists, skipping...
✅ Development data seeded successfully!
```

## Database Reset

To reset the database and re-seed:

```bash
# Drop and recreate the database
dotnet ef database drop --project src/WabbitBot.Core --startup-project src/WabbitBot.Host
dotnet ef database update --project src/WabbitBot.Core --startup-project src/WabbitBot.Host

# Seed data will run automatically on next app start
```

## Related Files

- `src/WabbitBot.Core/Common/Database/DevelopmentDataSeeder.cs` - Seeder implementation
- `src/WabbitBot.Host/Program.cs` - Calls the seeder during initialization
- `src/WabbitBot.Core/Common/Models/Common/Team.cs` - Team entity models
- `src/WabbitBot.Core/Common/Models/Common/Player.cs` - Player entity
- `src/WabbitBot.Core/Common/Models/Common/MashinaUser.cs` - Discord user entity

