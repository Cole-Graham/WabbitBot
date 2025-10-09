using System;
using System.Collections.Generic;
using System.Linq;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.Core.Common.Models.Common
{
    public partial class TeamCore
    {
        public class ScrimmageStats
        {
            public static double GetWinRate(ScrimmageTeamStats stats)
            {
                var totalMatches = stats.Wins + stats.Losses;
                return totalMatches == 0 ? 0 : (double)stats.Wins / totalMatches;
            }

            public static int GetTotalMatches(ScrimmageTeamStats stats)
            {
                return stats.Wins + stats.Losses;
            }

            public static double GetVarietyScore(TeamVarietyStats varietyStats)
            {
                if (varietyStats == null)
                    return 0.0;

                var entropyScore = varietyStats.VarietyEntropy;
                var bonusScore = varietyStats.VarietyBonus;

                return (entropyScore * 0.7) + (bonusScore * 0.3);
            }

            public static double GetEffectiveRating(ScrimmageTeamStats stats, TeamVarietyStats varietyStats)
            {
                var baseRating = stats.CurrentRating;
                var varietyScore = GetVarietyScore(varietyStats);
                var varietyBonus = varietyScore * 0.1 * baseRating;
                return baseRating + varietyBonus;
            }

            public static void UpdateVarietyStats(
                TeamVarietyStats varietyStats,
                List<TeamOpponentEncounter> recentEncounters
            )
            {
                if (!recentEncounters.Any())
                    return;

                var uniqueOpponents = recentEncounters.Select(e => e.OpponentId).Distinct().Count();
                var totalEncounters = recentEncounters.Count;

                var entropy = 0.0;
                var opponentGroups = recentEncounters.GroupBy(e => e.OpponentId);

                foreach (var group in opponentGroups)
                {
                    var probability = (double)group.Count() / totalEncounters;
                    entropy -= probability * Math.Log(probability);
                }

                var maxEntropy = Math.Log(uniqueOpponents);
                varietyStats.VarietyEntropy = maxEntropy == 0 ? 0 : entropy / maxEntropy;

                var uniqueBonus = Math.Min(uniqueOpponents * 0.1, 1.0);
                var repeatPenalty =
                    totalEncounters > uniqueOpponents ? (totalEncounters - uniqueOpponents) * 0.05 : 0.0;
                varietyStats.VarietyBonus = Math.Max(uniqueBonus - repeatPenalty, 0.0);

                varietyStats.TotalOpponents = totalEncounters;
                varietyStats.UniqueOpponents = uniqueOpponents;
                varietyStats.LastCalculated = DateTime.UtcNow;
            }
        }
    }
}
