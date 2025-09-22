using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WabbitBot.Core.Common.Migrations
{
    public partial class InitialSchema : Migration
    {
        protected void CreateMatchesTable(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "matches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    team1_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team2_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team1_player_ids = table.Column<List<Guid>>(type: "jsonb", nullable: false),
                    team2_player_ids = table.Column<List<Guid>>(type: "jsonb", nullable: false),
                    even_team_format = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    winner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_type = table.Column<string>(type: "text", nullable: true),
                    best_of = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    play_to_completion = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    channel_id = table.Column<long>(type: "bigint", nullable: true),
                    team1_thread_id = table.Column<long>(type: "bigint", nullable: true),
                    team2_thread_id = table.Column<long>(type: "bigint", nullable: true),
                    available_maps = table.Column<string>(type: "text", nullable: true),
                    team1_map_bans = table.Column<string>(type: "text", nullable: true),
                    team2_map_bans = table.Column<string>(type: "text", nullable: true),
                    team1_map_bans_submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    team2_map_bans_submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    state_history = table.Column<List<object>>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_matches", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_matches_team1_id",
                table: "matches",
                column: "team1_id");

            migrationBuilder.CreateIndex(
                name: "idx_matches_team2_id",
                table: "matches",
                column: "team2_id");
        }

        protected void CreateMatchStateSnapshotsTable(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "match_state_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_name = table.Column<string>(type: "text", nullable: false),
                    additional_data = table.Column<string>(type: "text", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    forfeited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    winner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    forfeited_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    forfeited_team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancellation_reason = table.Column<string>(type: "text", nullable: true),
                    forfeit_reason = table.Column<string>(type: "text", nullable: true),
                    current_game_number = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    current_map_id = table.Column<Guid>(type: "uuid", nullable: true),
                    final_score = table.Column<string>(type: "text", nullable: true),
                    available_maps = table.Column<List<string>>(type: "jsonb", nullable: true),
                    team1_map_bans = table.Column<List<string>>(type: "jsonb", nullable: true),
                    team2_map_bans = table.Column<List<string>>(type: "jsonb", nullable: true),
                    team1_bans_submitted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    team2_bans_submitted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    team1_bans_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    team2_bans_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    final_map_pool = table.Column<List<string>>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_match_state_snapshots", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_match_state_snapshots_match_id",
                table: "match_state_snapshots",
                column: "match_id");

            migrationBuilder.CreateIndex(
                name: "idx_match_state_snapshots_timestamp",
                table: "match_state_snapshots",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "idx_match_state_snapshots_winner_id",
                table: "match_state_snapshots",
                column: "winner_id");
        }

    }
}
