# Audit and Calculation Examples for Scrimmage Ratings

This guide shows how to populate the new entity fields during rating computation and how to use them later for audits without expensive recomputation.

## Populate snapshots during rating calculation

The calculator gathers inputs (ratings, confidences, variety bonuses), computes expected score, applies multipliers, and uses cosine gap scaling for the higher-rated team when applicable. Persist the exact inputs/outputs used.

```csharp
// src/WabbitBot.Core/Common/Models/Common/MatchCore.cs
public static class RatingApplication
{
    public static void ApplyScrimmageRating(
        Scrimmage scrimmage,
        ScrimmageTeamStats team1Stats,
        ScrimmageTeamStats team2Stats,
        double ratingRange,
        double team1VarietyBonus,
        double team2VarietyBonus,
        double team1CatchUpBonus = 0.0,
        double team2CatchUpBonus = 0.0
    )
    {
        // Snapshot inputs at calculation time
        scrimmage.Team1VarietyBonusUsed = team1VarietyBonus;
        scrimmage.Team2VarietyBonusUsed = team2VarietyBonus;
        scrimmage.RatingRangeAtMatch = ratingRange;

        // Confidence snapshots (immutable per scrimmage)
        scrimmage.Team1Confidence = team1Stats.Confidence;
        scrimmage.Team2Confidence = team2Stats.Confidence;

        // Expected score (ELO)
        var expectedTeam1 = 1.0 / (1.0 + Math.Pow(10.0, (scrimmage.Team2Rating - scrimmage.Team1Rating) / 400.0));
        var baseK = 40.0; // align with simulator config
        var baseDeltaTeam1 = baseK * (1.0 - expectedTeam1);

        // Multipliers: confidence-gated variety
        var team1Multiplier = Math.Min(2.0, (2.0 - scrimmage.Team1Confidence) + (scrimmage.Team1Confidence >= 1.0 ? team1VarietyBonus : 0.0));
        var team2Multiplier = Math.Min(2.0, (2.0 - scrimmage.Team2Confidence) - (scrimmage.Team2Confidence >= 1.0 ? team2VarietyBonus : 0.0));

        // Optional catch-up (additive after multiplier)
        if (team1CatchUpBonus > 0.0) team1Multiplier += team1CatchUpBonus;
        if (team2CatchUpBonus > 0.0) team2Multiplier += team2CatchUpBonus;

        // Gap scaling (cosine within 40% of leaderboard range / 2)
        var maxGap = ratingRange > 0.0 ? ratingRange * 0.4 / 2.0 : 0.0;
        var gap = Math.Abs(scrimmage.Team1Rating - scrimmage.Team2Rating);
        var gapScale = 1.0;
        Guid? higherRatedTeamId = null;

        if (ratingRange > 0.0 && gap <= maxGap)
        {
            var normalized = gap / maxGap;
            var cosine = (1.0 + Math.Cos(Math.PI * normalized * 0.7)) / 2.0;
            if (scrimmage.Team1Rating > scrimmage.Team2Rating && scrimmage.Team2Confidence >= 1.0)
            {
                gapScale = cosine;
                higherRatedTeamId = scrimmage.Team1Id;
            }
            else if (scrimmage.Team2Rating > scrimmage.Team1Rating && scrimmage.Team1Confidence >= 1.0)
            {
                gapScale = cosine;
                higherRatedTeamId = scrimmage.Team2Id;
            }
        }

        scrimmage.HigherRatedTeamId = higherRatedTeamId;
        scrimmage.GapScalingAppliedValue = gapScale;

        // Apply final deltas respecting which side is higher-rated
        var team1Change = baseDeltaTeam1 * team1Multiplier;
        var team2Change = -baseDeltaTeam1 * team2Multiplier;

        if (scrimmage.Team1Rating > scrimmage.Team2Rating)
        {
            // Higher-rated is Team1: scale the winner when Team1 wins, or the loser when Team1 loses
            team1Change *= gapScale;
        }
        else if (scrimmage.Team2Rating > scrimmage.Team1Rating)
        {
            team2Change *= gapScale;
        }

        scrimmage.Team1MultiplierUsed = team1Multiplier;
        scrimmage.Team2MultiplierUsed = team2Multiplier;
        scrimmage.Team1RatingChange = team1Change;
        scrimmage.Team2RatingChange = team2Change;
    }
}
```

## Recording variety calculation context

When (re)calculating a team’s variety, store compact context in `TeamVarietyStats` to explain the resulting bonus.

```csharp
// src/WabbitBot.Core/Common/Models/Common/TeamCore.cs
public static class VarietyComputation
{
    public static void UpdateVariety(
        TeamVarietyStats stats,
        double varietyEntropy,
        double varietyBonus,
        double averageEntropyAtCalc,
        double medianGamesAtCalc,
        double ratingRangeAtCalc,
        double neighborRangeAtCalc,
        int playerNeighborsAtCalc,
        int maxNeighborsObservedAtCalc,
        double availabilityFactorUsed
    )
    {
        stats.VarietyEntropy = varietyEntropy;
        stats.VarietyBonus = varietyBonus;
        stats.AverageVarietyEntropyAtCalc = averageEntropyAtCalc;
        stats.MedianGamesAtCalc = medianGamesAtCalc;
        stats.RatingRangeAtCalc = ratingRangeAtCalc;
        stats.NeighborRangeAtCalc = neighborRangeAtCalc;
        stats.PlayerNeighborsAtCalc = playerNeighborsAtCalc;
        stats.MaxNeighborsObservedAtCalc = maxNeighborsObservedAtCalc;
        stats.AvailabilityFactorUsed = availabilityFactorUsed;
        stats.LastCalculated = DateTime.UtcNow;
        stats.LastUpdated = DateTime.UtcNow;
    }
}
```

## Align encounter rows with enum TeamSize

Use the `TeamSize` enum across the model for consistency. Map to int at the EF layer if needed.

```csharp
// src/WabbitBot.Core/Common/Models/Common/Match.cs
public class TeamOpponentEncounter : Entity, IMatchEntity
{
    // ...
    public TeamSize TeamSize { get; set; }
    // ...
}
```

## Auditing a past scrimmage

Later, reconstruct exactly-what-happened without recomputing global distributions.

```csharp
// src/WabbitBot.Core/Common/Models/Scrimmage/ScrimmageAudit.cs
public static class ScrimmageAudit
{
    public static string Describe(Scrimmage s)
    {
        return $"Scrimmage {s.Id}:\n" +
               $"  Ratings: T1={s.Team1Rating:F1}, T2={s.Team2Rating:F1}\n" +
               $"  Confidence: T1={s.Team1Confidence:F2}, T2={s.Team2Confidence:F2}\n" +
               $"  VarietyUsed: T1={s.Team1VarietyBonusUsed:F2}, T2={s.Team2VarietyBonusUsed:F2}\n" +
               $"  MultiplierUsed: T1={s.Team1MultiplierUsed:F2}, T2={s.Team2MultiplierUsed:F2}\n" +
               $"  RatingRangeAtMatch={s.RatingRangeAtMatch:F1}, GapScale={s.GapScalingAppliedValue:F3}, HigherRated={s.HigherRatedTeamId}\n" +
               $"  Delta: T1={s.Team1RatingChange:+0.0;-0.0}, T2={s.Team2RatingChange:+0.0;-0.0}";
    }
}
```

This yields a deterministic narrative for post-hoc reviews and leaderboard dispute resolution.
