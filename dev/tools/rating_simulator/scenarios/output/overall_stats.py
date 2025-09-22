"""Functions for generating overall statistics tables."""

from typing import Dict, List
from collections import defaultdict
import statistics
from .base_output import get_max_width


def calculate_avg_opponent_target(
    player_name: str, results: List[Dict], players: List
) -> float:
    """Calculate the average target rating of opponents for a given player.

    Args:
        player_name: Name of the player
        results: List of match results
        players: List of player objects

    Returns:
        Average target rating of opponents
    """
    opponent_targets = []

    for match in results:
        if match["player_id"] == player_name:
            # Player was challenger, opponent is opponent_id
            opponent_name = match["opponent_id"]
            opponent_obj = next((p for p in players if p.name == opponent_name), None)
            if opponent_obj:
                opponent_targets.append(opponent_obj.target_rating)
        elif match["opponent_id"] == player_name:
            # Player was opponent, challenger is player_id
            challenger_name = match["player_id"]
            challenger_obj = next(
                (p for p in players if p.name == challenger_name), None
            )
            if challenger_obj:
                opponent_targets.append(challenger_obj.target_rating)

    if opponent_targets:
        return sum(opponent_targets) / len(opponent_targets)
    else:
        return 0.0


def get_max_width(header: str, rows: List[Dict], key: str) -> int:
    """Calculate the maximum width needed for a column.

    Args:
        header: The header text for the column
        rows: List of row dictionaries
        key: The key to access the value in each row

    Returns:
        Maximum width needed for the column
    """
    if not rows:
        return len(header)
    return max(
        len(header),
        max(len(str(row.get(key, ""))) for row in rows),
    )


def write_overall_stats(f, players: List, results: List[Dict]) -> None:
    """Write overall statistics section.

    Args:
        f: File object to write to
        players: List of player objects
        results: List of match results
    """
    f.write("## Overall Statistics\n\n")

    # Calculate statistics
    final_ratings = [player.rating for player in players]
    target_ratings = [player.target_rating for player in players]
    rating_changes = [player.rating - 1500 for player in players]  # All start at 1500

    # Group players by target rating
    target_groups = defaultdict(list)
    for player in players:
        target_groups[player.target_rating].append(player)

    # Calculate win rates
    player_wins = defaultdict(int)
    player_games = defaultdict(int)
    for match in results:
        # Calculate winner from player_won field
        winner = match["player_id"] if match["player_won"] else match["opponent_id"]
        player_wins[winner] += 1
        player_games[match["player_id"]] += 1
        player_games[match["opponent_id"]] += 1

    # Calculate match statistics
    total_matches = len(results)
    total_players = len(players)

    # Calculate matches played per player
    matches_played = [p.games_played for p in players]
    avg_matches = sum(matches_played) / len(matches_played)
    median_matches = statistics.median(matches_played)
    max_matches = max(matches_played)
    min_matches = min(matches_played)

    # Prepare data for overall statistics table
    overall_stats_rows = [
        {"metric": "Total Players", "value": f"{total_players:.1f}"},
        {"metric": "Total Matches", "value": f"{total_matches:.1f}"},
        {
            "metric": "Average Final Rating",
            "value": f"{statistics.mean(final_ratings):.1f}",
        },
        {
            "metric": "Median Final Rating",
            "value": f"{statistics.median(final_ratings):.1f}",
        },
        {
            "metric": "Rating Standard Deviation",
            "value": f"{statistics.stdev(final_ratings):.1f}",
        },
        {
            "metric": "Average Rating Change",
            "value": f"{statistics.mean(rating_changes):.1f}",
        },
        {
            "metric": "Median Rating Change",
            "value": f"{statistics.median(rating_changes):.1f}",
        },
    ]

    # Calculate column widths
    metric_width = get_max_width("Metric", overall_stats_rows, "metric")
    value_width = get_max_width("Value", overall_stats_rows, "value")

    # Write overall statistics table with dynamic widths
    f.write(f"| {'Metric':<{metric_width}} | {'Value':>{value_width}} |\n")
    f.write(f"| {'-' * metric_width} | {'-' * value_width} |\n")
    for row in overall_stats_rows:
        f.write(
            f"| {row['metric']:<{metric_width}} | {row['value']:>{value_width}} |\n"
        )
    f.write("\n")

    # Prepare data for match statistics table
    match_stats_rows = [
        {"metric": "Average Matches Played", "value": f"{avg_matches:.1f}"},
        {"metric": "Median Matches Played", "value": f"{median_matches:.1f}"},
        {"metric": "Maximum Matches Played", "value": f"{max_matches:.1f}"},
        {"metric": "Minimum Matches Played", "value": f"{min_matches:.1f}"},
    ]

    # Calculate column widths
    match_metric_width = get_max_width("Metric", match_stats_rows, "metric")
    match_value_width = get_max_width("Value", match_stats_rows, "value")

    # Write match statistics with dynamic widths
    f.write("## Match Statistics\n\n")
    f.write(f"| {'Metric':<{match_metric_width}} | {'Value':>{match_value_width}} |\n")
    f.write(f"| {'-' * match_metric_width} | {'-' * match_value_width} |\n")
    for row in match_stats_rows:
        f.write(
            f"| {row['metric']:<{match_metric_width}} | {row['value']:>{match_value_width}} |\n"
        )
    f.write("\n")

    # Write target rating achievement
    write_target_achievement(f, target_groups, player_wins, player_games)

    # Write rating distribution
    write_rating_distribution(f, final_ratings, total_players)

    # Write win rate analysis
    write_win_rate_analysis(f, players, player_wins, player_games)

    # Write top 100 players
    write_top_players(f, players, player_wins, player_games, results)

    # Write proven potential statistics
    write_proven_potential_stats(f, results, players)


def write_target_achievement(
    f, target_groups: Dict, player_wins: Dict, player_games: Dict
) -> None:
    """Write target rating achievement table.

    Args:
        f: File object to write to
        target_groups: Dictionary mapping target ratings to lists of players
        player_wins: Dictionary mapping player names to win counts
        player_games: Dictionary mapping player names to games played
    """
    f.write("## Target Rating Achievement\n\n")

    # Group players into 100-point rating increments
    rating_increments = {}
    for target_rating, players in target_groups.items():
        # Calculate which 100-point increment this rating falls into
        increment_start = (target_rating // 100) * 100
        increment_end = increment_start + 99

        if increment_start not in rating_increments:
            rating_increments[increment_start] = []
        rating_increments[increment_start].extend(players)

    # Calculate statistics for each increment
    target_stats = []
    for increment_start in sorted(rating_increments.keys()):
        players = rating_increments[increment_start]
        increment_end = increment_start + 99

        # Count how many players achieved their target rating
        achieved = sum(1 for p in players if p.rating >= p.target_rating)
        avg_final = statistics.mean(p.rating for p in players)

        target_stats.append(
            {
                "target": f"{increment_start}-{increment_end}",
                "players": len(players),
                "achieved": achieved,
                "percent": achieved / len(players) * 100,
                "avg_rating": avg_final,
            }
        )

    # Prepare data for target rating achievement table
    target_rows = []
    for stat in target_stats:
        target_rows.append(
            {
                "target": stat["target"],
                "players": str(stat["players"]),
                "achieved": str(stat["achieved"]),
                "percent": f"{stat['percent']:.1f}%",
                "avg_rating": f"{stat['avg_rating']:.1f}",
            }
        )

    # Calculate column widths
    target_width = get_max_width("Target Rating", target_rows, "target")
    players_width = get_max_width("Players", target_rows, "players")
    achieved_width = get_max_width("Achieved", target_rows, "achieved")
    percent_width = get_max_width("% Achieved", target_rows, "percent")
    avg_rating_width = get_max_width("Avg Final Rating", target_rows, "avg_rating")

    # Write target rating achievement table with dynamic widths
    f.write(
        f"| {'Target Rating':<{target_width}} | {'Players':>{players_width}} | {'Achieved':>{achieved_width}} | {'% Achieved':>{percent_width}} | {'Avg Final Rating':>{avg_rating_width}} |\n"
    )
    f.write(
        f"| {'-' * target_width} | {'-' * players_width} | {'-' * achieved_width} | {'-' * percent_width} | {'-' * avg_rating_width} |\n"
    )
    for row in target_rows:
        f.write(
            f"| {row['target']:<{target_width}} | {row['players']:>{players_width}} | {row['achieved']:>{achieved_width}} | {row['percent']:>{percent_width}} | {row['avg_rating']:>{avg_rating_width}} |\n"
        )
    f.write("\n")


def write_rating_distribution(
    f, final_ratings: List[float], total_players: int
) -> None:
    """Write rating distribution table.

    Args:
        f: File object to write to
        final_ratings: List of final player ratings
        total_players: Total number of players
    """
    f.write("## Rating Distribution\n\n")

    ranges = [
        (0, 1400),
        (1400, 1600),
        (1600, 1800),
        (1800, 2000),
        (2000, float("inf")),
    ]
    dist_stats = []
    for start, end in ranges:
        count = sum(1 for r in final_ratings if start <= r < end)
        dist_stats.append(
            {
                "range": f"{start}-{end if end != float('inf') else '∞'}",
                "players": count,
                "percent": count / total_players * 100,
            }
        )

    # Prepare data for rating distribution table
    dist_rows = []
    for stat in dist_stats:
        dist_rows.append(
            {
                "range": stat["range"],
                "players": str(stat["players"]),
                "percent": f"{stat['percent']:.1f}%",
            }
        )

    # Calculate column widths
    range_width = get_max_width("Rating Range", dist_rows, "range")
    dist_players_width = get_max_width("Players", dist_rows, "players")
    percent_width = get_max_width("% of Total", dist_rows, "percent")

    # Write rating distribution table with dynamic widths
    f.write(
        f"| {'Rating Range':<{range_width}} | {'Players':>{dist_players_width}} | {'% of Total':>{percent_width}} |\n"
    )
    f.write(
        f"| {'-' * range_width} | {'-' * dist_players_width} | {'-' * percent_width} |\n"
    )
    for row in dist_rows:
        f.write(
            f"| {row['range']:<{range_width}} | {row['players']:>{dist_players_width}} | {row['percent']:>{percent_width}} |\n"
        )
    f.write("\n")


def write_win_rate_analysis(
    f, players: List, player_wins: Dict, player_games: Dict
) -> None:
    """Write win rate analysis table.

    Args:
        f: File object to write to
        players: List of player objects
        player_wins: Dictionary mapping player names to win counts
        player_games: Dictionary mapping player names to games played
    """
    f.write("## Win Rate Analysis\n\n")

    ranges = [
        (0, 1400),
        (1400, 1600),
        (1600, 1800),
        (1800, 2000),
        (2000, float("inf")),
    ]
    win_stats = []
    for start, end in ranges:
        players_in_range = [p for p in players if start <= p.rating < end]
        if players_in_range:
            players_with_games = [
                p for p in players_in_range if player_games[p.name] > 0
            ]
            if players_with_games:
                avg_win_rate = statistics.mean(
                    player_wins[p.name] / player_games[p.name] * 100
                    for p in players_with_games
                )
                avg_games = statistics.mean(
                    player_games[p.name] for p in players_with_games
                )
                win_stats.append(
                    {
                        "range": f"{start}-{end if end != float('inf') else '∞'}",
                        "win_rate": avg_win_rate,
                        "games": avg_games,
                    }
                )
            else:
                win_stats.append(
                    {
                        "range": f"{start}-{end if end != float('inf') else '∞'}",
                        "win_rate": 0.0,
                        "games": 0.0,
                    }
                )

    # Prepare data for win rate analysis table
    win_rows = []
    for stat in win_stats:
        win_rows.append(
            {
                "range": stat["range"],
                "win_rate": f"{stat['win_rate']:.1f}%",
                "games": f"{stat['games']:.1f}",
            }
        )

    # Calculate column widths
    win_range_width = get_max_width("Rating Range", win_rows, "range")
    win_rate_width = get_max_width("Avg Win Rate", win_rows, "win_rate")
    games_width = get_max_width("Games per Player", win_rows, "games")

    # Write win rate analysis table with dynamic widths
    f.write(
        f"| {'Rating Range':<{win_range_width}} | {'Avg Win Rate':>{win_rate_width}} | {'Games per Player':>{games_width}} |\n"
    )
    f.write(
        f"| {'-' * win_range_width} | {'-' * win_rate_width} | {'-' * games_width} |\n"
    )
    for row in win_rows:
        f.write(
            f"| {row['range']:<{win_range_width}} | {row['win_rate']:>{win_rate_width}} | {row['games']:>{games_width}} |\n"
        )
    f.write("\n")


def write_top_players(
    f, players: List, player_wins: Dict, player_games: Dict, results: List[Dict] = None
) -> None:
    """Write top 100 players table.

    Args:
        f: File object to write to
        players: List of player objects
        player_wins: Dictionary mapping player names to win counts
        player_games: Dictionary mapping player names to games played
        results: List of match results (optional, for variety bonus calculation)
    """
    f.write("## Top 100 Players\n\n")

    # Calculate final variety bonus for each player from match history
    player_variety_bonuses = {}
    if results:
        for player in players:
            # Find the last match where this player participated
            last_match = None
            for match in reversed(results):
                if (
                    match["player_id"] == player.name
                    or match["opponent_id"] == player.name
                ):
                    last_match = match
                    break

            if last_match:
                # Get the variety bonus from the last match
                if match["player_id"] == player.name:
                    variety_bonus = match.get("p1_variety_bonus", 0.0)
                else:
                    variety_bonus = match.get("p2_variety_bonus", 0.0)
                player_variety_bonuses[player.name] = variety_bonus
            else:
                player_variety_bonuses[player.name] = 0.0
    else:
        # If no results provided, set all variety bonuses to 0
        for player in players:
            player_variety_bonuses[player.name] = 0.0

    # Calculate win rates for all players
    player_stats = []
    for player in players:
        games = player_games.get(player.name, 0)
        wins = player_wins.get(player.name, 0)
        win_rate = (wins / games * 100) if games > 0 else 0.0
        variety_bonus = player_variety_bonuses.get(player.name, 0.0)
        player_stats.append(
            {
                "name": player.name,
                "rating": player.rating,
                "target": player.target_rating,
                "win_rate": win_rate,
                "games": games,
                "variety_bonus": variety_bonus,
            }
        )

    # Sort by rating (descending) and take top 100
    player_stats.sort(key=lambda x: x["rating"], reverse=True)
    top_100 = player_stats[:100]

    # Prepare data for top 100 players table
    top_players_rows = []
    for i, player in enumerate(top_100, 1):
        top_players_rows.append(
            {
                "rank": str(i),
                "name": player["name"],
                "rating": f"{player['rating']:.1f}",
                "target": str(player["target"]),
                "win_rate": f"{player['win_rate']:.1f}%",
                "games": str(player["games"]),
                "variety_bonus": f"{player['variety_bonus']:.2f}",
            }
        )

    # Calculate column widths
    rank_width = get_max_width("Rank", top_players_rows, "rank")
    name_width = get_max_width("Player", top_players_rows, "name")
    final_rating_width = get_max_width("Final Rating", top_players_rows, "rating")
    target_rating_width = get_max_width("Target Rating", top_players_rows, "target")
    win_rate_width = get_max_width("Win Rate", top_players_rows, "win_rate")
    games_played_width = get_max_width("Games Played", top_players_rows, "games")
    variety_bonus_width = get_max_width(
        "Variety Bonus", top_players_rows, "variety_bonus"
    )

    # Write top 100 players table with dynamic widths
    f.write(
        f"| {'Rank':<{rank_width}} | {'Player':<{name_width}} | {'Final Rating':>{final_rating_width}} | {'Target Rating':>{target_rating_width}} | {'Win Rate':>{win_rate_width}} | {'Games Played':>{games_played_width}} | {'Variety Bonus':>{variety_bonus_width}} |\n"
    )
    f.write(
        f"| {'-' * rank_width} | {'-' * name_width} | {'-' * final_rating_width} | {'-' * target_rating_width} | {'-' * win_rate_width} | {'-' * games_played_width} | {'-' * variety_bonus_width} |\n"
    )
    for row in top_players_rows:
        f.write(
            f"| {row['rank']:<{rank_width}} | {row['name']:<{name_width}} | {row['rating']:>{final_rating_width}} | {row['target']:>{target_rating_width}} | {row['win_rate']:>{win_rate_width}} | {row['games']:>{games_played_width}} | {row['variety_bonus']:>{variety_bonus_width}} |\n"
        )
    f.write("\n")


def write_proven_potential_stats(f, results: List[Dict], players: List = None) -> None:
    """Write proven potential statistics.

    Args:
        f: File object to write to
        results: List of match results
    """
    f.write("## Proven Potential Statistics\n\n")

    # Count total proven potential adjustments
    total_pp_adjustments = 0
    total_pp_matches = 0
    player_pp_adjustments = defaultdict(int)
    player_pp_benefits = defaultdict(float)

    for match in results:
        # Count player proven potential adjustments
        player_pp_details = match.get("proven_potential_details", [])
        opponent_pp_details = match.get("opponent_proven_potential_details", [])

        if player_pp_details:
            total_pp_matches += 1
            player_id = match["player_id"]
            player_pp_adjustments[player_id] += len(player_pp_details)
            total_pp_adjustments += len(player_pp_details)

            # Calculate total benefit for this player
            for detail in player_pp_details:
                player_pp_benefits[player_id] += detail.get("player_adjustment", 0)

        if opponent_pp_details:
            total_pp_matches += 1
            opponent_id = match["opponent_id"]
            player_pp_adjustments[opponent_id] += len(opponent_pp_details)
            total_pp_adjustments += len(opponent_pp_details)

            # Calculate total benefit for this opponent
            for detail in opponent_pp_details:
                player_pp_benefits[opponent_id] += detail.get("opponent_adjustment", 0)

    # Calculate statistics
    total_matches = len(results)
    pp_match_percentage = (
        (total_pp_matches / total_matches * 100) if total_matches > 0 else 0
    )
    avg_adjustments_per_pp_match = (
        (total_pp_adjustments / total_pp_matches) if total_pp_matches > 0 else 0
    )

    # Find players with most proven potential adjustments
    top_pp_players = sorted(
        [(player, count) for player, count in player_pp_adjustments.items()],
        key=lambda x: x[1],
        reverse=True,
    )[:10]

    # Find players with most proven potential benefits
    top_pp_beneficiaries = sorted(
        [(player, benefit) for player, benefit in player_pp_benefits.items()],
        key=lambda x: x[1],
        reverse=True,
    )[:10]

    # Prepare data for overall proven potential statistics
    pp_stats_rows = [
        {
            "metric": "Total Proven Potential Adjustments",
            "value": str(total_pp_adjustments),
        },
        {"metric": "Matches with PP Adjustments", "value": str(total_pp_matches)},
        {"metric": "PP Match Percentage", "value": f"{pp_match_percentage:.1f}%"},
        {
            "metric": "Avg Adjustments per PP Match",
            "value": f"{avg_adjustments_per_pp_match:.1f}",
        },
        {
            "metric": "Players with PP Adjustments",
            "value": str(len(player_pp_adjustments)),
        },
    ]

    # Calculate column widths
    pp_metric_width = get_max_width("Metric", pp_stats_rows, "metric")
    pp_value_width = get_max_width("Value", pp_stats_rows, "value")

    # Write overall proven potential statistics with dynamic widths
    f.write(f"| {'Metric':<{pp_metric_width}} | {'Value':>{pp_value_width}} |\n")
    f.write(f"| {'-' * pp_metric_width} | {'-' * pp_value_width} |\n")
    for row in pp_stats_rows:
        f.write(
            f"| {row['metric']:<{pp_metric_width}} | {row['value']:>{pp_value_width}} |\n"
        )
    f.write("\n")

    # Write top players by proven potential adjustments
    if top_pp_players:
        # Prepare data for top players by PP adjustments table
        pp_adj_rows = []
        for i, (player, count) in enumerate(top_pp_players, 1):
            benefit = player_pp_benefits.get(player, 0.0)
            pp_adj_rows.append(
                {
                    "rank": str(i),
                    "player": player,
                    "adjustments": str(count),
                    "benefit": f"{benefit:.1f}",
                }
            )

        # Calculate column widths
        pp_adj_rank_width = get_max_width("Rank", pp_adj_rows, "rank")
        pp_adj_player_width = get_max_width("Player", pp_adj_rows, "player")
        pp_adj_adjustments_width = get_max_width(
            "PP Adjustments", pp_adj_rows, "adjustments"
        )
        pp_adj_benefit_width = get_max_width("Total Benefit", pp_adj_rows, "benefit")

        f.write("### Top Players by Proven Potential Adjustments\n\n")
        f.write(
            f"| {'Rank':<{pp_adj_rank_width}} | {'Player':<{pp_adj_player_width}} | {'PP Adjustments':>{pp_adj_adjustments_width}} | {'Total Benefit':>{pp_adj_benefit_width}} |\n"
        )
        f.write(
            f"| {'-' * pp_adj_rank_width} | {'-' * pp_adj_player_width} | {'-' * pp_adj_adjustments_width} | {'-' * pp_adj_benefit_width} |\n"
        )
        for row in pp_adj_rows:
            f.write(
                f"| {row['rank']:<{pp_adj_rank_width}} | {row['player']:<{pp_adj_player_width}} | {row['adjustments']:>{pp_adj_adjustments_width}} | {row['benefit']:>{pp_adj_benefit_width}} |\n"
            )
        f.write("\n")

    # Write top players by proven potential benefits
    if top_pp_beneficiaries:
        # Prepare data for top players by PP benefits table
        pp_benefit_rows = []
        for i, (player, benefit) in enumerate(top_pp_beneficiaries, 1):
            count = player_pp_adjustments.get(player, 0)

            # Find player object to get target rating and games played
            player_obj = next((p for p in players if p.name == player), None)
            target_rating = player_obj.target_rating if player_obj else "N/A"
            games_played = player_obj.games_played if player_obj else 0

            # Calculate average opponent target rating
            avg_opponent_target = calculate_avg_opponent_target(
                player, results, players
            )

            pp_benefit_rows.append(
                {
                    "rank": str(i),
                    "player": f"{player} ({target_rating})",
                    "benefit": f"{benefit:.1f}",
                    "adjustments": str(count),
                    "games": str(games_played),
                    "target": str(target_rating),
                    "avg_opponent": f"{avg_opponent_target:.0f}",
                }
            )

        # Calculate column widths
        pp_benefit_rank_width = get_max_width("Rank", pp_benefit_rows, "rank")
        pp_benefit_player_width = get_max_width("Player", pp_benefit_rows, "player")
        pp_benefit_benefit_width = get_max_width(
            "Total Benefit", pp_benefit_rows, "benefit"
        )
        pp_benefit_adjustments_width = get_max_width(
            "PP Adjustments", pp_benefit_rows, "adjustments"
        )
        pp_benefit_games_width = get_max_width("Games Played", pp_benefit_rows, "games")
        pp_benefit_target_width = get_max_width(
            "Target Rating", pp_benefit_rows, "target"
        )
        pp_benefit_avg_opponent_width = get_max_width(
            "Avg Opponent Target", pp_benefit_rows, "avg_opponent"
        )

        f.write("### Top Players by Proven Potential Benefits\n\n")
        f.write(
            f"| {'Rank':<{pp_benefit_rank_width}} | {'Player':<{pp_benefit_player_width}} | {'Total Benefit':>{pp_benefit_benefit_width}} | {'PP Adjustments':>{pp_benefit_adjustments_width}} | {'Games Played':>{pp_benefit_games_width}} | {'Target Rating':>{pp_benefit_target_width}} | {'Avg Opponent Target':>{pp_benefit_avg_opponent_width}} |\n"
        )
        f.write(
            f"| {'-' * pp_benefit_rank_width} | {'-' * pp_benefit_player_width} | {'-' * pp_benefit_benefit_width} | {'-' * pp_benefit_adjustments_width} | {'-' * pp_benefit_games_width} | {'-' * pp_benefit_target_width} | {'-' * pp_benefit_avg_opponent_width} |\n"
        )
        for row in pp_benefit_rows:
            f.write(
                f"| {row['rank']:<{pp_benefit_rank_width}} | {row['player']:<{pp_benefit_player_width}} | {row['benefit']:>{pp_benefit_benefit_width}} | {row['adjustments']:>{pp_benefit_adjustments_width}} | {row['games']:>{pp_benefit_games_width}} | {row['target']:>{pp_benefit_target_width}} | {row['avg_opponent']:>{pp_benefit_avg_opponent_width}} |\n"
            )
        f.write("\n")
