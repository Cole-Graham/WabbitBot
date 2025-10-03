namespace WabbitBot.Core.Common.Interfaces
{
    /// <summary>
    /// Interface for division-related core operations including statistics tracking,
    /// skill curve management, and performance analysis.
    /// </summary>
    public interface IDivisionCore : ICore
    {
        /// <summary>
        /// Updates statistics for a division after a game is completed.
        /// </summary>
        Task UpdateDivisionStatsAsync(Guid divisionId, Guid teamId, bool won, double gameDurationMinutes, Guid mapId, Guid? seasonId = null);

        /// <summary>
        /// Updates the learning curve for a team's progression with a division.
        /// Refits the mathematical model with new game data.
        /// Only tracked for 1v1 games.
        /// </summary>
        Task UpdateLearningCurveAsync(Guid teamId, Guid divisionId, bool won, double gameDurationMinutes, Guid? seasonId = null);

        /// <summary>
        /// Gets the current learning curve data for a team with a specific division.
        /// Returns the fitted curve parameters and model type.
        /// </summary>
        Task<Models.Common.DivisionLearningCurve?> GetLearningCurveAsync(Guid teamId, Guid divisionId, Guid? seasonId = null);

        /// <summary>
        /// Predicts winrate at a specific game count using the fitted learning curve.
        /// </summary>
        Task<double?> PredictWinrateAsync(Guid teamId, Guid divisionId, int gameCount, Guid? seasonId = null);

        /// <summary>
        /// Gets performance statistics for a division.
        /// </summary>
        /// <param name="divisionId">The division to query.</param>
        /// <param name="teamSize">Optional team size filter (1v1, 2v2, etc.).</param>
        /// <param name="seasonId">Optional season filter.</param>
        /// <param name="ratingTier">Optional rating tier filter (Novice, Expert, etc.). Defaults to All.</param>
        Task<Models.Common.DivisionStats?> GetDivisionStatsAsync(Guid divisionId, Models.Common.TeamSize? teamSize = null, Guid? seasonId = null, Models.Common.RatingTier ratingTier = Models.Common.RatingTier.All);

        /// <summary>
        /// Gets per-map performance statistics for a division.
        /// </summary>
        /// <param name="divisionId">The division to query.</param>
        /// <param name="mapId">The map to query.</param>
        /// <param name="teamSize">Optional team size filter.</param>
        /// <param name="seasonId">Optional season filter.</param>
        /// <param name="ratingTier">Optional rating tier filter. Defaults to All.</param>
        Task<Models.Common.DivisionMapStats?> GetDivisionMapStatsAsync(Guid divisionId, Guid mapId, Models.Common.TeamSize? teamSize = null, Guid? seasonId = null, Models.Common.RatingTier ratingTier = Models.Common.RatingTier.All);

        /// <summary>
        /// Gets learning curve data for teams using a specific division.
        /// Returns fitted curve parameters for analysis and visualization.
        /// </summary>
        /// <param name="divisionId">The division to query.</param>
        /// <param name="seasonId">Optional season filter.</param>
        /// <param name="ratingTier">Optional rating tier filter. Defaults to All.</param>
        Task<IEnumerable<Models.Common.DivisionLearningCurve>> GetLearningCurvesAsync(Guid divisionId, Guid? seasonId = null, Models.Common.RatingTier ratingTier = Models.Common.RatingTier.All);

        /// <summary>
        /// Gets the top performing divisions, optionally filtered by rating tier.
        /// Returns divisions ranked by winrate.
        /// </summary>
        /// <param name="teamSize">Team size to filter by.</param>
        /// <param name="seasonId">Optional season filter.</param>
        /// <param name="ratingTier">Rating tier to analyze. Defaults to All.</param>
        /// <param name="limit">Maximum number of results to return.</param>
        Task<IEnumerable<Models.Common.DivisionPerformanceSummary>> GetTopDivisionsAsync(Models.Common.TeamSize teamSize, Guid? seasonId = null, Models.Common.RatingTier ratingTier = Models.Common.RatingTier.All, int limit = 10);

        /// <summary>
        /// Calculates and stores new percentile breakpoints for a given team size and season.
        /// Should be called periodically (daily/weekly) to keep percentile tiers current
        /// while maintaining cache stability.
        /// </summary>
        /// <param name="teamSize">The team size to calculate breakpoints for.</param>
        /// <param name="seasonId">Optional season filter.</param>
        /// <param name="expiryHours">How long until these breakpoints expire (default: 24 hours).</param>
        Task<Models.Common.RatingPercentileBreakpoints> CalculatePercentileBreakpointsAsync(Models.Common.TeamSize teamSize, Guid? seasonId = null, int expiryHours = 24);

        /// <summary>
        /// Gets the current active percentile breakpoints for a team size and season.
        /// Returns cached breakpoints if still valid, otherwise triggers recalculation.
        /// </summary>
        Task<Models.Common.RatingPercentileBreakpoints?> GetActivePercentileBreakpointsAsync(Models.Common.TeamSize teamSize, Guid? seasonId = null);
    }
}

