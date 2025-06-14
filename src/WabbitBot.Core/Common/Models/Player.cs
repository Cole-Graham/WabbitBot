using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WabbitBot.Common.Models;
using WabbitBot.Common.Data.Utilities;

namespace WabbitBot.Core.Common.Models
{
    /// <summary>
    /// Represents a player in the game system, independent of Discord users.
    /// Players are always part of a team, even in 1v1 matches.
    /// </summary>
    public class Player : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public DateTime LastActive { get; set; }
        public bool IsArchived { get; set; }
        public DateTime? ArchivedAt { get; set; }

        [JsonIgnore]
        public Dictionary<GameSize, PlayerStats> Stats { get; set; } = new();

        [JsonIgnore]
        public List<string> TeamIds { get; set; } = new();

        [JsonIgnore]
        public List<string> PreviousUserIds { get; set; } = new(); // History of linked Discord users

        // JSON serialization properties
        [JsonPropertyName("Stats")]
        public string StatsJson
        {
            get => JsonUtil.Serialize(Stats);
            set => Stats = JsonUtil.Deserialize<Dictionary<GameSize, PlayerStats>>(value) ?? new();
        }

        [JsonPropertyName("TeamIds")]
        public string TeamIdsJson
        {
            get => JsonUtil.Serialize(TeamIds);
            set => TeamIds = JsonUtil.Deserialize<List<string>>(value) ?? new();
        }

        [JsonPropertyName("PreviousUserIds")]
        public string PreviousUserIdsJson
        {
            get => JsonUtil.Serialize(PreviousUserIds);
            set => PreviousUserIds = JsonUtil.Deserialize<List<string>>(value) ?? new();
        }

        public Player()
        {
            CreatedAt = DateTime.UtcNow;
            LastActive = DateTime.UtcNow;
            IsArchived = false;
            InitializeStats();
        }

        private void InitializeStats()
        {
            foreach (GameSize size in Enum.GetValues(typeof(GameSize)))
            {
                Stats[size] = new PlayerStats();
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

        public void AddPreviousUserId(string userId)
        {
            if (!PreviousUserIds.Contains(userId))
            {
                PreviousUserIds.Add(userId);
            }
        }

        public void AddTeam(string teamId)
        {
            if (!TeamIds.Contains(teamId))
            {
                TeamIds.Add(teamId);
            }
        }

        public void RemoveTeam(string teamId)
        {
            TeamIds.Remove(teamId);
        }
    }

    public class PlayerStats : BaseStats
    {
        // Player-specific stats can be added here if needed
    }
}
