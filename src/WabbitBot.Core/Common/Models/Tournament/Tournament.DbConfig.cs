using System;
using WabbitBot.Core.Common.Config;

namespace WabbitBot.Core.Common.Models
{
    /// <summary>
    /// Configuration for Tournament entities
    /// </summary>
    public class TournamentDbConfig : EntityConfig<Tournament>, IEntityConfig
    {
        public TournamentDbConfig() : base(
            tableName: "tournaments",
            archiveTableName: "tournament_archive",
            columns: new[] {
                "id", "name", "description", "game_size", "start_date", "end_date",
                "max_participants", "best_of", "current_state_snapshot", "state_history",
                "created_at", "updated_at"
            },
            idColumn: "id",
            maxCacheSize: 50,
            defaultCacheExpiry: TimeSpan.FromMinutes(30)
        )
        {
        }
    }

    /// <summary>
    /// Configuration for TournamentStateSnapshot entities
    /// </summary>
    public class TournamentStateSnapshotDbConfig : EntityConfig<TournamentStateSnapshot>, IEntityConfig
    {
        public TournamentStateSnapshotDbConfig() : base(
            tableName: "tournament_state_snapshots",
            archiveTableName: "tournament_state_snapshot_archive",
            columns: new[] {
                "id", "tournament_id", "timestamp", "user_id", "player_name", "additional_data",
                "registration_opened_at", "started_at", "completed_at", "cancelled_at",
                "name", "description", "start_date", "max_participants", "winner_team_id",
                "cancelled_by_user_id", "cancellation_reason", "registered_team_ids",
                "participant_team_ids", "active_match_ids", "completed_match_ids",
                "all_match_ids", "final_rankings", "current_participant_count",
                "current_round", "created_at", "updated_at"
            },
            idColumn: "id",
            maxCacheSize: 200,
            defaultCacheExpiry: TimeSpan.FromMinutes(15)
        )
        {
        }
    }
}
