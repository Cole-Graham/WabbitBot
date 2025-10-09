# C# Entity Integration for Rating Calculator

This document specifies the minimal, high-signal additions to C# entities so the Scrimmage rating calculator (modeled on the Python simulator) can:
- Persist the exact inputs used at calculation time,
- Store key outputs for deterministic audits/replays,
- Explain variety bonus decisions without recomputing whole-population stats,
- Avoid leaking scrimmage concerns into unrelated slices (e.g., tournaments).

The examples below are additive edits. Remove or rename only if explicitly stated.

## Scrimmage

Add per-team calculation outputs and gap-scaling context captured at calculation time. Keep existing rating and confidence fields.

```csharp
// src/WabbitBot.Core/Common/Models/Scrimmage/Scrimmage.cs
public partial class Scrimmage : Entity, IScrimmageEntity
{
    // Per-team variety and multiplier actually used
    public double Team1VarietyBonusUsed { get; set; }
    public double Team2VarietyBonusUsed { get; set; }
    public double Team1MultiplierUsed { get; set; }
    public double Team2MultiplierUsed { get; set; }

    // Gap scaling context (cosine scaling applied to higher-rated side when applicable)
    public double RatingRangeAtMatch { get; set; }
    public Guid? HigherRatedTeamId { get; set; }
    public double GapScalingAppliedValue { get; set; } // 0.0–1.0, 1.0 means no reduction

    // Optional: Season feature toggle for catch-up bonus
    public double? Team1CatchUpBonusUsed { get; set; }
    public double? Team2CatchUpBonusUsed { get; set; }

    // Optional: Proven Potential (PP) batched adjustments, without overwriting original changes
    public double? Team1AdjustedRatingChange { get; set; }
    public double? Team2AdjustedRatingChange { get; set; }
    public bool? ProvenPotentialApplied { get; set; }
    public DateTime? ProvenPotentialAppliedAt { get; set; }
}
```

Notes:
- Keep `Team1Confidence`/`Team2Confidence` as snapshot values used by the calculation. `ScrimmageTeamStats.Confidence` is the live aggregate; this field on `Scrimmage` is the immutable snapshot.

## Team

No changes. Keep ratings and variety per-slice (see `ScrimmageTeamStats`, `TeamVarietyStats`).

## ScrimmageTeamStats

Confidence was added (good). No need for a denormalized `GamesPlayed` if confidence is authoritative for gating.

```csharp
// src/WabbitBot.Core/Common/Models/Common/Team.cs
public class ScrimmageTeamStats : Entity, ITeamEntity
{
    // ...existing properties...

    // Confidence for rating gating (0.0–1.0)
    public double Confidence { get; set; } = 0.0;
}
```

## TeamVarietyStats

Add compact calculation context to explain how `VarietyBonus` was derived without recomputing global stats.

```csharp
// src/WabbitBot.Core/Common/Models/Common/Team.cs
public class TeamVarietyStats : Entity, ITeamEntity
{
    // ...existing properties...

    // Calculation context
    public double AverageVarietyEntropyAtCalc { get; set; }
    public double MedianGamesAtCalc { get; set; }
    public double RatingRangeAtCalc { get; set; }
    public double NeighborRangeAtCalc { get; set; }
    public int PlayerNeighborsAtCalc { get; set; }
    public int MaxNeighborsObservedAtCalc { get; set; }
    public double AvailabilityFactorUsed { get; set; }
}
```

## TeamOpponentEncounter

Align `TeamSize` to the domain enum for consistency with other entities.

```csharp
// src/WabbitBot.Core/Common/Models/Common/Match.cs
public class TeamOpponentEncounter : Entity, IMatchEntity
{
    // ...existing properties...

    public TeamSize TeamSize { get; set; } // enum for consistency
}
```

If the ORM requires int, map the enum to int at the EF configuration layer instead of changing the domain model.

## Match

No changes. Keep rating-specific details out of `Match` to preserve slice boundaries.

## ProvenPotentialRecord

No structural changes required. Continue to use it for triggers and batch finalizations; optional adjusted fields on `Scrimmage` allow auditing without rewriting original deltas.

```csharp
// src/WabbitBot.Core/Common/Models/Scrimmage/Scrimmage.cs
public class ProvenPotentialRecord : Entity, IScrimmageEntity
{
    // unchanged; continue storing trigger metadata and batch finalization summaries
}
```

## Rationale summary
- Persisting per-scrimmage variety, multipliers, and gap scaling mirrors simulator outputs for reproducibility.
- Storing compact variety context avoids heavy global recomputation and explains bonuses.
- Enum alignment cleans up model consistency.
- Confidence snapshot on `Scrimmage` plus live `ScrimmageTeamStats.Confidence` covers both audit and current-state needs.
