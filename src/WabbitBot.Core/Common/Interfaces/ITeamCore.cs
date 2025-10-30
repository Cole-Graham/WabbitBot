using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;

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
        /// Adds a player to a team with the specified role and roster group
        /// </summary>
        Task AddPlayer(Guid teamId, Guid playerId, TeamSizeRosterGroup rosterGroup, RosterRole role = RosterRole.Core);

        // Removing players is handled via deactivation

        /// <summary>
        /// Updates the role of a player in a team
        /// </summary>
        Task UpdatePlayerRole(Guid teamId, Guid playerId, RosterRole newRole, bool isMod = false);

        /// <summary>
        /// Changes the captain for a specific roster group in a team
        /// </summary>
        Task ChangeCaptain(Guid teamId, TeamSizeRosterGroup rosterGroup, Guid newCaptainId, bool isMod = false);

        /// <summary>
        /// Sets the team manager status of a player
        /// </summary>
        Task SetTeamManagerStatus(Guid teamId, Guid playerId, bool IsRosterManager);

        /// <summary>
        /// Deactivates a player for a specific roster group in a team
        /// </summary>
        Task DeactivatePlayer(Guid teamId, Guid playerId);

        /// <summary>
        /// Updates the Scrimmage stats for a team after a match result
        /// </summary>
        Task<Result> UpdateScrimmageStats(Guid teamId, TeamSize teamSize, bool isWin);

        /// <summary>
        /// Updates the Scrimmage rating for a team
        /// </summary>
        Task<Result> UpdateScrimmageRating(Guid teamId, TeamSize teamSize, double newRating);

        /// <summary>
        /// Updates the Tournament stats for a team
        /// </summary>
        Task<Result> UpdateTournamentStats(Guid teamId, TeamSize teamSize, bool isWin);

        /// <summary>
        /// Updates the Tournament rating for a team
        /// </summary>
        Task<Result> UpdateTournamentRating(Guid teamId, TeamSize teamSize, double newRating);
    }
}
