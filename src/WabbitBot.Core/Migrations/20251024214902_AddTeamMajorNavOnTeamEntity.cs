using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WabbitBot.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamMajorNavOnTeamEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(name: "idx_teams_team_major_id", table: "teams", column: "team_major_id");

            migrationBuilder.AddForeignKey(
                name: "FK_teams_players_team_major_id",
                table: "teams",
                column: "team_major_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_teams_players_team_major_id", table: "teams");

            migrationBuilder.DropIndex(name: "idx_teams_team_major_id", table: "teams");
        }
    }
}
