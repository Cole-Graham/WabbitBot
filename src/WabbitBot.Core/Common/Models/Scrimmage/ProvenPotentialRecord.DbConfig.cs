using System;
using WabbitBot.Core.Common.Config;

namespace WabbitBot.Core.Common.Models
{
    /// <summary>
    /// Configuration for ProvenPotentialRecord entities
    /// </summary>
    public class ProvenPotentialRecordConfig : EntityConfig<ProvenPotentialRecord>, IEntityConfig
    {
        public ProvenPotentialRecordConfig() : base(
            tableName: "proven_potential_records",
            archiveTableName: "proven_potential_record_archive",
            columns: new[] {
                "id", "original_match_id", "challenger_id", "opponent_id", "challenger_rating",
                "opponent_rating", "challenger_confidence", "opponent_confidence", "applied_thresholds",
                "challenger_original_rating_change", "opponent_original_rating_change", "rating_adjustment",
                "game_size", "last_checked_at", "is_complete", "created_at", "updated_at"
            },
            idColumn: "id",
            maxCacheSize: 500,
            defaultCacheExpiry: TimeSpan.FromMinutes(30)
        )
        {
        }
    }
}
