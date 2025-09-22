using System;
using System.Threading.Tasks;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Data.Interface;
using WabbitBot.Common.Data.Interfaces;

namespace WabbitBot.Core.Common.Services;

/// <summary>
/// Archive operations for PlayerService
/// Contains archive-specific operations and Player-specific archiving methods
/// </summary>
public partial class PlayerService
{
    #region Player-Specific Archive Operations

    /// <summary>
    /// Unarchives a player (business logic)
    /// </summary>
    public async Task<Result<Player>> UnarchivePlayerAsync(Player player)
    {
        try
        {
            // Basic validation
            if (player == null)
                return Result<Player>.Failure("Player cannot be null");

            if (!player.IsArchived)
                return Result<Player>.Failure("Player is not archived");

            // Update player to unarchived
            player.IsArchived = false;
            player.ArchivedAt = null;
            player.UpdatedAt = DateTime.UtcNow;

            // Use the standard UpdateAsync method to update in repository
            return await UpdateAsync(player, DatabaseComponent.Repository);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<Player>.Failure($"Failed to unarchive player: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates player archiving constraints using direct repository queries
    /// </summary>
    public async Task<PlayerArchiveValidationResult> ValidatePlayerForArchivingAsync(Guid playerId)
    {
        // Get player data
        var player = await PlayerRepository.GetByIdAsync(playerId);
        if (player == null)
            return PlayerArchiveValidationResult.Failure("Player not found");

        if (player.IsArchived)
            return PlayerArchiveValidationResult.Failure("Player is already archived");

        // Check inactivity threshold directly
        var inactivityThreshold = TimeSpan.FromDays(30);
        if (DateTime.UtcNow - player.LastActive < inactivityThreshold)
            return PlayerArchiveValidationResult.Failure("Player has been active within the last 30 days");

        // Direct database queries instead of events
        var hasActiveUsers = await CheckForActiveUsersAsync(playerId);
        var hasActiveMatches = await CheckForActiveMatchesAsync(playerId);

        if (hasActiveUsers)
            return PlayerArchiveValidationResult.Failure("Player is linked to an active user");

        if (hasActiveMatches)
            return PlayerArchiveValidationResult.Failure("Player has active matches");

        return PlayerArchiveValidationResult.Success(player);
    }

    #endregion

    #region Private Validation Methods

    /// <summary>
    /// Checks for active users using direct repository query
    /// </summary>
    private async Task<bool> CheckForActiveUsersAsync(Guid playerId)
    {
        var userRepository = WabbitBot.Core.Common.Data.DataServiceManager.UserRepository;

        const string sql = @"
            SELECT COUNT(*) FROM Users 
            WHERE CurrentPlayerId = @PlayerId 
            AND LastActive > @CutoffDate";

        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        var count = await WabbitBot.Common.Data.Utilities.QueryUtil.ExecuteScalarAsync<int>(
            await userRepository.GetConnectionAsync(),
            sql,
            new { PlayerId = playerId, CutoffDate = cutoffDate }
        );
        return count > 0;
    }

    /// <summary>
    /// Checks for active matches using direct repository query
    /// </summary>
    private async Task<bool> CheckForActiveMatchesAsync(Guid playerId)
    {
        var matchRepository = WabbitBot.Core.Common.Data.DataServiceManager.MatchRepository;

        const string sql = @"
            SELECT COUNT(*) FROM Matches 
            WHERE Status IN (0, 1) 
            AND (Team1PlayerIds LIKE @PlayerId OR Team2PlayerIds LIKE @PlayerId)";

        var count = await WabbitBot.Common.Data.Utilities.QueryUtil.ExecuteScalarAsync<int>(
            await matchRepository.GetConnectionAsync(),
            sql,
            new { PlayerId = playerId }
        );
        return count > 0;
    }

    #endregion
}

/// <summary>
/// Validation result class for player archiving operations
/// </summary>
public class PlayerArchiveValidationResult
{
    public bool IsValid { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Player? Player { get; private set; }

    public static PlayerArchiveValidationResult Success(Player player) =>
        new() { IsValid = true, Player = player };

    public static PlayerArchiveValidationResult Failure(string errorMessage) =>
        new() { IsValid = false, ErrorMessage = errorMessage };
}

