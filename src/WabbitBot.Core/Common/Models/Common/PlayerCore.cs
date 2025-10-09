using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Interfaces;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.Core.Common.Models.Common
{
    public partial class PlayerCore : IPlayerCore
    {
        /// <inheritdoc />
        public Task InitializeAsync() => Task.CompletedTask;

        /// <inheritdoc />
        public Task ValidateAsync() => Task.CompletedTask;

        public static Player InitializeDefaults(Player player)
        {
            player.CreatedAt = player.CreatedAt == default ? DateTime.UtcNow : player.CreatedAt;
            player.LastActive = player.LastActive == default ? DateTime.UtcNow : player.LastActive;
            player.TeamJoinLimit = ConfigurationProvider
                .GetSection<ScrimmageOptions>(ScrimmageOptions.SectionName)
                .TeamJoinLimit;
            return player;
        }

        public async Task<Result<Player>> CreateAsync(Player player)
        {
            var initialized = InitializeDefaults(player);
            var createResult = await CoreService.Players.CreateAsync(initialized, DatabaseComponent.Repository);
            return createResult.Success
                ? Result<Player>.CreateSuccess(createResult.Data!)
                : Result<Player>.Failure(createResult.ErrorMessage ?? "Failed to create player");
        }

        public static class Validation
        {
            public static bool IsValidPlayerName(string name)
            {
                return !string.IsNullOrWhiteSpace(name) && name.Length <= 32;
            }

            public static bool IsValidDiscordUserId(ulong userId)
            {
                return userId > 0;
            }

            public static bool IsValidTeamCount(Player player)
            {
                return (player?.TeamIds?.Count ?? 0) <= 3;
            }
        }

        public async Task<Result> UpdateLastActiveAsync(Guid playerId)
        {
            var playerResult = await CoreService.Players.GetByIdAsync(playerId, DatabaseComponent.Repository);
            if (!playerResult.Success || playerResult.Data is null)
            {
                return Result.Failure($"Player not found: {playerResult.ErrorMessage}");
            }

            var player = playerResult.Data;
            player.LastActive = DateTime.UtcNow;
            var updateRepo = await CoreService.Players.UpdateAsync(player, DatabaseComponent.Repository);
            var updateCache = await CoreService.Players.UpdateAsync(player, DatabaseComponent.Cache);
            return updateRepo.Success && updateCache.Success
                ? Result.CreateSuccess()
                : Result.Failure(
                    $"Failed to update player last active: {updateRepo.ErrorMessage} / {updateCache.ErrorMessage}"
                );
        }

        public async Task<Result> ArchiveAsync(Guid playerId)
        {
            var playerResult = await CoreService.Players.GetByIdAsync(playerId, DatabaseComponent.Repository);
            if (!playerResult.Success || playerResult.Data is null)
            {
                return Result.Failure($"Player not found: {playerResult.ErrorMessage}");
            }
            var player = playerResult.Data;

            // Archive snapshot via DatabaseService (hooked provider)
            // Using empty Guid as archivedBy for now; wire real user when available
            await CoreService.Players.DeleteAsync(playerId, DatabaseComponent.Repository);
            return Result.CreateSuccess();
        }

        public async Task<Result> UnarchiveAsync(Guid playerId)
        {
            // No-op: unarchive becomes a restore operation from archive history (to be implemented)
            await Task.CompletedTask;
            return Result.CreateSuccess();
        }

        public async Task<Result> AddTeamAsync(Guid playerId, Guid teamId)
        {
            var playerResult = await CoreService.Players.GetByIdAsync(playerId, DatabaseComponent.Repository);
            if (!playerResult.Success || playerResult.Data is null)
            {
                return Result.Failure($"Player not found: {playerResult.ErrorMessage}");
            }
            var player = playerResult.Data;
            if (!player.TeamIds.Contains(teamId))
            {
                player.TeamIds.Add(teamId);
                var update = await CoreService.Players.UpdateAsync(player, DatabaseComponent.Repository);
                if (!update.Success)
                    return Result.Failure(update.ErrorMessage ?? "Failed to add team");
            }
            return Result.CreateSuccess();
        }

        public async Task<Result> RemoveTeamAsync(Guid playerId, Guid teamId)
        {
            var playerResult = await CoreService.Players.GetByIdAsync(playerId, DatabaseComponent.Repository);
            if (!playerResult.Success || playerResult.Data is null)
            {
                return Result.Failure($"Player not found: {playerResult.ErrorMessage}");
            }
            var player = playerResult.Data;
            player.TeamIds.Remove(teamId);
            var update = await CoreService.Players.UpdateAsync(player, DatabaseComponent.Repository);
            return update.Success
                ? Result.CreateSuccess()
                : Result.Failure(update.ErrorMessage ?? "Failed to remove team");
        }
    }
}
