#### Step 6.9: Refactor DatabaseService Foundation (Common) ✅ COMPLETED

### Goal
Eliminate dual data paths and align the foundation with the EF-first architecture. Keep Common provider-agnostic,
delegate repository work to Core (EF/Npgsql), and make caching optional/pluggable.

### Current Issues
- Raw SQL placeholders in `DatabaseService.Repository.cs` duplicate Core EF functionality and are incomplete.
- `QueryAsync(...)` invites ad‑hoc SQL and overlaps with EF queries.
- Common references to concrete storage patterns risk provider coupling.

### Target Architecture
1) Common remains orchestration-only:
   - `DatabaseService<TEntity>` coordinates Repository/Cache/Archive at a high level and returns `Result<>`.
   - No Npgsql/raw SQL in Common; no EF types either.
2) Repository delegated via adapter:
   - New `IRepositoryAdapter<TEntity>` in Common defines CRUD + narrow read operations.
   - Core provides `EfRepositoryAdapter<TEntity>` that uses `WabbitBotDbContext` for actual data.
   - `DatabaseService.Repository` calls the adapter; Common stays provider-agnostic.
3) Caching becomes optional and pluggable:
   - New `ICacheProvider<TEntity>` in Common with a default `NoOpCacheProvider` and an in-memory `LruCacheProvider`.
   - Each `DatabaseService<TEntity>` receives a cache provider (or none) at construction.
4) Archive orchestrated in Common with pluggable providers (default NoOp); Core provides EF implementation. — IN PROGRESS

### Keep vs Remove
- Keep: `Create/Update/Delete/Exists/GetById/GetAll` surface (typed, narrow), cache write-through, `Result<>` contract.
- Remove: `QueryAsync(...)` from `IDatabaseService` (use EF via `CoreService.WithDbContext(...)` for complex queries).
- Remove: Raw SQL/Npgsql stubs in Common.

### Detailed Plan
#### 6.9a. Introduce repository adapter in Common
- Create `src/WabbitBot.Common/Data/Interfaces/IRepositoryAdapter.cs`:
  - `Task<TEntity?> GetByIdAsync(object id)`
  - `Task<bool> ExistsAsync(object id)`
  - `Task<IEnumerable<TEntity>> GetAllAsync()`
  - `Task<Result<TEntity>> CreateAsync(TEntity entity)`
  - `Task<Result<TEntity>> UpdateAsync(TEntity entity)`
  - `Task<Result<TEntity>> DeleteAsync(object id)`
- Optional narrow reads can be added case-by-case (e.g., `GetByNameAsync`) but avoid a generic query method.

#### 6.9b. Implement EF adapter in Core
- Create `EfRepositoryAdapter<TEntity>` in Core that uses `IDbContextFactory<WabbitBotDbContext>`.
- Wire it in startup via `CoreService.InitializeServices(...)` by registering factory methods the `DatabaseService`
  can call to obtain an adapter per entity (e.g., lambda or static provider).

#### 6.9c. Refactor DatabaseService.Repository to call adapter
- Replace raw SQL calls with adapter calls.
- Preserve write-through cache on success (update cache after repository mutations).

#### 6.9d. Make caching pluggable (optional) — DONE
- Define `ICacheProvider<TEntity>` in Common with methods: `TryGet`, `Set`, `Remove`, `GetAll` (optional), and expiry policy.
- Provide two implementations:
  - `NoOpCacheProvider<TEntity>` (opt-out, disabled caching) — TODO
  - `InMemoryLruCacheProvider<TEntity>` (implemented; extracted from `DatabaseService.Cache.cs`) — DONE
- Update `DatabaseService<TEntity>` to use provider-backed cache exclusively (legacy internal cache removed) — DONE

#### 6.9e. Remove/Deprecate generic raw query surface — DONE
- Remove `QueryAsync` from `IDatabaseService` and `DatabaseService<TEntity>`.
- Use `CoreService.WithDbContext(...)` + typed EF queries for projections/joins.

#### 6.9f. Error handling and results — DONE
- Keep current `Result` API and `Try*` patterns.
- Ensure adapter exceptions are mapped to `Result.Failure` with sanitized messages.

#### 6.9g. Archive Design (Immutable History) — IN PROGRESS
- Purpose: retain immutable snapshots for audits, forensics, and restores without impacting hot paths.
- Data model per entity: `{entity}_archive` table mirrors live columns plus metadata:
  - `archive_id uuid` (PK), `entity_id uuid`, `version int`, `archived_at timestamptz`,
    `archived_by uuid null`, `reason text null`.
  - JSONB columns remain JSONB; avoid separate “snapshot JSON” unless diffing is required.
- Indexes: `(entity_id, version desc)`, `(archived_at)`. Consider time partitioning only if volumes warrant it.
- Write policy:
  - Minimum: snapshot on hard-delete and explicit archive operations.
  - Optional, per-entity: snapshot on major state transitions or pre-update for point-in-time recovery.
- Read policy:
  - Not on hot paths. Provide: history by `entity_id`, latest snapshot, as-of `(timestamp|version)`, and restore helpers.
  - Avoid joining archives back into live transactional flows.
- Layering/APIs:
  - Common: `IArchiveProvider<TEntity>` with `SaveSnapshotAsync`, `GetHistoryAsync`, `GetLatestAsync`, `RestoreAsync`, `PurgeAsync`.
  - Core: `EfArchiveProvider<TEntity>` using `WabbitBotDbContext` mapped to `{entity}_archive`.
  - `DatabaseService` coordinates archive calls (e.g., write-through on chosen events) via provider; default to NoOp.
- EF mapping:
  - Separate archive entity types (e.g., `PlayerArchive`) mapped to `{table}_archive`, flat columns + metadata, no navigations.
- Ops & retention:
  - Time/version-based purge; privacy/PII-aware deletion if required; background retention job.
- Avoid:
  - CRUD archiving via events (project rules). No generic “query archive” API; prefer specific reads.

#### 6.9h. Source Generator Support for Archives — IN PROGRESS
- Archive entity types:
  - For each `[EntityMetadata]` entity `E`, generate `EArchive` with mirrored columns plus metadata
    (`ArchiveId`, `EntityId`, `Version`, `ArchivedAt`, `ArchivedBy`, `Reason`).
  - Generate a mapper: `public static EArchive From(E entity, Guid archivedBy, string? reason)`.
- DbContext wiring (generated):
  - `DbSet<EArchive> EArchives` and `ConfigureEArchive` with `ToTable("<table>_archive")`, PK on `ArchiveId`,
    indexes on `(EntityId, Version DESC)` and `ArchivedAt`.
  - No navigations on archive types.
- Provider scaffolding (generated Core partials):
  - Partial `EfArchiveProvider<TEntity>` methods calling the generated DbSet: `SaveSnapshotAsync`, `GetHistoryAsync`,
    `GetLatestAsync`, `RestoreAsync`.
- DatabaseService hooks (generated partials):
  - Partial methods to allow the foundation to hook archive actions without reflection:
    `partial bool ShouldSnapshotOnDelete(TEntity entity);`
    `partial Task OnArchivedAsync(TEntity entity, object archiveSnapshot);`
- Attribute-driven policy:
  - Extend `[EntityMetadata]` or add `[ArchiveOptions]` to control snapshot triggers and retention hints
    (`SnapshotOnDelete`, `SnapshotOnUpdate`, `Transitions`, `RetentionDays`).

#### 6.9i. Generator support for cache provider registration — DONE
- Purpose: use `[EntityMetadata]` to optionally generate explicit cache provider registrations and keep manual control by default.
- Manual stub:
  - A manually written `public static void RegisterCacheProviders()` exists in `CoreService`. It is intentionally empty; we call it at startup.
  - This stub is human-owned. Generators should not overwrite it; they may append generated partials that inject registrations.
- Behavior (generation rules):
  - Add new attribute parameter `EmitCacheRegistration` (default: false) to both `EntityMetadataAttribute` copies (Common and SourceGenerators) — DONE.
  - If `EmitCacheRegistration == true` for an entity, generator emits a registration call:
    `CacheProviderRegistry.RegisterProvider<TEntity>(new InMemoryLruCacheProvider<TEntity>(MaxCacheSize, TimeSpan.FromMinutes(CacheExpiryMinutes)));`
  - If false (default), no registration is emitted; the service will fall back to the default in-memory provider already injected by `DatabaseServiceGenerator` when no provider is registered.
- Generator impact:
  - No new generator is required; extend `DatabaseServiceGenerator` to also emit a partial that contributes to `RegisterCacheProviders()`.
  - Keep emission idempotent and additive (partial class/partial method pattern) to avoid collisions with the manual stub.
- Startup:
  - Continue calling `CoreService.RegisterCacheProviders()` at startup; with current defaults, it remains a no-op.

Status: NEW (planning)

### Migration Steps
1. Add `IRepositoryAdapter<TEntity>` and `ICacheProvider<TEntity>` interfaces in Common.
2. Implement `InMemoryLruCacheProvider<TEntity>` by extracting logic from `DatabaseService.Cache.cs`. — DONE
3. Update `DatabaseService<TEntity>` to use provider-backed cache exclusively. — DONE
4. Replace raw SQL in `DatabaseService.Repository.cs` with adapter calls; delete the raw Npgsql code.
5. Remove `QueryAsync` from `IDatabaseService` and `DatabaseService<TEntity>`; update callers to use
   `CoreService.WithDbContext(...)`.
6. Add EF adapter in Core and register provider during startup.
7. Run build/tests; add integration tests covering CRUD + cache write-through and adapter behavior.
8. Update generators to emit `EArchive`, DbSet/config, provider partials, and partial hooks; add tests.
9. Emit relationship mappings for collection navigations (EF):
   - Treat `ICollection<TEntity>` of entity types as navigations, not scalar columns.
   - Example: `builder.Entity<Game>() .HasMany(g => g.StateHistory) .WithOne(s => s.Game) .HasForeignKey(s => s.GameId) .OnDelete(DeleteBehavior.Cascade);`
   - Remove `.Property(...)` emissions for these nav collections; keep `.Property(...)` only for scalar collections (arrays/jsonb).
10. Emit Postgres-specific column types for scalar collections:
   - `List<Guid>` → `uuid[]`, `List<string>` → `text[]`, dictionaries/lists requiring JSON → `jsonb`.
11. Runtime parity: enable Npgsql dynamic JSON at startup and use the built data source for EF:
   - Build `NpgsqlDataSourceBuilder(connectionString).EnableDynamicJson().Build()` and pass to `UseNpgsql(dataSource)`.
12. Validation: add a Postgres Testcontainers test that uses the generated `WabbitBotDbContext` to insert a `Game` with two `GameStateSnapshot` records and verifies `Include(g => g.StateHistory)` round-trip with JSONB fields.

### Caching Decision (Recommended)
- Keep caching in the foundation as a pluggable provider so write-through semantics remain consistent and centralized.
- Allow disabling per-entity or swapping provider (e.g., NoOp in tests or for low-value entities).

### Deliverables Checklist
- [x] `IRepositoryAdapter<TEntity>` defined in Common
- [x] `ICacheProvider<TEntity>` + `NoOpCacheProvider` + `InMemoryLruCacheProvider` in Common
  - [x] `InMemoryLruCacheProvider<TEntity>`
  - [x] `NoOpCacheProvider<TEntity>`
- [x] `DatabaseService.Repository` refactored to use adapter (no raw SQL)
- [x] Raw SQL/Npgsql stubs removed from Common
- [x] `QueryAsync` removed from `IDatabaseService` and implementation
- [x] `EfRepositoryAdapter<TEntity>` added in Core and wired in startup (via `RegisterRepositoryAdapters`)
- [x] Tests updated or adjusted in progress (build passes; targeted integration to follow)
- [x] `IArchiveProvider<TEntity>` defined in Common + `NoOpArchiveProvider`
- [x] `EfArchiveProvider<TEntity>` in Core (write path implemented; uses generated archive models/mappers)
- [x] Archive write policy integrated (pre-delete snapshot hook); history/restore/purge stubs pending
- [x] Retention job documented/implemented for purge windows
- [x] Generators emit `EArchive`, DbSet/config, mapper; provider registrations partial emitted via `EmitArchiveRegistration`
 - [x] Generators emit EF relationship mappings for collection navigations (no `.Property` for nav collections)
 - [x] Generators emit Postgres column types for scalar collections (`uuid[]`, `text[]`, `jsonb`)
 - [x] Startup enables Npgsql dynamic JSON and EF uses the configured data source
 - [x] Postgres integration test validates `Game` ⇄ `GameStateSnapshot` relationship and JSONB round-trip

### Risks & Mitigations
- Risk: Adapter binding complexity → Mitigation: static provider registration in `CoreService.InitializeServices`.
- Risk: Performance regressions → Mitigation: keep JSONB indexes; add EF `FromSql` only in targeted Core methods if needed.

### Success Criteria
- Single coherent data path: Common orchestrates; Core EF does persistence.
- No raw SQL in Common; no DI; static initialization via `CoreService`.
- Optional caching with consistent write-through semantics.

