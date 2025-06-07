using System;
using System.Threading.Tasks;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Events;
using WabbitBot.Core.Common.BotCore;

namespace WabbitBot.Core.Common.Data.Validation
{
    /// <summary>
    /// Provides validation rules for player archiving operations.
    /// </summary>
    public static class PlayerArchiveValidation
    {
        private static readonly TimeSpan InactivityThreshold = TimeSpan.FromDays(30);

        /// <summary>
        /// Validates if a player can be archived based on inactivity threshold.
        /// </summary>
        public static bool CanArchivePlayer(Player player)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            return DateTime.UtcNow - player.LastActive >= InactivityThreshold;
        }

        /// <summary>
        /// Validates if a player can be archived based on their current state.
        /// </summary>
        public static async Task<(bool CanArchive, string? Reason)> ValidatePlayerForArchivingAsync(Player player)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            // Check if player is already archived
            if (player.IsArchived)
                return (false, "Player is already archived");

            // Check inactivity threshold
            if (!CanArchivePlayer(player))
                return (false, $"Player has been active within the last {InactivityThreshold.TotalDays} days");

            // Publish event to check player status
            var checkEvent = new PlayerArchiveCheckEvent(player.Id);
            await CoreEventBus.Instance.PublishAsync(checkEvent);

            if (checkEvent.HasActiveUsers)
                return (false, "Player is linked to an active user");

            if (checkEvent.HasActiveMatches)
                return (false, "Player has active matches");

            return (true, null);
        }

        /// <summary>
        /// Validates if a player can be unarchived.
        /// </summary>
        public static bool CanUnarchivePlayer(Player player)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            return player.IsArchived;
        }
    }
}