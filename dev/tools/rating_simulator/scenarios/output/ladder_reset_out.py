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
    pp_finalizations: List[Dict] = None,
    output_dir: str = "simulation_results",
) -> None:
    """Save ladder reset simulation results.

    Args:
        players: List of player objects
        results: List of match results
        pp_finalizations: List of PP batch finalization events
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
            "Simulates a ladder reset where all players start at 1000 rating. Players have target ratings they will tend towards over time, with matchmaking favoring closer matches and players who have played fewer games.\n\n"
        )

        # Create player lookup dictionary
        players_dict = {player.name: player for player in players}

        # Write overall statistics first (like the old format)
        write_overall_stats(f, players, results)

        # Write largest PP batch adjustments
        write_largest_pp_batch_adjustments(f, pp_finalizations, players_dict)

        # Write detailed individual match information
        f.write("## Detailed Individual Match Information\n\n")

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


def write_largest_pp_batch_adjustments(
    f, pp_finalizations: List[Dict], players_dict: Dict[str, Dict]
) -> None:
    """Write the 10 largest batched Proven Potential adjustments for new players.

    Ranks by absolute total adjustment across all triggers in each batch.

    Args:
        f: File object to write to
        pp_finalizations: List of PP batch events
        players_dict: Dictionary mapping player names to player data
    """
    f.write("## 10 Largest Batched Proven Potential Adjustments for New Players\n\n")
    f.write(
        "These show the total PP adjustments applied in batches (sum across multiple triggers for the same new player, "
        "applied all at once after final tracking). Includes rating before/after the entire batch at application time.\n\n"
    )

    if not pp_finalizations:
        f.write("No PP finalizations occurred.\n\n")
        return

    # Sort by |total_adj_new|, descending, filter non-zero
    batches = [b for b in pp_finalizations if abs(b["total_adj_new"]) > 0.01]
    batches.sort(key=lambda x: abs(x["total_adj_new"]), reverse=True)

    # Take top 10
    top_batches = batches[:10]

    if not top_batches:
        f.write("No significant batched PP adjustments found.\n\n")
        return

    f.write(f"Top {len(top_batches)} largest batched PP adjustments:\n\n")

    for i, batch in enumerate(top_batches, 1):
        new_player = batch["new_player"]
        total_adj_new = batch["total_adj_new"]
        before = batch["before_rating"]
        after = batch["after_rating"]
        applied_at = batch["applied_at_match"]
        num_triggers = batch["num_triggers"]
        trigger_matches = batch["trigger_matches"]
        est_adjustments = batch["est_adjustments"]  # dict est_name: total_adj

        f.write(
            f"### #{i}: New Player {new_player} (Batch Applied at Match {applied_at}, Total Impact: {abs(total_adj_new):.1f})\n\n"
        )

        # Total batch adjustment for new player
        f.write(
            f"**New Player Total Batch Adjustment:** Δ{total_adj_new:+.1f} (from {num_triggers} triggers at matches {trigger_matches})\n"
        )
        f.write(
            f"**New Player Ratings:** Before Batch: {before:.1f} → After Batch: {after:.1f} (Δ{total_adj_new:+.1f})\n"
        )

        # Est adjustments (summed per est if multiple)
        f.write("**Established Players Total Adjustments:**\n")
        for est_name, adj_est in est_adjustments.items():
            if abs(adj_est) > 0.01:  # Only show meaningful
                est_player = players_dict.get(est_name)
                est_true = est_player.target_rating if est_player else "N/A"
                f.write(f"- {est_name} (True Skill: {est_true}): Δ{adj_est:+.1f}\n")

        # New player true skill
        new_player_obj = players_dict.get(new_player)
        new_true = new_player_obj.target_rating if new_player_obj else "N/A"
        f.write(f"**New Player True Skill:** {new_true}\n\n")

    f.write("\n")


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
            "pp_applicable": match.get("pp_applicable", False),
            "pp_scaling": match.get("pp_scaling", 1.0),
            "pp_crossed_thresholds": match.get("pp_crossed_thresholds", 0),
            "adjusted_player_change": match.get("player_adjusted_change"),
            "adjusted_opponent_change": match.get("opponent_adjusted_change"),
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
    player_games_before = match["player_games_played_before"]
    opponent_games_before = match["opponent_games_played_before"]

    # Get player objects
    challenger_data = players.get(player_id)
    opponent_data = players.get(opponent_id)

    if challenger_data:
        challenger_name = challenger_data.name
        challenger_target = challenger_data.target_rating
        challenger_details = f"{challenger_name} ({challenger_target})"
    else:
        challenger_details = player_id

    if opponent_data:
        opponent_name = opponent_data.name
        opponent_target = opponent_data.target_rating
        opponent_details = f"{opponent_name} ({opponent_target})"
    else:
        opponent_details = opponent_id

    # Calculate original rating changes (before PP)
    player_change = match["player_rating_after"] - match["player_rating_before"]
    opponent_change = match["opponent_rating_after"] - match["opponent_rating_before"]

    # Get adjusted rating changes (after PP)
    player_adjusted_change = match.get("player_adjusted_change", player_change)
    opponent_adjusted_change = match.get("opponent_adjusted_change", opponent_change)

    # Determine winner and loser for adjusted changes
    if player_won:
        winner_adjusted = player_adjusted_change
        loser_adjusted = opponent_adjusted_change
    else:
        winner_adjusted = opponent_adjusted_change
        loser_adjusted = player_adjusted_change

    # Prepare data for dynamic column width calculation
    table_rows = [
        {
            "category": f"Challenger ({player_games_before} Games)",
            "details": challenger_details,
        },
        {
            "category": f"Opponent ({opponent_games_before} Games)",
            "details": opponent_details,
        },
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
                "details": f"Winner: {player_change if player_won else opponent_change:.1f}, Loser: {opponent_change if player_won else player_change:.1f}",
            },
            {
                "category": "Rating Changes after PP",
                "details": f"Winner: {match.get('adjusted_winner_change', winner_adjusted):.1f}, Loser: {match.get('adjusted_loser_change', loser_adjusted):.1f}",
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
