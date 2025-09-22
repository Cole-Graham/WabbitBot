using System;
using System.Collections.Generic;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Models
{
    /// <summary>
    /// Represents a player in the game system, independent of Discord users.
    /// Players are always part of a team, even in 1v1 matches.
    /// </summary>
    public class Player : Entity
    {
        public string Name { get; set; } = string.Empty;
        public DateTime LastActive { get; set; }
        public bool IsArchived { get; set; }
        public DateTime? ArchivedAt { get; set; }

        public List<Guid> TeamIds { get; set; } = new();
        /// <summary>
        /// Key: Platform name (e.g., "Discord", "Steam"), Value: List of user IDs from that platform.
        /// </summary>
        public Dictionary<string, List<string>> PreviousUserIds { get; set; } = new();

        // Game usernames parsed from submitted replay data
        public string? GameUsername { get; set; }
        public List<string> PreviousGameUsernames { get; set; } = new();

        public Player()
        {
            CreatedAt = DateTime.UtcNow;
            LastActive = DateTime.UtcNow;
            IsArchived = false;
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

        public void AddTeam(Guid teamId)
        {
            if (!TeamIds.Contains(teamId))
            {
                TeamIds.Add(teamId);
            }
        }

        public void RemoveTeam(Guid teamId)
        {
            TeamIds.Remove(teamId);
        }

        /// <summary>
        /// Validation methods for Player model
        /// </summary>
        public static class Validation
        {
            /// <summary>
            /// Validates if a player name is valid
            /// </summary>
            public static bool IsValidPlayerName(string name)
            {
                return !string.IsNullOrWhiteSpace(name) && name.Length <= 32;
            }

            /// <summary>
            /// Validates if a Discord user ID is valid
            /// </summary>
            public static bool IsValidDiscordUserId(ulong userId)
            {
                return userId > 0;
            }

            /// <summary>
            /// Validates if a player can be archived (no active teams or matches)
            /// </summary>
            public static bool CanBeArchived(Player player)
            {
                return !player.TeamIds.Any() && !player.IsArchived;
            }

            /// <summary>
            /// Validates if a player is in a valid number of teams
            /// </summary>
            public static bool IsValidTeamCount(Player player)
            {
                return player.TeamIds.Count <= 3; // Max 3 teams per player
            }
        }
    }
}
