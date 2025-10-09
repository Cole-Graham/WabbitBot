using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Models.Scrimmage;

namespace WabbitBot.Core.Common.Interfaces
{
    /// <summary>
    /// Interface for scrimmage-related core operations
    /// </summary>
    public interface IScrimmageCore : ICore
    {
        /// <summary>
        /// Creates a new scrimmage challenge
        /// </summary>
        Task<Result<Scrimmage>> CreateScrimmageAsync(
            Guid ChallengeId,
            Team ChallengerTeam,
            Team OpponentTeam,
            List<Player> Team1Players,
            List<Player> Team2Players,
            Player IssuedByPlayer,
            Player AcceptedByPlayer);

        /// <summary>
        /// Accepts a scrimmage challenge
        /// </summary>
        Task<Result> AcceptScrimmageAsync(Guid scrimmageId);
    }
}
