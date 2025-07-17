"""Main output file for ladder reset scenario."""

import os
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
    filename = os.path.join(output_path, f"ladder_reset_{timestamp}.md")

    with open(filename, "w", encoding="utf-8") as f:
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
        f.write("### First 30 Matches\n\n")
        for i, match in enumerate(results[:30], 1):
            f.write(f"#### Match {i}\n\n")
            write_simple_match_details(f, match)
            f.write("\n")

        # Write last 10 matches
        if len(results) > 30:
            f.write("### Last 10 Matches\n\n")
            for i, match in enumerate(results[-10:], len(results) - 9):
                f.write(f"#### Match {i}\n\n")
                write_simple_match_details(f, match)
                f.write("\n")

    print(f"Results saved to {filename}")


def write_simple_match_details(f, match: Dict) -> None:
    """Write simple match details in the old format.

    Args:
        f: File object to write to
        match: Match data dictionary
    """
    # Get player data
    p1 = match["player1"]
    p2 = match["player2"]
    p1_won = match["winner"] == p1["name"]

    # Calculate rating changes
    p1_change = p1["rating_after"] - p1["rating"]
    p2_change = p2["rating_after"] - p2["rating"]

    # Determine winner and loser
    winner = p1 if p1_won else p2
    loser = p2 if p1_won else p1
    winner_change = p1_change if p1_won else p2_change
    loser_change = p2_change if p1_won else p1_change

    # Write the simple table format from the old output
    f.write("| Category                 | Details                    |\n")
    f.write("|--------------------------|----------------------------|\n")
    f.write(
        f"| Challenger               | {p1['name']} (Target: {p1['target_rating']}) |\n"
    )
    f.write(
        f"| Opponent                 | {p2['name']} (Target: {p2['target_rating']}) |\n"
    )
    f.write(
        f"| Result                   | {'Win' if p1_won else 'Loss'}                        |\n"
    )
    f.write(
        f"| Challenger Rating        | {p1['rating']:.1f} -> {p1['rating_after']:.1f}           |\n"
    )
    f.write(
        f"| Opponent Rating          | {p2['rating']:.1f} -> {p2['rating_after']:.1f}           |\n"
    )
    f.write(
        f"| Win Probability          | {match['win_probability']*100:.1f}%                      |\n"
    )
    f.write(
        f"| Challenger Confidence    | {match['p1_confidence']:.2f}                       |\n"
    )
    f.write(
        f"| Opponent Confidence      | {match['p2_confidence']:.2f}                       |\n"
    )
    f.write(
        f"| Challenger Variety Bonus | {match.get('p1_variety_bonus', 0.0):.2f}                       |\n"
    )
    f.write(
        f"| Opponent Variety Bonus   | {match.get('p2_variety_bonus', 0.0):.2f}                       |\n"
    )
    # Determine winner and loser multipliers
    if p1["name"] == match["winner"]:
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
