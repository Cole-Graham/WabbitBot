using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WabbitBot.Common.Models;
using WabbitBot.Common.Data.Utilities;

namespace WabbitBot.Core.Common.Models
{
    public class Team : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string TeamCaptainId { get; set; } = string.Empty;
        public GameSize TeamSize { get; set; }
        public int MaxRosterSize { get; set; }
        public DateTime LastActive { get; set; }
        public bool IsArchived { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public string? Tag { get; set; }
        public string? Description { get; set; }

        [JsonIgnore]
        public List<TeamMember> Roster { get; set; } = new();

        [JsonIgnore]
        public Dictionary<GameSize, TeamStats> Stats { get; set; } = new();

        // JSON serialization properties
        [JsonPropertyName("Roster")]
        public string RosterJson
        {
            get => JsonUtil.Serialize(Roster);
            set => Roster = JsonUtil.Deserialize<List<TeamMember>>(value) ?? new();
        }

        [JsonPropertyName("Stats")]
        public string StatsJson
        {
            get => JsonUtil.Serialize(Stats);
            set => Stats = JsonUtil.Deserialize<Dictionary<GameSize, TeamStats>>(value) ?? new();
        }

        public Team()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            LastActive = DateTime.UtcNow;
            IsArchived = false;
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

        public void UpdateLastActive()
        {
            LastActive = DateTime.UtcNow;
        }

        public void Archive()
        {
            IsArchived = true;
            ArchivedAt = DateTime.UtcNow;
        }

        public void Unarchive()
        {
            IsArchived = false;
            ArchivedAt = null;
        }

        public void AddPlayer(string playerId, TeamRole role = TeamRole.Core)
        {
            if (Roster.Count >= MaxRosterSize)
            {
                throw new InvalidOperationException($"Team roster is full (max size: {MaxRosterSize})");
            }

            if (Roster.Exists(m => m.PlayerId == playerId))
            {
                throw new InvalidOperationException("Player is already on the team");
            }

            Roster.Add(new TeamMember
            {
                PlayerId = playerId,
                Role = role,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            });
        }

        public void RemovePlayer(string playerId)
        {
            var member = Roster.Find(m => m.PlayerId == playerId);
            if (member != null)
            {
                Roster.Remove(member);
            }
        }

        public void UpdatePlayerRole(string playerId, TeamRole newRole)
        {
            var member = Roster.Find(m => m.PlayerId == playerId);
            if (member != null)
            {
                member.Role = newRole;
            }
        }

        public void DeactivatePlayer(string playerId)
        {
            var member = Roster.Find(m => m.PlayerId == playerId);
            if (member != null)
            {
                member.IsActive = false;
            }
        }

        public void ReactivatePlayer(string playerId)
        {
            var member = Roster.Find(m => m.PlayerId == playerId);
            if (member != null)
            {
                member.IsActive = true;
            }
        }

        /// <summary>
        /// Creates a 1v1 team for a player. This is used when a player participates in 1v1 matches.
        /// The team name will be the same as the player's name (which should match their Discord nickname).
        /// </summary>
        public static Team CreateOneVOneTeam(Player player)
        {
            var team = new Team
            {
                Name = player.Name,
                TeamSize = GameSize.OneVOne,
                TeamCaptainId = player.Id.ToString(),
                Tag = player.Name[..Math.Min(3, player.Name.Length)].ToUpper()
            };

            team.AddPlayer(player.Id.ToString(), TeamRole.Captain);
            return team;
        }

        public bool HasPlayer(ulong playerId)
        {
            return Roster.Any(m => m.PlayerId == playerId.ToString());
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

    public class TeamStats : BaseStats
    {
        public Dictionary<string, double> OpponentDistribution { get; set; } = new();

        public override void UpdateStats(bool isWin)
        {
            base.UpdateStats(isWin);
            // Team-specific stat updates can be added here
        }

        public override void UpdateRating(int newRating)
        {
            base.UpdateRating(newRating);
            // Team-specific rating updates can be added here
        }
    }
}
