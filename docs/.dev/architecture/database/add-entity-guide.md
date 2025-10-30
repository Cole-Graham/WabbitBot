## Add an Entity (Option A - EntityMetadata-driven)

### Purpose
Single-source entity metadata on the entity class and let generators produce EF model, config factory, and
service glue. PostgreSQL-only. No runtime DI. CRUD is not event-driven.

### Prerequisites
- Entity class lives under the correct domain (e.g., `WabbitBot.Core/Common/Models/...`).
- Entity inherits from `Entity` and implements appropriate marker interface(s).
- PostgreSQL connection configured in `appsettings*.json` under `Bot:Database`.

### 1) Define/Update the entity class
Ensure the entity declares its domain and uses simple CLR types (JSONB supported for complex types).

```csharp
// Example: Common domain entity
public class Tournament : Entity, ITournamentEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public override Domain Domain => Domain.Tournament;
}
```

### 2) Decorate with [EntityMetadata]
Keep attributes minimal; generators infer columns, JSONB, and relationships.

```csharp
[EntityMetadata(
    TableName = "tournaments",
    ArchiveTableName = "tournament_archive",
    MaxCacheSize = 50,
    CacheExpiryMinutes = 30
)]
public class Tournament : Entity, ITournamentEntity { /* ... */ }
```

Notes:
- Columns are inferred from public properties and converted to snake_case.
- Complex types like List<T>, Dictionary<K,V> map to JSONB.

### 3) Build to generate code
Build the solution. Generators emit:
- EF Core context: `WabbitBotDbContext` DbSet and `ConfigureXxx` in obj/generated (generator-owned).
- Configs + factory: `EntityDbConfigs`, `EntityConfigFactory` (access via `EntityConfigFactory.Xxx`).
- DatabaseService accessors (where applicable) and supporting glue.

Troubleshooting:
- If the IDE cannot resolve generated symbols, ensure a minimal manual `partial` stub exists (e.g., for
  `EntityConfigFactory`). The generator remains the source of members.

### 4) Create or update migrations
Run a migration after adding or changing properties:
- Add migration: name it meaningfully (e.g., `AddTournamentBasics`).
- Review the SQL, ensure JSONB and indexes meet expectations.
- Apply to dev database.

#### Schema versioning (SchemaMetadata)
- WabbitBot tracks schema versions in a dedicated `schema_metadata` table. Use version strings like
  `SCHEMA-MAJOR.MINOR` (example: `001-1.0`).
- Each migration that changes the schema should write an entry in `Up` and remove it in `Down`.
- Sample insert in a migration:
```csharp
migrationBuilder.Sql(
    "INSERT INTO schema_metadata (schema_version, applied_at, applied_by, description, is_breaking_change, migration_name) " +
    "VALUES ('001-1.1', NOW(), 'EFCore', 'Add X', FALSE, 'AddX');"
);
```

#### Startup migration flags
- Configure per environment under `Bot:Database`:
```json
{
  "Bot": {
    "Database": {
      "RunMigrationsOnStartup": true,
      "UseEnsureCreated": false
    }
  }
}
```
- Development: set `RunMigrationsOnStartup=true`.
- Production: keep `RunMigrationsOnStartup=false` and apply migrations via a deployment step.

### 5) Use DatabaseService in Core logic
Access data via `DatabaseService<TEntity>`; no DI container.

```csharp
public class TournamentCore
{
    private readonly DatabaseService<Tournament> _tournamentData = new();

    public async Task<Result<Tournament>> CreateAsync(Tournament t)
    {
        return await _tournamentData.CreateAsync(t);
    }
}
```

### 6) Validate with tests
- Config tests: assert `EntityConfigFactory.Tournament` values (table names, cache, columns).
- EF integration: CRUD roundtrip including JSONB columns if present.
- Performance: ensure expected indexes exist and common queries are efficient.

### 7) Documentation alignment
- Ensure docs reference Option A (entity-attribute source of truth).
- Remove any guidance pointing to manual `.DbConfig.cs` files.

### Constraints and guardrails
- PostgreSQL + Npgsql only. Remove SQLite remnants.
- No runtime DI. Direct instantiation and event buses only.
- Events are not for CRUD; repositories/services handle persistence.

### Checklist
- Entity updated with correct Domain and marker interfaces.
- `[EntityMetadata]` applied with minimal required fields.
- Build succeeds; generated context and configs present.
- Migration created/applied; schema verified.
- DatabaseService-based access in Core logic.
- Tests updated: config assertions, CRUD/JSONB roundtrip.
- Docs reflect Option A and PostgreSQL-only policy.

### PostgreSQL temporal constraints and filtered indexes (example)
- For temporal membership models like `TeamMember (ValidFrom, ValidTo)`, add:
  - A filtered unique index to enforce a single active row per key:
```csharp
entity
    .HasIndex(e => new { e.TeamRosterId, e.PlayerId })
    .IsUnique()
    .HasFilter("valid_to IS NULL");
```
  - A PostgreSQL exclusion constraint to prevent overlapping periods (requires `btree_gist`):
```csharp
migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
migrationBuilder.Sql(@"ALTER TABLE team_members
ADD CONSTRAINT team_members_no_overlap
EXCLUDE USING gist (
  team_roster_id WITH =,
  player_id WITH =,
  tstzrange(valid_from, COALESCE(valid_to, 'infinity')) WITH &&
);");
```

### Migration runbook (quick reference)
#### Development
1. Update entity, build to generate code.
2. `dotnet ef migrations add MeaningfulName -p src/WabbitBot.Core -s src/WabbitBot.Host -o Migrations`
3. `dotnet ef database update -p src/WabbitBot.Core -s src/WabbitBot.Host`
4. Verify constraints and indexes; tests pass.

#### Production
1. Ensure appsettings.json has `Bot:Database:RunMigrationsOnStartup=false`.
2. Take a DB backup.
3. Apply migrations out-of-band (preferred):
   `dotnet ef database update -p src/WabbitBot.Core -s src/WabbitBot.Host`
4. Deploy app; startup validates schema via `SchemaVersionTracker` against `schema_metadata`.


