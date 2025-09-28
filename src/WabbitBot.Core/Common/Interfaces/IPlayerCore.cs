using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Interfaces
{
    /// <summary>
    /// Interface for player-related core operations
    /// </summary>
    public interface IPlayerCore : ICore
    {
        /// <summary>
        /// Creates a new player with default initialization
        /// </summary>
        Task<Result<Player>> CreateAsync(Player player);

        /// <summary>
        /// Updates the last active timestamp for a player
        /// </summary>
        Task<Result> UpdateLastActiveAsync(Guid playerId);

        /// <summary>
        /// Archives a player
        /// </summary>
        Task<Result> ArchiveAsync(Guid playerId);

        /// <summary>
        /// Unarchives a player
        /// </summary>
        Task<Result> UnarchiveAsync(Guid playerId);

        /// <summary>
        /// Adds a team to a player's team list
        /// </summary>
        Task<Result> AddTeamAsync(Guid playerId, Guid teamId);

        /// <summary>
        /// Removes a team from a player's team list
        /// </summary>
        Task<Result> RemoveTeamAsync(Guid playerId, Guid teamId);
    }
}
