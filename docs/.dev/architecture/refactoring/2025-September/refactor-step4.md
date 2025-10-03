#### Step 4: Schema Migration ✅ COMPLETE

### Database Schema Evolution Strategy

**🎯 CRITICAL**: Since this application has **no production deployments or existing data**, we can design the schema from scratch for maximum PostgreSQL JSON capabilities.

#### 4a. Implement Database Versioning Strategy

Set up EF Core migrations to manage database schema evolution independently of application deployments.

```bash
# Create initial migration
dotnet ef migrations add InitialSchema --project WabbitBot.Core --startup-project WabbitBot.Core

# Apply migrations on startup
await dbContext.Database.MigrateAsync();
```

#### 4b. Handle Schema Migration Scripts

Create migration files that define the complete PostgreSQL schema with JSONB support.

```csharp
// 20241201120000_InitialSchema.cs
public partial class InitialSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

        migrationBuilder.CreateTable(
            name: "players",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                name = table.Column<string>(type: "varchar(255)", nullable: false),
                last_active = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                team_memberships = table.Column<List<TeamMembership>>(type: "jsonb", nullable: false),
                stats = table.Column<PlayerStats>(type: "jsonb", nullable: false),
                metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_players", x => x.id);
            });

        // Create GIN indexes for JSONB performance
        migrationBuilder.CreateIndex(
            name: "idx_players_team_memberships",
            table: "players",
            column: "team_memberships")
            .Annotation("Npgsql:IndexMethod", "gin");

        migrationBuilder.CreateIndex(
            name: "idx_players_stats",
            table: "players",
            column: "stats")
            .Annotation("Npgsql:IndexMethod", "gin");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "players");
    }
}
```

#### 4c. Add JSONB Indexes for Performance Optimization

Implement GIN (Generalized Inverted Index) indexes specifically designed for JSONB querying.

```sql
-- GIN indexes for JSONB columns enable fast queries
CREATE INDEX idx_players_team_memberships ON players USING GIN (team_memberships);
CREATE INDEX idx_players_stats ON players USING GIN (stats);
CREATE INDEX idx_players_metadata ON players USING GIN (metadata);

-- Query examples that benefit from GIN indexes
SELECT * FROM players WHERE team_memberships @> '[{"teamId": "team-123"}]';
SELECT * FROM players WHERE stats ->> 'gamesPlayed' > '100';
```

#### 4d. Update Table Structures for Native JSON Support

Design tables that maximize PostgreSQL JSONB capabilities without manual serialization.

```sql
-- ✅ NEW: Native JSONB columns everywhere
CREATE TABLE players (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    last_active TIMESTAMP NOT NULL,
    is_archived BOOLEAN NOT NULL DEFAULT FALSE,
    archived_at TIMESTAMP NULL,
    team_memberships JSONB NOT NULL,  -- Complex array
    stats JSONB NOT NULL,            -- Complex object
    metadata JSONB NOT NULL,         -- Flexible data
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- JSONB supports advanced querying
SELECT
    p.name,
    p.stats ->> 'gamesPlayed' as games_played,
    p.stats ->> 'wins' as wins,
    jsonb_array_length(p.team_memberships) as team_count
FROM players p
WHERE p.stats ->> 'gamesPlayed' > '50'
  AND p.team_memberships @> '[{"teamId": "team-123"}]';
```

#### 4e. Migrate Existing Data (if any)

Since this is a new application with no existing data, implement a clean schema from scratch.

```csharp
// For future migrations with data, use this pattern:
public partial class AddNewFeature : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Schema changes
        migrationBuilder.AddColumn<string>(
            name: "new_field",
            table: "players",
            type: "text",
            nullable: true);

        // Data migration if needed
        migrationBuilder.Sql(@"
            UPDATE players
            SET new_field = 'default_value'
            WHERE new_field IS NULL;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "new_field",
            table: "players");
    }
}
```

#### STEP 4 IMPACT:

### Migration Strategy Overview

For comprehensive guidance on handling database schema migrations when deploying entity definition changes to production, see: [`database-migration-strategy.md`](./database-migration-strategy.md)

This separate document covers:
- Risk-based migration categories (Low/Medium/High Risk)
- Production deployment workflows
- Rollback strategies and best practices
- EF Core migration implementation patterns
- Testing strategies for schema changes

### Zero Migration Risk Advantage

**🎯 HUGE ADVANTAGE**: Since there's no existing production data, we can:
- Design the perfect schema from scratch
- Use native PostgreSQL JSONB everywhere
- Optimize indexes for JSON queries
- Avoid complex migration scripts
- Focus on performance and scalability

### Schema Design Freedom

```sql
-- ✅ NEW: Native JSONB columns everywhere
CREATE TABLE players (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(255) NOT NULL,
    LastActive TIMESTAMP NOT NULL,
    IsArchived BOOLEAN NOT NULL DEFAULT FALSE,
    ArchivedAt TIMESTAMP NULL,
    TeamMemberships JSONB,  -- Native JSONB array
    Stats JSONB,           -- Native JSONB object
    Metadata JSONB,        -- Flexible JSONB data
    CreatedAt TIMESTAMP NOT NULL DEFAULT NOW(),
    UpdatedAt TIMESTAMP NOT NULL DEFAULT NOW()
);

-- JSONB indexes for performance
CREATE INDEX idx_players_team_memberships ON players USING GIN (TeamMemberships);
CREATE INDEX idx_players_stats ON players USING GIN (Stats);
CREATE INDEX idx_players_metadata ON players USING GIN (Metadata);
```

**This schema migration step establishes our PostgreSQL foundation with zero migration complexity!** 🎯
