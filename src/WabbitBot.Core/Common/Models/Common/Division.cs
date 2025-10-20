using WabbitBot.Common.Attributes;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Models.Common;

#region Faction
/// <summary>
/// Represents the two main factions/alliances in the game.
/// BLUFOR (Blue Forces) and REDFOR (Red Forces) are generic military terms
/// that can represent any two opposing sides (e.g., NATO vs PACT, etc.).
/// </summary>
public enum Faction
{
    BLUFOR,
    REDFOR,
}
#endregion

#region RatingTier
/// <summary>
/// Percentile-based skill/rating tiers for filtering and analyzing division statistics.
/// Uses "sticky" percentile breakpoints that are calculated periodically (daily/weekly)
/// rather than in real-time to enable efficient caching and stable tier boundaries.
///
/// Percentiles represent the TOP X% of players (e.g., Top10 = top 10% of all players).
/// </summary>
public enum RatingTier
{
    /// <summary>All rating ranges (no filter).</summary>
    All = 0,

    /// <summary>Top 90% and above (bottom 10% excluded).</summary>
    Top90Plus = 1,

    /// <summary>Top 80% and above.</summary>
    Top80Plus = 2,

    /// <summary>Top 70% and above.</summary>
    Top70Plus = 3,

    /// <summary>Top 60% and above.</summary>
    Top60Plus = 4,

    /// <summary>Top 50% and above (median and higher).</summary>
    Top50Plus = 5,

    /// <summary>Top 40% and above.</summary>
    Top40Plus = 6,

    /// <summary>Top 30% and above.</summary>
    Top30Plus = 7,

    /// <summary>Top 20% and above.</summary>
    Top20Plus = 8,

    /// <summary>Top 10% and above (elite players).</summary>
    Top10Plus = 9,
}
#endregion

#region RatingPercentileBreakpoints
/// <summary>
/// Represents the calculated rating thresholds for each percentile tier.
/// These are "sticky" - calculated periodically rather than in real-time
/// to maintain cache stability while adapting to player population changes.
/// </summary>
[EntityMetadata(
    tableName: "rating_percentile_breakpoints",
    archiveTableName: "rating_percentile_breakpoint_archive",
    maxCacheSize: 50,
    cacheExpiryMinutes: 1440, // 24 hours
    servicePropertyName: "RatingPercentileBreakpoints",
    emitCacheRegistration: true,
    emitArchiveRegistration: false
)]
public class RatingPercentileBreakpoints : Entity, IDivisionEntity
{
    /// <summary>
    /// The team size these breakpoints apply to (percentiles differ by game mode).
    /// </summary>
    public TeamSize TeamSize { get; set; }

    /// <summary>
    /// Optional season these breakpoints apply to.
    /// Null means current/active season.
    /// </summary>
    public Guid? SeasonId { get; set; }

    /// <summary>
    /// When these percentile breakpoints were calculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When these breakpoints expire and should be recalculated.
    /// Typically set to 24-48 hours after calculation.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Total number of active players used to calculate these percentiles.
    /// Useful for validation and debugging.
    /// </summary>
    public int TotalPlayersInSample { get; set; }

    /// <summary>
    /// Dictionary mapping each RatingTier to its minimum rating threshold.
    /// Example: { Top10Plus: 1650.5, Top20Plus: 1420.3, ... }
    /// </summary>
    public Dictionary<RatingTier, double> Breakpoints { get; set; } = new();

    /// <summary>
    /// Whether these breakpoints are currently active/valid.
    /// Set to false when new breakpoints are calculated.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public override Domain Domain => Domain.Common;
}
#endregion

#region Division
/// <summary>
/// Represents a division (subfaction) that players can choose within a faction.
/// Divisions are defined in BotConfiguration and can be updated without code changes.
/// </summary>
[EntityMetadata(
    tableName: "divisions",
    archiveTableName: "division_archive",
    maxCacheSize: 100,
    cacheExpiryMinutes: 120,
    servicePropertyName: "Divisions",
    emitCacheRegistration: true,
    emitArchiveRegistration: true
)]
public class Division : Entity, IDivisionEntity
{
    /// <summary>
    /// The display name of the division.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The faction this division belongs to (BLUFOR or REDFOR).
    /// </summary>
    public Faction Faction { get; set; }

    /// <summary>
    /// Optional description of the division.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the division is currently active in the system.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The filename of the division's icon image.
    /// Expected to be manually uploaded to the division icons directory via FileSystemService.
    /// </summary>
    public string? IconFilename { get; set; }

    /// <summary>
    /// Foreign key collections for navigation properties.
    /// </summary>
    public ICollection<Guid> StatsIds { get; set; } = [];
    public ICollection<Guid> MapStatsIds { get; set; } = [];
    public ICollection<Guid> LearningCurveIds { get; set; } = [];

    /// <summary>
    /// Navigation property to division statistics.
    /// </summary>
    public virtual ICollection<DivisionStats> Stats { get; set; } = new List<DivisionStats>();

    /// <summary>
    /// Navigation property to per-map division statistics.
    /// </summary>
    public virtual ICollection<DivisionMapStats> MapStats { get; set; } = new List<DivisionMapStats>();

    /// <summary>
    /// Navigation property to learning curve tracking for this division.
    /// Uses mathematical curve fitting for precise analysis.
    /// </summary>
    public virtual ICollection<DivisionLearningCurve> LearningCurves { get; set; } = new List<DivisionLearningCurve>();

    public override Domain Domain => Domain.Common;
}
#endregion

#region DivisionStats
/// <summary>
/// Represents statistical data for a division, primarily tracking winrate statistics.
/// Statistics can be segmented by team size, time period, or other criteria.
/// </summary>
[EntityMetadata(
    tableName: "division_stats",
    archiveTableName: "division_stats_archive",
    maxCacheSize: 500,
    cacheExpiryMinutes: 60,
    servicePropertyName: "DivisionStats",
    emitCacheRegistration: true,
    emitArchiveRegistration: true
)]
public class DivisionStats : Entity, IDivisionEntity
{
    /// <summary>
    /// The division these statistics belong to.
    /// </summary>
    public Guid DivisionId { get; set; }
    public virtual Division Division { get; set; } = null!;

    /// <summary>
    /// Optional team size filter (if tracking stats per team size).
    /// Null means stats are aggregated across all team sizes.
    /// </summary>
    public TeamSize? TeamSize { get; set; }

    /// <summary>
    /// Optional season identifier for seasonal statistics.
    /// Null means all-time statistics.
    /// </summary>
    public Guid? SeasonId { get; set; }

    /// <summary>
    /// Total number of games played with this division.
    /// </summary>
    public int GamesPlayed { get; set; } = 0;

    /// <summary>
    /// Total number of games won with this division.
    /// </summary>
    public int GamesWon { get; set; } = 0;

    /// <summary>
    /// Total number of games lost with this division.
    /// </summary>
    public int GamesLost { get; set; } = 0;

    /// <summary>
    /// Calculated winrate (GamesWon / GamesPlayed).
    /// Stored for efficient querying.
    /// </summary>
    public double Winrate { get; set; } = 0.0;

    /// <summary>
    /// Average game duration in minutes for games played with this division.
    /// </summary>
    public double AverageGameDurationMinutes { get; set; } = 0.0;

    /// <summary>
    /// Total duration in minutes of all games played (for calculating average).
    /// </summary>
    public double TotalGameDurationMinutes { get; set; } = 0.0;

    /// <summary>
    /// Performance statistics broken down by map density (Low, Medium, High).
    /// Tracks games played, won, lost, and winrate for each density level.
    /// </summary>
    public Dictionary<MapDensity, DensityStats> DensityPerformance { get; set; } = new();

    /// <summary>
    /// Performance statistics broken down by game length buckets (5-minute increments).
    /// Tracks winrate vs game length and game length distribution.
    /// Key is bucket start minute (0, 5, 10, 15, 20, 25, 30, 35).
    /// </summary>
    public Dictionary<int, GameLengthBucket> GameLengthPerformance { get; set; } = new();

    /// <summary>
    /// Last time these statistics were updated.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata for future extensibility.
    /// </summary>
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();

    public override Domain Domain => Domain.Common;
}
#endregion

#region DensityStats
/// <summary>
/// Represents performance statistics for a division on a specific map density level.
/// This is a value object stored as JSONB within DivisionStats, not a separate entity.
/// It doesn't inherit from Entity because it has no independent lifecycle.
/// </summary>
public class DensityStats // Value object, doesn't inherit from Entity because it has no independent lifecycle.
{
    /// <summary>
    /// Map density level these stats apply to.
    /// </summary>
    public MapDensity Density { get; set; }

    /// <summary>
    /// Total games played on maps with this density.
    /// </summary>
    public int GamesPlayed { get; set; } = 0;

    /// <summary>
    /// Total games won on maps with this density.
    /// </summary>
    public int GamesWon { get; set; } = 0;

    /// <summary>
    /// Total games lost on maps with this density.
    /// </summary>
    public int GamesLost { get; set; } = 0;

    /// <summary>
    /// Calculated winrate for this density (GamesWon / GamesPlayed).
    /// </summary>
    public double Winrate { get; set; } = 0.0;

    /// <summary>
    /// Average game duration in minutes on this density.
    /// </summary>
    public double AverageGameDurationMinutes { get; set; } = 0.0;
}
#endregion

#region GameLengthBucket
/// <summary>
/// Represents performance statistics for a specific game length bucket.
/// Used for tracking "Win Rate vs Game Length" and "Game Length Distribution" graphs.
/// This is a value object stored as JSONB within DivisionStats and DivisionMapStats.
/// </summary>
public class GameLengthBucket // Value object, doesn't inherit from Entity because it has no independent lifecycle.
{
    /// <summary>
    /// Minimum duration in minutes for this bucket (inclusive).
    /// E.g., 0, 5, 10, 15, 20, 25, 30, 35
    /// </summary>
    public int MinMinutes { get; set; }

    /// <summary>
    /// Maximum duration in minutes for this bucket (exclusive).
    /// E.g., 5, 10, 15, 20, 25, 30, 35, 40
    /// </summary>
    public int MaxMinutes { get; set; }

    /// <summary>
    /// Total number of games that fell into this duration bucket.
    /// </summary>
    public int GamesPlayed { get; set; } = 0;

    /// <summary>
    /// Number of games won in this duration bucket.
    /// </summary>
    public int GamesWon { get; set; } = 0;

    /// <summary>
    /// Number of games lost in this duration bucket.
    /// </summary>
    public int GamesLost { get; set; } = 0;

    /// <summary>
    /// Calculated winrate for games in this duration bucket (GamesWon / GamesPlayed).
    /// Used for "Win Rate vs Game Length" graph (Y-axis).
    /// </summary>
    public double Winrate { get; set; } = 0.0;

    /// <summary>
    /// Percentage of total games that fall into this bucket.
    /// Used for "Game Length Distribution" graph (Y-axis).
    /// Calculated as: (GamesPlayed in bucket / Total games across all buckets) * 100
    /// </summary>
    public double PercentageOfTotal { get; set; } = 0.0;
}
#endregion

#region GameLengthBuckets
/// <summary>
/// Utility class for managing game length buckets and bucket calculations.
/// Defines standard bucket sizes and provides helper methods for bucketing game durations.
/// </summary>
public static class GameLengthBuckets // Value object, doesn't inherit from Entity because it has no independent lifecycle.
{
    /// <summary>
    /// Size of each bucket in minutes.
    /// </summary>
    public const int BucketSizeMinutes = 5;

    /// <summary>
    /// Maximum game length to track in minutes.
    /// Games longer than this are capped to the last bucket.
    /// </summary>
    public const int MaxGameLengthMinutes = 40;

    /// <summary>
    /// Gets all bucket start points (0, 5, 10, 15, 20, 25, 30, 35).
    /// </summary>
    public static IEnumerable<int> AllBuckets =>
        Enumerable.Range(0, MaxGameLengthMinutes / BucketSizeMinutes).Select(i => i * BucketSizeMinutes);

    /// <summary>
    /// Determines which bucket a game duration falls into.
    /// Returns the bucket start minute (0, 5, 10, 15, 20, 25, 30, or 35).
    /// </summary>
    /// <param name="gameDurationMinutes">The game duration in minutes.</param>
    /// <returns>The bucket start minute (0-35).</returns>
    public static int GetBucketKey(double gameDurationMinutes)
    {
        // Floor to nearest bucket
        int bucket = ((int)gameDurationMinutes / BucketSizeMinutes) * BucketSizeMinutes;

        // Cap at last bucket (35 for 35-40 minute range)
        return Math.Min(bucket, MaxGameLengthMinutes - BucketSizeMinutes);
    }

    /// <summary>
    /// Creates an empty bucket with proper min/max values.
    /// </summary>
    /// <param name="bucketStart">The bucket start minute (0, 5, 10, etc.).</param>
    public static GameLengthBucket CreateEmptyBucket(int bucketStart)
    {
        return new GameLengthBucket { MinMinutes = bucketStart, MaxMinutes = bucketStart + BucketSizeMinutes };
    }

    /// <summary>
    /// Recalculates PercentageOfTotal for all buckets based on total games.
    /// Should be called after updating bucket statistics.
    /// </summary>
    public static void RecalculatePercentages(Dictionary<int, GameLengthBucket> buckets)
    {
        int totalGames = buckets.Values.Sum(b => b.GamesPlayed);
        if (totalGames == 0)
            return;

        foreach (var bucket in buckets.Values)
        {
            bucket.PercentageOfTotal = (bucket.GamesPlayed / (double)totalGames) * 100.0;
        }
    }

    /// <summary>
    /// Gets a human-readable label for a bucket (e.g., "0-5 min", "15-20 min").
    /// </summary>
    public static string GetBucketLabel(int bucketStart)
    {
        return $"{bucketStart}-{bucketStart + BucketSizeMinutes} min";
    }
}
#endregion

#region DivisionMapStats
/// <summary>
/// Represents performance statistics for a division on a specific map.
/// Allows detailed tracking of per-map performance for divisions.
/// </summary>
[EntityMetadata(
    tableName: "division_map_stats",
    archiveTableName: "division_map_stats_archive",
    maxCacheSize: 1000,
    cacheExpiryMinutes: 60,
    servicePropertyName: "DivisionMapStats",
    emitCacheRegistration: true,
    emitArchiveRegistration: true
)]
public class DivisionMapStats : Entity, IDivisionEntity
{
    /// <summary>
    /// The division these statistics belong to.
    /// </summary>
    public Guid DivisionId { get; set; }
    public virtual Division Division { get; set; } = null!;

    /// <summary>
    /// The map these statistics apply to.
    /// </summary>
    public Guid MapId { get; set; }
    public virtual Map Map { get; set; } = null!;

    /// <summary>
    /// Optional team size filter (if tracking stats per team size).
    /// Null means stats are aggregated across all team sizes.
    /// </summary>
    public TeamSize? TeamSize { get; set; }

    /// <summary>
    /// Optional season identifier for seasonal statistics.
    /// Null means all-time statistics.
    /// </summary>
    public Guid? SeasonId { get; set; }

    /// <summary>
    /// Total number of games played with this division on this map.
    /// </summary>
    public int GamesPlayed { get; set; } = 0;

    /// <summary>
    /// Total number of games won with this division on this map.
    /// </summary>
    public int GamesWon { get; set; } = 0;

    /// <summary>
    /// Total number of games lost with this division on this map.
    /// </summary>
    public int GamesLost { get; set; } = 0;

    /// <summary>
    /// Calculated winrate on this map (GamesWon / GamesPlayed).
    /// </summary>
    public double Winrate { get; set; } = 0.0;

    /// <summary>
    /// Average game duration in minutes on this map.
    /// </summary>
    public double AverageGameDurationMinutes { get; set; } = 0.0;

    /// <summary>
    /// Total duration in minutes of all games on this map (for calculating average).
    /// </summary>
    public double TotalGameDurationMinutes { get; set; } = 0.0;

    /// <summary>
    /// Performance statistics broken down by game length buckets (5-minute increments).
    /// Tracks winrate vs game length and game length distribution for this specific map.
    /// Key is bucket start minute (0, 5, 10, 15, 20, 25, 30, 35).
    /// </summary>
    public Dictionary<int, GameLengthBucket> GameLengthPerformance { get; set; } = new();

    /// <summary>
    /// Last time these statistics were updated.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    public override Domain Domain => Domain.Common;
}
#endregion

#region CurveModel
/// <summary>
/// Mathematical models for fitting learning/skill curves.
/// Each model represents a different pattern of skill acquisition over time.
/// </summary>
public enum CurveModel
{
    /// <summary>
    /// Linear model: y = a + b*x
    /// Constant learning rate. Used as fallback when data is insufficient.
    /// </summary>
    Linear,

    /// <summary>
    /// Logarithmic model: y = a + b*ln(x)
    /// Fast initial learning that slows over time. Common in skill acquisition.
    /// Good for divisions with early mastery.
    /// </summary>
    Logarithmic,

    /// <summary>
    /// Power law model: y = a * x^b
    /// Similar to logarithmic but more flexible. Classic learning curve model.
    /// Good for divisions with consistent improvement patterns.
    /// </summary>
    PowerLaw,

    /// <summary>
    /// Exponential approach model: y = ceiling - (ceiling - initial) * e^(-k*x)
    /// Asymptotically approaches a skill ceiling. Best for high skill-cap divisions.
    /// Shows initial struggle followed by rapid improvement toward mastery.
    /// </summary>
    ExponentialApproach,
}
#endregion

#region DivisionLearningCurve
/// <summary>
/// Tracks how a team's performance with a specific division evolves over time
/// using mathematical curve fitting rather than discrete milestones.
///
/// Stores regression parameters that allow precise winrate calculation at any game count.
/// High skill ceiling divisions show steep curves with high ceilings,
/// while easy-to-use divisions show flat curves near their starting performance.
///
/// NOTE: Currently only tracked for 1v1 games to avoid complexity of team coordination effects.
/// </summary>
[EntityMetadata(
    tableName: "division_learning_curves",
    archiveTableName: "division_learning_curve_archive",
    maxCacheSize: 500,
    cacheExpiryMinutes: 120,
    servicePropertyName: "DivisionLearningCurves",
    emitCacheRegistration: true,
    emitArchiveRegistration: true
)]
public class DivisionLearningCurve : Entity, IDivisionEntity
{
    /// <summary>
    /// The team whose progression is being tracked.
    /// </summary>
    public Guid TeamId { get; set; }
    public virtual Team Team { get; set; } = null!;

    /// <summary>
    /// The division being tracked.
    /// </summary>
    public Guid DivisionId { get; set; }
    public virtual Division Division { get; set; } = null!;

    /// <summary>
    /// Optional season identifier for seasonal learning curves.
    /// Null means all-time progression tracking.
    /// </summary>
    public Guid? SeasonId { get; set; }

    /// <summary>
    /// The mathematical model used to fit this curve.
    /// Automatically selected based on best fit (highest R²).
    /// </summary>
    public CurveModel ModelType { get; set; } = CurveModel.Linear;

    /// <summary>
    /// Model parameters as key-value pairs.
    /// Contents depend on ModelType:
    /// - Linear: { "a": intercept, "b": slope }
    /// - Logarithmic: { "a": base, "b": coefficient }
    /// - PowerLaw: { "a": coefficient, "b": exponent }
    /// - ExponentialApproach: { "ceiling": max_winrate, "initial": start_winrate, "k": rate_constant }
    /// </summary>
    public Dictionary<string, double> Parameters { get; set; } = new();

    /// <summary>
    /// R² (coefficient of determination) indicating goodness of fit.
    /// Range: 0.0 to 1.0, where 1.0 is perfect fit.
    /// Values below 0.5 may indicate insufficient data or poor model selection.
    /// </summary>
    public double RSquared { get; set; } = 0.0;

    /// <summary>
    /// Total number of games this team has played with this division.
    /// Used for validation and minimum data requirements.
    /// </summary>
    public int TotalGamesPlayed { get; set; } = 0;

    /// <summary>
    /// Total number of games won (used for curve fitting).
    /// </summary>
    public int TotalGamesWon { get; set; } = 0;

    /// <summary>
    /// Last time this curve was recalculated.
    /// Curves should be periodically refitted as new data arrives.
    /// </summary>
    public DateTime LastRecalculated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this curve has enough data to be considered reliable.
    /// Typically requires at least 10-20 games for meaningful fitting.
    /// </summary>
    public bool IsReliable { get; set; } = false;

    public override Domain Domain => Domain.Common;
}
#endregion

#region LearningCurveHelpers
/// <summary>
/// Helper methods for working with learning curves, including curve fitting and evaluation.
/// </summary>
public static class LearningCurveHelpers
{
    /// <summary>
    /// Minimum games required for reliable curve fitting.
    /// Below this threshold, use simple linear approximation.
    /// </summary>
    public const int MinGamesForReliableFit = 15;

    /// <summary>
    /// Evaluates a learning curve model at a specific game count.
    /// Returns the predicted winrate based on the fitted curve.
    /// </summary>
    /// <param name="curve">The learning curve to evaluate</param>
    /// <param name="gameCount">The game count to predict winrate for</param>
    /// <returns>Predicted winrate (0.0 to 1.0), or null if curve is invalid</returns>
    public static double? EvaluateCurve(DivisionLearningCurve curve, int gameCount)
    {
        if (gameCount <= 0 || curve.Parameters is null || curve.Parameters.Count == 0)
            return null;

        try
        {
            return curve.ModelType switch
            {
                CurveModel.Linear => EvaluateLinear(curve.Parameters, gameCount),
                CurveModel.Logarithmic => EvaluateLogarithmic(curve.Parameters, gameCount),
                CurveModel.PowerLaw => EvaluatePowerLaw(curve.Parameters, gameCount),
                CurveModel.ExponentialApproach => EvaluateExponentialApproach(curve.Parameters, gameCount),
                _ => null,
            };
        }
        catch
        {
            return null;
        }
    }

    private static double EvaluateLinear(Dictionary<string, double> p, int x) => p["a"] + p["b"] * x;

    private static double EvaluateLogarithmic(Dictionary<string, double> p, int x) => p["a"] + p["b"] * Math.Log(x);

    private static double EvaluatePowerLaw(Dictionary<string, double> p, int x) => p["a"] * Math.Pow(x, p["b"]);

    private static double EvaluateExponentialApproach(Dictionary<string, double> p, int x) =>
        p["ceiling"] - (p["ceiling"] - p["initial"]) * Math.Exp(-p["k"] * x);

    /// <summary>
    /// Calculates the learning rate (derivative) at a specific game count.
    /// Higher values indicate faster improvement at that point in the curve.
    /// </summary>
    public static double? GetLearningRate(DivisionLearningCurve curve, int gameCount)
    {
        if (gameCount <= 0 || curve.Parameters is null)
            return null;

        var p = curve.Parameters;
        try
        {
            return curve.ModelType switch
            {
                CurveModel.Linear => p["b"],
                CurveModel.Logarithmic => p["b"] / gameCount,
                CurveModel.PowerLaw => p["a"] * p["b"] * Math.Pow(gameCount, p["b"] - 1),
                CurveModel.ExponentialApproach => (p["ceiling"] - p["initial"])
                    * p["k"]
                    * Math.Exp(-p["k"] * gameCount),
                _ => null,
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Estimates the skill ceiling (asymptotic maximum winrate) for the division.
    /// </summary>
    public static double? GetSkillCeiling(DivisionLearningCurve curve)
    {
        if (curve.Parameters is null)
            return null;

        var p = curve.Parameters;
        return curve.ModelType switch
        {
            CurveModel.ExponentialApproach => p.GetValueOrDefault("ceiling"),
            CurveModel.Linear => null, // No ceiling in linear model
            CurveModel.Logarithmic => null, // Theoretically unbounded
            CurveModel.PowerLaw => null, // Depends on exponent sign
            _ => null,
        };
    }
}
#endregion
