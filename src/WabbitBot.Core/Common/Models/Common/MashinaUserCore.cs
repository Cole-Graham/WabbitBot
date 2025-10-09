using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Interfaces;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.Core.Common.Models.Common
{
    public partial class MashinaUserCore : IMashinaUserCore
    {
        /// <inheritdoc />
        public Task InitializeAsync() => Task.CompletedTask;

        /// <inheritdoc />
        public Task ValidateAsync() => Task.CompletedTask;

        public async Task<Result> UpdateLastActiveAsync(Guid mashinaUserId)
        {
            var userResult = await CoreService.MashinaUsers.GetByIdAsync(
                mashinaUserId,
                WabbitBot.Common.Data.Interfaces.DatabaseComponent.Repository
            );
            if (!userResult.Success || userResult.Data is null)
            {
                return Result.Failure($"User not found: {userResult.ErrorMessage}");
            }
            var user = userResult.Data;
            user.LastActive = DateTime.UtcNow;
            var updateRepo = await CoreService.MashinaUsers.UpdateAsync(
                user,
                WabbitBot.Common.Data.Interfaces.DatabaseComponent.Repository
            );
            var updateCache = await CoreService.MashinaUsers.UpdateAsync(
                user,
                WabbitBot.Common.Data.Interfaces.DatabaseComponent.Cache
            );
            return updateRepo.Success && updateCache.Success
                ? Result.CreateSuccess()
                : Result.Failure(
                    $"Failed to update user last active: {updateRepo.ErrorMessage} / {updateCache.ErrorMessage}"
                );
        }

        public async Task<Result> SetActiveAsync(Guid mashinaUserId, bool isActive)
        {
            var userResult = await CoreService.MashinaUsers.GetByIdAsync(
                mashinaUserId,
                WabbitBot.Common.Data.Interfaces.DatabaseComponent.Repository
            );
            if (!userResult.Success || userResult.Data is null)
            {
                return Result.Failure($"User not found: {userResult.ErrorMessage}");
            }
            var user = userResult.Data;
            user.IsActive = isActive;
            user.LastActive = DateTime.UtcNow;
            var update = await CoreService.MashinaUsers.UpdateAsync(
                user,
                WabbitBot.Common.Data.Interfaces.DatabaseComponent.Repository
            );
            return update.Success
                ? Result.CreateSuccess()
                : Result.Failure(update.ErrorMessage ?? "Failed to update user active state");
        }

        public async Task<Result> LinkPlayerAsync(Guid mashinaUserId, Guid playerId)
        {
            if (playerId == Guid.Empty)
            {
                return Result.Failure("Player ID cannot be empty");
            }
            var userResult = await CoreService.MashinaUsers.GetByIdAsync(
                mashinaUserId,
                WabbitBot.Common.Data.Interfaces.DatabaseComponent.Repository
            );
            if (!userResult.Success || userResult.Data is null)
            {
                return Result.Failure($"User not found: {userResult.ErrorMessage}");
            }
            var user = userResult.Data;
            user.PlayerId = playerId;
            var update = await CoreService.MashinaUsers.UpdateAsync(
                user,
                WabbitBot.Common.Data.Interfaces.DatabaseComponent.Repository
            );
            return update.Success
                ? Result.CreateSuccess()
                : Result.Failure(update.ErrorMessage ?? "Failed to link player");
        }

        public async Task<Result> UnlinkPlayerAsync(Guid mashinaUserId)
        {
            var userResult = await CoreService.MashinaUsers.GetByIdAsync(
                mashinaUserId,
                WabbitBot.Common.Data.Interfaces.DatabaseComponent.Repository
            );
            if (!userResult.Success || userResult.Data is null)
            {
                return Result.Failure($"User not found: {userResult.ErrorMessage}");
            }
            var user = userResult.Data;
            user.PlayerId = null;
            var update = await CoreService.MashinaUsers.UpdateAsync(
                user,
                WabbitBot.Common.Data.Interfaces.DatabaseComponent.Repository
            );
            return update.Success
                ? Result.CreateSuccess()
                : Result.Failure(update.ErrorMessage ?? "Failed to unlink player");
        }
    }
}
