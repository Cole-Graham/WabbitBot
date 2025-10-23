using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using WabbitBot.Core.Common.Models.Common;

#nullable disable

namespace WabbitBot.Core.Migrations
{
    /// <inheritdoc />
    public partial class TeamMemberTemporalConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
            migrationBuilder.CreateTable(
                name: "division_learning_curves_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    division_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: true),
                    model_type = table.Column<int>(type: "integer", nullable: false),
                    parameters = table.Column<Dictionary<string, double>>(type: "jsonb", nullable: false),
                    r_squared = table.Column<double>(type: "double precision", nullable: false),
                    total_games_played = table.Column<int>(type: "integer", nullable: false),
                    total_games_won = table.Column<int>(type: "integer", nullable: false),
                    last_recalculated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_reliable = table.Column<bool>(type: "boolean", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_division_learning_curves_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "division_map_stats_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    division_id = table.Column<Guid>(type: "uuid", nullable: false),
                    map_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: true),
                    season_id = table.Column<Guid>(type: "uuid", nullable: true),
                    games_played = table.Column<int>(type: "integer", nullable: false),
                    games_won = table.Column<int>(type: "integer", nullable: false),
                    games_lost = table.Column<int>(type: "integer", nullable: false),
                    winrate = table.Column<double>(type: "double precision", nullable: false),
                    average_game_duration_minutes = table.Column<double>(type: "double precision", nullable: false),
                    total_game_duration_minutes = table.Column<double>(type: "double precision", nullable: false),
                    game_length_performance = table.Column<Dictionary<int, GameLengthBucket>>(
                        type: "jsonb",
                        nullable: false
                    ),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_division_map_stats_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "division_stats_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    division_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: true),
                    season_id = table.Column<Guid>(type: "uuid", nullable: true),
                    games_played = table.Column<int>(type: "integer", nullable: false),
                    games_won = table.Column<int>(type: "integer", nullable: false),
                    games_lost = table.Column<int>(type: "integer", nullable: false),
                    winrate = table.Column<double>(type: "double precision", nullable: false),
                    average_game_duration_minutes = table.Column<double>(type: "double precision", nullable: false),
                    total_game_duration_minutes = table.Column<double>(type: "double precision", nullable: false),
                    density_performance = table.Column<Dictionary<MapDensity, DensityStats>>(
                        type: "jsonb",
                        nullable: false
                    ),
                    game_length_performance = table.Column<Dictionary<int, GameLengthBucket>>(
                        type: "jsonb",
                        nullable: false
                    ),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    additional_metrics = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_division_stats_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "divisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    faction = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    icon_filename = table.Column<string>(type: "text", nullable: true),
                    stats_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    map_stats_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    learning_curve_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_divisions", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "divisions_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    faction = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    icon_filename = table.Column<string>(type: "text", nullable: true),
                    stats_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    map_stats_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    learning_curve_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_divisions_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "game_state_snapshots_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    triggered_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    triggered_by_user_name = table.Column<string>(type: "text", nullable: false),
                    additional_data = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    player_deck_codes = table.Column<Dictionary<Guid, string>>(type: "jsonb", nullable: false),
                    player_deck_submitted_at = table.Column<Dictionary<Guid, DateTime>>(type: "jsonb", nullable: false),
                    player_deck_confirmed = table.Column<HashSet<Guid>>(type: "uuid[]", nullable: false),
                    player_deck_confirmed_at = table.Column<Dictionary<Guid, DateTime>>(type: "jsonb", nullable: false),
                    map_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    team1_player_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    team2_player_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    game_number = table.Column<int>(type: "integer", nullable: false),
                    team1_division_id = table.Column<Guid>(type: "uuid", nullable: true),
                    team2_division_id = table.Column<Guid>(type: "uuid", nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_state_snapshots_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "games_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    map_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team1_division_id = table.Column<Guid>(type: "uuid", nullable: true),
                    team2_division_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state_history_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    replay_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    game_number = table.Column<int>(type: "integer", nullable: false),
                    team1_player_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    team2_player_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    team1_game_container_msg_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    team2_game_container_msg_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_games_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "maps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    scenario_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    size = table.Column<string>(type: "text", nullable: false),
                    density = table.Column<int>(type: "integer", nullable: false),
                    is_in_random_pool = table.Column<bool>(type: "boolean", nullable: false),
                    is_in_tournament_pool = table.Column<bool>(type: "boolean", nullable: false),
                    thumbnail_filename = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maps", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "maps_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    scenario_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    size = table.Column<string>(type: "text", nullable: false),
                    density = table.Column<int>(type: "integer", nullable: false),
                    is_in_random_pool = table.Column<bool>(type: "boolean", nullable: false),
                    is_in_tournament_pool = table.Column<bool>(type: "boolean", nullable: false),
                    thumbnail_filename = table.Column<string>(type: "text", nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maps_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "mashina_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: true),
                    discord_user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    previous_discord_user_ids = table.Column<List<ulong>>(type: "jsonb", nullable: false),
                    discord_username = table.Column<string>(type: "text", nullable: true),
                    previous_discord_usernames = table.Column<List<string>>(type: "jsonb", nullable: false),
                    discord_globalname = table.Column<string>(type: "text", nullable: true),
                    previous_discord_globalnames = table.Column<List<string>>(type: "jsonb", nullable: false),
                    discord_mention = table.Column<string>(type: "text", nullable: true),
                    previous_discord_mentions = table.Column<List<string>>(type: "jsonb", nullable: false),
                    discord_avatar_url = table.Column<string>(type: "text", nullable: true),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_active = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mashina_users", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "mashina_users_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    player_id = table.Column<Guid>(type: "uuid", nullable: true),
                    discord_user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    previous_discord_user_ids = table.Column<List<ulong>>(type: "jsonb", nullable: false),
                    discord_username = table.Column<string>(type: "text", nullable: true),
                    previous_discord_usernames = table.Column<List<string>>(type: "jsonb", nullable: false),
                    discord_globalname = table.Column<string>(type: "text", nullable: true),
                    previous_discord_globalnames = table.Column<List<string>>(type: "jsonb", nullable: false),
                    discord_mention = table.Column<string>(type: "text", nullable: true),
                    previous_discord_mentions = table.Column<List<string>>(type: "jsonb", nullable: false),
                    discord_avatar_url = table.Column<string>(type: "text", nullable: true),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_active = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mashina_users_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "match_state_snapshots_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    triggered_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    triggered_by_user_name = table.Column<string>(type: "text", nullable: false),
                    additional_data = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    current_game_number = table.Column<int>(type: "integer", nullable: false),
                    current_map_id = table.Column<Guid>(type: "uuid", nullable: true),
                    final_score = table.Column<string>(type: "text", nullable: true),
                    available_maps = table.Column<List<string>>(type: "jsonb", nullable: false),
                    final_map_pool = table.Column<List<string>>(type: "jsonb", nullable: false),
                    team1_map_bans = table.Column<List<string>>(type: "jsonb", nullable: false),
                    team2_map_bans = table.Column<List<string>>(type: "jsonb", nullable: false),
                    team1_bans_submitted = table.Column<bool>(type: "boolean", nullable: false),
                    team2_bans_submitted = table.Column<bool>(type: "boolean", nullable: false),
                    team1_bans_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    team2_bans_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_state_snapshots_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "matches_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    team1_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team2_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state_history_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    game_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    winner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_type = table.Column<int>(type: "integer", nullable: true),
                    best_of = table.Column<int>(type: "integer", nullable: false),
                    play_to_completion = table.Column<bool>(type: "boolean", nullable: false),
                    available_maps = table.Column<List<string>>(type: "jsonb", nullable: false),
                    final_map_pool = table.Column<List<string>>(type: "jsonb", nullable: false),
                    team1_map_bans = table.Column<List<string>>(type: "jsonb", nullable: false),
                    team2_map_bans = table.Column<List<string>>(type: "jsonb", nullable: false),
                    team1_map_bans_confirmed_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    team2_map_bans_confirmed_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    team1_thread_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    team2_thread_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    team1_overview_container_msg_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    team2_overview_container_msg_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    team1_match_results_msg_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    team2_match_results_msg_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matches_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "players_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    mashina_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    last_active = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    team_join_limit = table.Column<int>(type: "integer", nullable: false),
                    team_join_cooldowns = table.Column<Dictionary<Guid, DateTime>>(type: "jsonb", nullable: false),
                    team_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    current_platform_ids = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    previous_platform_ids = table.Column<Dictionary<string, List<string>>>(
                        type: "jsonb",
                        nullable: false
                    ),
                    current_steam_username = table.Column<string>(type: "text", nullable: true),
                    previous_steam_usernames = table.Column<List<string>>(type: "jsonb", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_players_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "proven_potential_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    original_match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    challenger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opponent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    new_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    established_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trigger_match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    applied_at_match_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tracking_end_match_id = table.Column<Guid>(type: "uuid", nullable: true),
                    new_player_match_count_at_creation = table.Column<int>(type: "integer", nullable: false),
                    crossed_thresholds = table.Column<int>(type: "integer", nullable: false),
                    closure_fraction = table.Column<double>(type: "double precision", nullable: true),
                    scaling_applied = table.Column<double>(type: "double precision", nullable: true),
                    adjusted_new_change = table.Column<double>(type: "double precision", nullable: true),
                    adjusted_established_change = table.Column<double>(type: "double precision", nullable: true),
                    challenger_rating = table.Column<double>(type: "double precision", nullable: false),
                    opponent_rating = table.Column<double>(type: "double precision", nullable: false),
                    challenger_confidence = table.Column<double>(type: "double precision", nullable: false),
                    opponent_confidence = table.Column<double>(type: "double precision", nullable: false),
                    applied_thresholds = table.Column<HashSet<double>>(type: "jsonb", nullable: false),
                    challenger_original_rating_change = table.Column<double>(type: "double precision", nullable: false),
                    opponent_original_rating_change = table.Column<double>(type: "double precision", nullable: false),
                    rating_adjustment = table.Column<double>(type: "double precision", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    last_checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_complete = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proven_potential_records", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "proven_potential_records_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    original_match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    challenger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opponent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    new_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    established_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trigger_match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    applied_at_match_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tracking_end_match_id = table.Column<Guid>(type: "uuid", nullable: true),
                    new_player_match_count_at_creation = table.Column<int>(type: "integer", nullable: false),
                    crossed_thresholds = table.Column<int>(type: "integer", nullable: false),
                    closure_fraction = table.Column<double>(type: "double precision", nullable: true),
                    scaling_applied = table.Column<double>(type: "double precision", nullable: true),
                    adjusted_new_change = table.Column<double>(type: "double precision", nullable: true),
                    adjusted_established_change = table.Column<double>(type: "double precision", nullable: true),
                    challenger_rating = table.Column<double>(type: "double precision", nullable: false),
                    opponent_rating = table.Column<double>(type: "double precision", nullable: false),
                    challenger_confidence = table.Column<double>(type: "double precision", nullable: false),
                    opponent_confidence = table.Column<double>(type: "double precision", nullable: false),
                    applied_thresholds = table.Column<HashSet<double>>(type: "jsonb", nullable: false),
                    challenger_original_rating_change = table.Column<double>(type: "double precision", nullable: false),
                    opponent_original_rating_change = table.Column<double>(type: "double precision", nullable: false),
                    rating_adjustment = table.Column<double>(type: "double precision", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    last_checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_complete = table.Column<bool>(type: "boolean", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proven_potential_records_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "rating_percentile_breakpoints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: true),
                    calculated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_players_in_sample = table.Column<int>(type: "integer", nullable: false),
                    breakpoints = table.Column<Dictionary<RatingTier, double>>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rating_percentile_breakpoints", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "rating_percentile_breakpoints_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: true),
                    calculated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_players_in_sample = table.Column<int>(type: "integer", nullable: false),
                    breakpoints = table.Column<Dictionary<RatingTier, double>>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rating_percentile_breakpoints_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "replay_players_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    replay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_user_id = table.Column<string>(type: "text", nullable: false),
                    player_name = table.Column<string>(type: "text", nullable: false),
                    player_elo = table.Column<string>(type: "text", nullable: true),
                    player_level = table.Column<string>(type: "text", nullable: true),
                    player_alliance = table.Column<string>(type: "text", nullable: false),
                    player_score_limit = table.Column<string>(type: "text", nullable: true),
                    player_income_rate = table.Column<string>(type: "text", nullable: true),
                    player_avatar = table.Column<string>(type: "text", nullable: true),
                    player_ready = table.Column<string>(type: "text", nullable: true),
                    player_deck_content = table.Column<string>(type: "text", nullable: true),
                    player_deck_name = table.Column<string>(type: "text", nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_replay_players_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "replays_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    player_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_mode = table.Column<string>(type: "text", nullable: false),
                    allow_observers = table.Column<string>(type: "text", nullable: true),
                    observer_delay = table.Column<string>(type: "text", nullable: true),
                    seed = table.Column<string>(type: "text", nullable: true),
                    @private = table.Column<string>(name: "private", type: "text", nullable: true),
                    server_name = table.Column<string>(type: "text", nullable: true),
                    unique_session_id = table.Column<string>(type: "text", nullable: true),
                    mod_list = table.Column<string>(type: "text", nullable: true),
                    mod_tag_list = table.Column<string>(type: "text", nullable: true),
                    environment_settings = table.Column<string>(type: "text", nullable: true),
                    game_type = table.Column<string>(type: "text", nullable: true),
                    map = table.Column<string>(type: "text", nullable: false),
                    init_money = table.Column<string>(type: "text", nullable: true),
                    time_limit = table.Column<string>(type: "text", nullable: true),
                    score_limit = table.Column<string>(type: "text", nullable: true),
                    combat_rule = table.Column<string>(type: "text", nullable: true),
                    income_rate = table.Column<string>(type: "text", nullable: true),
                    upkeep = table.Column<string>(type: "text", nullable: true),
                    original_filename = table.Column<string>(type: "text", nullable: true),
                    file_path = table.Column<string>(type: "text", nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    victory_code = table.Column<string>(type: "text", nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_replays_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "schema_metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    schema_version = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false
                    ),
                    applied_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    applied_by = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_breaking_change = table.Column<bool>(type: "boolean", nullable: false),
                    compatibility_notes = table.Column<string>(
                        type: "character varying(1000)",
                        maxLength: 1000,
                        nullable: true
                    ),
                    migration_name = table.Column<string>(
                        type: "character varying(255)",
                        maxLength: 255,
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schema_metadata", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "scrimmage_challenges_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    challenger_team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opponent_team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issued_by_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accepted_by_player_id = table.Column<Guid>(type: "uuid", nullable: true),
                    challenger_teammate_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    opponent_teammate_ids = table.Column<Guid[]>(type: "uuid[]", nullable: true),
                    challenge_status = table.Column<int>(type: "integer", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    best_of = table.Column<int>(type: "integer", nullable: false),
                    challenge_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    challenge_message_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    challenge_channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrimmage_challenges_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "scrimmage_leaderboard_items_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    scrimmage_leaderboard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    wins = table.Column<int>(type: "integer", nullable: false),
                    losses = table.Column<int>(type: "integer", nullable: false),
                    draws = table.Column<int>(type: "integer", nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    rating = table.Column<double>(type: "double precision", nullable: false),
                    recent_rating_change = table.Column<double>(type: "double precision", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrimmage_leaderboard_items_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "scrimmage_leaderboards_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leaderboard_item_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrimmage_leaderboards_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "scrimmage_state_snapshots_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    scrimmage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrimmage_state_snapshots_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "scrimmage_team_stats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    opponent_encounter_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    wins = table.Column<int>(type: "integer", nullable: false),
                    losses = table.Column<int>(type: "integer", nullable: false),
                    draws = table.Column<int>(type: "integer", nullable: false),
                    initial_rating = table.Column<double>(type: "double precision", nullable: false),
                    current_rating = table.Column<double>(type: "double precision", nullable: false),
                    highest_rating = table.Column<double>(type: "double precision", nullable: false),
                    recent_rating_change = table.Column<double>(type: "double precision", nullable: false),
                    confidence = table.Column<double>(type: "double precision", nullable: false),
                    current_streak = table.Column<int>(type: "integer", nullable: false),
                    longest_streak = table.Column<int>(type: "integer", nullable: false),
                    last_match_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrimmage_team_stats", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "scrimmage_team_stats_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    opponent_encounter_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    wins = table.Column<int>(type: "integer", nullable: false),
                    losses = table.Column<int>(type: "integer", nullable: false),
                    draws = table.Column<int>(type: "integer", nullable: false),
                    initial_rating = table.Column<double>(type: "double precision", nullable: false),
                    current_rating = table.Column<double>(type: "double precision", nullable: false),
                    highest_rating = table.Column<double>(type: "double precision", nullable: false),
                    recent_rating_change = table.Column<double>(type: "double precision", nullable: false),
                    confidence = table.Column<double>(type: "double precision", nullable: false),
                    current_streak = table.Column<int>(type: "integer", nullable: false),
                    longest_streak = table.Column<int>(type: "integer", nullable: false),
                    last_match_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrimmage_team_stats_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "scrimmages_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    scrimmage_challenge_id = table.Column<Guid>(type: "uuid", nullable: false),
                    challenger_team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opponent_team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issued_by_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accepted_by_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: true),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    best_of = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    winner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    challenger_team_rating = table.Column<double>(type: "double precision", nullable: false),
                    opponent_team_rating = table.Column<double>(type: "double precision", nullable: false),
                    challenger_team_rating_change = table.Column<double>(type: "double precision", nullable: true),
                    opponent_team_rating_change = table.Column<double>(type: "double precision", nullable: true),
                    challenger_team_confidence = table.Column<double>(type: "double precision", nullable: false),
                    opponent_team_confidence = table.Column<double>(type: "double precision", nullable: false),
                    challenger_team_score = table.Column<int>(type: "integer", nullable: true),
                    opponent_team_score = table.Column<int>(type: "integer", nullable: true),
                    challenger_team_variety_bonus_used = table.Column<double>(type: "double precision", nullable: true),
                    opponent_team_variety_bonus_used = table.Column<double>(type: "double precision", nullable: true),
                    challenger_team_multiplier_used = table.Column<double>(type: "double precision", nullable: true),
                    opponent_team_multiplier_used = table.Column<double>(type: "double precision", nullable: true),
                    higher_rated_team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating_range_at_match = table.Column<double>(type: "double precision", nullable: false),
                    challenger_team_gap_scaling_applied_value = table.Column<double>(
                        type: "double precision",
                        nullable: true
                    ),
                    opponent_team_gap_scaling_applied_value = table.Column<double>(
                        type: "double precision",
                        nullable: true
                    ),
                    challenger_team_catch_up_bonus_used = table.Column<double>(
                        type: "double precision",
                        nullable: true
                    ),
                    opponent_team_catch_up_bonus_used = table.Column<double>(type: "double precision", nullable: true),
                    challenger_team_adjusted_rating_change = table.Column<double>(
                        type: "double precision",
                        nullable: true
                    ),
                    opponent_team_adjusted_rating_change = table.Column<double>(
                        type: "double precision",
                        nullable: true
                    ),
                    challenger_team_proven_potential_applied = table.Column<bool>(type: "boolean", nullable: false),
                    opponent_team_proven_potential_applied = table.Column<bool>(type: "boolean", nullable: false),
                    challenger_team_proven_potential_applied_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    opponent_team_proven_potential_applied_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrimmages_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "season_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reset_scrimmage_ratings_on_start = table.Column<bool>(type: "boolean", nullable: false),
                    reset_tournament_ratings_on_start = table.Column<bool>(type: "boolean", nullable: false),
                    scrimmage_rating_decay = table.Column<bool>(type: "boolean", nullable: false),
                    tournament_rating_decay = table.Column<bool>(type: "boolean", nullable: false),
                    scrimmage_decay_rate_per_week = table.Column<double>(type: "double precision", nullable: false),
                    tournament_decay_rate_per_week = table.Column<double>(type: "double precision", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_season_configs", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "season_configs_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    reset_scrimmage_ratings_on_start = table.Column<bool>(type: "boolean", nullable: false),
                    reset_tournament_ratings_on_start = table.Column<bool>(type: "boolean", nullable: false),
                    scrimmage_rating_decay = table.Column<bool>(type: "boolean", nullable: false),
                    tournament_rating_decay = table.Column<bool>(type: "boolean", nullable: false),
                    scrimmage_decay_rate_per_week = table.Column<double>(type: "double precision", nullable: false),
                    tournament_decay_rate_per_week = table.Column<double>(type: "double precision", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_season_configs_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "seasons_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    season_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scrimmage_leaderboard_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    tournament_leaderboard_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seasons_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "team_members_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    team_roster_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mashina_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discord_user_id = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: true),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_roster_manager = table.Column<bool>(type: "boolean", nullable: false),
                    receive_scrimmage_pings = table.Column<bool>(type: "boolean", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_members_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "team_opponent_encounters_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opponent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    encountered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    won = table.Column<bool>(type: "boolean", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_opponent_encounters_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "team_rosters_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    roster_group = table.Column<int>(type: "integer", nullable: false),
                    max_roster_size = table.Column<int>(type: "integer", nullable: false),
                    last_active = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    core_role_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    captain_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_rosters_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "team_variety_stats_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    variety_entropy = table.Column<double>(type: "double precision", nullable: false),
                    variety_bonus = table.Column<double>(type: "double precision", nullable: false),
                    total_opponents = table.Column<int>(type: "integer", nullable: false),
                    unique_opponents = table.Column<int>(type: "integer", nullable: false),
                    last_calculated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    average_variety_entropy_at_calc = table.Column<double>(type: "double precision", nullable: false),
                    median_games_at_calc = table.Column<double>(type: "double precision", nullable: false),
                    rating_range_at_calc = table.Column<double>(type: "double precision", nullable: false),
                    neighbor_range_at_calc = table.Column<double>(type: "double precision", nullable: false),
                    player_neighbors_at_calc = table.Column<int>(type: "integer", nullable: false),
                    max_neighbors_observed_at_calc = table.Column<int>(type: "integer", nullable: false),
                    availability_factor_used = table.Column<double>(type: "double precision", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_variety_stats_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scrimmage_team_stats = table.Column<Dictionary<TeamSize, ScrimmageTeamStats>>(
                        type: "jsonb",
                        nullable: false
                    ),
                    tournament_team_stats = table.Column<Dictionary<TeamSize, TournamentTeamStats>>(
                        type: "jsonb",
                        nullable: false
                    ),
                    name = table.Column<string>(type: "text", nullable: false),
                    team_major_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_active = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tag = table.Column<string>(type: "text", nullable: true),
                    team_type = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "teams_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    scrimmage_team_stats = table.Column<Dictionary<TeamSize, ScrimmageTeamStats>>(
                        type: "jsonb",
                        nullable: false
                    ),
                    tournament_team_stats = table.Column<Dictionary<TeamSize, TournamentTeamStats>>(
                        type: "jsonb",
                        nullable: false
                    ),
                    name = table.Column<string>(type: "text", nullable: false),
                    team_major_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_active = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tag = table.Column<string>(type: "text", nullable: true),
                    team_type = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "tournament_leaderboard_items_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    tournament_leaderboard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    tournament_points = table.Column<int>(type: "integer", nullable: false),
                    tournament_placements = table.Column<List<int>>(type: "jsonb", nullable: false),
                    tournaments_played_count = table.Column<int>(type: "integer", nullable: false),
                    average_placement = table.Column<double>(type: "double precision", nullable: false),
                    best_placement = table.Column<int>(type: "integer", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    rating = table.Column<double>(type: "double precision", nullable: false),
                    recent_rating_change = table.Column<double>(type: "double precision", nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_leaderboard_items_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "tournament_leaderboards_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    leaderboard_item_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_leaderboards_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "tournament_state_snapshots_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    tournament_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    triggered_by_mashina_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    additional_data = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    registration_opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    max_participants = table.Column<int>(type: "integer", nullable: false),
                    winner_team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_by_mashina_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancellation_reason = table.Column<string>(type: "text", nullable: true),
                    registered_team_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    participant_team_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    active_match_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    completed_match_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    all_match_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    final_rankings = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    current_participant_count = table.Column<int>(type: "integer", nullable: false),
                    current_round = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_state_snapshots_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "tournament_team_stats_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tournament_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    games_won = table.Column<int>(type: "integer", nullable: false),
                    games_lost = table.Column<int>(type: "integer", nullable: false),
                    games_drawn = table.Column<int>(type: "integer", nullable: false),
                    matches_won = table.Column<int>(type: "integer", nullable: false),
                    matches_lost = table.Column<int>(type: "integer", nullable: false),
                    initial_rating = table.Column<double>(type: "double precision", nullable: false),
                    current_rating = table.Column<double>(type: "double precision", nullable: false),
                    highest_rating = table.Column<double>(type: "double precision", nullable: false),
                    recent_rating_change = table.Column<double>(type: "double precision", nullable: false),
                    confidence = table.Column<double>(type: "double precision", nullable: false),
                    last_match_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_team_stats_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "tournaments_archive",
                columns: table => new
                {
                    archive_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    max_participants = table.Column<int>(type: "integer", nullable: false),
                    best_of = table.Column<int>(type: "integer", nullable: false),
                    state_history_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournaments_archive", x => x.archive_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "division_stats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    division_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: true),
                    season_id = table.Column<Guid>(type: "uuid", nullable: true),
                    games_played = table.Column<int>(type: "integer", nullable: false),
                    games_won = table.Column<int>(type: "integer", nullable: false),
                    games_lost = table.Column<int>(type: "integer", nullable: false),
                    winrate = table.Column<double>(type: "double precision", nullable: false),
                    average_game_duration_minutes = table.Column<double>(type: "double precision", nullable: false),
                    total_game_duration_minutes = table.Column<double>(type: "double precision", nullable: false),
                    density_performance = table.Column<Dictionary<MapDensity, DensityStats>>(
                        type: "jsonb",
                        nullable: false
                    ),
                    game_length_performance = table.Column<Dictionary<int, GameLengthBucket>>(
                        type: "jsonb",
                        nullable: false
                    ),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    additional_metrics = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_division_stats", x => x.id);
                    table.ForeignKey(
                        name: "FK_division_stats_divisions_division_id",
                        column: x => x.division_id,
                        principalTable: "divisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "division_map_stats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    division_id = table.Column<Guid>(type: "uuid", nullable: false),
                    map_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: true),
                    season_id = table.Column<Guid>(type: "uuid", nullable: true),
                    games_played = table.Column<int>(type: "integer", nullable: false),
                    games_won = table.Column<int>(type: "integer", nullable: false),
                    games_lost = table.Column<int>(type: "integer", nullable: false),
                    winrate = table.Column<double>(type: "double precision", nullable: false),
                    average_game_duration_minutes = table.Column<double>(type: "double precision", nullable: false),
                    total_game_duration_minutes = table.Column<double>(type: "double precision", nullable: false),
                    game_length_performance = table.Column<Dictionary<int, GameLengthBucket>>(
                        type: "jsonb",
                        nullable: false
                    ),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_division_map_stats", x => x.id);
                    table.ForeignKey(
                        name: "FK_division_map_stats_divisions_division_id",
                        column: x => x.division_id,
                        principalTable: "divisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_division_map_stats_maps_map_id",
                        column: x => x.map_id,
                        principalTable: "maps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "seasons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scrimmage_leaderboard_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    tournament_leaderboard_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seasons", x => x.id);
                    table.ForeignKey(
                        name: "FK_seasons_season_configs_season_config_id",
                        column: x => x.season_config_id,
                        principalTable: "season_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "division_learning_curves",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    division_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: true),
                    model_type = table.Column<int>(type: "integer", nullable: false),
                    parameters = table.Column<Dictionary<string, double>>(type: "jsonb", nullable: false),
                    r_squared = table.Column<double>(type: "double precision", nullable: false),
                    total_games_played = table.Column<int>(type: "integer", nullable: false),
                    total_games_won = table.Column<int>(type: "integer", nullable: false),
                    last_recalculated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_reliable = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_division_learning_curves", x => x.id);
                    table.ForeignKey(
                        name: "FK_division_learning_curves_divisions_division_id",
                        column: x => x.division_id,
                        principalTable: "divisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_division_learning_curves_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "matches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team1_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team2_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state_history_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    game_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    winner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_type = table.Column<int>(type: "integer", nullable: true),
                    best_of = table.Column<int>(type: "integer", nullable: false),
                    play_to_completion = table.Column<bool>(type: "boolean", nullable: false),
                    available_maps = table.Column<List<string>>(type: "jsonb", nullable: false),
                    final_map_pool = table.Column<List<string>>(type: "jsonb", nullable: false),
                    team1_map_bans = table.Column<List<string>>(type: "jsonb", nullable: false),
                    team2_map_bans = table.Column<List<string>>(type: "jsonb", nullable: false),
                    team1_map_bans_confirmed_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    team2_map_bans_confirmed_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    team1_thread_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    team2_thread_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    team1_overview_container_msg_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    team2_overview_container_msg_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    team1_match_results_msg_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    team2_match_results_msg_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matches", x => x.id);
                    table.ForeignKey(
                        name: "FK_matches_teams_team1_id",
                        column: x => x.team1_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_matches_teams_team2_id",
                        column: x => x.team2_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "team_rosters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    roster_group = table.Column<int>(type: "integer", nullable: false),
                    max_roster_size = table.Column<int>(type: "integer", nullable: false),
                    last_active = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    core_role_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    captain_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_rosters", x => x.id);
                    table.ForeignKey(
                        name: "FK_team_rosters_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "team_variety_stats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    variety_entropy = table.Column<double>(type: "double precision", nullable: false),
                    variety_bonus = table.Column<double>(type: "double precision", nullable: false),
                    total_opponents = table.Column<int>(type: "integer", nullable: false),
                    unique_opponents = table.Column<int>(type: "integer", nullable: false),
                    last_calculated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    average_variety_entropy_at_calc = table.Column<double>(type: "double precision", nullable: false),
                    median_games_at_calc = table.Column<double>(type: "double precision", nullable: false),
                    rating_range_at_calc = table.Column<double>(type: "double precision", nullable: false),
                    neighbor_range_at_calc = table.Column<double>(type: "double precision", nullable: false),
                    player_neighbors_at_calc = table.Column<int>(type: "integer", nullable: false),
                    max_neighbors_observed_at_calc = table.Column<int>(type: "integer", nullable: false),
                    availability_factor_used = table.Column<double>(type: "double precision", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_variety_stats", x => x.id);
                    table.ForeignKey(
                        name: "FK_team_variety_stats_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "tournament_team_stats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tournament_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    games_won = table.Column<int>(type: "integer", nullable: false),
                    games_lost = table.Column<int>(type: "integer", nullable: false),
                    games_drawn = table.Column<int>(type: "integer", nullable: false),
                    matches_won = table.Column<int>(type: "integer", nullable: false),
                    matches_lost = table.Column<int>(type: "integer", nullable: false),
                    initial_rating = table.Column<double>(type: "double precision", nullable: false),
                    current_rating = table.Column<double>(type: "double precision", nullable: false),
                    highest_rating = table.Column<double>(type: "double precision", nullable: false),
                    recent_rating_change = table.Column<double>(type: "double precision", nullable: false),
                    confidence = table.Column<double>(type: "double precision", nullable: false),
                    last_match_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_team_stats", x => x.id);
                    table.ForeignKey(
                        name: "FK_tournament_team_stats_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "scrimmage_leaderboards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leaderboard_item_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrimmage_leaderboards", x => x.id);
                    table.ForeignKey(
                        name: "FK_scrimmage_leaderboards_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "tournament_leaderboards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    leaderboard_item_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_leaderboards", x => x.id);
                    table.ForeignKey(
                        name: "FK_tournament_leaderboards_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "games",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    map_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team1_division_id = table.Column<Guid>(type: "uuid", nullable: true),
                    team2_division_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state_history_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    replay_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    game_number = table.Column<int>(type: "integer", nullable: false),
                    team1_player_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    team2_player_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    team1_game_container_msg_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    team2_game_container_msg_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_games", x => x.id);
                    table.ForeignKey(
                        name: "FK_games_divisions_team1_division_id",
                        column: x => x.team1_division_id,
                        principalTable: "divisions",
                        principalColumn: "id"
                    );
                    table.ForeignKey(
                        name: "FK_games_divisions_team2_division_id",
                        column: x => x.team2_division_id,
                        principalTable: "divisions",
                        principalColumn: "id"
                    );
                    table.ForeignKey(
                        name: "FK_games_maps_map_id",
                        column: x => x.map_id,
                        principalTable: "maps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_games_matches_match_id",
                        column: x => x.match_id,
                        principalTable: "matches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "match_state_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    triggered_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    triggered_by_user_name = table.Column<string>(type: "text", nullable: false),
                    additional_data = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    current_game_number = table.Column<int>(type: "integer", nullable: false),
                    current_map_id = table.Column<Guid>(type: "uuid", nullable: true),
                    final_score = table.Column<string>(type: "text", nullable: true),
                    available_maps = table.Column<List<string>>(type: "jsonb", nullable: false),
                    final_map_pool = table.Column<List<string>>(type: "jsonb", nullable: false),
                    team1_map_bans = table.Column<List<string>>(type: "jsonb", nullable: false),
                    team2_map_bans = table.Column<List<string>>(type: "jsonb", nullable: false),
                    team1_bans_submitted = table.Column<bool>(type: "boolean", nullable: false),
                    team2_bans_submitted = table.Column<bool>(type: "boolean", nullable: false),
                    team1_bans_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    team2_bans_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_state_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_match_state_snapshots_matches_match_id",
                        column: x => x.match_id,
                        principalTable: "matches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "team_opponent_encounters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opponent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    encountered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    won = table.Column<bool>(type: "boolean", nullable: false),
                    ScrimmageTeamStatsId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_opponent_encounters", x => x.id);
                    table.ForeignKey(
                        name: "FK_team_opponent_encounters_matches_match_id",
                        column: x => x.match_id,
                        principalTable: "matches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_team_opponent_encounters_scrimmage_team_stats_ScrimmageTeam~",
                        column: x => x.ScrimmageTeamStatsId,
                        principalTable: "scrimmage_team_stats",
                        principalColumn: "id"
                    );
                    table.ForeignKey(
                        name: "FK_team_opponent_encounters_teams_opponent_id",
                        column: x => x.opponent_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_team_opponent_encounters_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "team_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_roster_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mashina_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discord_user_id = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: true),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_roster_manager = table.Column<bool>(type: "boolean", nullable: false),
                    receive_scrimmage_pings = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_team_members_mashina_users_mashina_user_id",
                        column: x => x.mashina_user_id,
                        principalTable: "mashina_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_team_members_team_rosters_team_roster_id",
                        column: x => x.team_roster_id,
                        principalTable: "team_rosters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "tournaments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    max_participants = table.Column<int>(type: "integer", nullable: false),
                    best_of = table.Column<int>(type: "integer", nullable: false),
                    state_history_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    TournamentTeamStatsId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournaments", x => x.id);
                    table.ForeignKey(
                        name: "FK_tournaments_tournament_team_stats_TournamentTeamStatsId",
                        column: x => x.TournamentTeamStatsId,
                        principalTable: "tournament_team_stats",
                        principalColumn: "id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "scrimmage_leaderboard_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    scrimmage_leaderboard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    wins = table.Column<int>(type: "integer", nullable: false),
                    losses = table.Column<int>(type: "integer", nullable: false),
                    draws = table.Column<int>(type: "integer", nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    rating = table.Column<double>(type: "double precision", nullable: false),
                    recent_rating_change = table.Column<double>(type: "double precision", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrimmage_leaderboard_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_scrimmage_leaderboard_items_scrimmage_leaderboards_scrimmag~",
                        column: x => x.scrimmage_leaderboard_id,
                        principalTable: "scrimmage_leaderboards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_scrimmage_leaderboard_items_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "tournament_leaderboard_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    tournament_leaderboard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    tournament_points = table.Column<int>(type: "integer", nullable: false),
                    tournament_placements = table.Column<List<int>>(type: "jsonb", nullable: false),
                    tournaments_played_count = table.Column<int>(type: "integer", nullable: false),
                    average_placement = table.Column<double>(type: "double precision", nullable: false),
                    best_placement = table.Column<int>(type: "integer", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    rating = table.Column<double>(type: "double precision", nullable: false),
                    recent_rating_change = table.Column<double>(type: "double precision", nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_leaderboard_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_tournament_leaderboard_items_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_tournament_leaderboard_items_tournament_leaderboards_tourna~",
                        column: x => x.tournament_leaderboard_id,
                        principalTable: "tournament_leaderboards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "game_state_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    triggered_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    triggered_by_user_name = table.Column<string>(type: "text", nullable: false),
                    additional_data = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    player_deck_codes = table.Column<Dictionary<Guid, string>>(type: "jsonb", nullable: false),
                    player_deck_submitted_at = table.Column<Dictionary<Guid, DateTime>>(type: "jsonb", nullable: false),
                    player_deck_confirmed = table.Column<HashSet<Guid>>(type: "uuid[]", nullable: false),
                    player_deck_confirmed_at = table.Column<Dictionary<Guid, DateTime>>(type: "jsonb", nullable: false),
                    map_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    team1_player_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    team2_player_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    game_number = table.Column<int>(type: "integer", nullable: false),
                    team1_division_id = table.Column<Guid>(type: "uuid", nullable: true),
                    team2_division_id = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_state_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_game_state_snapshots_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_game_state_snapshots_matches_match_id",
                        column: x => x.match_id,
                        principalTable: "matches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "replays",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_mode = table.Column<string>(type: "text", nullable: false),
                    allow_observers = table.Column<string>(type: "text", nullable: true),
                    observer_delay = table.Column<string>(type: "text", nullable: true),
                    seed = table.Column<string>(type: "text", nullable: true),
                    @private = table.Column<string>(name: "private", type: "text", nullable: true),
                    server_name = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<string>(type: "text", nullable: true),
                    unique_session_id = table.Column<string>(type: "text", nullable: true),
                    mod_list = table.Column<string>(type: "text", nullable: true),
                    mod_tag_list = table.Column<string>(type: "text", nullable: true),
                    environment_settings = table.Column<string>(type: "text", nullable: true),
                    game_type = table.Column<string>(type: "text", nullable: true),
                    map = table.Column<string>(type: "text", nullable: false),
                    init_money = table.Column<string>(type: "text", nullable: true),
                    time_limit = table.Column<string>(type: "text", nullable: true),
                    score_limit = table.Column<string>(type: "text", nullable: true),
                    combat_rule = table.Column<string>(type: "text", nullable: true),
                    income_rate = table.Column<string>(type: "text", nullable: true),
                    upkeep = table.Column<string>(type: "text", nullable: true),
                    original_filename = table.Column<string>(type: "text", nullable: true),
                    file_path = table.Column<string>(type: "text", nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    victory_code = table.Column<string>(type: "text", nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_replays", x => x.id);
                    table.ForeignKey(
                        name: "FK_replays_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "tournament_state_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tournament_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    triggered_by_mashina_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    additional_data = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    registration_opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    max_participants = table.Column<int>(type: "integer", nullable: false),
                    winner_team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_by_mashina_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancellation_reason = table.Column<string>(type: "text", nullable: true),
                    registered_team_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    participant_team_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    active_match_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    completed_match_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    all_match_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    final_rankings = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    current_participant_count = table.Column<int>(type: "integer", nullable: false),
                    current_round = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_state_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_tournament_state_snapshots_mashina_users_cancelled_by_mashi~",
                        column: x => x.cancelled_by_mashina_user_id,
                        principalTable: "mashina_users",
                        principalColumn: "id"
                    );
                    table.ForeignKey(
                        name: "FK_tournament_state_snapshots_mashina_users_triggered_by_mashi~",
                        column: x => x.triggered_by_mashina_user_id,
                        principalTable: "mashina_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_tournament_state_snapshots_tournaments_tournament_id",
                        column: x => x.tournament_id,
                        principalTable: "tournaments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "replay_players",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    replay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_user_id = table.Column<string>(type: "text", nullable: false),
                    player_name = table.Column<string>(type: "text", nullable: false),
                    player_elo = table.Column<string>(type: "text", nullable: true),
                    player_level = table.Column<string>(type: "text", nullable: true),
                    player_alliance = table.Column<string>(type: "text", nullable: false),
                    player_score_limit = table.Column<string>(type: "text", nullable: true),
                    player_income_rate = table.Column<string>(type: "text", nullable: true),
                    player_avatar = table.Column<string>(type: "text", nullable: true),
                    player_ready = table.Column<string>(type: "text", nullable: true),
                    player_deck_content = table.Column<string>(type: "text", nullable: true),
                    player_deck_name = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_replay_players", x => x.id);
                    table.ForeignKey(
                        name: "FK_replay_players_replays_replay_id",
                        column: x => x.replay_id,
                        principalTable: "replays",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    mashina_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    last_active = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    team_join_limit = table.Column<int>(type: "integer", nullable: false),
                    team_join_cooldowns = table.Column<Dictionary<Guid, DateTime>>(type: "jsonb", nullable: false),
                    team_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    current_platform_ids = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    previous_platform_ids = table.Column<Dictionary<string, List<string>>>(
                        type: "jsonb",
                        nullable: false
                    ),
                    current_steam_username = table.Column<string>(type: "text", nullable: true),
                    previous_steam_usernames = table.Column<List<string>>(type: "jsonb", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    ScrimmageId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScrimmageId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    ScrimmageLeaderboardItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    TournamentLeaderboardItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_players", x => x.id);
                    table.ForeignKey(
                        name: "FK_players_mashina_users_mashina_user_id",
                        column: x => x.mashina_user_id,
                        principalTable: "mashina_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_players_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "matches",
                        principalColumn: "id"
                    );
                    table.ForeignKey(
                        name: "FK_players_matches_MatchId1",
                        column: x => x.MatchId1,
                        principalTable: "matches",
                        principalColumn: "id"
                    );
                    table.ForeignKey(
                        name: "FK_players_scrimmage_leaderboard_items_ScrimmageLeaderboardIte~",
                        column: x => x.ScrimmageLeaderboardItemId,
                        principalTable: "scrimmage_leaderboard_items",
                        principalColumn: "id"
                    );
                    table.ForeignKey(
                        name: "FK_players_tournament_leaderboard_items_TournamentLeaderboardI~",
                        column: x => x.TournamentLeaderboardItemId,
                        principalTable: "tournament_leaderboard_items",
                        principalColumn: "id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "scrimmage_challenges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    challenger_team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opponent_team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issued_by_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accepted_by_player_id = table.Column<Guid>(type: "uuid", nullable: true),
                    challenger_teammate_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    opponent_teammate_ids = table.Column<Guid[]>(type: "uuid[]", nullable: true),
                    challenge_status = table.Column<int>(type: "integer", nullable: false),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    best_of = table.Column<int>(type: "integer", nullable: false),
                    challenge_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    challenge_message_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    challenge_channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrimmage_challenges", x => x.id);
                    table.ForeignKey(
                        name: "FK_scrimmage_challenges_players_accepted_by_player_id",
                        column: x => x.accepted_by_player_id,
                        principalTable: "players",
                        principalColumn: "id"
                    );
                    table.ForeignKey(
                        name: "FK_scrimmage_challenges_players_issued_by_player_id",
                        column: x => x.issued_by_player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_scrimmage_challenges_teams_challenger_team_id",
                        column: x => x.challenger_team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_scrimmage_challenges_teams_opponent_team_id",
                        column: x => x.opponent_team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "scrimmages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scrimmage_challenge_id = table.Column<Guid>(type: "uuid", nullable: false),
                    challenger_team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opponent_team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issued_by_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accepted_by_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: true),
                    team_size = table.Column<int>(type: "integer", nullable: false),
                    best_of = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    winner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    challenger_team_rating = table.Column<double>(type: "double precision", nullable: false),
                    opponent_team_rating = table.Column<double>(type: "double precision", nullable: false),
                    challenger_team_rating_change = table.Column<double>(type: "double precision", nullable: true),
                    opponent_team_rating_change = table.Column<double>(type: "double precision", nullable: true),
                    challenger_team_confidence = table.Column<double>(type: "double precision", nullable: false),
                    opponent_team_confidence = table.Column<double>(type: "double precision", nullable: false),
                    challenger_team_score = table.Column<int>(type: "integer", nullable: true),
                    opponent_team_score = table.Column<int>(type: "integer", nullable: true),
                    challenger_team_variety_bonus_used = table.Column<double>(type: "double precision", nullable: true),
                    opponent_team_variety_bonus_used = table.Column<double>(type: "double precision", nullable: true),
                    challenger_team_multiplier_used = table.Column<double>(type: "double precision", nullable: true),
                    opponent_team_multiplier_used = table.Column<double>(type: "double precision", nullable: true),
                    higher_rated_team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating_range_at_match = table.Column<double>(type: "double precision", nullable: false),
                    challenger_team_gap_scaling_applied_value = table.Column<double>(
                        type: "double precision",
                        nullable: true
                    ),
                    opponent_team_gap_scaling_applied_value = table.Column<double>(
                        type: "double precision",
                        nullable: true
                    ),
                    challenger_team_catch_up_bonus_used = table.Column<double>(
                        type: "double precision",
                        nullable: true
                    ),
                    opponent_team_catch_up_bonus_used = table.Column<double>(type: "double precision", nullable: true),
                    challenger_team_adjusted_rating_change = table.Column<double>(
                        type: "double precision",
                        nullable: true
                    ),
                    opponent_team_adjusted_rating_change = table.Column<double>(
                        type: "double precision",
                        nullable: true
                    ),
                    challenger_team_proven_potential_applied = table.Column<bool>(type: "boolean", nullable: false),
                    opponent_team_proven_potential_applied = table.Column<bool>(type: "boolean", nullable: false),
                    challenger_team_proven_potential_applied_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    opponent_team_proven_potential_applied_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrimmages", x => x.id);
                    table.ForeignKey(
                        name: "FK_scrimmages_matches_match_id",
                        column: x => x.match_id,
                        principalTable: "matches",
                        principalColumn: "id"
                    );
                    table.ForeignKey(
                        name: "FK_scrimmages_players_accepted_by_player_id",
                        column: x => x.accepted_by_player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_scrimmages_players_issued_by_player_id",
                        column: x => x.issued_by_player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_scrimmages_scrimmage_challenges_scrimmage_challenge_id",
                        column: x => x.scrimmage_challenge_id,
                        principalTable: "scrimmage_challenges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_scrimmages_teams_challenger_team_id",
                        column: x => x.challenger_team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_scrimmages_teams_opponent_team_id",
                        column: x => x.opponent_team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "scrimmage_state_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scrimmage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrimmage_state_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_scrimmage_state_snapshots_scrimmages_scrimmage_id",
                        column: x => x.scrimmage_id,
                        principalTable: "scrimmages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_division_learning_curves_parameters",
                    table: "division_learning_curves",
                    column: "parameters"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_division_learning_curves_division_id",
                table: "division_learning_curves",
                column: "division_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_division_learning_curves_team_id",
                table: "division_learning_curves",
                column: "team_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_division_learning_curves_archive_archived_at",
                table: "division_learning_curves_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_division_learning_curves_archive_entity_version",
                table: "division_learning_curves_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_division_map_stats_game_length_performance",
                    table: "division_map_stats",
                    column: "game_length_performance"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_division_map_stats_division_id",
                table: "division_map_stats",
                column: "division_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_division_map_stats_map_id",
                table: "division_map_stats",
                column: "map_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_division_map_stats_archive_archived_at",
                table: "division_map_stats_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_division_map_stats_archive_entity_version",
                table: "division_map_stats_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_division_stats_additional_metrics",
                    table: "division_stats",
                    column: "additional_metrics"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_division_stats_density_performance",
                    table: "division_stats",
                    column: "density_performance"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_division_stats_game_length_performance",
                    table: "division_stats",
                    column: "game_length_performance"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_division_stats_division_id",
                table: "division_stats",
                column: "division_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_division_stats_archive_archived_at",
                table: "division_stats_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_division_stats_archive_entity_version",
                table: "division_stats_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.CreateIndex(
                name: "idx_divisions_archive_archived_at",
                table: "divisions_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_divisions_archive_entity_version",
                table: "divisions_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_game_state_snapshots_additional_data",
                    table: "game_state_snapshots",
                    column: "additional_data"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_game_state_snapshots_player_deck_codes",
                    table: "game_state_snapshots",
                    column: "player_deck_codes"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_game_state_snapshots_player_deck_confirmed_at",
                    table: "game_state_snapshots",
                    column: "player_deck_confirmed_at"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_game_state_snapshots_player_deck_submitted_at",
                    table: "game_state_snapshots",
                    column: "player_deck_submitted_at"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_game_state_snapshots_game_id",
                table: "game_state_snapshots",
                column: "game_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_game_state_snapshots_match_id",
                table: "game_state_snapshots",
                column: "match_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_game_state_snapshots_archive_archived_at",
                table: "game_state_snapshots_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_game_state_snapshots_archive_entity_version",
                table: "game_state_snapshots_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.CreateIndex(name: "idx_games_map_id", table: "games", column: "map_id");

            migrationBuilder.CreateIndex(name: "idx_games_match_id", table: "games", column: "match_id");

            migrationBuilder.CreateIndex(
                name: "idx_games_team1_division_id",
                table: "games",
                column: "team1_division_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_games_team2_division_id",
                table: "games",
                column: "team2_division_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_games_archive_archived_at",
                table: "games_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_games_archive_entity_version",
                table: "games_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.CreateIndex(
                name: "idx_maps_archive_archived_at",
                table: "maps_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_maps_archive_entity_version",
                table: "maps_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_mashina_users_previous_discord_globalnames",
                    table: "mashina_users",
                    column: "previous_discord_globalnames"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_mashina_users_previous_discord_mentions",
                    table: "mashina_users",
                    column: "previous_discord_mentions"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_mashina_users_previous_discord_user_ids",
                    table: "mashina_users",
                    column: "previous_discord_user_ids"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_mashina_users_previous_discord_usernames",
                    table: "mashina_users",
                    column: "previous_discord_usernames"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_mashina_users_player_id",
                table: "mashina_users",
                column: "player_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_mashina_users_archive_archived_at",
                table: "mashina_users_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_mashina_users_archive_entity_version",
                table: "mashina_users_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_match_state_snapshots_additional_data",
                    table: "match_state_snapshots",
                    column: "additional_data"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_match_state_snapshots_available_maps",
                    table: "match_state_snapshots",
                    column: "available_maps"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_match_state_snapshots_final_map_pool",
                    table: "match_state_snapshots",
                    column: "final_map_pool"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_match_state_snapshots_team1_map_bans",
                    table: "match_state_snapshots",
                    column: "team1_map_bans"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_match_state_snapshots_team2_map_bans",
                    table: "match_state_snapshots",
                    column: "team2_map_bans"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_match_state_snapshots_match_id",
                table: "match_state_snapshots",
                column: "match_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_match_state_snapshots_archive_archived_at",
                table: "match_state_snapshots_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_match_state_snapshots_archive_entity_version",
                table: "match_state_snapshots_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder
                .CreateIndex(name: "gin_idx_matches_available_maps", table: "matches", column: "available_maps")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder
                .CreateIndex(name: "gin_idx_matches_final_map_pool", table: "matches", column: "final_map_pool")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder
                .CreateIndex(name: "gin_idx_matches_team1_map_bans", table: "matches", column: "team1_map_bans")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder
                .CreateIndex(name: "gin_idx_matches_team2_map_bans", table: "matches", column: "team2_map_bans")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(name: "idx_matches_team1_id", table: "matches", column: "team1_id");

            migrationBuilder.CreateIndex(name: "idx_matches_team2_id", table: "matches", column: "team2_id");

            migrationBuilder.CreateIndex(
                name: "idx_matches_archive_archived_at",
                table: "matches_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_matches_archive_entity_version",
                table: "matches_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_players_current_platform_ids",
                    table: "players",
                    column: "current_platform_ids"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_players_previous_platform_ids",
                    table: "players",
                    column: "previous_platform_ids"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_players_previous_steam_usernames",
                    table: "players",
                    column: "previous_steam_usernames"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_players_team_join_cooldowns",
                    table: "players",
                    column: "team_join_cooldowns"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_players_mashina_user_id",
                table: "players",
                column: "mashina_user_id",
                unique: true
            );

            migrationBuilder.CreateIndex(name: "IX_players_MatchId", table: "players", column: "MatchId");

            migrationBuilder.CreateIndex(name: "IX_players_MatchId1", table: "players", column: "MatchId1");

            migrationBuilder.CreateIndex(name: "IX_players_ScrimmageId", table: "players", column: "ScrimmageId");

            migrationBuilder.CreateIndex(name: "IX_players_ScrimmageId1", table: "players", column: "ScrimmageId1");

            migrationBuilder.CreateIndex(
                name: "IX_players_ScrimmageLeaderboardItemId",
                table: "players",
                column: "ScrimmageLeaderboardItemId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_players_TournamentLeaderboardItemId",
                table: "players",
                column: "TournamentLeaderboardItemId"
            );

            migrationBuilder.CreateIndex(
                name: "idx_players_archive_archived_at",
                table: "players_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_players_archive_entity_version",
                table: "players_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_proven_potential_records_applied_thresholds",
                    table: "proven_potential_records",
                    column: "applied_thresholds"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_proven_potential_records_archive_archived_at",
                table: "proven_potential_records_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_proven_potential_records_archive_entity_version",
                table: "proven_potential_records_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_rating_percentile_breakpoints_breakpoints",
                    table: "rating_percentile_breakpoints",
                    column: "breakpoints"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_rating_percentile_breakpoints_archive_archived_at",
                table: "rating_percentile_breakpoints_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_rating_percentile_breakpoints_archive_entity_version",
                table: "rating_percentile_breakpoints_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.CreateIndex(
                name: "idx_replay_players_replay_id",
                table: "replay_players",
                column: "replay_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_replay_players_archive_archived_at",
                table: "replay_players_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_replay_players_archive_entity_version",
                table: "replay_players_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.CreateIndex(name: "IX_replays_game_id", table: "replays", column: "game_id");

            migrationBuilder.CreateIndex(
                name: "idx_replays_archive_archived_at",
                table: "replays_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_replays_archive_entity_version",
                table: "replays_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.CreateIndex(
                name: "ix_schema_metadata_applied_at",
                table: "schema_metadata",
                column: "applied_at"
            );

            migrationBuilder.CreateIndex(
                name: "ix_schema_metadata_migration_name",
                table: "schema_metadata",
                column: "migration_name"
            );

            migrationBuilder.CreateIndex(
                name: "ix_schema_metadata_schema_version",
                table: "schema_metadata",
                column: "schema_version"
            );

            // Record initial schema version in SchemaMetadata
            migrationBuilder.Sql(
                "INSERT INTO schema_metadata (schema_version, applied_at, applied_by, description, is_breaking_change, migration_name) VALUES ('001-1.0', NOW(), 'EFCore', 'Initial schema with temporal membership constraints', FALSE, 'TeamMemberTemporalConstraints');"
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmage_challenges_accepted_by_player_id",
                table: "scrimmage_challenges",
                column: "accepted_by_player_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmage_challenges_challenger_team_id",
                table: "scrimmage_challenges",
                column: "challenger_team_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmage_challenges_issued_by_player_id",
                table: "scrimmage_challenges",
                column: "issued_by_player_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmage_challenges_opponent_team_id",
                table: "scrimmage_challenges",
                column: "opponent_team_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmage_challenges_archive_archived_at",
                table: "scrimmage_challenges_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmage_challenges_archive_entity_version",
                table: "scrimmage_challenges_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmage_leaderboard_items_scrimmage_leaderboard_id",
                table: "scrimmage_leaderboard_items",
                column: "scrimmage_leaderboard_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmage_leaderboard_items_team_id",
                table: "scrimmage_leaderboard_items",
                column: "team_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmage_leaderboard_items_archive_archived_at",
                table: "scrimmage_leaderboard_items_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmage_leaderboard_items_archive_entity_version",
                table: "scrimmage_leaderboard_items_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmage_leaderboards_season_id",
                table: "scrimmage_leaderboards",
                column: "season_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmage_leaderboards_archive_archived_at",
                table: "scrimmage_leaderboards_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmage_leaderboards_archive_entity_version",
                table: "scrimmage_leaderboards_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmage_state_snapshots_scrimmage_id",
                table: "scrimmage_state_snapshots",
                column: "scrimmage_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmage_state_snapshots_archive_archived_at",
                table: "scrimmage_state_snapshots_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmage_state_snapshots_archive_entity_version",
                table: "scrimmage_state_snapshots_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmage_team_stats_archive_archived_at",
                table: "scrimmage_team_stats_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmage_team_stats_archive_entity_version",
                table: "scrimmage_team_stats_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmages_accepted_by_player_id",
                table: "scrimmages",
                column: "accepted_by_player_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmages_challenger_team_id",
                table: "scrimmages",
                column: "challenger_team_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmages_issued_by_player_id",
                table: "scrimmages",
                column: "issued_by_player_id"
            );

            migrationBuilder.CreateIndex(name: "idx_scrimmages_match_id", table: "scrimmages", column: "match_id");

            migrationBuilder.CreateIndex(
                name: "idx_scrimmages_opponent_team_id",
                table: "scrimmages",
                column: "opponent_team_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmages_scrimmage_challenge_id",
                table: "scrimmages",
                column: "scrimmage_challenge_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmages_archive_archived_at",
                table: "scrimmages_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_scrimmages_archive_entity_version",
                table: "scrimmages_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.CreateIndex(
                name: "idx_season_configs_archive_archived_at",
                table: "season_configs_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_season_configs_archive_entity_version",
                table: "season_configs_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_seasons_season_config_id",
                table: "seasons",
                column: "season_config_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_seasons_archive_archived_at",
                table: "seasons_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_seasons_archive_entity_version",
                table: "seasons_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.CreateIndex(
                name: "idx_team_members_mashina_user_id",
                table: "team_members",
                column: "mashina_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_team_members_team_roster_id",
                table: "team_members",
                column: "team_roster_id"
            );

            migrationBuilder.CreateIndex(
                name: "ux_team_members_active_roster_player",
                table: "team_members",
                columns: new[] { "team_roster_id", "player_id" },
                unique: true,
                filter: "valid_to IS NULL"
            );

            migrationBuilder.Sql(
                @"ALTER TABLE team_members
ADD CONSTRAINT team_members_no_overlap
EXCLUDE USING gist (
  team_roster_id WITH =,
  player_id WITH =,
  tstzrange(valid_from, COALESCE(valid_to, 'infinity')) WITH &&
);"
            );

            migrationBuilder.CreateIndex(
                name: "idx_team_members_archive_archived_at",
                table: "team_members_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_team_members_archive_entity_version",
                table: "team_members_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.CreateIndex(
                name: "idx_team_opponent_encounters_match_id",
                table: "team_opponent_encounters",
                column: "match_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_team_opponent_encounters_opponent_id",
                table: "team_opponent_encounters",
                column: "opponent_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_team_opponent_encounters_team_id",
                table: "team_opponent_encounters",
                column: "team_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_team_opponent_encounters_ScrimmageTeamStatsId",
                table: "team_opponent_encounters",
                column: "ScrimmageTeamStatsId"
            );

            migrationBuilder.CreateIndex(
                name: "idx_team_opponent_encounters_archive_archived_at",
                table: "team_opponent_encounters_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_team_opponent_encounters_archive_entity_version",
                table: "team_opponent_encounters_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.CreateIndex(name: "idx_team_rosters_team_id", table: "team_rosters", column: "team_id");

            migrationBuilder.CreateIndex(
                name: "idx_team_rosters_archive_archived_at",
                table: "team_rosters_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_team_rosters_archive_entity_version",
                table: "team_rosters_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.CreateIndex(
                name: "idx_team_variety_stats_team_id",
                table: "team_variety_stats",
                column: "team_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_team_variety_stats_archive_archived_at",
                table: "team_variety_stats_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_team_variety_stats_archive_entity_version",
                table: "team_variety_stats_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder
                .CreateIndex(name: "gin_idx_teams_scrimmage_team_stats", table: "teams", column: "scrimmage_team_stats")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_teams_tournament_team_stats",
                    table: "teams",
                    column: "tournament_team_stats"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_teams_archive_archived_at",
                table: "teams_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_teams_archive_entity_version",
                table: "teams_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_tournament_leaderboard_items_tournament_placements",
                    table: "tournament_leaderboard_items",
                    column: "tournament_placements"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_tournament_leaderboard_items_team_id",
                table: "tournament_leaderboard_items",
                column: "team_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_tournament_leaderboard_items_tournament_leaderboard_id",
                table: "tournament_leaderboard_items",
                column: "tournament_leaderboard_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_tournament_leaderboard_items_archive_archived_at",
                table: "tournament_leaderboard_items_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_tournament_leaderboard_items_archive_entity_version",
                table: "tournament_leaderboard_items_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.CreateIndex(
                name: "idx_tournament_leaderboards_season_id",
                table: "tournament_leaderboards",
                column: "season_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_tournament_leaderboards_archive_archived_at",
                table: "tournament_leaderboards_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_tournament_leaderboards_archive_entity_version",
                table: "tournament_leaderboards_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder
                .CreateIndex(
                    name: "gin_idx_tournament_state_snapshots_additional_data",
                    table: "tournament_state_snapshots",
                    column: "additional_data"
                )
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_tournament_state_snapshots_cancelled_by_mashina_user_id",
                table: "tournament_state_snapshots",
                column: "cancelled_by_mashina_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_tournament_state_snapshots_tournament_id",
                table: "tournament_state_snapshots",
                column: "tournament_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_tournament_state_snapshots_triggered_by_mashina_user_id",
                table: "tournament_state_snapshots",
                column: "triggered_by_mashina_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_tournament_state_snapshots_archive_archived_at",
                table: "tournament_state_snapshots_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_tournament_state_snapshots_archive_entity_version",
                table: "tournament_state_snapshots_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.CreateIndex(
                name: "idx_tournament_team_stats_team_id",
                table: "tournament_team_stats",
                column: "team_id"
            );

            migrationBuilder.CreateIndex(
                name: "idx_tournament_team_stats_archive_archived_at",
                table: "tournament_team_stats_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_tournament_team_stats_archive_entity_version",
                table: "tournament_team_stats_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_tournaments_TournamentTeamStatsId",
                table: "tournaments",
                column: "TournamentTeamStatsId"
            );

            migrationBuilder.CreateIndex(
                name: "idx_tournaments_archive_archived_at",
                table: "tournaments_archive",
                column: "archived_at"
            );

            migrationBuilder.CreateIndex(
                name: "idx_tournaments_archive_entity_version",
                table: "tournaments_archive",
                columns: new[] { "entity_id", "version" }
            );

            migrationBuilder.AddForeignKey(
                name: "FK_players_scrimmages_ScrimmageId",
                table: "players",
                column: "ScrimmageId",
                principalTable: "scrimmages",
                principalColumn: "id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_players_scrimmages_ScrimmageId1",
                table: "players",
                column: "ScrimmageId1",
                principalTable: "scrimmages",
                principalColumn: "id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_matches_teams_team1_id", table: "matches");

            migrationBuilder.DropForeignKey(name: "FK_matches_teams_team2_id", table: "matches");

            migrationBuilder.DropForeignKey(
                name: "FK_scrimmage_challenges_teams_challenger_team_id",
                table: "scrimmage_challenges"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_scrimmage_challenges_teams_opponent_team_id",
                table: "scrimmage_challenges"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_scrimmage_leaderboard_items_teams_team_id",
                table: "scrimmage_leaderboard_items"
            );

            migrationBuilder.DropForeignKey(name: "FK_scrimmages_teams_challenger_team_id", table: "scrimmages");

            migrationBuilder.DropForeignKey(name: "FK_scrimmages_teams_opponent_team_id", table: "scrimmages");

            migrationBuilder.DropForeignKey(
                name: "FK_tournament_leaderboard_items_teams_team_id",
                table: "tournament_leaderboard_items"
            );

            migrationBuilder.DropForeignKey(name: "FK_players_matches_MatchId", table: "players");

            migrationBuilder.DropForeignKey(name: "FK_players_matches_MatchId1", table: "players");

            migrationBuilder.DropForeignKey(name: "FK_scrimmages_matches_match_id", table: "scrimmages");

            migrationBuilder.DropForeignKey(name: "FK_players_mashina_users_mashina_user_id", table: "players");

            migrationBuilder.DropForeignKey(
                name: "FK_players_scrimmage_leaderboard_items_ScrimmageLeaderboardIte~",
                table: "players"
            );

            migrationBuilder.DropForeignKey(name: "FK_players_scrimmages_ScrimmageId", table: "players");

            migrationBuilder.DropForeignKey(name: "FK_players_scrimmages_ScrimmageId1", table: "players");

            migrationBuilder.DropTable(name: "division_learning_curves");

            migrationBuilder.DropTable(name: "division_learning_curves_archive");

            migrationBuilder.DropTable(name: "division_map_stats");

            migrationBuilder.DropTable(name: "division_map_stats_archive");

            migrationBuilder.DropTable(name: "division_stats");

            migrationBuilder.DropTable(name: "division_stats_archive");

            migrationBuilder.DropTable(name: "divisions_archive");

            migrationBuilder.DropTable(name: "game_state_snapshots");

            migrationBuilder.DropTable(name: "game_state_snapshots_archive");

            migrationBuilder.DropTable(name: "games_archive");

            migrationBuilder.DropTable(name: "maps_archive");

            migrationBuilder.DropTable(name: "mashina_users_archive");

            migrationBuilder.DropTable(name: "match_state_snapshots");

            migrationBuilder.DropTable(name: "match_state_snapshots_archive");

            migrationBuilder.DropTable(name: "matches_archive");

            migrationBuilder.DropTable(name: "players_archive");

            migrationBuilder.DropTable(name: "proven_potential_records");

            migrationBuilder.DropTable(name: "proven_potential_records_archive");

            migrationBuilder.DropTable(name: "rating_percentile_breakpoints");

            migrationBuilder.DropTable(name: "rating_percentile_breakpoints_archive");

            migrationBuilder.DropTable(name: "replay_players");

            migrationBuilder.DropTable(name: "replay_players_archive");

            migrationBuilder.DropTable(name: "replays_archive");

            migrationBuilder.DropTable(name: "schema_metadata");

            migrationBuilder.DropTable(name: "scrimmage_challenges_archive");

            migrationBuilder.DropTable(name: "scrimmage_leaderboard_items_archive");

            migrationBuilder.DropTable(name: "scrimmage_leaderboards_archive");

            migrationBuilder.DropTable(name: "scrimmage_state_snapshots");

            migrationBuilder.DropTable(name: "scrimmage_state_snapshots_archive");

            migrationBuilder.DropTable(name: "scrimmage_team_stats_archive");

            migrationBuilder.DropTable(name: "scrimmages_archive");

            migrationBuilder.DropTable(name: "season_configs_archive");

            migrationBuilder.DropTable(name: "seasons_archive");

            migrationBuilder.DropTable(name: "team_members");

            migrationBuilder.DropTable(name: "team_members_archive");

            migrationBuilder.DropTable(name: "team_opponent_encounters");

            migrationBuilder.DropTable(name: "team_opponent_encounters_archive");

            migrationBuilder.DropTable(name: "team_rosters_archive");

            migrationBuilder.DropTable(name: "team_variety_stats");

            migrationBuilder.DropTable(name: "team_variety_stats_archive");

            migrationBuilder.DropTable(name: "teams_archive");

            migrationBuilder.DropTable(name: "tournament_leaderboard_items_archive");

            migrationBuilder.DropTable(name: "tournament_leaderboards_archive");

            migrationBuilder.DropTable(name: "tournament_state_snapshots");

            migrationBuilder.DropTable(name: "tournament_state_snapshots_archive");

            migrationBuilder.DropTable(name: "tournament_team_stats_archive");

            migrationBuilder.DropTable(name: "tournaments_archive");

            migrationBuilder.DropTable(name: "replays");

            migrationBuilder.DropTable(name: "team_rosters");

            migrationBuilder.DropTable(name: "scrimmage_team_stats");

            migrationBuilder.DropTable(name: "tournaments");

            migrationBuilder.DropTable(name: "games");

            migrationBuilder.DropTable(name: "tournament_team_stats");

            migrationBuilder.DropTable(name: "divisions");

            migrationBuilder.DropTable(name: "maps");

            migrationBuilder.DropTable(name: "teams");

            migrationBuilder.DropTable(name: "matches");

            migrationBuilder.DropTable(name: "mashina_users");

            migrationBuilder.DropTable(name: "scrimmage_leaderboard_items");

            migrationBuilder.DropTable(name: "scrimmage_leaderboards");

            migrationBuilder.DropTable(name: "scrimmages");

            migrationBuilder.DropTable(name: "scrimmage_challenges");

            migrationBuilder.DropTable(name: "players");

            migrationBuilder.DropTable(name: "tournament_leaderboard_items");

            migrationBuilder.DropTable(name: "tournament_leaderboards");

            migrationBuilder.DropTable(name: "seasons");

            migrationBuilder.DropTable(name: "season_configs");
        }
    }
}
