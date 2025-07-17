using System;
using System.Collections.Generic;
using System.Data;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Scrimmages.ScrimmageRating;
using WabbitBot.Core.Scrimmages.ScrimmageRating.Interface;

namespace WabbitBot.Core.Scrimmages.ScrimmageRating
{
    public class ProvenPotentialRepository : BaseJsonRepository<ProvenPotentialRecord>, IProvenPotentialRepository
    {
        public ProvenPotentialRepository(IDatabaseConnection connection)
            : base(connection, "ProvenPotentialRecords", new[]
            {
                "OriginalMatchId", "Team1Id", "Team2Id", "Team1Rating", "Team2Rating",
                "Team1Confidence", "Team2Confidence", "AppliedThresholds", "RatingAdjustment",
                "LastCheckedAt", "IsComplete"
            })
        {
        }

        public async Task<IEnumerable<ProvenPotentialRecord>> GetActiveRecordsForTeamAsync(string teamId)
        {
            return await QueryAsync(
                "Team1Id = @TeamId OR Team2Id = @TeamId AND IsComplete = 0",
                new { TeamId = teamId }
            );
        }

        public async Task<IEnumerable<ProvenPotentialRecord>> GetRecordsForMatchAsync(Guid matchId)
        {
            return await QueryAsync(
                "OriginalMatchId = @MatchId",
                new { MatchId = matchId }
            );
        }

        protected override ProvenPotentialRecord CreateEntity()
        {
            return new ProvenPotentialRecord();
        }
    }
}