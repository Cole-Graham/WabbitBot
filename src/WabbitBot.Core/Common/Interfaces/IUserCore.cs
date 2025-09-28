using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Interfaces
{
    /// <summary>
    /// Interface for user-related core operations
    /// </summary>
    public interface IUserCore : ICore
    {
        /// <summary>
        /// Updates the last active timestamp for a user
        /// </summary>
        Task<Result> UpdateLastActiveAsync(Guid userId);

        /// <summary>
        /// Sets the active state of a user
        /// </summary>
        Task<Result> SetActiveAsync(Guid userId, bool isActive);

        /// <summary>
        /// Links a player to a user
        /// </summary>
        Task<Result> LinkPlayerAsync(Guid userId, Guid playerId);

        /// <summary>
        /// Unlinks the player from a user
        /// </summary>
        Task<Result> UnlinkPlayerAsync(Guid userId);
    }
}
