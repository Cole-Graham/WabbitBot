"""Main output file for ladder reset scenario."""

import os
import json
from datetime import datetime
from typing import Dict, List

from .match_details import write_match_details
from .overall_stats import write_overall_stats


def save_ladder_reset_results(
    players: List,
    results: List[Dict],
    output_dir: str = "simulation_results",
) -> None:
    """Save ladder reset simulation results.

    Args:
        players: List of player objects
        results: List of match results
        output_dir: Directory to save output files (relative to dev/rating_simulator/)
    """
    # Get the dev/tools/rating_simulator directory (3 levels up from this file)
    simulator_dir = os.path.dirname(
        os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    )

    # Create the full path to the output directory
    output_path = os.path.join(simulator_dir, output_dir)

    # Create output directory if it doesn't exist
    os.makedirs(output_path, exist_ok=True)

    # Generate filename based on timestamp
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    md_filename = os.path.join(output_path, f"ladder_reset_{timestamp}.md")
    json_filename = os.path.join(output_path, f"ladder_reset_{timestamp}.json")

    # Save structured data for match lookup
    save_structured_data(players, results, json_filename)

    # Save markdown report
    with open(md_filename, "w", encoding="utf-8") as f:
        # Write header
        f.write("# Ladder Reset Simulation Results\n\n")
        f.write(
            "Simulates a ladder reset where all players start at 1500 rating. Players have target ratings they will tend towards over time, with matchmaking favoring closer matches and players who have played fewer games.\n\n"
        )

        # Write overall statistics first (like the old format)
        write_overall_stats(f, players, results)

        # Write detailed individual match information
        f.write("## Detailed Individual Match Information\n\n")

        # Create player lookup dictionary
        players_dict = {player.name: player for player in players}

        # Write first 30 matches
        f.write("### First 100 Matches\n\n")
        for i, match in enumerate(results[:100], 1):
            f.write(f"#### Match {i}\n\n")
            write_simple_match_details(f, match, players_dict)
            f.write("\n")

        # Write last 10 matches
        if len(results) > 100:
            f.write("### Last 10 Matches\n\n")
            for i, match in enumerate(results[-10:], len(results) - 9):
                f.write(f"#### Match {i}\n\n")
                write_simple_match_details(f, match, players_dict)
                f.write("\n")

    print(f"Results saved to {md_filename}")
    print(f"Structured data saved to {json_filename}")


def save_structured_data(
    players: List, results: List[Dict], json_filename: str
) -> None:
    """Save structured data for match lookup.

    Args:
        players: List of player objects
        results: List of match results
        json_filename: Path to save the JSON file
    """
    # Convert players to serializable format
    players_data = []
    for player in players:
        player_data = {
            "id": player.name,  # Use name as ID since Player class doesn't have id
            "name": player.name,
            "rating": player.rating,
            "target_rating": player.target_rating,
            "confidence": getattr(player, "confidence", 0.0),  # May not exist
            "games_played": player.games_played,
            "wins": getattr(player, "wins", 0),  # May not exist
            "losses": getattr(player, "losses", 0),  # May not exist
            "activity_multiplier": getattr(
                player, "activity_multiplier", 1.0
            ),  # May not exist
        }
        players_data.append(player_data)

    # Convert results to serializable format with match numbers
    matches_data = []
    for i, match in enumerate(results, 1):
        match_data = {
            "match_number": i,
            "player_id": match["player_id"],
            "opponent_id": match["opponent_id"],
            "player_won": match["player_won"],
            "player_rating_before": match["player_rating_before"],
            "player_rating_after": match["player_rating_after"],
            "opponent_rating_before": match["opponent_rating_before"],
            "opponent_rating_after": match["opponent_rating_after"],
            "win_probability": match["win_probability"],
            "player_confidence": match["player_confidence"],
            "opponent_confidence": match["opponent_confidence"],
            "p1_variety_bonus": match.get("p1_variety_bonus", 0.0),
            "p2_variety_bonus": match.get("p2_variety_bonus", 0.0),
            "p1_multiplier": match["p1_multiplier"],
            "p2_multiplier": match["p2_multiplier"],
            "proven_potential_details": match.get("proven_potential_details", []),
            "opponent_proven_potential_details": match.get(
                "opponent_proven_potential_details", []
            ),
        }
        matches_data.append(match_data)

    # Create the complete data structure
    simulation_data = {
        "metadata": {
            "scenario": "ladder_reset",
            "timestamp": datetime.now().isoformat(),
            "total_players": len(players),
            "total_matches": len(results),
        },
        "players": players_data,
        "matches": matches_data,
    }

    # Save to JSON file
    with open(json_filename, "w", encoding="utf-8") as f:
        json.dump(simulation_data, f, indent=2, ensure_ascii=False)


def write_simple_match_details(f, match: Dict, players: Dict[str, Dict]) -> None:
    """Write simple match details in the new format.

    Args:
        f: File object to write to
        match: Match data dictionary
        players: Dictionary mapping player IDs to player data
    """
    # Get player data from new format
    player_id = match["player_id"]
    opponent_id = match["opponent_id"]
    player_won = match["player_won"]

    # Get player objects
    challenger_data = players.get(player_id)
    opponent_data = players.get(opponent_id)

    if challenger_data:
        challenger_name = challenger_data.name
        challenger_target = challenger_data.target_rating
        challenger_name = f"{challenger_name} ({challenger_target})"
    else:
        challenger_name = player_id

    if opponent_data:
        opponent_name = opponent_data.name
        opponent_target = opponent_data.target_rating
        opponent_name = f"{opponent_name} ({opponent_target})"
    else:
        opponent_name = opponent_id

    # Calculate rating changes
    player_change = match["player_rating_after"] - match["player_rating_before"]
    opponent_change = match["opponent_rating_after"] - match["opponent_rating_before"]

    # Determine winner and loser
    winner = player_id if player_won else opponent_id
    loser = opponent_id if player_won else player_id
    winner_change = player_change if player_won else opponent_change
    loser_change = opponent_change if player_won else player_change

    # Prepare data for dynamic column width calculation
    table_rows = [
        {"category": "Challenger", "details": challenger_name},
        {"category": "Opponent", "details": opponent_name},
        {"category": "Result", "details": "Win" if player_won else "Loss"},
        {
            "category": "Challenger Rating",
            "details": f"{match['player_rating_before']:.1f} -> {match['player_rating_after']:.1f}",
        },
        {
            "category": "Opponent Rating",
            "details": f"{match['opponent_rating_before']:.1f} -> {match['opponent_rating_after']:.1f}",
        },
        {
            "category": "Win Probability",
            "details": f"{match['win_probability']*100:.1f}%",
        },
        {
            "category": "Challenger Confidence",
            "details": f"{match['player_confidence']:.2f}",
        },
        {
            "category": "Opponent Confidence",
            "details": f"{match['opponent_confidence']:.2f}",
        },
        {
            "category": "Challenger Variety Bonus",
            "details": f"{match.get('p1_variety_bonus', 0.0):.2f}",
        },
        {
            "category": "Opponent Variety Bonus",
            "details": f"{match.get('p2_variety_bonus', 0.0):.2f}",
        },
    ]

    # Determine winner and loser multipliers
    if player_won:
        winner_multiplier = match["p1_multiplier"]
        loser_multiplier = match["p2_multiplier"]
    else:
        winner_multiplier = match["p2_multiplier"]
        loser_multiplier = match["p1_multiplier"]

    table_rows.extend(
        [
            {
                "category": "Rating Changes",
                "details": f"Winner: {winner_change:.1f}, Loser: {loser_change:.1f}",
            },
            {
                "category": "Multipliers",
                "details": f"Winner: {winner_multiplier:.2f}, Loser: {loser_multiplier:.2f}",
            },
        ]
    )

    # Calculate column widths
    def get_max_width(header, rows, key):
        if not rows:
            return len(header)
        return max(
            len(header),
            max(len(str(row.get(key, ""))) for row in rows),
        )

    category_width = get_max_width("Category", table_rows, "category")
    details_width = get_max_width("Details", table_rows, "details")

    # Write header with dynamic widths
    f.write(f"| {'Category':<{category_width}} | {'Details':<{details_width}} |\n")
    f.write(f"| {'-' * category_width} | {'-' * details_width} |\n")

    # Write data rows with dynamic widths
    for row in table_rows:
        f.write(
            f"| {row['category']:<{category_width}} | {row['details']:<{details_width}} |\n"
        )
