using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WabbitBot.Core.Common.Migrations
{
    public partial class InitialSchema : Migration
    {
        protected void CreateScrimmagesTable(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scrimmages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    team1_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team2_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team1_roster_ids = table.Column<List<Guid>>(type: "jsonb", nullable: false),
                    team2_roster_ids = table.Column<List<Guid>>(type: "jsonb", nullable: false),
                    even_team_format = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    winner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    team1_rating = table.Column<double>(type: "double precision", nullable: false),
                    team2_rating = table.Column<double>(type: "double precision", nullable: false),
                    team1_rating_change = table.Column<double>(type: "double precision", nullable: false),
                    team2_rating_change = table.Column<double>(type: "double precision", nullable: false),
                    team1_confidence = table.Column<double>(type: "double precision", nullable: false),
                    team2_confidence = table.Column<double>(type: "double precision", nullable: false),
                    team1_score = table.Column<int>(type: "integer", nullable: false),
                    team2_score = table.Column<int>(type: "integer", nullable: false),
                    challenge_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_accepted = table.Column<bool>(type: "boolean", nullable: false),
                    best_of = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scrimmages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_scrimmages_team1_id",
                table: "scrimmages",
                column: "team1_id");

            migrationBuilder.CreateIndex(
                name: "idx_scrimmages_team2_id",
                table: "scrimmages",
                column: "team2_id");
        }

        protected void CreateProvenPotentialRecordsTable(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "proven_potential_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    original_match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    challenger_id = table.Column<string>(type: "text", nullable: false),
                    opponent_id = table.Column<string>(type: "text", nullable: false),
                    challenger_rating = table.Column<double>(type: "double precision", nullable: false),
                    opponent_rating = table.Column<double>(type: "double precision", nullable: false),
                    challenger_confidence = table.Column<double>(type: "double precision", nullable: false),
                    opponent_confidence = table.Column<double>(type: "double precision", nullable: false),
                    applied_thresholds = table.Column<HashSet<double>>(type: "jsonb", nullable: false),
                    challenger_original_rating_change = table.Column<double>(type: "double precision", nullable: false),
                    opponent_original_rating_change = table.Column<double>(type: "double precision", nullable: false),
                    rating_adjustment = table.Column<double>(type: "double precision", nullable: false),
                    even_team_format = table.Column<int>(type: "integer", nullable: false),
                    last_checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_complete = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proven_potential_records", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_proven_potential_records_challenger_id",
                table: "proven_potential_records",
                column: "challenger_id");

            migrationBuilder.CreateIndex(
                name: "idx_proven_potential_records_opponent_id",
                table: "proven_potential_records",
                column: "opponent_id");

            migrationBuilder.CreateIndex(
                name: "idx_proven_potential_records_game_size",
                table: "proven_potential_records",
                column: "even_team_format");
        }

    }
}
