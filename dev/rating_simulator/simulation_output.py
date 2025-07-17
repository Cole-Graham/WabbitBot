import os
from datetime import datetime
from typing import Dict, List
from rating_calculator import RATING_CONFIG, MULTIPLIER_CONFIG
from simulation_config import MATCH_CONFIG, SIMULATION_CONFIG


def save_simulation_results(
    results: List[Dict], output_dir: str = "simulation_results"
):
    """Save simulation results to a markdown file.

    Args:
        results: List of match results to save
        output_dir: Name of the output directory (default: "simulation_results")
                   The directory will be created relative to this script's location
    """
    # Get the directory where this script is located
    script_dir = os.path.dirname(os.path.abspath(__file__))

    # Create the full path to the output directory
    output_path = os.path.join(script_dir, output_dir)

    # Create output directory if it doesn't exist
    os.makedirs(output_path, exist_ok=True)

    # Create filename with timestamp
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    output_file = os.path.join(output_path, f"rating_simulation_{timestamp}.md")

    with open(output_file, "w", encoding="utf-8") as f:
        f.write("# Rating Simulation Results\n\n")

        f.write(f"## Configuration\n\n")

        f.write(f"Player Target ELO: {MATCH_CONFIG['player_target_elo']}\n")
        f.write(f"Player Win Probability: {MATCH_CONFIG['player_win_probability']}\n")
        f.write(f"Use Elo Scaling: {MATCH_CONFIG['use_elo_scaling']}\n")
        f.write(
            f"Use Target ELO for Win Prob: {MATCH_CONFIG['use_target_elo_for_win_prob']}\n"
        )
        f.write(f"Number of Matches: {SIMULATION_CONFIG['num_matches']}\n")
        f.write(f"Number of Opponents: {SIMULATION_CONFIG['num_opponents']}\n")
        f.write(f"Starting Rating: {RATING_CONFIG['starting_rating']}\n")
        f.write(f"Base Rating Change: {RATING_CONFIG['base_rating_change']}\n")
        f.write(f"ELO Divisor: {RATING_CONFIG['elo_divisor']}\n\n")

        # Write match history table
        f.write("## Match History\n\n")
        f.write(
            "| Match | Player     | Opponent   | Result | Player | Opponent | Win  | Conf  | Var   | Final  | Player  | Opponent | Opp Var | Opp  |\n"
        )
        f.write(
            "|       | Rating     | Rating     |        | Change | Change   | Prob |       | Bonus | Mult   | PP Adj  | PP Adj   | Score   | Num  |\n"
        )
        f.write(
            "|-------|------------|------------|--------|--------|----------|------|-------|-------|--------|---------|----------|---------|------|\n"
        )

        # Calculate summary statistics
        total_wins = 0
        total_losses = 0
        total_opponent_elo = 0
        total_win_prob = 0
        total_opponent_adj = 0
        total_player_pp_adj = 0
        total_opponent_pp_adj = 0

        for match in results:
            # Get opponent adjustment from match data
            opponent_adjustment = match.get("proven_potential_adjustment", 0)
            player_adjustment = (
                -opponent_adjustment
            )  # Player adjustment is opposite of opponent

            # Update summary statistics
            if match["player_won"]:
                total_wins += 1
            else:
                total_losses += 1
            total_opponent_elo += match["opponent_rating_before"]
            total_win_prob += match["win_probability"]
            total_opponent_adj += opponent_adjustment
            total_player_pp_adj += player_adjustment
            total_opponent_pp_adj += opponent_adjustment

            # Calculate original ratings (before proven potential adjustments)
            original_player_after = match["player_rating_before"] + (
                match["rating_change"]
                if match["player_won"]
                else -match["rating_change"]
            )
            original_opponent_after = match["opponent_rating_before"] + (
                -match["rating_change"]
                if match["player_won"]
                else match["rating_change"]
            )

            # Format each column with exact width
            match_num = f"{match['match_number']:<5}"
            player = f"{match['player_rating_before']}->{original_player_after}"
            opponent = f"{match['opponent_rating_before']}->{original_opponent_after}"
            result = f"{'W' if match['player_won'] else 'L':<6}"

            # Calculate player and opponent changes
            player_change = (
                match["rating_change"]
                if match["player_won"]
                else -match["rating_change"]
            )
            opponent_change = (
                -match["rating_change"]
                if match["player_won"]
                else match["rating_change"]
            )

            # Format changes with signs
            player_change_str = f"{player_change:+d}".ljust(6)
            opponent_change_str = f"{opponent_change:+d}".ljust(8)

            win_prob = f"{match['win_probability']:<4.2f}"
            confidence = f"{match['player_confidence']:<5.2f}"
            variety_bonus = f"{match['player_variety_bonus']:<5.2f}"
            multiplier = f"{match['final_multiplier']:<6.2f}"

            # Format proven potential adjustments
            player_pp_adj = f"{player_adjustment:+7.1f}"
            opponent_pp_adj = f"{opponent_adjustment:+8.1f}"

            # Extract opponent number from opponent_id (e.g. "Opponent11" -> "11")
            opponent_num = match["opponent_id"].replace("Opponent", "")

            # Format opponent variety score and number
            opp_var_score = f"{match.get('opponent_variety_bonus', 0):<7.2f}"
            opp_num = f"{opponent_num:<4}"

            f.write(
                f"| {match_num} | {player:<10} | {opponent:<10} | {result} | {player_change_str} | {opponent_change_str} | {win_prob} | {confidence} | {variety_bonus} | {multiplier} | {player_pp_adj} | {opponent_pp_adj} | {opp_var_score} | {opp_num} |\n"
            )

        # Write summary statistics
        f.write("\n## Summary Statistics\n\n")
        f.write("|           Metric           | Value  |\n")
        f.write("|----------------------------|--------|\n")
        f.write(f"|      Total Matches         | {total_wins + total_losses:<6} |\n")
        f.write(f"|          Wins              | {total_wins:<6} |\n")
        f.write(f"|         Losses             | {total_losses:<6} |\n")
        f.write(f"|        Win Rate            | {total_wins/(total_wins + total_losses):<6.2%} |\n")
        f.write(f"|    Average Opponent ELO    | {total_opponent_elo/(total_wins + total_losses):<6.0f} |\n")
        f.write(f"|  Average Win Probability   | {total_win_prob/(total_wins + total_losses):<6.2%} |\n")
        f.write(f"| Total Opponent Adjustments | {total_opponent_adj:<+6d} |\n")
        f.write(f"|    Final Player Rating     | {results[-1]['player_rating_after']:<6} |\n")
        f.write(f"|   Final Opponent Rating    | {results[-1]['opponent_rating_after']:<6} |\n")
        f.write(f"|    Total Player PP Adj     | {total_player_pp_adj:<+6d} |\n")
        f.write(f"|   Total Opponent PP Adj    | {total_opponent_pp_adj:<+6d} |\n")

        # Write proven potential details
        f.write("\n## Proven Potential Details\n\n")
        f.write(
            "| Match | Previous | Original | Current |  Gap   | Gap    | Original | New    | Rating  | Applied   |\n"
        )
        f.write(
            "|       | Match    | Gap      | Gap     | Closed | %      | Change   | Change | Adj     | Threshold |\n"
        )
        f.write(
            "|-------|----------|----------|---------|--------|--------|----------|--------|---------|-----------|\n"
        )

        for match in results:
            if "proven_potential_details" in match:
                for detail in match["proven_potential_details"]:
                    # Format each column with proper width
                    match_num = f"{match['match_number']:<5}"
                    prev_match = f"{detail['previous_match_number']:<8}"
                    orig_gap = f"{detail['original_gap']:<8.0f}"
                    curr_gap = f"{detail['current_gap']:<7.0f}"
                    gap_closed = f"{detail['gap_closed']:<6.0f}"
                    gap_pct = f"{detail['gap_closure_percent']:<6.1%}"
                    orig_change = f"{detail['original_rating_change']:<+8d}"
                    new_change = f"{detail['new_rating_change']:<+6d}"
                    rating_adj = f"{detail['rating_adjustment']:<+6d}"
                    threshold = f"{detail['threshold_applied']:<9.1%}"

                    f.write(
                        f"| {match_num} | {prev_match} | {orig_gap} | {curr_gap} | {gap_closed} | {gap_pct} | {orig_change:<8} | {new_change:<4} | {rating_adj:<7} | {threshold} |\n"
                    )

        # Write detailed match information
        f.write("\n## Detailed Match Information\n\n")

        for match in results:
            # Calculate original ratings (before proven potential adjustments)
            original_player_after = match["player_rating_before"] + (
                match["rating_change"]
                if match["player_won"]
                else -match["rating_change"]
            )
            original_opponent_after = match["opponent_rating_before"] + (
                -match["rating_change"]
                if match["player_won"]
                else match["rating_change"]
            )

            f.write(f"### Match {match['match_number']}\n\n")
            f.write(f"- **Opponent**: {match['opponent_id']}\n")
            f.write(f"- **Result**: {'Win' if match['player_won'] else 'Loss'}\n")
            f.write(
                f"- **Player Rating**: {match['player_rating_before']} -> {original_player_after}\n"
            )
            f.write(
                f"- **Opponent Rating**: {match['opponent_rating_before']} -> {original_opponent_after}\n"
            )
            f.write(f"- **Original Rating Change**: {match['rating_change']}\n")
            f.write(f"- **Win Probability**: {match['win_probability']:.2f}\n")
            f.write(f"- **Player Confidence**: {match['player_confidence']:.2f}\n")
            f.write(f"- **Opponent Confidence**: {match['opponent_confidence']:.2f}\n")
            f.write(
                f"- **Player Variety Bonus**: {match['player_variety_bonus']:.2f}\n"
            )
            if "opponent_variety_bonus" in match:
                f.write(
                    f"- **Opponent Variety Bonus**: {match['opponent_variety_bonus']:.2f}\n"
                )
            f.write(f"- **Expected Score**: {match['expected_score']:.2f}\n")
            f.write(f"- **Base Rating Change**: {match['base_rating_change']:.2f}\n")
            f.write(
                f"- **Confidence Multiplier**: {match['confidence_multiplier']:.2f}\n"
            )
            f.write(f"- **Final Multiplier**: {match['final_multiplier']:.2f}\n")
            f.write(f"  - Combines variety bonuses and confidence factors\n")
            f.write(
                f"  - Clamped between {MULTIPLIER_CONFIG['min_multiplier']} and {MULTIPLIER_CONFIG['max_multiplier']}\n"
            )
            f.write(
                f"  - Formula: (1 + player_variety) * (1 + opponent_variety) * (1 - player_conf) * (1 - opponent_conf)\n\n"
            )

            # Write proven potential adjustments
            f.write("#### Opponent Rating Adjustments\n\n")
            
            # Define headers in two rows
            headers_row1 = {
                "prev_match": "Previous",
                "orig_gap": "Original",
                "curr_gap": "Current",
                "gap_closed": "Gap",
                "gap_pct": "Closure",
                "orig_change": "Original",
                "new_change": "New",
                "opponent_adj": "Opponent"
            }
            
            headers_row2 = {
                "prev_match": "Match",
                "orig_gap": "Gap",
                "curr_gap": "Gap",
                "gap_closed": "Closed",
                "gap_pct": "%",
                "orig_change": "Change",
                "new_change": "Change",
                "opponent_adj": "Adjustment"
            }
            
            # Store all rows of data first
            rows = []
            if "proven_potential_details" in match:
                for detail in match["proven_potential_details"]:
                    adjustment = detail["rating_adjustment"]
                    opponent_adj_sign = "+" if adjustment > 0 else "-"
                    
                    row = {
                        "prev_match": str(detail["previous_match_number"]),
                        "orig_gap": str(detail["original_gap"]),
                        "curr_gap": str(detail["current_gap"]),
                        "gap_closed": str(detail["gap_closed"]),
                        "gap_pct": f"{detail['gap_closure_percent']*100:.2f}%",
                        "orig_change": str(detail["original_rating_change"]),
                        "new_change": str(detail["new_rating_change"]),
                        "opponent_adj": f"{opponent_adj_sign}{abs(adjustment):.2f}"
                    }
                    rows.append(row)
            
            # Calculate max width for each column including both header rows
            def get_max_width(header1, header2, rows, key):
                if not rows:
                    return max(len(header1), len(header2))
                return max(
                    len(header1),
                    len(header2),
                    max(len(row[key]) for row in rows),
                )

            column_widths = {
                "prev_match": get_max_width(
                    headers_row1["prev_match"],
                    headers_row2["prev_match"],
                    rows,
                    "prev_match",
                ),
                "orig_gap": get_max_width(
                    headers_row1["orig_gap"],
                    headers_row2["orig_gap"],
                    rows,
                    "orig_gap",
                ),
                "curr_gap": get_max_width(
                    headers_row1["curr_gap"],
                    headers_row2["curr_gap"],
                    rows,
                    "curr_gap",
                ),
                "gap_closed": get_max_width(
                    headers_row1["gap_closed"],
                    headers_row2["gap_closed"],
                    rows,
                    "gap_closed",
                ),
                "gap_pct": get_max_width(
                    headers_row1["gap_pct"],
                    headers_row2["gap_pct"],
                    rows,
                    "gap_pct",
                ),
                "orig_change": get_max_width(
                    headers_row1["orig_change"],
                    headers_row2["orig_change"],
                    rows,
                    "orig_change",
                ),
                "new_change": get_max_width(
                    headers_row1["new_change"],
                    headers_row2["new_change"],
                    rows,
                    "new_change",
                ),
                "opponent_adj": get_max_width(
                    headers_row1["opponent_adj"],
                    headers_row2["opponent_adj"],
                    rows,
                    "opponent_adj",
                ),
            }
            
            # Write header rows
            f.write(
                f"| {headers_row1['prev_match']:<{column_widths['prev_match']}} | "
                f"{headers_row1['orig_gap']:<{column_widths['orig_gap']}} | "
                f"{headers_row1['curr_gap']:<{column_widths['curr_gap']}} | "
                f"{headers_row1['gap_closed']:<{column_widths['gap_closed']}} | "
                f"{headers_row1['gap_pct']:>{column_widths['gap_pct']}} | "
                f"{headers_row1['orig_change']:>{column_widths['orig_change']}} | "
                f"{headers_row1['new_change']:>{column_widths['new_change']}} | "
                f"{headers_row1['opponent_adj']:>{column_widths['opponent_adj']}} |\n"
            )
            
            f.write(
                f"| {headers_row2['prev_match']:<{column_widths['prev_match']}} | "
                f"{headers_row2['orig_gap']:<{column_widths['orig_gap']}} | "
                f"{headers_row2['curr_gap']:<{column_widths['curr_gap']}} | "
                f"{headers_row2['gap_closed']:<{column_widths['gap_closed']}} | "
                f"{headers_row2['gap_pct']:>{column_widths['gap_pct']}} | "
                f"{headers_row2['orig_change']:>{column_widths['orig_change']}} | "
                f"{headers_row2['new_change']:>{column_widths['new_change']}} | "
                f"{headers_row2['opponent_adj']:>{column_widths['opponent_adj']}} |\n"
            )
            
            # Write separator
            f.write(
                f"|{'-' * (column_widths['prev_match'] + 2)}|"
                f"{'-' * (column_widths['orig_gap'] + 2)}|"
                f"{'-' * (column_widths['curr_gap'] + 2)}|"
                f"{'-' * (column_widths['gap_closed'] + 2)}|"
                f"{'-' * (column_widths['gap_pct'] + 2)}|"
                f"{'-' * (column_widths['orig_change'] + 2)}|"
                f"{'-' * (column_widths['new_change'] + 2)}|"
                f"{'-' * (column_widths['opponent_adj'] + 2)}|\n"
            )
            
            # Write data rows with calculated widths
            for row in rows:
                f.write(
                    f"| {row['prev_match']:<{column_widths['prev_match']}} | "
                    f"{row['orig_gap']:<{column_widths['orig_gap']}} | "
                    f"{row['curr_gap']:<{column_widths['curr_gap']}} | "
                    f"{row['gap_closed']:<{column_widths['gap_closed']}} | "
                    f"{row['gap_pct']:>{column_widths['gap_pct']}} | "
                    f"{row['orig_change']:>{column_widths['orig_change']}} | "
                    f"{row['new_change']:>{column_widths['new_change']}} | "
                    f"{row['opponent_adj']:>{column_widths['opponent_adj']}} |\n"
                )
            f.write("\n")

        # Write summary statistics
        f.write("## Summary Statistics\n\n")
        f.write(f"- **Final Rating**: {results[-1]['player_rating_after']}\n")
        f.write(f"- **Total Matches**: {len(results)}\n")
        f.write(f"- **Wins**: {sum(1 for m in results if m['player_won'])}\n")
        f.write(f"- **Losses**: {sum(1 for m in results if not m['player_won'])}\n")
        f.write(
            f"- **Win Rate**: {sum(1 for m in results if m['player_won'])/len(results)*100:.2f}%\n"
        )
        f.write(
            f"- **Average Rating Change**: {sum(m['rating_change'] for m in results)/len(results):.2f}\n"
        )
        f.write(
            f"- **Max Rating Change**: {max(m['rating_change'] for m in results)}\n"
        )
        f.write(
            f"- **Min Rating Change**: {min(m['rating_change'] for m in results)}\n"
        )
        f.write(f"- **Starting Confidence**: {results[0]['player_confidence']:.2f}\n")
        f.write(f"- **Final Confidence**: {results[-1]['player_confidence']:.2f}\n")
        f.write(
            f"- **Average Variety Bonus**: {sum(m['player_variety_bonus'] for m in results)/len(results):.2f}\n"
        )
        f.write(
            f"- **Max Variety Bonus**: {max(m['player_variety_bonus'] for m in results):.2f}\n"
        )
        f.write(
            f"- **Min Variety Bonus**: {min(m['player_variety_bonus'] for m in results):.2f}\n"
        )
        f.write(
            f"- **Average Multiplier**: {sum(m['final_multiplier'] for m in results)/len(results):.2f}\n"
        )
        f.write(
            f"- **Max Multiplier**: {max(m['final_multiplier'] for m in results):.2f}\n"
        )
        f.write(
            f"- **Min Multiplier**: {min(m['final_multiplier'] for m in results):.2f}\n"
        )

        # Calculate total opponent adjustments
        total_opponent_adjustment = sum(
            m.get("proven_potential_adjustment", 0) for m in results
        )

        f.write(
            f"- **Total Opponent Rating Adjustments**: {total_opponent_adjustment:.2f}\n"
        )
        f.write(
            f"- **Max Opponent Rating Adjustment**: {max(abs(m.get('proven_potential_adjustment', 0)) for m in results):.2f}\n"
        )
        f.write(
            f"- **Matches with Opponent Adjustments**: {sum(1 for m in results if 'proven_potential_adjustment' in m)}\n"
        )
