using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WabbitBot.Core.Common.Migrations
{
    public partial class InitialSchema : Migration
    {
        protected void CreateMapsTable(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "maps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "varchar(255)", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    size = table.Column<string>(type: "text", nullable: true),
                    is_in_random_pool = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_in_tournament_pool = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    thumbnail_filename = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_maps", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_maps_name",
                table: "maps",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_maps_size",
                table: "maps",
                column: "size");

            migrationBuilder.CreateIndex(
                name: "idx_maps_is_active",
                table: "maps",
                column: "is_active");
        }

    }
}
