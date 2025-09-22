using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WabbitBot.Core.Common.Migrations
{
    public partial class InitialSchema : Migration
    {
        protected void CreatePlayersTable(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "varchar(255)", nullable: false),
                    last_active = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    team_ids = table.Column<List<Guid>>(type: "jsonb", nullable: false),
                    previous_user_ids = table.Column<Dictionary<string, List<string>>>(type: "jsonb", nullable: false),
                    game_username = table.Column<string>(type: "varchar(255)", nullable: true),
                    previous_game_usernames = table.Column<List<string>>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_players", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_players_team_ids",
                table: "players",
                column: "team_ids")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_players_name",
                table: "players",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_players_is_archived",
                table: "players",
                column: "is_archived");
        }

    }
}
