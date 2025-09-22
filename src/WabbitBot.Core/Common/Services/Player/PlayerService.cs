using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.ErrorHandling;
using WabbitBot.Common.Models;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Data.Service;

namespace WabbitBot.Core.Common.Services;

/// <summary>
/// Player-specific business logic service operations with unified data access
/// </summary>
[GenerateEventPublisher(EventBusType = EventBusType.Core, EnableValidation = true, EnableTimestamps = true)]
public partial class PlayerService
{
    private readonly DatabaseService<Player> _playerDb;

    public PlayerService(DatabaseService<Player> playerDb)
    {
        _playerDb = playerDb ?? throw new ArgumentNullException(nameof(playerDb));
    }


    /// <summary>
    /// Creates a new player with business logic validation
    /// </summary>
    public async Task<Result<Models.Player>> CreatePlayerAsync(string playerName)
    {
        try
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(playerName))
                return Result<Models.Player>.Failure("Player name cannot be empty");

            // Check if player name already exists (try cache first, then repository)
            var existingPlayer = await _playerDb.GetByNameAsync(playerName);
            if (existingPlayer != null)
            {
                return Result<Models.Player>.Failure($"Player with name '{playerName}' already exists");
            }

            // Create new player
            var player = new Models.Player
            {
                Id = Guid.NewGuid(),
                Name = playerName,
                CreatedAt = DateTime.UtcNow,
                LastActive = DateTime.UtcNow,
                IsArchived = false
            };

            // Create player using unified database service (handles cache-first coordination)
            return await _playerDb.CreateAsync(player);
        }
        catch (Exception ex)
        {
            return Result<Models.Player>.Failure($"Failed to create player: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing player with business logic validation
    /// </summary>
    public async Task<Result<Models.Player>> UpdatePlayerAsync(Models.Player player)
    {
        try
        {
            // Basic validation
            if (player == null)
                return Result<Models.Player>.Failure("Player cannot be null");

            if (string.IsNullOrWhiteSpace(player.Name))
                return Result<Models.Player>.Failure("Player name cannot be empty");

            // Update player using unified database service (handles cache-first coordination)
            return await _playerDb.UpdateAsync(player);
        }
        catch (Exception ex)
        {
            return Result<Models.Player>.Failure($"Failed to update player: {ex.Message}");
        }
    }

    /// <summary>
    /// Archives a player with business logic validation
    /// </summary>
    public async Task<Result<Models.Player>> ArchivePlayerAsync(Models.Player player)
    {
        try
        {
            // Basic validation
            if (player == null)
                return Result<Models.Player>.Failure("Player cannot be null");

            if (player.IsArchived)
                return Result<Models.Player>.Failure("Player is already archived");

            // Use the standard CreateAsync method to archive the player
            return await CreateAsync(player, DatabaseComponent.Archive);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(ex);
            return Result<Models.Player>.Failure($"Failed to archive player: {ex.Message}");
        }
    }





}
