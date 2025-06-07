using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Matches;
using WabbitBot.Core.Common.BotCore;

namespace WabbitBot.Core.Scrimmages
{
    public class Scrimmage : BaseEntity
    {
        public string Team1Id { get; set; } = string.Empty;
        public string Team2Id { get; set; } = string.Empty;
        public List<string> Team1RosterIds { get; set; } = new();
        public List<string> Team2RosterIds { get; set; } = new();
        public GameSize GameSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? WinnerId { get; set; }
        public ScrimmageStatus Status { get; set; }
        public int Team1Rating { get; set; }
        public int Team2Rating { get; set; }
        public int RatingChange { get; set; }
        public double RatingMultiplier { get; set; } = 1.0;
        public DateTime? ChallengeExpiresAt { get; set; }
        public bool IsAccepted { get; set; }
        public Match? Match { get; private set; }
        public int BestOf { get; set; } = 1;

        public Scrimmage()
        {
            CreatedAt = DateTime.UtcNow;
            Status = ScrimmageStatus.Created;
            ChallengeExpiresAt = DateTime.UtcNow.AddHours(24); // 24 hour challenge window
        }

        public bool IsTeamMatch => GameSize != GameSize.OneVOne;

        public static Scrimmage Create(string team1Id, string team2Id, GameSize gameSize, int bestOf = 1)
        {
            var scrimmage = new Scrimmage
            {
                Team1Id = team1Id,
                Team2Id = team2Id,
                GameSize = gameSize,
                Status = ScrimmageStatus.Created,
                CreatedAt = DateTime.UtcNow,
                BestOf = bestOf
            };

            // Event will be published by source generator
            return scrimmage;
        }

        public void Accept()
        {
            if (Status != ScrimmageStatus.Created)
                throw new InvalidOperationException("Scrimmage can only be accepted when in Created state");

            if (DateTime.UtcNow > ChallengeExpiresAt)
                throw new InvalidOperationException("Challenge has expired");

            IsAccepted = true;
            Status = ScrimmageStatus.Accepted;

            // Create the match
            Match = Match.Create(Team1Id, Team2Id, GameSize, Id.ToString(), "Scrimmage", BestOf);

            // Event will be published by source generator
        }

        public void Decline()
        {
            if (Status != ScrimmageStatus.Created)
                throw new InvalidOperationException("Scrimmage can only be declined when in Created state");

            if (DateTime.UtcNow > ChallengeExpiresAt)
                throw new InvalidOperationException("Challenge has expired");

            Status = ScrimmageStatus.Declined;

            // Event will be published by source generator
        }

        public void Start()
        {
            if (Status != ScrimmageStatus.Accepted)
                throw new InvalidOperationException("Scrimmage can only be started when in Accepted state");

            if (Match == null)
                throw new InvalidOperationException("Match must be created before starting scrimmage");

            StartedAt = DateTime.UtcNow;
            Status = ScrimmageStatus.InProgress;
            Match.Start();

            // Event will be published by source generator
        }

        public void Complete(string winnerId, int team1Rating, int team2Rating, int ratingChange)
        {
            if (Status != ScrimmageStatus.InProgress)
                throw new InvalidOperationException("Scrimmage can only be completed when in Progress state");

            if (Match == null)
                throw new InvalidOperationException("Match must exist to complete scrimmage");

            if (winnerId != Team1Id && winnerId != Team2Id)
                throw new ArgumentException("Winner must be one of the participating teams");

            CompletedAt = DateTime.UtcNow;
            WinnerId = winnerId;
            Status = ScrimmageStatus.Completed;
            Team1Rating = team1Rating;
            Team2Rating = team2Rating;
            RatingChange = ratingChange;

            Match.Complete(winnerId);

            // Event will be published by source generator
        }

        public void Cancel(string reason, string cancelledBy)
        {
            if (Status == ScrimmageStatus.Completed)
                throw new InvalidOperationException("Cannot cancel a completed scrimmage");

            Status = ScrimmageStatus.Cancelled;

            if (Match != null)
            {
                Match.Cancel(reason, cancelledBy);
            }

            // Event will be published by source generator
        }

        public void Forfeit(string forfeitedTeamId, string reason)
        {
            if (Status != ScrimmageStatus.InProgress)
                throw new InvalidOperationException("Scrimmage can only be forfeited when in Progress state");

            if (Match == null)
                throw new InvalidOperationException("Match must exist to forfeit scrimmage");

            if (forfeitedTeamId != Team1Id && forfeitedTeamId != Team2Id)
                throw new ArgumentException("Forfeited team must be one of the participating teams");

            Status = ScrimmageStatus.Forfeited;
            WinnerId = forfeitedTeamId == Team1Id ? Team2Id : Team1Id;

            Match.Forfeit(forfeitedTeamId, reason);

            // Event will be published by source generator
        }

        public void AddRosterMember(string playerId, int teamNumber)
        {
            if (Status != ScrimmageStatus.Created)
                throw new InvalidOperationException("Roster members can only be added when scrimmage is in Created state");

            if (teamNumber != 1 && teamNumber != 2)
                throw new ArgumentException("Team number must be 1 or 2");

            var teamList = teamNumber == 1 ? Team1RosterIds : Team2RosterIds;
            if (!teamList.Contains(playerId))
            {
                teamList.Add(playerId);
                // Event will be published by source generator
            }
        }

        public void RemoveRosterMember(string playerId, int teamNumber)
        {
            if (Status != ScrimmageStatus.Created)
                throw new InvalidOperationException("Roster members can only be removed when scrimmage is in Created state");

            if (teamNumber != 1 && teamNumber != 2)
                throw new ArgumentException("Team number must be 1 or 2");

            var teamList = teamNumber == 1 ? Team1RosterIds : Team2RosterIds;
            if (teamList.Contains(playerId))
            {
                teamList.Remove(playerId);
                // Event will be published by source generator
            }
        }
    }

    public enum ScrimmageStatus
    {
        Created,
        Accepted,
        Declined,
        InProgress,
        Completed,
        Cancelled,
        Forfeited
    }

    /// <summary>
    /// Static class for calculating scrimmage ratings.
    /// Uses the event system to communicate with other components.
    /// </summary>
    public static class ScrimmageRatingCalculator
    {
        private const double BASE_RATING_CHANGE = 16.0;

        // Maximum and minimum variety bonus multipliers
        private const double MAX_VARIETY_BONUS = 0.2;  // +20% max bonus for high variety
        private const double MIN_VARIETY_BONUS = -0.1; // -10% penalty for low variety

        // Time window for considering opponent diversity
        private const int VARIETY_WINDOW_DAYS = 30;

        // Max percentage difference (normalized) before opponent weight is zeroed out
        private const double MAX_GAP_PERCENT = 0.4; // 40% of rating range

        private static readonly ICoreEventBus _eventBus = CoreEventBus.Instance;

        public static async Task<double> CalculateRatingMultiplierAsync(
            string teamId, string opponentId,
            int teamRating, int opponentRating)
        {
            var recentMatches = await GetRecentMatchesAsync(teamId, opponentId);

            // Multiplier based on whether this is a repeated opponent
            double directMatchMultiplier = CalculateDirectMatchMultiplier(recentMatches.Count());

            // Get the current global rating range (min and max)
            var ratingRange = await GetCurrentRatingRangeAsync();

            // Reduce bonus for opponents much lower than the team
            double opponentWeight = GetOpponentWeight(teamRating, opponentRating, ratingRange.Highest, ratingRange.Lowest);

            // Get how frequently team has played each opponent recently, normalized and weighted
            var varietyDistribution = await GetTeamOpponentDistributionAsync(teamId, ratingRange);

            // Calculate team-specific entropy (higher = more opponent diversity)
            double teamVarietyEntropy = CalculateWeightedEntropy(varietyDistribution);

            // Calculate global average entropy (this could alternatively use **median** for robustness against skew)
            double averageVarietyEntropy = await GetAverageVarietyEntropyAsync(ratingRange);

            // Scale the bonus/penalty proportionally to how far the team's entropy is from the average
            double varietyBonus = CalculateVarietyBonus(teamVarietyEntropy, averageVarietyEntropy);

            // Final multiplier combines match frequency, variety, and opponent strength
            double combinedMultiplier = (1.0 + varietyBonus) * (directMatchMultiplier * opponentWeight);

            return Math.Clamp(combinedMultiplier, 0.5, 2.0);
        }

        public static async Task<int> CalculateRatingChangeAsync(
            string winnerId, string loserId,
            int winnerRating, int loserRating,
            double multiplier)
        {
            // Standard ELO expected score
            var expectedScore = 1.0 / (1.0 + Math.Pow(10, (loserRating - winnerRating) / 400.0));

            // Scale rating gain by surprise of win (underdogs gain more)
            var ratingChange = (int)(BASE_RATING_CHANGE * (1 - expectedScore));

            return (int)(ratingChange * multiplier);
        }

        private static double CalculateDirectMatchMultiplier(int matchCount)
        {
            if (matchCount == 0)
                return 1.0 + MAX_VARIETY_BONUS; // full bonus for a first-time matchup

            double penalty = 0.1 * matchCount; // flat 10% penalty per repeat
            return Math.Max(1.0 - penalty, 0.5); // never drop below 0.5x multiplier
        }

        private static double GetOpponentWeight(int teamRating, int opponentRating, int highestRating, int lowestRating)
        {
            double range = highestRating - lowestRating;
            if (range <= 0) return 1.0;

            // Normalize gap as percent of global range
            double gap = Math.Abs(teamRating - opponentRating) / range;

            // Linear decay to 0 as gap approaches MAX_GAP_PERCENT
            return Math.Max(1.0 - (gap / MAX_GAP_PERCENT), 0.0);
        }

        private static double CalculateWeightedEntropy(Dictionary<string, double> opponentWeights)
        {
            double entropy = 0.0;
            foreach (var weight in opponentWeights.Values)
            {
                if (weight > 0)
                    entropy -= weight * Math.Log(weight, 2); // Shannon entropy
            }
            return entropy;
        }

        private static double CalculateVarietyBonus(double teamEntropy, double averageEntropy)
        {
            // Bonus scales proportionally with difference from average
            double relativeDiff = (teamEntropy - averageEntropy) / (averageEntropy == 0 ? 1 : averageEntropy);

            // Could use `Math.Min` or `Math.Tanh` here for softer clamping
            return Math.Clamp(relativeDiff * MAX_VARIETY_BONUS, MIN_VARIETY_BONUS, MAX_VARIETY_BONUS);

            /* Adjustment Option:
            - Use Median instead of Average: less sensitive to outliers
            - Use fixed target entropy: encourage reaching an ideal level of diversity */
        }

        private static async Task<IEnumerable<Scrimmage>> GetRecentMatchesAsync(string teamId, string opponentId)
        {
            var startDate = DateTime.UtcNow.AddDays(-VARIETY_WINDOW_DAYS);
            var request = new ScrimmageHistoryRequest
            {
                Team1Id = teamId,
                Team2Id = opponentId,
                Since = startDate
            };

            var response = await _eventBus.RequestAsync<ScrimmageHistoryRequest, ScrimmageHistoryResponse>(request);
            return response?.Matches ?? Array.Empty<Scrimmage>();
        }

        private static async Task<(int Highest, int Lowest)> GetCurrentRatingRangeAsync()
        {
            var request = new AllTeamRatingsRequest();
            var response = await _eventBus.RequestAsync<AllTeamRatingsRequest, AllTeamRatingsResponse>(request);

            if (response?.Ratings == null || !response.Ratings.Any())
                return (2000, 1000); // Fallback

            int max = response.Ratings.Max();
            int min = response.Ratings.Min();
            return (max, min);
        }

        private static async Task<Dictionary<string, double>> GetTeamOpponentDistributionAsync(string teamId, (int Highest, int Lowest) ratingRange)
        {
            var startDate = DateTime.UtcNow.AddDays(-VARIETY_WINDOW_DAYS);
            var request = new TeamOpponentStatsRequest
            {
                TeamId = teamId,
                Since = startDate
            };

            var response = await _eventBus.RequestAsync<TeamOpponentStatsRequest, TeamOpponentStatsResponse>(request);
            var rawFrequencies = response?.OpponentMatches ?? new Dictionary<string, (int Count, int Rating)>();

            // Calculate gap-weighted match frequency distribution
            double total = 0;
            var weighted = new Dictionary<string, double>();
            foreach (var (opponentId, (count, rating)) in rawFrequencies)
            {
                double gap = Math.Abs(rating - response.TeamRating) / (double)(ratingRange.Highest - ratingRange.Lowest);
                double weight = Math.Max(1.0 - (gap / MAX_GAP_PERCENT), 0.0) * count;
                weighted[opponentId] = weight;
                total += weight;
            }

            // Normalize distribution to sum to 1
            if (total > 0)
            {
                foreach (var key in weighted.Keys.ToList())
                {
                    weighted[key] /= total;
                }
            }

            return weighted;
        }

        private static async Task<double> GetAverageVarietyEntropyAsync((int Highest, int Lowest) ratingRange)
        {
            var request = new AllTeamOpponentDistributionsRequest
            {
                Since = DateTime.UtcNow.AddDays(-VARIETY_WINDOW_DAYS)
            };

            var response = await _eventBus.RequestAsync<AllTeamOpponentDistributionsRequest, AllTeamOpponentDistributionsResponse>(request);
            if (response?.Distributions == null || response.Distributions.Count == 0)
                return 0;

            // Entropy mean across all teams
            double totalEntropy = 0;
            int count = 0;

            foreach (var dist in response.Distributions)
            {
                double entropy = CalculateWeightedEntropy(dist.NormalizedWeights);
                totalEntropy += entropy;
                count++;
            }

            return count > 0 ? totalEntropy / count : 0;

            /* Adjustment Option:
            - Use Median instead of Average to avoid influence of extreme outliers
            - Use Percentile filtering to exclude top/bottom 5% */
        }
    }
}
