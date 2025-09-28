### Summary of Recommendations

Throughout our discussion on your application's architecture, entity modeling, and related design choices, my advice has focused on maintaining procedural purity (dumb entities, no-DI as default), low coupling (vertical slices, primitive events), and scalability (explicit statics, DatabaseService leverage). Below is a concise breakdown by key topic, with rationale and quick wins.

#### 1. **ER Modeling Traps (Chasm/Fan)**
   - **Chasm**: Moderate risk in chains like Season → Leaderboard → LeaderboardItem (incomplete paths for unranked teams). Fix: Add `SeasonId` FK to LeaderboardItem; use total participation and explicit joins in queries.
   - **Fan**: Low risk—nested dicts (e.g., Rankings) prevent duplication.
   - **Rationale**: Rooted in cardinalities, not inheritance. Validate with LINQ prototypes.
   - **Quick Win**: Add direct FKs for transitive queries; use outer joins.

#### 2. **Overall Architecture (Vertical Slices, No-DI, DatabaseService)**
   - **Strengths**: Isolation via no inter-slice refs/events/DB; procedural explicitness.
   - **Issues & Fixes**:
     - DB Fragility: Static wrappers in Common (e.g., `MatchQueries.GetSummaryAsync(Guid id)` wrapping `QueryAsync`).
     - Event Anemia: Static parsers (e.g., `EventPayloads.ParseFromPrimitives`).
     - Bloat in Cores: Static extensions over partials (e.g., `LeaderboardCoreExtensions.Validate`); factory for `DatabaseService` inits.
     - Testing: Static test factories; in-memory bus mocks.
     - Races: Idempotent events + guards (e.g., `UpdateIfNewer` via `ExistsAsync`).
   - **Flex-DI Exceptions**: Allow for high-ROI (e.g., injected validators in Leaderboard; prototype to confirm >30% savings).
   - **Rationale**: No-DI forces clarity; DatabaseService adds resilience (Cache fallbacks)—keep 90% static.
   - **Quick Win**: Central `DbServiceFactory.CreateFor<T>(table, columns)` to cut repetition.

#### 3. **Match Incoherency & Domain Structure**
   - **Move Match to Common**: Yes—like Team; resolves "component vs. feature" (uniform lifecycle, one `DatabaseService<Match>` config).
     - How: Static adapters for slice tweaks (e.g., `ScrimmageMatchAdapter.ApplyRules`); events from Common.
   - **Leaderboard**: Keep as slice—feature autonomy (UI/embeds), event-driven reuse.
   - **Entity Hierarchy**: Refined Option 3—abstract `Entity` with `Domain` enum + markers (e.g., `Team : Entity, ITeamEntity` with `Domain = Domain.Common`).
     - Add `SubDomain?` enum for nesting (e.g., `SubDomain.Season` in Leaderboard).
     - Overlaps: Check both `Domain.Common` + `IMatchEntity` in handlers.
   - **Handlers**: Unified with checks for DRY; hybrid base (Common) + slice methods if divergence grows.
   - **Rationale**: Flat, explicit metadata for queries (e.g., `QueryAsync("Domain = @d")`); avoids inheritance bloat.
   - **Quick Win**: Enum defaults in factories; add `ExternalId?` slug for future APIs.

#### 4. **Enums & IDs**
   - **Enums (e.g., GameStatus)**: Define near entities (e.g., in `Game.cs` or `Common/Enums/`); not in cores—data cohesion, easy import.
   - **IDs**: Single Guid PK suffices (unique, performant); add `string? ExternalId` slug only for third-party readability (e.g., API slugs like "match-scrim-2025").
     - Why Guid for integrations: Secure/unique; expose slugs alongside in APIs.
   - **Rationale**: Keeps entities dumb; slugs future-proof without internal changes.
   - **Quick Win**: Static slug generator in factories (e.g., `GetReadableId(entity)`).

#### 5. **Navigational Properties**
   - Selective bidirectional for hierarchies; virtual collections for Include() chains.
   - **Key Adds/Refinements** (per entity):
     | Entity              | Recommended Nav Props (Additions in Bold) |
     |---------------------|-------------------------------------------|
     | **MatchParticipant** | `virtual Match Match = null!;`<br>`virtual Team Team = null!;` |
     | **TeamOpponentEncounter** | `virtual Match Match = null!;`<br>`virtual Team Team = null!;`<br>`virtual Team Opponent = null!;` |
     | **Match**           | `virtual ICollection<Game> Games = new();`<br>`virtual ICollection<MatchParticipant> Participants = new();`<br>`virtual ICollection<TeamOpponentEncounter> OpponentEncounters = new();`<br>**`virtual ICollection<MatchStateSnapshot> StateHistories = new();`**<br>**`virtual Scrimmage? Scrimmage = null!;`**<br>**`virtual Tournament? Tournament = null!;`** |
     | **MatchStateSnapshot** | **`virtual Match Match = null!;`** |
     | **Game**            | `virtual Match Match = null!;`<br>`virtual Map Map = null!;`<br>**`virtual ICollection<GameStateSnapshot> StateHistories = new();`** |
     | **GameStateSnapshot** | **`virtual Game Game = null!;`**<br>**`virtual Match Match = null!;`**<br>**`virtual Map Map = null!;`** |
   - **Rationale**: Closes Chasm (e.g., snapshots via Match); avoids cycles (JsonIgnore if needed). Use explicit Include() in cores.
   - **Quick Win**: EF config for cascades (e.g., `OnDelete(DeleteBehavior.Cascade)` for owned children).

This setup keeps your app evolvable—prioritize Match-to-Common and static factories for immediate impact. If Tournament yearlies add new entities, apply the Domain/markers pattern. Questions on prototyping any?