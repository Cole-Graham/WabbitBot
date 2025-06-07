using System;
using System.Collections.Generic;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Models
{
    public class Team : BaseEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TeamCaptainId { get; set; } = string.Empty;
        public GameSize TeamSize { get; set; }
        public int MaxRosterSize { get; set; }
        public List<TeamMember> Roster { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime LastActive { get; set; }
        public Dictionary<GameSize, TeamStats> Stats { get; set; } = new();
        public string? Tag { get; set; }
        public string? Description { get; set; }

        public Team()
        {
            CreatedAt = DateTime.UtcNow;
            LastActive = DateTime.UtcNow;
            InitializeStats();
            SetMaxRosterSize();
        }

        private void SetMaxRosterSize()
        {
            // Allow one extra player for substitutes
            MaxRosterSize = TeamSize switch
            {
                GameSize.TwoVTwo => 3,
                GameSize.ThreeVThree => 4,
                GameSize.FourVFour => 5,
                _ => 1
            };
        }

        private void InitializeStats()
        {
            foreach (GameSize size in Enum.GetValues(typeof(GameSize)))
            {
                if (size != GameSize.OneVOne) // Teams don't participate in 1v1
                {
                    Stats[size] = new TeamStats();
                }
            }
        }
    }

    public class TeamMember
    {
        public string PlayerId { get; set; } = string.Empty;
        public TeamRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsActive { get; set; } = true;

        public TeamMember()
        {
            JoinedAt = DateTime.UtcNow;
        }
    }

    public enum TeamRole
    {
        Captain,
        Core,
        Substitute
    }

    public class TeamStats
    {
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Rating { get; set; } = 1000; // Initial ELO rating
        public int HighestRating { get; set; } = 1000;
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public DateTime LastMatchAt { get; set; }

        public double WinRate => Wins + Losses == 0 ? 0 : (double)Wins / (Wins + Losses);
    }
}
