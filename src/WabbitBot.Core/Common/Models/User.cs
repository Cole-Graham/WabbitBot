using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Models
{
    /// <summary>
    /// Represents a Discord user in the system.
    /// </summary>
    public class User : Entity
    {
        public string DiscordId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Nickname { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime LastActive { get; set; }
        public bool IsActive { get; set; }
        public Guid? PlayerId { get; set; } // Reference to associated Player entity

        public User()
        {
            JoinedAt = DateTime.UtcNow;
            LastActive = DateTime.UtcNow;
            IsActive = true;
        }

        public void UpdateLastActive()
        {
            LastActive = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
            LastActive = DateTime.UtcNow;
        }

        public void Reactivate()
        {
            IsActive = true;
            LastActive = DateTime.UtcNow;
        }

        public void LinkPlayer(Guid playerId)
        {
            if (playerId == Guid.Empty)
                throw new ArgumentException("Player ID cannot be null or empty", nameof(playerId));

            PlayerId = playerId;
        }

        public void UnlinkPlayer()
        {
            PlayerId = null;
        }
    }
}