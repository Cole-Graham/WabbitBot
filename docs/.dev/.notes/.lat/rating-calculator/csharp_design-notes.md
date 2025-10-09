I'll scan the C# entities you listed to see current properties and gaps against the Python simulation, then map concrete property tweaks.

[5 tools called]

I'll open the C# entity files for `Scrimmage`, `Team`, `ScrimmageTeamStats`, `TeamVarietyStats`, `TeamOpponentEncounter`, and `Match` to compare current properties with what the Python simulation requires, then map precise property tweaks.

[3 tools called]

I reviewed the simulator and the C# entities you listed. Below are focused tweaks so the C# model can capture all inputs/outputs used by the calculation and enable auditability without over-bloating storage.

Observations (current properties)
```csharp
// Navigation properties
public Guid ChallengeId { get; set; }
public Guid Team1Id { get; set; } // Challenger
public Guid Team2Id { get; set; } // Opponent
public List<Guid> Team1PlayerIds { get; set; } = new();
public List<Guid> Team2PlayerIds { get; set; } = new();
public Match? Match { get; set; }
public List<ScrimmageStateSnapshot> StateHistory { get; set; } = new();

// Data properties
public TeamSize TeamSize { get; set; }
public DateTime? StartedAt { get; set; }
public DateTime? CompletedAt { get; set; }
public Guid? WinnerId { get; set; }
public double Team1Rating { get; set; }
public double Team2Rating { get; set; }
public double Team1RatingChange { get; set; }
public double Team2RatingChange { get; set; }
public double Team1Confidence { get; set; } = 0.0; // Confidence at time of match
public double Team2Confidence { get; set; } = 0.0; // Confidence at time of match
public int Team1Score { get; set; } = 0; // Score at completion
public int Team2Score { get; set; } = 0; // Score at completion
public int BestOf { get; set; } = 1;
```

```csharp
public class Team : Entity, ITeamEntity
{
    // Navigation properties
    public Dictionary<TeamSize, ScrimmageTeamStats> ScrimmageTeamStats { get; set; } = new();
    public Dictionary<TeamSize, TournamentTeamStats> TournamentTeamStats { get; set; } = new();
    
    // Core team data
    public string Name { get; set; } = string.Empty;
    public Guid TeamCaptainId { get; set; }
    public TeamSize TeamSize { get; set; }
    public int MaxRosterSize { get; set; }
    public DateTime LastActive { get; set; }

    public string? Tag { get; set; }
    public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();

    // VarietyStats are for the scrimmage rating system
    public Dictionary<TeamSize, TeamVarietyStats> VarietyStats { get; set; } = new();
    public virtual ICollection<TeamOpponentEncounter> RecentScrimmageOpponents {
         get; set; } = new List<TeamOpponentEncounter>(); // Top 10 most recent
    public virtual ICollection<MatchParticipant> RecentScrimmageParticipations {
         get; set; } = new List<MatchParticipant>(); // Recent matches
```

```csharp
public class ScrimmageTeamStats : Entity, ITeamEntity
{
    // Team identification (for team stats)
    public Guid TeamId { get; set; }
    public TeamSize TeamSize { get; set; }

    // Basic stats
    public int Wins { get; set; }
    public int Losses { get; set; }

    // Rating system (using double for precision as per user preference)
    public double InitialRating { get; set; } = 1000.0;
    public double CurrentRating { get; set; } = 1000.0;
    public double HighestRating { get; set; } = 1000.0;

    // Streak tracking
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }

    // Timing
    public DateTime LastMatchAt { get; set; }
    public DateTime LastUpdated { get; set; }

    // Variety statistics (replaces OpponentDistributionScore)
    public ICollection<TeamVarietyStats> VarietyStats { get; set; } = new List<TeamVarietyStats>();

    // Navigation property
    public Team Team { get; set; } = null!;
```

```csharp
public class TeamVarietyStats : Entity, ITeamEntity
{
    public Guid TeamId { get; set; }
    public TeamSize TeamSize { get; set; }
    public double VarietyEntropy { get; set; } = 0.0;
    public double VarietyBonus { get; set; } = 0.0;
    public int TotalOpponents { get; set; } = 0;
    public int UniqueOpponents { get; set; } = 0;
    public DateTime LastCalculated { get; set; }
    public DateTime LastUpdated { get; set; }

    // Navigation property
    public Team Team { get; set; } = null!;
```

```csharp
public class TeamOpponentEncounter : Entity, IMatchEntity
{
    // Navigation properties
    public Guid MatchId { get; set; }
    public virtual Match Match { get; set; } = null!;

    public Guid TeamId { get; set; }
    public virtual Team Team { get; set; } = null!;

    public Guid OpponentId { get; set; }
    public virtual Team Opponent { get; set; } = null!;

    // State properties
    public int TeamSize { get; set; } // Using int instead of enum for DB compatibility
    public DateTime EncounteredAt { get; set; }
    public bool Won { get; set; }
```

```csharp
public class Match : Entity, IMatchEntity
{
    // Navigation properties
    public Guid Team1Id { get; set; }
    public Guid Team2Id { get; set; }
    public List<Guid> Team1PlayerIds { get; set; } = new();
    public List<Guid> Team2PlayerIds { get; set; } = new();
    public virtual Team Team1 { get; set; } = null!;
    public virtual Team Team2 { get; set; } = null!;
    public virtual ICollection<MatchParticipant> Participants { get; set; } = new List<MatchParticipant>();
    public virtual ICollection<TeamOpponentEncounter> OpponentEncounters { get; set; } = new List<TeamOpponentEncounter>();
    public ICollection<MatchStateSnapshot> StateHistory { get; set; } = new List<MatchStateSnapshot>();
    public virtual ICollection<Game> Games { get; set; } = new List<Game>();

    // Core match data
    public TeamSize TeamSize { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? WinnerId { get; set; }
    public Guid? ParentId { get; set; } // ID of parent Scrimmage, Tournament, or Casual match
    public MatchParentType? ParentType { get; set; } // Type of parent (Scrimmage, Tournament, or Casual)
    public int BestOf { get; set; } = 1; // Number of games to win the match
    public bool PlayToCompletion { get; set; } // Used for tournament matches.
```

Reasoning (mapping to simulator)
- Variety and multipliers: The calculator uses per-team variety bonus and per-team multiplier, gated by confidence, to compute rating deltas. Persisting these per scrimmage enables deterministic replays and audits.
- Gap scaling: A cosine scaling applies only to the higher-rated team’s change when the lower-rated team has max confidence, and it depends on the leaderboard rating range at the time. Persisting the range and the applied scaling value avoids expensive retroactive reconstruction.
- Confidence: At match time this is already stored on `Scrimmage`. Aggregate confidence can be recomputed from games played in `ScrimmageTeamStats` when needed.
- Variety computation context: Entropy is compared to an adjusted average, scaled by median games and opponent availability (neighbors). Storing a light “calculation context” with the derived factors explains how the final bonus was obtained without recomputing global stats.
- Opponent encounter: Encounter rows form the basis for per-opponent frequency counts used in entropy. Using `TeamSize` as an enum keeps parity with the rest of the model.

Recommendations (entity tweaks)
- Scrimmage
  - Add per-team calculation outputs:
    - double Team1VarietyBonusUsed, Team2VarietyBonusUsed
    - double Team1MultiplierUsed, Team2MultiplierUsed
  - Persist gap scaling context:
    - double RatingRangeAtMatch
    - Guid? HigherRatedTeamId
    - double GapScalingAppliedValue // 0.0–1.0; 1.0 means no reduction
  - Optional, if catch-up bonus is enabled for seasons:
    - double Team1CatchUpBonusUsed, Team2CatchUpBonusUsed
  - Optional, to reflect post-PP adjustments without overwriting originals:
    - double? Team1AdjustedRatingChange, Team2AdjustedRatingChange
    - DateTime? ProvenPotentialAppliedAt, bool ProvenPotentialApplied
  - Rationale: Mirrors simulator outputs: variety bonus, multiplier, gap scaling, optional catch-up, and optional PP adjustments. Enables replay and debugging of rating math per match.

- Team
  - No new fields required. `ScrimmageTeamStats` and `TeamVarietyStats` already hold per-slice data. Keep rating data out of `Team` to preserve slice boundaries.

- ScrimmageTeamStats
  - Optional convenience (computed today as Wins + Losses):
    - int GamesPlayed // denormalized for faster confidence checks
  - Rationale: Confidence threshold checks (reach 1.0 after N games) become a simple field read, reducing query complexity in hot paths.

- TeamVarietyStats
  - Keep existing core fields and add a compact “calculation context” to reflect the current simulator’s inputs:
    - double AverageVarietyEntropyAtCalc // adjusted global average used
    - double MedianGamesAtCalc
    - double RatingRangeAtCalc
    - double NeighborRangeAtCalc
    - int PlayerNeighborsAtCalc
    - int MaxNeighborsObservedAtCalc
    - double AvailabilityFactorUsed
  - Rationale: Simulator scales entropy by median games and an availability factor based on neighbors within a dynamic range tied to the current rating spread. Persisting these few numbers explains the resulting `VarietyBonus` without recomputing whole-population stats.

- TeamOpponentEncounter
  - Change type for consistency:
    - TeamSize TeamSize // replace int TeamSize
  - Rationale: The rest of the model uses the `TeamSize` enum; this aligns usage and avoids ad hoc casts. If EF mapping needs an int, configure enum-to-int mapping at the ORM layer rather than shaping the domain model to the database.

- Match
  - No change. Keep rating-specific fields off `Match` to avoid leaking scrimmage rating concerns into tournaments or casual modes.

Notes on what not to add
- Simulator-only fields such as `target_rating` and `activity_multiplier` are for synthetic data generation and should not be persisted.
- We do not recommend storing full per-opponent distribution snapshots or “subsequent afters” lists for Proven Potential; those can be derived on demand from encounters and scrimmage history, while `ProvenPotentialRecord` can continue to carry trigger and finalization metadata.

Status update
I reviewed the simulator logic and the C# entities you listed, and mapped missing calculation outputs and minimal context needed for deterministic auditing. Ready to implement these fields if you want me to proceed.

- Suggested additions to `Scrimmage` for variety bonus, multipliers, gap scaling, and optional PP/catch-up.
- Minimal context on `TeamVarietyStats` to explain entropy scaling.
- Align `TeamOpponentEncounter.TeamSize` to the `TeamSize` enum.
- Optional denormalized `GamesPlayed` on `ScrimmageTeamStats` for cheap confidence checks.