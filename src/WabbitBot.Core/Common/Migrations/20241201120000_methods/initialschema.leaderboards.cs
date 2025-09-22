using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WabbitBot.Core.Common.Migrations
{
    public partial class InitialSchema : Migration
    {
        protected void CreateLeaderboardsTable(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "leaderboards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    rankings = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leaderboards", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_leaderboards_rankings",
                table: "leaderboards",
                column: "rankings")
                .Annotation("Npgsql:IndexMethod", "gin");
        }


        protected void CreateSeasonsTable(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "seasons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    season_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_size = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    participating_teams = table.Column<List<string>>(type: "jsonb", nullable: false),
                    season_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    config_data = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seasons", x => x.id);
                });
        }


        protected void CreateSeasonGroupsTable(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "season_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "varchar(255)", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_season_groups", x => x.id);
                });
        }

    }
}
