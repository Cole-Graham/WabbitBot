using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;

namespace WabbitBot.Core.Common.Interfaces
{
    /// <summary>
    /// Interface for match-related core operations
    /// </summary>
    public interface IMatchCore : ICore
    {
        /// <summary>
        /// Starts a match with the specified teams and players
        /// </summary>
        Task<Result> StartMatchAsync(Guid matchId, Guid team1Id, Guid team2Id, List<Guid> team1PlayerIds, List<Guid> team2PlayerIds);

        /// <summary>
        /// Completes a match and determines the winner
        /// </summary>
        Task<Result> CompleteMatchAsync(Guid matchId, Guid winnerId);
    }
}
