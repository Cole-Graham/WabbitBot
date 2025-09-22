using System;
using WabbitBot.Core.Common.Config;

namespace WabbitBot.Core.Common.Models
{
    /// <summary>
    /// Configuration for Game entities
    /// </summary>
    public class GameDbConfig : EntityConfig<Game>, IEntityConfig
    {
        public GameDbConfig() : base(
            tableName: "games",
            archiveTableName: "game_archive",
            columns: new[] {
                "id", "match_id", "map_id", "game_size", "team1_player_ids",
                "team2_player_ids", "game_number", "started_at", "completed_at",
                "winner_id", "best_of", "play_to_completion", "channel_id",
                "team1_thread_id", "team2_thread_id", "available_maps",
                "team1_map_bans", "team2_map_bans", "team1_map_bans_submitted_at",
                "team2_map_bans_submitted_at", "state_history", "created_at",
                "updated_at"
            },
            idColumn: "id",
            maxCacheSize: 300,
            defaultCacheExpiry: TimeSpan.FromMinutes(10))
        {
        }
    }

    /// <summary>
    /// Configuration for GameStateSnapshot entities
    /// </summary>
    public class GameStateSnapshotDbConfig : EntityConfig<GameStateSnapshot>, IEntityConfig
    {
        public GameStateSnapshotDbConfig() : base(
            tableName: "game_state_snapshots",
            archiveTableName: "game_state_snapshot_archive",
            columns: new[] {
                "id", "game_id", "timestamp", "user_id", "player_name", "additional_data",
                "started_at", "completed_at", "cancelled_at", "forfeited_at", "winner_id",
                "cancelled_by_user_id", "forfeited_by_user_id", "forfeited_team_id",
                "cancellation_reason", "forfeit_reason", "team1_deck_code", "team2_deck_code",
                "team1_deck_submitted_at", "team2_deck_submitted_at", "team1_deck_confirmed",
                "team2_deck_confirmed", "team1_deck_confirmed_at", "team2_deck_confirmed_at",
                "match_id", "map_id", "game_size", "team1_player_ids", "team2_player_ids",
                "game_number", "created_at", "updated_at"
            },
            idColumn: "id",
            maxCacheSize: 500,
            defaultCacheExpiry: TimeSpan.FromMinutes(5))
        {
        }
    }
}
