using System;

namespace WabbitBot.Core.Common.Models
{
    /// <summary>
    /// Base class for stats tracking, providing common functionality for both Player and Team stats.
    /// </summary>
    public abstract class BaseStats
    {
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Rating { get; set; } = 1000; // Starting rating changed to 1000
        public int HighestRating { get; set; } = 1000; // Starting rating changed to 1000
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public DateTime LastMatchAt { get; set; }

        public double WinRate => Wins + Losses == 0 ? 0 : (double)Wins / (Wins + Losses);

        public virtual void UpdateStats(bool isWin)
        {
            if (isWin)
            {
                Wins++;
                CurrentStreak = Math.Max(0, CurrentStreak) + 1;
            }
            else
            {
                Losses++;
                CurrentStreak = Math.Min(0, CurrentStreak) - 1;
            }

            LongestStreak = Math.Max(Math.Abs(CurrentStreak), LongestStreak);
            LastMatchAt = DateTime.UtcNow;
        }

        public virtual void UpdateRating(int newRating)
        {
            Rating = newRating;
            HighestRating = Math.Max(HighestRating, newRating);
        }
    }
}