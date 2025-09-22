using System;
using System.Collections.Generic;
using System.Data;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Scrimmages.ScrimmageRating;
using WabbitBot.Core.Scrimmages.ScrimmageRating.Interface;

namespace WabbitBot.Core.Scrimmages.ScrimmageRating
{
    public class ProvenPotentialRepository : JsonRepository<ProvenPotentialRecord>, IProvenPotentialRepository
    {
        public ProvenPotentialRepository(IDatabaseConnection connection)
            : base(connection, "ProvenPotentialRecords", new[]
            {
                "OriginalMatchId", "ChallengerId", "OpponentId", "ChallengerRating", "OpponentRating",
                "ChallengerConfidence", "OpponentConfidence", "AppliedThresholds", "ChallengerOriginalRatingChange",
                "OpponentOriginalRatingChange", "RatingAdjustment", "LastCheckedAt", "IsComplete"
            })
        {
        }

        public async Task<IEnumerable<ProvenPotentialRecord>> GetActiveRecordsForTeamAsync(string teamId)
        {
            return await QueryAsync(
                "(ChallengerId = @TeamId OR OpponentId = @TeamId) AND IsComplete = 0",
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