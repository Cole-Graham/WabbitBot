using System;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Events;
using WabbitBot.Core.Common.BotCore;

namespace WabbitBot.Core.Common;

/// <summary>
/// Player-specific validation rules and operations
/// </summary>
public static partial class CoreValidation
{
    /// <summary>
    /// Validates that a player has no active teams
    /// </summary>
    public static Func<Models.Player, bool> HasNoActiveTeams() =>
        player => !player.TeamIds.Any();

    /// <summary>
    /// Validates that a player has no active users
    /// </summary>
    public static Func<Models.Player, bool> HasNoActiveUsers() =>
        player => string.IsNullOrEmpty(player.PreviousUserIds?.LastOrDefault());

    /// <summary>
    /// Validates that a player has no active matches
    /// </summary>
    public static async Task<bool> HasNoActiveMatches(Models.Player player)
    {
        // Publish event to check for active matches
        var checkEvent = new PlayerArchiveCheckEvent(player.Id);
        await CoreEventBus.Instance.PublishAsync(checkEvent).ConfigureAwait(false);
        return !checkEvent.HasActiveMatches;
    }

    /// <summary>
    /// Validates that a player can be archived based on inactivity threshold
    /// </summary>
    public static bool CanArchivePlayer(Models.Player player)
    {
        if (player == null)
            throw new ArgumentNullException(nameof(player));

        var inactivityThreshold = TimeSpan.FromDays(30);
        return DateTime.UtcNow - player.LastActive >= inactivityThreshold;
    }

    /// <summary>
    /// Validates that a player can be unarchived
    /// </summary>
    public static bool CanUnarchivePlayer(Models.Player player)
    {
        if (player == null)
            throw new ArgumentNullException(nameof(player));

        return player.IsArchived;
    }

    /// <summary>
    /// Comprehensive validation for player archiving
    /// </summary>
    public static async Task<Result<Models.Player>> ValidateForArchivingAsync(Models.Player player)
    {
        if (player == null)
            return Result<Models.Player>.Failure("Player cannot be null");

        // Check if player is already archived
        if (player.IsArchived)
            return Result<Models.Player>.Failure("Player is already archived");

        // Check inactivity threshold
        if (!CanArchivePlayer(player))
            return Result<Models.Player>.Failure("Player has been active within the last 30 days");

        // Publish event to check player status
        var checkEvent = new PlayerArchiveCheckEvent(player.Id);
        await CoreEventBus.Instance.PublishAsync(checkEvent);

        if (checkEvent.HasActiveUsers)
            return Result<Models.Player>.Failure("Player is linked to an active user");

        if (checkEvent.HasActiveMatches)
            return Result<Models.Player>.Failure("Player has active matches");

        return Result<Models.Player>.CreateSuccess(player);
    }
}
