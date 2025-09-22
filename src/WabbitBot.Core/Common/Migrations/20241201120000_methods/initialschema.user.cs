using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WabbitBot.Core.Common.Migrations
{
    public partial class InitialSchema : Migration
    {
        protected void CreateUsersTable(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    discord_id = table.Column<string>(type: "text", nullable: false),
                    username = table.Column<string>(type: "varchar(255)", nullable: false),
                    nickname = table.Column<string>(type: "varchar(255)", nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_active = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    player_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.UniqueConstraint("ak_users_discord_id", x => x.discord_id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_users_discord_id",
                table: "users",
                column: "discord_id");

            migrationBuilder.CreateIndex(
                name: "idx_users_username",
                table: "users",
                column: "username");

            migrationBuilder.CreateIndex(
                name: "idx_users_is_active",
                table: "users",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "idx_users_player_id",
                table: "users",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "idx_users_last_active",
                table: "users",
                column: "last_active");
        }

    }
}
