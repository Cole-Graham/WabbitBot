"""Functions for generating overall statistics tables."""

from typing import Dict, List
from collections import defaultdict
import statistics
from .base_output import get_max_width


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

    # Write overall statistics table in old format
    f.write("| Metric                    |  Value |\n")
    f.write("|---------------------------|--------|\n")
    f.write(f"| Total Players             | {total_players:6.1f} |\n")
    f.write(f"| Total Matches             | {total_matches:6.1f} |\n")
    f.write(f"| Average Final Rating      | {statistics.mean(final_ratings):6.1f} |\n")
    f.write(
        f"| Median Final Rating       | {statistics.median(final_ratings):6.1f} |\n"
    )
    f.write(f"| Rating Standard Deviation | {statistics.stdev(final_ratings):6.1f} |\n")
    f.write(f"| Average Rating Change     | {statistics.mean(rating_changes):6.1f} |\n")
    f.write(
        f"| Median Rating Change      | {statistics.median(rating_changes):6.1f} |\n\n"
    )

    # Write match statistics
    f.write("## Match Statistics\n\n")
    f.write("| Metric                    |  Value |\n")
    f.write("|---------------------------|--------|\n")
    f.write(f"| Average Matches Played    | {avg_matches:6.1f} |\n")
    f.write(f"| Median Matches Played     | {median_matches:6.1f} |\n")
    f.write(f"| Maximum Matches Played    | {max_matches:6.1f} |\n")
    f.write(f"| Minimum Matches Played    | {min_matches:6.1f} |\n\n")

    # Write target rating achievement
    write_target_achievement(f, target_groups, player_wins, player_games)

    # Write rating distribution
    write_rating_distribution(f, final_ratings, total_players)

    # Write win rate analysis
    write_win_rate_analysis(f, players, player_wins, player_games)

    # Write top 10 players
    write_top_players(f, players, player_wins, player_games, results)

    # Write proven potential statistics
    write_proven_potential_stats(f, results)


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

    # Write in old format
    f.write("| Target Rating | Players | Achieved | % Achieved | Avg Final Rating |\n")
    f.write("|---------------|---------|----------|------------|------------------|\n")
    for stat in target_stats:
        f.write(
            f"| {stat['target']:<13} | {stat['players']:>7} | {stat['achieved']:>8} | {stat['percent']:>10.1f}% | {stat['avg_rating']:>15.1f} |\n"
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

    # Write in old format
    f.write("| Rating Range | Players | % of Total |\n")
    f.write("|--------------|---------|------------|\n")
    for stat in dist_stats:
        f.write(
            f"| {stat['range']:<12} | {stat['players']:>7} | {stat['percent']:>10.1f}% |\n"
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

    # Write in old format
    f.write("| Rating Range | Avg Win Rate | Games per Player |\n")
    f.write("|--------------|--------------|------------------|\n")
    for stat in win_stats:
        f.write(
            f"| {stat['range']:<12} | {stat['win_rate']:>12.1f}% | {stat['games']:>16.1f} |\n"
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

    # Write in old format with variety bonus column
    f.write(
        "| Rank | Player   | Final Rating | Target Rating | Win Rate | Games Played | Variety Bonus |\n"
    )
    f.write(
        "|------|----------|--------------|---------------|----------|--------------|---------------|\n"
    )
    for i, player in enumerate(top_100, 1):
        f.write(
            f"| {i:<4} | {player['name']:<8} | {player['rating']:>11.1f} | {player['target']:>13} | {player['win_rate']:>8.1f}% | {player['games']:>12} | {player['variety_bonus']:>13.2f} |\n"
        )
    f.write("\n")


def write_proven_potential_stats(f, results: List[Dict]) -> None:
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
                player_pp_benefits[player_id] += detail.get("rating_adjustment", 0)

        if opponent_pp_details:
            total_pp_matches += 1
            opponent_id = match["opponent_id"]
            player_pp_adjustments[opponent_id] += len(opponent_pp_details)
            total_pp_adjustments += len(opponent_pp_details)

            # Calculate total benefit for this opponent
            for detail in opponent_pp_details:
                player_pp_benefits[opponent_id] += detail.get("rating_adjustment", 0)

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

    # Write overall proven potential statistics
    f.write("| Metric                           | Value |\n")
    f.write("|----------------------------------|-------|\n")
    f.write(f"| Total Proven Potential Adjustments | {total_pp_adjustments:>6} |\n")
    f.write(f"| Matches with PP Adjustments        | {total_pp_matches:>6} |\n")
    f.write(f"| PP Match Percentage                | {pp_match_percentage:>6.1f}% |\n")
    f.write(
        f"| Avg Adjustments per PP Match       | {avg_adjustments_per_pp_match:>6.1f} |\n"
    )
    f.write(
        f"| Players with PP Adjustments        | {len(player_pp_adjustments):>6} |\n"
    )
    f.write("\n")

    # Write top players by proven potential adjustments
    if top_pp_players:
        f.write("### Top Players by Proven Potential Adjustments\n\n")
        f.write("| Rank | Player   | PP Adjustments | Total Benefit |\n")
        f.write("|------|----------|----------------|---------------|\n")
        for i, (player, count) in enumerate(top_pp_players, 1):
            benefit = player_pp_benefits.get(player, 0.0)
            f.write(f"| {i:<4} | {player:<8} | {count:>14} | {benefit:>13.1f} |\n")
        f.write("\n")

    # Write top players by proven potential benefits
    if top_pp_beneficiaries:
        f.write("### Top Players by Proven Potential Benefits\n\n")
        f.write("| Rank | Player   | Total Benefit | PP Adjustments |\n")
        f.write("|------|----------|---------------|----------------|\n")
        for i, (player, benefit) in enumerate(top_pp_beneficiaries, 1):
            count = player_pp_adjustments.get(player, 0)
            f.write(f"| {i:<4} | {player:<8} | {benefit:>13.1f} | {count:>14} |\n")
        f.write("\n")
