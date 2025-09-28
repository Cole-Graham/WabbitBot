using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Interfaces
{
    /// <summary>
    /// Interface for team-related core operations
    /// </summary>
    public interface ITeamCore : ICore
    {
        /// <summary>
        /// Updates the last active timestamp for a team
        /// </summary>
        Task UpdateLastActive(Guid teamId);

        /// <summary>
        /// Adds a player to a team with the specified role
        /// </summary>
        Task AddPlayer(Guid teamId, Guid playerId, TeamRole role = TeamRole.Core);

        /// <summary>
        /// Removes a player from a team
        /// </summary>
        Task RemovePlayer(Guid teamId, Guid playerId);

        /// <summary>
        /// Updates the role of a player in a team
        /// </summary>
        Task UpdatePlayerRole(Guid teamId, Guid playerId, TeamRole newRole);

        /// <summary>
        /// Changes the captain of a team
        /// </summary>
        Task ChangeCaptain(Guid teamId, Guid newCaptainId);

        /// <summary>
        /// Sets the team manager status of a player
        /// </summary>
        Task SetTeamManagerStatus(Guid teamId, Guid playerId, bool isTeamManager);

        /// <summary>
        /// Deactivates a player in a team
        /// </summary>
        Task DeactivatePlayer(Guid teamId, Guid playerId);

        /// <summary>
        /// Reactivates a player in a team
        /// </summary>
        Task ReactivatePlayer(Guid teamId, Guid playerId);

        /// <summary>
        /// Updates the stats for a team after a match result
        /// </summary>
        Task<Result> UpdateStats(Guid teamId, TeamSize teamSize, bool isWin);

        /// <summary>
        /// Updates the rating for a team
        /// </summary>
        Task<Result> UpdateRating(Guid teamId, TeamSize teamSize, double newRating);
    }
}
