using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models;

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
            Guid challengerTeamId,
            Guid opponentTeamId,
            List<Guid> challengerRosterIds,
            List<Guid> opponentRosterIds,
            TeamSize teamSize,
            int bestOf = 1);

        /// <summary>
        /// Accepts a scrimmage challenge
        /// </summary>
        Task<Result> AcceptScrimmageAsync(Guid scrimmageId);
    }
}
