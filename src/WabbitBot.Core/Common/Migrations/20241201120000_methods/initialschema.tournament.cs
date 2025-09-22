using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WabbitBot.Core.Common.Migrations
{
    public partial class InitialSchema : Migration
    {
        protected void CreateTournamentsTable(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tournaments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "varchar(255)", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    even_team_format = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    max_participants = table.Column<int>(type: "integer", nullable: false),
                    best_of = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    state_history = table.Column<List<object>>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tournaments", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_tournaments_game_size",
                table: "tournaments",
                column: "even_team_format");
        }

        protected void CreateTournamentStateSnapshotsTable(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tournament_state_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tournament_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_name = table.Column<string>(type: "text", nullable: false),
                    additional_data = table.Column<string>(type: "text", nullable: true),
                    registration_opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    max_participants = table.Column<int>(type: "integer", nullable: false),
                    winner_team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancellation_reason = table.Column<string>(type: "text", nullable: true),
                    registered_team_ids = table.Column<List<Guid>>(type: "jsonb", nullable: true),
                    participant_team_ids = table.Column<List<Guid>>(type: "jsonb", nullable: true),
                    active_match_ids = table.Column<List<Guid>>(type: "jsonb", nullable: true),
                    completed_match_ids = table.Column<List<Guid>>(type: "jsonb", nullable: true),
                    all_match_ids = table.Column<List<Guid>>(type: "jsonb", nullable: true),
                    final_rankings = table.Column<List<object>>(type: "jsonb", nullable: true),
                    current_participant_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    current_round = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tournament_state_snapshots", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_tournament_state_snapshots_tournament_id",
                table: "tournament_state_snapshots",
                column: "tournament_id");

            migrationBuilder.CreateIndex(
                name: "idx_tournament_state_snapshots_timestamp",
                table: "tournament_state_snapshots",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "idx_tournament_state_snapshots_winner_team_id",
                table: "tournament_state_snapshots",
                column: "winner_team_id");
        }

    }
}
