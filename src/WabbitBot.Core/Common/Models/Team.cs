using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WabbitBot.Common.Models;
using WabbitBot.Common.Data.Utilities;
using System.Linq;

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

        [JsonIgnore]
        public List<TeamMember> Roster { get; set; } = new();

        // JSON serialization properties
        [JsonPropertyName("Roster")]
        public string RosterJson
        {
            get => JsonUtil.Serialize(Roster);
            set => Roster = JsonUtil.Deserialize<List<TeamMember>>(value) ?? new();
        }

        public Team()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            LastActive = DateTime.UtcNow;
            IsArchived = false;
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
                IsActive = true,
                IsTeamManager = role == TeamRole.Captain // Captains are automatically team managers
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
                // Captains are automatically team managers
                if (newRole == TeamRole.Captain)
                {
                    member.IsTeamManager = true;
                }
            }
        }

        public void ChangeCaptain(string newCaptainId)
        {
            // Find current captain
            var currentCaptain = Roster.Find(m => m.Role == TeamRole.Captain);
            if (currentCaptain != null)
            {
                // Demote current captain to Core
                currentCaptain.Role = TeamRole.Core;
                // Remove manager status from outgoing captain
                currentCaptain.IsTeamManager = false;
            }

            // Find new captain
            var newCaptain = Roster.Find(m => m.PlayerId == newCaptainId);
            if (newCaptain != null)
            {
                // Promote new captain
                newCaptain.Role = TeamRole.Captain;
                // Ensure new captain has manager status
                newCaptain.IsTeamManager = true;
                // Update team captain ID
                TeamCaptainId = newCaptainId;
            }
        }

        public void SetTeamManagerStatus(string playerId, bool isTeamManager)
        {
            var member = Roster.Find(m => m.PlayerId == playerId);
            if (member != null)
            {
                // Captains are always team managers and cannot be demoted
                if (member.Role == TeamRole.Captain && !isTeamManager)
                {
                    throw new InvalidOperationException("Team captains cannot have their manager status removed");
                }

                member.IsTeamManager = isTeamManager;
            }
        }

        public List<string> GetTeamManagerIds()
        {
            return Roster.Where(m => m.IsActive && m.IsTeamManager).Select(m => m.PlayerId).ToList();
        }

        public List<TeamMember> GetActiveMembers()
        {
            return Roster.Where(m => m.IsActive).ToList();
        }

        public TeamMember? GetMember(string playerId)
        {
            return Roster.FirstOrDefault(m => m.PlayerId == playerId);
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

        public bool HasPlayer(string playerId)
        {
            return Roster.Exists(m => m.PlayerId == playerId);
        }

        public bool HasActivePlayer(string playerId)
        {
            return Roster.Exists(m => m.PlayerId == playerId && m.IsActive);
        }

        public int GetActivePlayerCount()
        {
            return Roster.Count(m => m.IsActive);
        }

        public List<string> GetActivePlayerIds()
        {
            return Roster.Where(m => m.IsActive).Select(m => m.PlayerId).ToList();
        }

        public List<string> GetCorePlayerIds()
        {
            return Roster.Where(m => m.IsActive && (m.Role == TeamRole.Core || m.Role == TeamRole.Captain)).Select(m => m.PlayerId).ToList();
        }

        public List<string> GetCaptainIds()
        {
            return Roster.Where(m => m.IsActive && m.Role == TeamRole.Captain).Select(m => m.PlayerId).ToList();
        }

        public bool IsCaptain(string playerId)
        {
            return Roster.Exists(m => m.PlayerId == playerId && m.IsActive && m.Role == TeamRole.Captain);
        }

        public bool IsTeamManager(string playerId)
        {
            return Roster.Exists(m => m.PlayerId == playerId && m.IsActive && m.IsTeamManager);
        }

        public bool IsValidForGameSize(GameSize gameSize)
        {
            var requiredPlayers = gameSize switch
            {
                GameSize.TwoVTwo => 2,
                GameSize.ThreeVThree => 3,
                GameSize.FourVFour => 4,
                _ => 1
            };

            return GetActivePlayerCount() >= requiredPlayers;
        }
    }

    public enum TeamRole
    {
        Captain,
        Core,
        Substitute
    }

    public class TeamMember
    {
        public string PlayerId { get; set; } = string.Empty;
        public TeamRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsTeamManager { get; set; }
    }
}
