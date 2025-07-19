using System;
using System.Collections.Generic;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Leaderboards
{
    public class Season : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public Dictionary<GameSize, Dictionary<string, SeasonTeamStats>> TeamStats { get; set; } = new();
        public SeasonConfig Config { get; set; } = new();

        public Season()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            // Initialize stats for each game size
            foreach (GameSize size in Enum.GetValues(typeof(GameSize)))
            {
                TeamStats[size] = new Dictionary<string, SeasonTeamStats>();
            }
        }

        public static Season Create(string name, DateTime startDate, DateTime endDate, SeasonConfig config)
        {
            return new Season
            {
                Name = name,
                StartDate = startDate,
                EndDate = endDate,
                IsActive = true,
                Config = config
            };
        }

        public void End()
        {
            if (!IsActive)
                throw new InvalidOperationException("Season is already ended");

            IsActive = false;
            EndDate = DateTime.UtcNow;
        }

        public void AddTeamStats(string teamId, GameSize gameSize, int ratingChange, bool isWin)
        {
            if (!IsActive)
                throw new InvalidOperationException("Cannot add stats to an inactive season");

            if (!TeamStats[gameSize].TryGetValue(teamId, out var stats))
            {
                stats = new SeasonTeamStats
                {
                    TeamId = teamId,
                    GameSize = gameSize,
                    InitialRating = Leaderboard.InitialRating
                };
                TeamStats[gameSize][teamId] = stats;
            }

            stats.MatchesCount++;
            if (isWin)
                stats.Wins++;
            else
                stats.Losses++;

            // Apply rating change directly (no weighting)
            stats.CurrentRating += ratingChange;
            stats.LastUpdated = DateTime.UtcNow;
        }

        public void ApplyRatingDecay()
        {
            if (!Config.RatingDecayEnabled)
                return;

            var now = DateTime.UtcNow;
            foreach (var gameSize in TeamStats.Keys)
            {
                foreach (var stats in TeamStats[gameSize].Values)
                {
                    var weeksSinceUpdate = (now - stats.LastUpdated).TotalDays / 7;
                    if (weeksSinceUpdate >= 1)
                    {
                        var decayAmount = (int)(weeksSinceUpdate * Config.DecayRatePerWeek);
                        stats.CurrentRating = Math.Max(
                            stats.CurrentRating - decayAmount,
                            Config.MinimumRating
                        );
                        stats.LastUpdated = now;
                    }
                }
            }
        }
    }

    public class SeasonTeamStats
    {
        public string TeamId { get; set; } = string.Empty;
        public GameSize GameSize { get; set; }
        public int InitialRating { get; set; }
        public int CurrentRating { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int MatchesCount { get; set; }
        public DateTime LastUpdated { get; set; }
        public Dictionary<string, double> OpponentDistribution { get; set; } = new();
        public double WinRate => MatchesCount == 0 ? 0 : (double)Wins / MatchesCount;
        public int RecentMatchesCount { get; set; }  // Number of games played within the variety window
    }

    public class SeasonConfig
    {
        public bool RatingDecayEnabled { get; set; }
        public int DecayRatePerWeek { get; set; }
        public int MinimumRating { get; set; }
    }
}
