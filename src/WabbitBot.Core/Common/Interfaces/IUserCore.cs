using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Interfaces
{
    /// <summary>
    /// Interface for user-related core operations
    /// </summary>
    public interface IMashinaUserCore : ICore
    {
        /// <summary>
        /// Updates the last active timestamp for a user
        /// </summary>
        Task<Result> UpdateLastActiveAsync(Guid mashinaUserId);

        /// <summary>
        /// Sets the active state of a user
        /// </summary>
        Task<Result> SetActiveAsync(Guid mashinaUserId, bool isActive);

        /// <summary>
        /// Links a player to a user
        /// </summary>
        Task<Result> LinkPlayerAsync(Guid mashinaUserId, Guid playerId);

        /// <summary>
        /// Unlinks the player from a user
        /// </summary>
        Task<Result> UnlinkPlayerAsync(Guid mashinaUserId);
    }
}
