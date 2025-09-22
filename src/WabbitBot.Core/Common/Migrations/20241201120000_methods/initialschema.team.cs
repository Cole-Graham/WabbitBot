using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WabbitBot.Core.Common.Migrations
{
    public partial class InitialSchema : Migration
    {
        protected void CreateTeamsTable(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "varchar(255)", nullable: false),
                    team_captain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    max_roster_size = table.Column<int>(type: "integer", nullable: false),
                    roster = table.Column<List<object>>(type: "jsonb", nullable: false),
                    stats = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    last_active = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tag = table.Column<string>(type: "varchar(50)", nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_teams", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_teams_roster",
                table: "teams",
                column: "roster")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_teams_name",
                table: "teams",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_teams_stats",
                table: "teams",
                column: "stats")
                .Annotation("Npgsql:IndexMethod", "gin");
        }


    }
}
