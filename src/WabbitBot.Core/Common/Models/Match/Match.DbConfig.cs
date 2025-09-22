using System;
using WabbitBot.Core.Common.Config;

namespace WabbitBot.Core.Common.Models
{
    /// <summary>
    /// Configuration for Match entities
    /// </summary>
    public class MatchDbConfig : EntityConfig<Match>, IEntityConfig
    {
        public MatchDbConfig() : base(
            tableName: "matches",
            archiveTableName: "match_archive",
            columns: new[] {
                "id", "team1_id", "team2_id", "team1_player_ids", "team2_player_ids",
                "game_size", "started_at", "completed_at", "winner_id", "parent_id",
                "parent_type", "games", "best_of", "play_to_completion", "available_maps",
                "team1_map_bans", "team2_map_bans", "team1_map_bans_submitted_at",
                "team2_map_bans_submitted_at", "channel_id", "team1_thread_id", "team2_thread_id",
                "current_state_snapshot", "state_history", "created_at", "updated_at"
            },
            idColumn: "id",
            maxCacheSize: 200,
            defaultCacheExpiry: TimeSpan.FromMinutes(10)
        )
        {
        }
    }

    /// <summary>
    /// Configuration for MatchStateSnapshot entities
    /// </summary>
    public class MatchStateSnapshotDbConfig : EntityConfig<MatchStateSnapshot>, IEntityConfig
    {
        public MatchStateSnapshotDbConfig() : base(
            tableName: "match_state_snapshots",
            archiveTableName: "match_state_snapshot_archive",
            columns: new[] {
                "id", "match_id", "timestamp", "user_id", "player_name", "additional_data",
                "started_at", "completed_at", "cancelled_at", "forfeited_at", "winner_id",
                "cancelled_by_user_id", "forfeited_by_user_id", "forfeited_team_id",
                "cancellation_reason", "forfeit_reason", "current_game_number", "games",
                "current_map_id", "final_score", "final_games", "available_maps",
                "team1_map_bans", "team2_map_bans", "team1_bans_submitted", "team2_bans_submitted",
                "team1_bans_confirmed", "team2_bans_confirmed", "final_map_pool",
                "created_at", "updated_at"
            },
            idColumn: "id",
            maxCacheSize: 300,
            defaultCacheExpiry: TimeSpan.FromMinutes(10)
        )
        {
        }
    }
}
