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

        # Prepare data for match history table to calculate column widths
        match_history_rows = []
        for match in results:
            # Get opponent adjustment from match data
            opponent_adjustment = match.get("proven_potential_adjustment", 0)
            player_adjustment = (
                -opponent_adjustment
            )  # Player adjustment is opposite of opponent

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

            # Extract opponent number from opponent_id (e.g. "Opponent11" -> "11")
            opponent_num = match["opponent_id"].replace("Opponent", "")

            match_history_rows.append(
                {
                    "match": str(match["match_number"]),
                    "player_rating": f"{match['player_rating_before']}->{original_player_after}",
                    "opponent_rating": f"{match['opponent_rating_before']}->{original_opponent_after}",
                    "result": "W" if match["player_won"] else "L",
                    "player_change": f"{player_change:+d}",
                    "opponent_change": f"{opponent_change:+d}",
                    "win_prob": f"{match['win_probability']:.2f}",
                    "confidence": f"{match['player_confidence']:.2f}",
                    "variety_bonus": f"{match['player_variety_bonus']:.2f}",
                    "multiplier": f"{match['final_multiplier']:.2f}",
                    "player_pp_adj": f"{player_adjustment:+.1f}",
                    "opponent_pp_adj": f"{opponent_adjustment:+.1f}",
                    "opp_var_score": f"{match.get('opponent_variety_bonus', 0):.2f}",
                    "opp_num": opponent_num,
                }
            )

        # Calculate column widths
        def get_max_width(header1, header2, rows, key):
            if not rows:
                return max(len(header1), len(header2))
            return max(
                len(header1),
                len(header2),
                max(len(str(row.get(key, ""))) for row in rows),
            )

        # Define headers
        headers_row1 = {
            "match": "Match",
            "player_rating": "Player",
            "opponent_rating": "Opponent",
            "result": "Result",
            "player_change": "Player",
            "opponent_change": "Opponent",
            "win_prob": "Win",
            "confidence": "Conf",
            "variety_bonus": "Var",
            "multiplier": "Final",
            "player_pp_adj": "Player",
            "opponent_pp_adj": "Opponent",
            "opp_var_score": "Opp Var",
            "opp_num": "Opp",
        }

        headers_row2 = {
            "match": "",
            "player_rating": "Rating",
            "opponent_rating": "Rating",
            "result": "",
            "player_change": "Change",
            "opponent_change": "Change",
            "win_prob": "Prob",
            "confidence": "",
            "variety_bonus": "Bonus",
            "multiplier": "Mult",
            "player_pp_adj": "PP Adj",
            "opponent_pp_adj": "PP Adj",
            "opp_var_score": "Score",
            "opp_num": "Num",
        }

        column_widths = {
            "match": get_max_width(
                headers_row1["match"],
                headers_row2["match"],
                match_history_rows,
                "match",
            ),
            "player_rating": get_max_width(
                headers_row1["player_rating"],
                headers_row2["player_rating"],
                match_history_rows,
                "player_rating",
            ),
            "opponent_rating": get_max_width(
                headers_row1["opponent_rating"],
                headers_row2["opponent_rating"],
                match_history_rows,
                "opponent_rating",
            ),
            "result": get_max_width(
                headers_row1["result"],
                headers_row2["result"],
                match_history_rows,
                "result",
            ),
            "player_change": get_max_width(
                headers_row1["player_change"],
                headers_row2["player_change"],
                match_history_rows,
                "player_change",
            ),
            "opponent_change": get_max_width(
                headers_row1["opponent_change"],
                headers_row2["opponent_change"],
                match_history_rows,
                "opponent_change",
            ),
            "win_prob": get_max_width(
                headers_row1["win_prob"],
                headers_row2["win_prob"],
                match_history_rows,
                "win_prob",
            ),
            "confidence": get_max_width(
                headers_row1["confidence"],
                headers_row2["confidence"],
                match_history_rows,
                "confidence",
            ),
            "variety_bonus": get_max_width(
                headers_row1["variety_bonus"],
                headers_row2["variety_bonus"],
                match_history_rows,
                "variety_bonus",
            ),
            "multiplier": get_max_width(
                headers_row1["multiplier"],
                headers_row2["multiplier"],
                match_history_rows,
                "multiplier",
            ),
            "player_pp_adj": get_max_width(
                headers_row1["player_pp_adj"],
                headers_row2["player_pp_adj"],
                match_history_rows,
                "player_pp_adj",
            ),
            "opponent_pp_adj": get_max_width(
                headers_row1["opponent_pp_adj"],
                headers_row2["opponent_pp_adj"],
                match_history_rows,
                "opponent_pp_adj",
            ),
            "opp_var_score": get_max_width(
                headers_row1["opp_var_score"],
                headers_row2["opp_var_score"],
                match_history_rows,
                "opp_var_score",
            ),
            "opp_num": get_max_width(
                headers_row1["opp_num"],
                headers_row2["opp_num"],
                match_history_rows,
                "opp_num",
            ),
        }

        # Write header rows with dynamic widths
        f.write(
            f"| {headers_row1['match']:<{column_widths['match']}} | "
            f"{headers_row1['player_rating']:<{column_widths['player_rating']}} | "
            f"{headers_row1['opponent_rating']:<{column_widths['opponent_rating']}} | "
            f"{headers_row1['result']:<{column_widths['result']}} | "
            f"{headers_row1['player_change']:<{column_widths['player_change']}} | "
            f"{headers_row1['opponent_change']:<{column_widths['opponent_change']}} | "
            f"{headers_row1['win_prob']:<{column_widths['win_prob']}} | "
            f"{headers_row1['confidence']:<{column_widths['confidence']}} | "
            f"{headers_row1['variety_bonus']:<{column_widths['variety_bonus']}} | "
            f"{headers_row1['multiplier']:<{column_widths['multiplier']}} | "
            f"{headers_row1['player_pp_adj']:<{column_widths['player_pp_adj']}} | "
            f"{headers_row1['opponent_pp_adj']:<{column_widths['opponent_pp_adj']}} | "
            f"{headers_row1['opp_var_score']:<{column_widths['opp_var_score']}} | "
            f"{headers_row1['opp_num']:<{column_widths['opp_num']}} |\n"
        )

        f.write(
            f"| {headers_row2['match']:<{column_widths['match']}} | "
            f"{headers_row2['player_rating']:<{column_widths['player_rating']}} | "
            f"{headers_row2['opponent_rating']:<{column_widths['opponent_rating']}} | "
            f"{headers_row2['result']:<{column_widths['result']}} | "
            f"{headers_row2['player_change']:<{column_widths['player_change']}} | "
            f"{headers_row2['opponent_change']:<{column_widths['opponent_change']}} | "
            f"{headers_row2['win_prob']:<{column_widths['win_prob']}} | "
            f"{headers_row2['confidence']:<{column_widths['confidence']}} | "
            f"{headers_row2['variety_bonus']:<{column_widths['variety_bonus']}} | "
            f"{headers_row2['multiplier']:<{column_widths['multiplier']}} | "
            f"{headers_row2['player_pp_adj']:<{column_widths['player_pp_adj']}} | "
            f"{headers_row2['opponent_pp_adj']:<{column_widths['opponent_pp_adj']}} | "
            f"{headers_row2['opp_var_score']:<{column_widths['opp_var_score']}} | "
            f"{headers_row2['opp_num']:<{column_widths['opp_num']}} |\n"
        )

        # Write separator with dynamic widths (with spaces)
        f.write(
            f"| {'-' * column_widths['match']} |"
            f" {'-' * column_widths['player_rating']} |"
            f" {'-' * column_widths['opponent_rating']} |"
            f" {'-' * column_widths['result']} |"
            f" {'-' * column_widths['player_change']} |"
            f" {'-' * column_widths['opponent_change']} |"
            f" {'-' * column_widths['win_prob']} |"
            f" {'-' * column_widths['confidence']} |"
            f" {'-' * column_widths['variety_bonus']} |"
            f" {'-' * column_widths['multiplier']} |"
            f" {'-' * column_widths['player_pp_adj']} |"
            f" {'-' * column_widths['opponent_pp_adj']} |"
            f" {'-' * column_widths['opp_var_score']} |"
            f" {'-' * column_widths['opp_num']} |\n"
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

        # Write data rows with calculated widths
        for row in match_history_rows:
            f.write(
                f"| {row['match']:<{column_widths['match']}} | "
                f"{row['player_rating']:<{column_widths['player_rating']}} | "
                f"{row['opponent_rating']:<{column_widths['opponent_rating']}} | "
                f"{row['result']:<{column_widths['result']}} | "
                f"{row['player_change']:<{column_widths['player_change']}} | "
                f"{row['opponent_change']:<{column_widths['opponent_change']}} | "
                f"{row['win_prob']:<{column_widths['win_prob']}} | "
                f"{row['confidence']:<{column_widths['confidence']}} | "
                f"{row['variety_bonus']:<{column_widths['variety_bonus']}} | "
                f"{row['multiplier']:<{column_widths['multiplier']}} | "
                f"{row['player_pp_adj']:<{column_widths['player_pp_adj']}} | "
                f"{row['opponent_pp_adj']:<{column_widths['opponent_pp_adj']}} | "
                f"{row['opp_var_score']:<{column_widths['opp_var_score']}} | "
                f"{row['opp_num']:<{column_widths['opp_num']}} |\n"
            )

        # Write summary statistics
        f.write("\n## Summary Statistics\n\n")

        # Prepare data for summary statistics table
        summary_stats_rows = [
            {"metric": "Total Matches", "value": str(total_wins + total_losses)},
            {"metric": "Wins", "value": str(total_wins)},
            {"metric": "Losses", "value": str(total_losses)},
            {
                "metric": "Win Rate",
                "value": f"{total_wins/(total_wins + total_losses):.2%}",
            },
            {
                "metric": "Average Opponent ELO",
                "value": f"{total_opponent_elo/(total_wins + total_losses):.0f}",
            },
            {
                "metric": "Average Win Probability",
                "value": f"{total_win_prob/(total_wins + total_losses):.2%}",
            },
            {
                "metric": "Total Opponent Adjustments",
                "value": f"{total_opponent_adj:+d}",
            },
            {
                "metric": "Final Player Rating",
                "value": str(results[-1]["player_rating_after"]),
            },
            {
                "metric": "Final Opponent Rating",
                "value": str(results[-1]["opponent_rating_after"]),
            },
            {"metric": "Total Player PP Adj", "value": f"{total_player_pp_adj:+d}"},
            {"metric": "Total Opponent PP Adj", "value": f"{total_opponent_pp_adj:+d}"},
        ]

        # Calculate column widths for summary statistics
        summary_metric_width = get_max_width("Metric", summary_stats_rows, "metric")
        summary_value_width = get_max_width("Value", summary_stats_rows, "value")

        # Write summary statistics table with dynamic widths
        f.write(
            f"| {'Metric':<{summary_metric_width}} | {'Value':>{summary_value_width}} |\n"
        )
        f.write(f"| {'-' * summary_metric_width} | {'-' * summary_value_width} |\n")
        for row in summary_stats_rows:
            f.write(
                f"| {row['metric']:<{summary_metric_width}} | {row['value']:>{summary_value_width}} |\n"
            )

        # Write proven potential details
        f.write("\n## Proven Potential Details\n\n")

        # Prepare data for proven potential details table
        pp_details_rows = []
        for match in results:
            if "proven_potential_details" in match:
                for detail in match["proven_potential_details"]:
                    pp_details_rows.append(
                        {
                            "match": str(match["match_number"]),
                            "prev_match": str(detail["previous_match_number"]),
                            "orig_gap": f"{detail['original_gap']:.0f}",
                            "curr_gap": f"{detail['current_gap']:.0f}",
                            "gap_closed": f"{detail['gap_closed']:.0f}",
                            "gap_pct": f"{detail['gap_closure_percent']:.1%}",
                            "orig_change": f"{detail['original_rating_change']:+d}",
                            "new_change": f"{detail['new_rating_change']:+d}",
                            "rating_adj": f"{detail['rating_adjustment']:+d}",
                            "threshold": f"{detail['threshold_applied']:.1%}",
                        }
                    )

        if pp_details_rows:
            # Define headers for proven potential details
            pp_headers_row1 = {
                "match": "Match",
                "prev_match": "Previous",
                "orig_gap": "Original",
                "curr_gap": "Current",
                "gap_closed": "Gap",
                "gap_pct": "Gap",
                "orig_change": "Original",
                "new_change": "New",
                "rating_adj": "Rating",
                "threshold": "Applied",
            }

            pp_headers_row2 = {
                "match": "",
                "prev_match": "Match",
                "orig_gap": "Gap",
                "curr_gap": "Gap",
                "gap_closed": "Closed",
                "gap_pct": "%",
                "orig_change": "Change",
                "new_change": "Change",
                "rating_adj": "Adj",
                "threshold": "Threshold",
            }

            # Calculate column widths for proven potential details
            pp_column_widths = {
                "match": get_max_width(
                    pp_headers_row1["match"],
                    pp_headers_row2["match"],
                    pp_details_rows,
                    "match",
                ),
                "prev_match": get_max_width(
                    pp_headers_row1["prev_match"],
                    pp_headers_row2["prev_match"],
                    pp_details_rows,
                    "prev_match",
                ),
                "orig_gap": get_max_width(
                    pp_headers_row1["orig_gap"],
                    pp_headers_row2["orig_gap"],
                    pp_details_rows,
                    "orig_gap",
                ),
                "curr_gap": get_max_width(
                    pp_headers_row1["curr_gap"],
                    pp_headers_row2["curr_gap"],
                    pp_details_rows,
                    "curr_gap",
                ),
                "gap_closed": get_max_width(
                    pp_headers_row1["gap_closed"],
                    pp_headers_row2["gap_closed"],
                    pp_details_rows,
                    "gap_closed",
                ),
                "gap_pct": get_max_width(
                    pp_headers_row1["gap_pct"],
                    pp_headers_row2["gap_pct"],
                    pp_details_rows,
                    "gap_pct",
                ),
                "orig_change": get_max_width(
                    pp_headers_row1["orig_change"],
                    pp_headers_row2["orig_change"],
                    pp_details_rows,
                    "orig_change",
                ),
                "new_change": get_max_width(
                    pp_headers_row1["new_change"],
                    pp_headers_row2["new_change"],
                    pp_details_rows,
                    "new_change",
                ),
                "rating_adj": get_max_width(
                    pp_headers_row1["rating_adj"],
                    pp_headers_row2["rating_adj"],
                    pp_details_rows,
                    "rating_adj",
                ),
                "threshold": get_max_width(
                    pp_headers_row1["threshold"],
                    pp_headers_row2["threshold"],
                    pp_details_rows,
                    "threshold",
                ),
            }

            # Write header rows for proven potential details
            f.write(
                f"| {pp_headers_row1['match']:<{pp_column_widths['match']}} | "
                f"{pp_headers_row1['prev_match']:<{pp_column_widths['prev_match']}} | "
                f"{pp_headers_row1['orig_gap']:<{pp_column_widths['orig_gap']}} | "
                f"{pp_headers_row1['curr_gap']:<{pp_column_widths['curr_gap']}} | "
                f"{pp_headers_row1['gap_closed']:<{pp_column_widths['gap_closed']}} | "
                f"{pp_headers_row1['gap_pct']:<{pp_column_widths['gap_pct']}} | "
                f"{pp_headers_row1['orig_change']:<{pp_column_widths['orig_change']}} | "
                f"{pp_headers_row1['new_change']:<{pp_column_widths['new_change']}} | "
                f"{pp_headers_row1['rating_adj']:<{pp_column_widths['rating_adj']}} | "
                f"{pp_headers_row1['threshold']:<{pp_column_widths['threshold']}} |\n"
            )

            f.write(
                f"| {pp_headers_row2['match']:<{pp_column_widths['match']}} | "
                f"{pp_headers_row2['prev_match']:<{pp_column_widths['prev_match']}} | "
                f"{pp_headers_row2['orig_gap']:<{pp_column_widths['orig_gap']}} | "
                f"{pp_headers_row2['curr_gap']:<{pp_column_widths['curr_gap']}} | "
                f"{pp_headers_row2['gap_closed']:<{pp_column_widths['gap_closed']}} | "
                f"{pp_headers_row2['gap_pct']:<{pp_column_widths['gap_pct']}} | "
                f"{pp_headers_row2['orig_change']:<{pp_column_widths['orig_change']}} | "
                f"{pp_headers_row2['new_change']:<{pp_column_widths['new_change']}} | "
                f"{pp_headers_row2['rating_adj']:<{pp_column_widths['rating_adj']}} | "
                f"{pp_headers_row2['threshold']:<{pp_column_widths['threshold']}} |\n"
            )

            # Write separator for proven potential details
            f.write(
                f"| {'-' * pp_column_widths['match']} |"
                f" {'-' * pp_column_widths['prev_match']} |"
                f" {'-' * pp_column_widths['orig_gap']} |"
                f" {'-' * pp_column_widths['curr_gap']} |"
                f" {'-' * pp_column_widths['gap_closed']} |"
                f" {'-' * pp_column_widths['gap_pct']} |"
                f" {'-' * pp_column_widths['orig_change']} |"
                f" {'-' * pp_column_widths['new_change']} |"
                f" {'-' * pp_column_widths['rating_adj']} |"
                f" {'-' * pp_column_widths['threshold']} |\n"
            )

            # Write data rows for proven potential details
            for row in pp_details_rows:
                f.write(
                    f"| {row['match']:<{pp_column_widths['match']}} | "
                    f"{row['prev_match']:<{pp_column_widths['prev_match']}} | "
                    f"{row['orig_gap']:<{pp_column_widths['orig_gap']}} | "
                    f"{row['curr_gap']:<{pp_column_widths['curr_gap']}} | "
                    f"{row['gap_closed']:<{pp_column_widths['gap_closed']}} | "
                    f"{row['gap_pct']:<{pp_column_widths['gap_pct']}} | "
                    f"{row['orig_change']:<{pp_column_widths['orig_change']}} | "
                    f"{row['new_change']:<{pp_column_widths['new_change']}} | "
                    f"{row['rating_adj']:<{pp_column_widths['rating_adj']}} | "
                    f"{row['threshold']:<{pp_column_widths['threshold']}} |\n"
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
                "opponent_adj": "Opponent",
            }

            headers_row2 = {
                "prev_match": "Match",
                "orig_gap": "Gap",
                "curr_gap": "Gap",
                "gap_closed": "Closed",
                "gap_pct": "%",
                "orig_change": "Change",
                "new_change": "Change",
                "opponent_adj": "Adjustment",
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
                        "opponent_adj": f"{opponent_adj_sign}{abs(adjustment):.2f}",
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
