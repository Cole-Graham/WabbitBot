using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WabbitBot.Core.Common.Migrations
{
    public partial class InitialSchema : Migration
    {
        protected void CreateGamesTable(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "games",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    map_id = table.Column<Guid>(type: "uuid", nullable: false),
                    even_team_format = table.Column<int>(type: "integer", nullable: false),
                    team1_player_ids = table.Column<List<Guid>>(type: "jsonb", nullable: false),
                    team2_player_ids = table.Column<List<Guid>>(type: "jsonb", nullable: false),
                    game_number = table.Column<int>(type: "integer", nullable: false),
                    state_history = table.Column<List<object>>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_games", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_games_match_id",
                table: "games",
                column: "match_id");

            migrationBuilder.CreateIndex(
                name: "idx_games_team1_player_ids",
                table: "games",
                column: "team1_player_ids")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_games_team2_player_ids",
                table: "games",
                column: "team2_player_ids")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        protected void CreateGameStateSnapshotsTable(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "game_state_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    team1_deck_code = table.Column<string>(type: "text", nullable: true),
                    team2_deck_code = table.Column<string>(type: "text", nullable: true),
                    team1_deck_submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    team2_deck_submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    team1_deck_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    team2_deck_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    team1_deck_confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    team2_deck_confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_state_snapshots", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_game_state_snapshots_game_id",
                table: "game_state_snapshots",
                column: "game_id");

            migrationBuilder.CreateIndex(
                name: "idx_game_state_snapshots_timestamp",
                table: "game_state_snapshots",
                column: "timestamp");
        }

    }
}
