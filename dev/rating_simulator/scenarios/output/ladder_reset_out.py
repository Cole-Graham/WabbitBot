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
        output_dir: Directory to save output files (relative to workspace root)
    """
    # Get the workspace root directory (3 levels up from this file)
    workspace_root = os.path.dirname(
        os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    )

    # Create the full path to the output directory
    output_path = os.path.join(workspace_root, output_dir)

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

        # Write first 30 matches
        f.write("### First 100 Matches\n\n")
        for i, match in enumerate(results[:100], 1):
            f.write(f"#### Match {i}\n\n")
            write_simple_match_details(f, match)
            f.write("\n")

        # Write last 10 matches
        if len(results) > 100:
            f.write("### Last 10 Matches\n\n")
            for i, match in enumerate(results[-10:], len(results) - 9):
                f.write(f"#### Match {i}\n\n")
                write_simple_match_details(f, match)
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


def write_simple_match_details(f, match: Dict) -> None:
    """Write simple match details in the new format.

    Args:
        f: File object to write to
        match: Match data dictionary
    """
    # Get player data from new format
    player_id = match["player_id"]
    opponent_id = match["opponent_id"]
    player_won = match["player_won"]

    # Calculate rating changes
    player_change = match["player_rating_after"] - match["player_rating_before"]
    opponent_change = match["opponent_rating_after"] - match["opponent_rating_before"]

    # Determine winner and loser
    winner = player_id if player_won else opponent_id
    loser = opponent_id if player_won else player_id
    winner_change = player_change if player_won else opponent_change
    loser_change = opponent_change if player_won else player_change

    # Write the simple table format
    f.write("| Category                 | Details                    |\n")
    f.write("|--------------------------|----------------------------|\n")
    f.write(f"| Challenger               | {player_id} |\n")
    f.write(f"| Opponent                 | {opponent_id} |\n")
    f.write(
        f"| Result                   | {'Win' if player_won else 'Loss'}                        |\n"
    )
    f.write(
        f"| Challenger Rating        | {match['player_rating_before']:.1f} -> {match['player_rating_after']:.1f}           |\n"
    )
    f.write(
        f"| Opponent Rating          | {match['opponent_rating_before']:.1f} -> {match['opponent_rating_after']:.1f}           |\n"
    )
    f.write(
        f"| Win Probability          | {match['win_probability']*100:.1f}%                      |\n"
    )
    f.write(
        f"| Challenger Confidence    | {match['player_confidence']:.2f}                       |\n"
    )
    f.write(
        f"| Opponent Confidence      | {match['opponent_confidence']:.2f}                       |\n"
    )
    f.write(
        f"| Challenger Variety Bonus | {match.get('p1_variety_bonus', 0.0):.2f}                       |\n"
    )
    f.write(
        f"| Opponent Variety Bonus   | {match.get('p2_variety_bonus', 0.0):.2f}                       |\n"
    )

    # Determine winner and loser multipliers
    if player_won:
        winner_multiplier = match["p1_multiplier"]
        loser_multiplier = match["p2_multiplier"]
    else:
        winner_multiplier = match["p2_multiplier"]
        loser_multiplier = match["p1_multiplier"]

    f.write(
        f"| Rating Changes           | Winner: {winner_change:.1f}, Loser: {loser_change:.1f} |\n"
    )
    f.write(
        f"| Multipliers              | Winner: {winner_multiplier:.2f}, Loser: {loser_multiplier:.2f}  |\n"
    )

    # Add proven potential details if available
    proven_potential_details = match.get("proven_potential_details", [])
    opponent_proven_potential_details = match.get(
        "opponent_proven_potential_details", []
    )

    if proven_potential_details or opponent_proven_potential_details:
        f.write("| Proven Potential        | ")
        if proven_potential_details:
            f.write(f"Player: {len(proven_potential_details)} adjustments")
        if opponent_proven_potential_details:
            if proven_potential_details:
                f.write(", ")
            f.write(f"Opponent: {len(opponent_proven_potential_details)} adjustments")
        f.write(" |\n")

        # Show details of proven potential adjustments
        for i, detail in enumerate(proven_potential_details[:3]):  # Show first 3
            f.write(
                f"| PP Detail {i+1}           | Historical Match {detail['previous_match_number']}: {detail['gap_closure_percent']:.1%} gap closed, {detail['rating_adjustment']:+d} pts |\n"
            )

        for i, detail in enumerate(
            opponent_proven_potential_details[:3]
        ):  # Show first 3
            # Note: This shows compensation going to the opponent from the historical match
            # The actual player receiving compensation is from the historical match, not the current opponent
            f.write(
                f"| Opp PP Detail {i+1}      | Historical Match {detail['previous_match_number']} opponent: {detail['gap_closure_percent']:.1%} gap closed, {detail['rating_adjustment']:+d} pts |\n"
            )
