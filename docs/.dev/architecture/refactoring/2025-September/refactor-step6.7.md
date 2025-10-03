#### Step 6.7: Source Generators ⏳ IN PROGRESS

### Goal
Finalize database-related generators with full relationship and index coverage while keeping the generator-owned
`WabbitBotDbContext` model authoritative. Ensure complete `[EntityMetadata]` adoption across entities.

#### 6.7a. DbContext Generator: Foreign Key Relationships
- Generate `HasOne/WithMany` and `HasMany/WithOne` mappings based on detected FK + navigation pairs
- Emit `.HasForeignKey(...)` using scalar FK properties (e.g., `LeaderboardItem.LeaderboardId`)
- Support optional relationships and delete behaviors where inferred (Restrict by default)

#### 6.7b. Index Generation
- Emit standard indexes for frequently queried columns
- Emit GIN indexes for JSONB columns where beneficial
- Keep indexes in the same generated partial for visibility and atomic regeneration

#### 6.7c. Ownership Model
- Generator owns `WabbitBotDbContext`; manual code provides only a stub `partial` class to stabilize IDE symbol discovery
- Expose partial methods/hooks as needed for future override points without requiring manual EF configuration files

#### 6.7d. Entity Coverage
- Ensure 100% of entities are decorated with `[EntityMetadata]`
- Validate generator logs/diagnostics for missing metadata; fail build with clear diagnostics where required

### Deliverables
- Updated DbContext generator emitting relationships and indexes
- Validation diagnostics for missing/ambiguous metadata
- Checklist confirming all entities have `[EntityMetadata]`

