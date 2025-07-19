#!/usr/bin/env python3
"""
Match Lookup Script for Rating Simulator

This script allows you to look up details for matches from simulation runs.
It can read the most recent simulation data and output match details to markdown files.

Usage:
    python match_lookup.py [options]

Options:
    --match <number>           Look up a specific match number
    --range <start>-<end>      Look up a range of matches (e.g., 1-10)
    --output <filename>        Output file name (default: match_details.md)
    --data-file <path>         Specify a specific data file (default: most recent)
    --list-recent              List recent simulation files
    --help                     Show this help message
"""

import os
import sys
import json
import argparse
from datetime import datetime
from pathlib import Path
from typing import List, Dict, Optional, Tuple
import glob

# Add the project root to the Python path
project_root = str(Path(__file__).parent.parent.parent)
if project_root not in sys.path:
    sys.path.append(project_root)


def find_most_recent_simulation_file() -> Optional[str]:
    """Find the most recent simulation results file.

    Returns:
        Path to the most recent simulation file, or None if not found
    """
    # Look for simulation results in the correct directory
    simulation_dir = (
        Path(project_root) / "dev" / "rating_simulator" / "simulation_results"
    )

    if not simulation_dir.exists():
        return None

    # Look for ladder reset simulation JSON files
    pattern = simulation_dir / "ladder_reset_*.json"
    files = list(glob.glob(str(pattern)))

    if not files:
        return None

    # Sort by modification time (most recent first)
    files.sort(key=lambda x: os.path.getmtime(x), reverse=True)
    return files[0]


def list_recent_simulation_files() -> None:
    """List recent simulation files with their timestamps."""
    simulation_dir = (
        Path(project_root) / "dev" / "rating_simulator" / "simulation_results"
    )

    if not simulation_dir.exists():
        print("No simulation_results directory found.")
        return

    pattern = simulation_dir / "ladder_reset_*.json"
    files = list(glob.glob(str(pattern)))

    if not files:
        print("No ladder reset simulation files found.")
        return

    # Sort by modification time (most recent first)
    files.sort(key=lambda x: os.path.getmtime(x), reverse=True)

    print("Recent simulation files:")
    print("-" * 80)
    for i, file_path in enumerate(files[:10], 1):  # Show last 10 files
        file_name = os.path.basename(file_path)
        mod_time = datetime.fromtimestamp(os.path.getmtime(file_path))

        # Try to get metadata from the JSON file
        try:
            with open(file_path, "r", encoding="utf-8") as f:
                data = json.load(f)
                metadata = data.get("metadata", {})
                total_matches = metadata.get("total_matches", "Unknown")
                total_players = metadata.get("total_players", "Unknown")
                timestamp = metadata.get("timestamp", "Unknown")
        except:
            total_matches = "Unknown"
            total_players = "Unknown"
            timestamp = "Unknown"

        print(f"{i:2d}. {file_name}")
        print(f"    Modified: {mod_time.strftime('%Y-%m-%d %H:%M:%S')}")
        print(f"    Players: {total_players}, Matches: {total_matches}")
        if timestamp != "Unknown":
            print(f"    Created: {timestamp}")
        print()

    if len(files) > 10:
        print(f"... and {len(files) - 10} more files")


def load_simulation_data(file_path: str) -> Dict:
    """Load simulation data from JSON file.

    Args:
        file_path: Path to the JSON simulation file

    Returns:
        Dictionary containing simulation data
    """
    with open(file_path, "r", encoding="utf-8") as f:
        data = json.load(f)

    return data


def format_match_details(
    match: Dict,
    players: Dict[str, Dict],
    player_match_number: int = None,
    match_history: List[Dict] = None,
) -> str:
    """Format match details for markdown output to match original format.

    Args:
        match: Match data dictionary
        players: Dictionary mapping player IDs to player data
        player_match_number: Player's personal match number (optional)
    """
    output = []

    # Show both global match number and player match number if available
    if player_match_number is not None:
        output.append(
            f"#### Match {match['match_number']} (Player Match #{player_match_number})"
        )
    else:
        output.append(f"#### Match {match['match_number']}")
    output.append("")

    # Get player names
    challenger_name = players.get(match["player_id"], {}).get(
        "name", match["player_id"]
    )
    opponent_name = players.get(match["opponent_id"], {}).get(
        "name", match["opponent_id"]
    )

    # Calculate rating changes
    challenger_change = match["player_rating_after"] - match["player_rating_before"]
    opponent_change = match["opponent_rating_after"] - match["opponent_rating_before"]

    # Determine winner and loser
    winner_change = challenger_change if match["player_won"] else opponent_change
    loser_change = opponent_change if match["player_won"] else challenger_change

    # Determine winner and loser multipliers
    if match["player_won"]:
        winner_multiplier = match["p1_multiplier"]
        loser_multiplier = match["p2_multiplier"]
    else:
        winner_multiplier = match["p2_multiplier"]
        loser_multiplier = match["p1_multiplier"]

    # Write the table format matching the original
    output.append("| Category                 | Details                    |")
    output.append("|--------------------------|----------------------------|")
    output.append(f"| Challenger               | {challenger_name} |")
    output.append(f"| Opponent                 | {opponent_name} |")
    output.append(
        f"| Result                   | {'Win' if match['player_won'] else 'Loss'}                        |"
    )
    output.append(
        f"| Challenger Rating        | {match['player_rating_before']:.1f} -> {match['player_rating_after']:.1f}           |"
    )
    output.append(
        f"| Opponent Rating          | {match['opponent_rating_before']:.1f} -> {match['opponent_rating_after']:.1f}           |"
    )
    output.append(
        f"| Win Probability          | {match['win_probability']*100:.1f}%                      |"
    )
    output.append(
        f"| Challenger Confidence    | {match['player_confidence']:.2f}                       |"
    )
    output.append(
        f"| Opponent Confidence      | {match['opponent_confidence']:.2f}                       |"
    )
    output.append(
        f"| Challenger Variety Bonus | {match['p1_variety_bonus']:.2f}                       |"
    )
    output.append(
        f"| Opponent Variety Bonus   | {match['p2_variety_bonus']:.2f}                       |"
    )
    output.append(
        f"| Rating Changes           | Winner: {winner_change:.1f}, Loser: {loser_change:.1f} |"
    )
    output.append(
        f"| Multipliers              | Winner: {winner_multiplier:.2f}, Loser: {loser_multiplier:.2f}  |"
    )

    # Add proven potential details if available
    proven_potential_details = match.get("proven_potential_details", [])
    opponent_proven_potential_details = match.get(
        "opponent_proven_potential_details", []
    )

    if proven_potential_details or opponent_proven_potential_details:
        output.append("| Proven Potential        | ")
        if proven_potential_details:
            output.append(f"Player: {len(proven_potential_details)} adjustments")
        if opponent_proven_potential_details:
            if proven_potential_details:
                output.append(", ")
            output.append(
                f"Opponent: {len(opponent_proven_potential_details)} adjustments"
            )
        output.append(" |")

        # Show details of proven potential adjustments
        for i, detail in enumerate(proven_potential_details[:3]):  # Show first 3
            # Find the original match to get the player names for clarity
            original_match_number = detail["previous_match_number"]
            original_player_name = f"Match {original_match_number} player"
            original_opponent_name = f"Match {original_match_number} opponent"

            # Try to get the actual player names from match history if available
            if match_history:
                try:
                    original_match = next(
                        m
                        for m in match_history
                        if m["match_number"] == original_match_number
                    )
                    original_player_name = original_match["player_id"]
                    original_opponent_name = original_match["opponent_id"]
                except StopIteration:
                    pass  # Keep the generic names if match not found

            output.append(
                f"| PP Detail {i+1}           | Match {detail['previous_match_number']} - {original_player_name} gets {detail['player_adjustment']:+.0f} pts, {original_opponent_name} gets {detail['opponent_adjustment']:+.0f} pts compensation: {detail['gap_closure_percent']:.1%} gap closed |"
            )

        for i, detail in enumerate(
            opponent_proven_potential_details[:3]
        ):  # Show first 3
            # Find the original match to get the opponent name for clarity
            original_match_number = detail["previous_match_number"]
            original_opponent_name = f"Match {original_match_number} opponent"

            # Try to get the actual opponent name from match history if available
            if match_history:
                try:
                    original_match = next(
                        m
                        for m in match_history
                        if m["match_number"] == original_match_number
                    )
                    original_opponent_name = original_match["opponent_id"]
                except StopIteration:
                    pass  # Keep the generic name if match not found

            # Try to get the actual player name from match history if available
            original_player_name = f"Player_{detail['previous_match_number']}"
            if match_history:
                try:
                    original_match = next(
                        m
                        for m in match_history
                        if m["match_number"] == original_match_number
                    )
                    original_player_name = original_match["player_id"]
                except StopIteration:
                    pass  # Keep the generic name if match not found

            output.append(
                f"| Opp PP Detail {i+1}      | Match {detail['previous_match_number']} - {original_player_name} gets {detail['player_adjustment']:+.0f} pts, {original_opponent_name} gets {detail['opponent_adjustment']:+.0f} pts compensation: {detail['gap_closure_percent']:.1%} gap closed |"
            )

    return "\n".join(output)


def find_player_matches(
    matches: List[Dict], players: Dict[str, Dict], player_query: str
) -> List[int]:
    """Find all match numbers for a specific player.

    Args:
        matches: List of all matches
        players: Dictionary mapping player IDs to player data
        player_query: Player name or ID to search for

    Returns:
        List of match numbers where the player participated
    """
    player_matches = []

    # Try to find the player by name or ID
    player_id = None
    player_name = None

    # First try exact match by ID
    if player_query in players:
        player_id = player_query
        player_name = players[player_query]["name"]
    else:
        # Try to find by name (case-insensitive)
        for pid, player_data in players.items():
            if player_data["name"].lower() == player_query.lower():
                player_id = pid
                player_name = player_data["name"]
                break

    if not player_id:
        # Try partial name match
        for pid, player_data in players.items():
            if player_query.lower() in player_data["name"].lower():
                player_id = pid
                player_name = player_data["name"]
                break

    if not player_id:
        print(f"Error: Player '{player_query}' not found.")
        print("Available players:")
        for pid, player_data in players.items():
            print(f"  - {player_data['name']} (ID: {pid})")
        return []

    print(f"Found player: {player_name} (ID: {player_id})")

    # Find all matches where this player participated
    for match in matches:
        if match["player_id"] == player_id or match["opponent_id"] == player_id:
            player_matches.append(match["match_number"])

    return sorted(player_matches)


def lookup_matches(
    data_file: str,
    match_numbers: List[int],
    output_file: str,
    data: Dict = None,
    args=None,
) -> None:
    """Look up specific matches and output to markdown file.

    Args:
        data_file: Path to the simulation data file
        match_numbers: List of match numbers to look up
        output_file: Output file path
        data: Pre-loaded simulation data (optional)
    """
    if data is None:
        print(f"Reading simulation data from: {data_file}")
        data = load_simulation_data(data_file)

    matches = data.get("matches", [])
    players_data = data.get("players", [])
    metadata = data.get("metadata", {})

    if not matches:
        print("No matches found in the simulation file.")
        return

    print(f"Found {len(matches)} matches in the simulation.")
    print(f"Total players: {metadata.get('total_players', 'Unknown')}")

    # Create player lookup dictionary
    players = {player["id"]: player for player in players_data}

    # Filter matches by requested numbers
    available_matches = {m["match_number"]: m for m in matches}
    requested_matches = []

    for match_num in match_numbers:
        if match_num in available_matches:
            requested_matches.append(available_matches[match_num])
        else:
            print(f"Warning: Match {match_num} not found in simulation data.")

    if not requested_matches:
        print("No requested matches found in the simulation data.")
        return

    # Sort by match number
    requested_matches.sort(key=lambda x: x["match_number"])

    # Generate output
    output_lines = []
    output_lines.append("# Match Details Lookup")
    output_lines.append("")
    output_lines.append(f"**Source**: {os.path.basename(data_file)}")
    output_lines.append(
        f"**Generated**: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}"
    )
    output_lines.append(
        f"**Matches**: {', '.join(str(m['match_number']) for m in requested_matches)}"
    )
    output_lines.append("")

    # Add simulation metadata
    if metadata:
        output_lines.append("## Simulation Information")
        output_lines.append("")
        output_lines.append(f"- **Scenario**: {metadata.get('scenario', 'Unknown')}")
        output_lines.append(
            f"- **Total Players**: {metadata.get('total_players', 'Unknown')}"
        )
        output_lines.append(
            f"- **Total Matches**: {metadata.get('total_matches', 'Unknown')}"
        )
        output_lines.append(f"- **Created**: {metadata.get('timestamp', 'Unknown')}")
        output_lines.append("")

    # Calculate player match numbers if this is a player lookup
    player_match_numbers = {}
    if args.player:
        player_id = None
        for pid, player_data in players.items():
            if (
                player_data["name"].lower() == args.player.lower()
                or args.player.lower() in player_data["name"].lower()
            ):
                player_id = pid
                break

        if player_id:
            player_match_count = 1
            for match in requested_matches:
                if match["player_id"] == player_id or match["opponent_id"] == player_id:
                    player_match_numbers[match["match_number"]] = player_match_count
                    player_match_count += 1

    for match in requested_matches:
        player_match_number = player_match_numbers.get(match["match_number"])
        output_lines.append(
            format_match_details(match, players, player_match_number, matches)
        )
        output_lines.append("")

    # Write output file
    with open(output_file, "w", encoding="utf-8") as f:
        f.write("\n".join(output_lines))

    print(f"Match details written to: {output_file}")
    print(f"Looked up {len(requested_matches)} matches.")


def main():
    """Main function."""
    parser = argparse.ArgumentParser(
        description="Look up match details from simulation runs",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )

    parser.add_argument(
        "--match",
        type=int,
        action="append",
        help="Look up a specific match number (can be used multiple times)",
    )

    parser.add_argument(
        "--range", type=str, help="Look up a range of matches (e.g., 1-10)"
    )

    parser.add_argument(
        "--player",
        type=str,
        help="Look up all matches for a specific player (by name or ID)",
    )

    parser.add_argument(
        "--output",
        type=str,
        default=None,
        help="Output file name (default: auto-generated with timestamp)",
    )

    parser.add_argument(
        "--data-file",
        type=str,
        help="Specify a specific data file (default: most recent)",
    )

    parser.add_argument(
        "--list-recent", action="store_true", help="List recent simulation files"
    )

    args = parser.parse_args()

    if args.list_recent:
        list_recent_simulation_files()
        return

    # Determine data file
    if args.data_file:
        data_file = args.data_file
        if not os.path.exists(data_file):
            print(f"Error: Data file not found: {data_file}")
            return
    else:
        data_file = find_most_recent_simulation_file()
        if not data_file:
            print("Error: No simulation files found. Run a simulation first.")
            return
        print(f"Using most recent simulation file: {os.path.basename(data_file)}")

    # Load simulation data first (needed for player lookup)
    print(f"Reading simulation data from: {data_file}")
    data = load_simulation_data(data_file)
    matches = data.get("matches", [])
    players_data = data.get("players", [])
    players = {player["id"]: player for player in players_data}

    # Determine match numbers
    match_numbers = []
    player_name = None

    if args.match:
        match_numbers.extend(args.match)

    if args.range:
        try:
            start, end = map(int, args.range.split("-"))
            match_numbers.extend(range(start, end + 1))
        except ValueError:
            print("Error: Invalid range format. Use 'start-end' (e.g., 1-10)")
            return

    if args.player:
        player_matches = find_player_matches(matches, players, args.player)
        if not player_matches:
            return
        match_numbers.extend(player_matches)

        # Get player name for filename
        for pid, player_data in players.items():
            if (
                player_data["name"].lower() == args.player.lower()
                or args.player.lower() in player_data["name"].lower()
            ):
                player_name = player_data["name"]
                break

    if not match_numbers:
        print(
            "Error: No matches specified. Use --match, --range, or --player to specify matches."
        )
        return

    # Remove duplicates and sort
    match_numbers = sorted(list(set(match_numbers)))

    # Determine output file
    if args.output:
        output_file = args.output
    else:
        # Auto-generate output file with matching timestamp
        data_file_name = os.path.basename(data_file)
        # Extract timestamp from data file name (e.g., ladder_reset_20241201_143022.json)
        if "_" in data_file_name and ".json" in data_file_name:
            # Extract the timestamp part
            timestamp_part = data_file_name.replace("ladder_reset_", "").replace(
                ".json", ""
            )

            # Include player name in filename if looking up player matches
            if player_name:
                # Clean player name for filename (replace spaces and special chars)
                clean_player_name = player_name.replace(" ", "_").replace("-", "_")
                output_file = f"match_lookup_{clean_player_name}_{timestamp_part}.md"
            else:
                output_file = f"match_lookup_{timestamp_part}.md"
        else:
            # Fallback to current timestamp
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            if player_name:
                clean_player_name = player_name.replace(" ", "_").replace("-", "_")
                output_file = f"match_lookup_{clean_player_name}_{timestamp}.md"
            else:
                output_file = f"match_lookup_{timestamp}.md"

        # Save to the same directory as the data file
        output_file = os.path.join(os.path.dirname(data_file), output_file)

    # Look up matches
    lookup_matches(data_file, match_numbers, output_file, data, args)


if __name__ == "__main__":
    main()
