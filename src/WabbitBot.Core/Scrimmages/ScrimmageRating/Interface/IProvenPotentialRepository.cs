using WabbitBot.Common.Data.Interfaces;

namespace WabbitBot.Core.Scrimmages.ScrimmageRating.Interface
{
    public interface IProvenPotentialRepository : IBaseRepository<ProvenPotentialRecord>
    {
        Task<IEnumerable<ProvenPotentialRecord>> GetActiveRecordsForTeamAsync(string teamId);
        Task<IEnumerable<ProvenPotentialRecord>> GetRecordsForMatchAsync(Guid matchId);
    }
}