using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WabbitBot.Core.Common.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            CreateGamesTable(migrationBuilder);

            CreateLeaderboardsTable(migrationBuilder);

            CreateSeasonGroupsTable(migrationBuilder);

            CreateSeasonsTable(migrationBuilder);

            CreateMapsTable(migrationBuilder);

            CreateMatchStateSnapshotsTable(migrationBuilder);

            CreateMatchesTable(migrationBuilder);

            CreatePlayersTable(migrationBuilder);

            CreateProvenPotentialRecordsTable(migrationBuilder);

            CreateScrimmagesTable(migrationBuilder);


            CreateTeamsTable(migrationBuilder);

            CreateTournamentStateSnapshotsTable(migrationBuilder);

            CreateTournamentsTable(migrationBuilder);

            CreateUsersTable(migrationBuilder);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "games");

            migrationBuilder.DropTable(
                name: "game_state_snapshots");

            migrationBuilder.DropTable(
                name: "leaderboards");

            migrationBuilder.DropTable(
                name: "maps");

            migrationBuilder.DropTable(
                name: "match_state_snapshots");

            migrationBuilder.DropTable(
                name: "matches");

            migrationBuilder.DropTable(
                name: "players");

            migrationBuilder.DropTable(
                name: "proven_potential_records");

            migrationBuilder.DropTable(
                name: "scrimmages");

            migrationBuilder.DropTable(
                name: "season_configs");

            migrationBuilder.DropTable(
                name: "season_groups");

            migrationBuilder.DropTable(
                name: "seasons");


            migrationBuilder.DropTable(
                name: "teams");

            migrationBuilder.DropTable(
                name: "tournament_state_snapshots");

            migrationBuilder.DropTable(
                name: "tournaments");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
