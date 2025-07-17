"""Functions for generating match-specific output tables."""

from typing import Dict, List
from .base_output import get_max_width


def write_match_details(f, match: Dict, players: List) -> None:
    """Write detailed information about a single match.

    Args:
        f: File object to write to
        match: Match data dictionary
        players: List of player objects
    """
    # Write matchmaking parameters
    write_matchmaking_params(f, match)

    # Write challenger selection
    write_challenger_selection(f, match)

    # Write potential opponents
    write_potential_opponents(f, match)

    # Write match result
    write_match_result(f, match)


def write_matchmaking_params(f, match: Dict) -> None:
    """Write matchmaking parameters table.

    Args:
        f: File object to write to
        match: Match data dictionary
    """
    f.write("#### Matchmaking Parameters\n\n")

    params = [
        {"param": "Bias Target Gap", "value": match["bias_target_gap"]},
        {"param": "Max Match Gap", "value": match["max_match_gap"]},
        {"param": "Target Rating Range", "value": match["target_rating_range"]},
    ]

    param_width = get_max_width("Parameter", params, "param")
    value_width = get_max_width("Value", params, "value")

    f.write(f"| {'Parameter':<{param_width}} | {'Value':>{value_width}} |\n")
    f.write(f"|{'-' * (param_width + 2)}|{'-' * (value_width + 2)}|\n")
    for param in params:
        f.write(
            f"| {param['param']:<{param_width}} | {param['value']:>{value_width}.1f} |\n"
        )
    f.write("\n")


def write_challenger_selection(f, match: Dict) -> None:
    """Write challenger selection table.

    Args:
        f: File object to write to
        match: Match data dictionary
    """
    f.write("#### Challenger Selection\n\n")

    challengers = []
    for player in match["challenger_selection"]:
        challengers.append(
            {
                "player": player["name"],
                "rating": player["rating"],
                "target": player["target_rating"],
                "games": player["games_played"],
                "weight": player["selection_weight"],
            }
        )

    player_width = get_max_width("Player", challengers, "player")
    rating_width = get_max_width("Current Rating", challengers, "rating")
    target_width = get_max_width("Target Rating", challengers, "target")
    games_width = get_max_width("Games Played", challengers, "games")
    weight_width = get_max_width("Selection Weight", challengers, "weight")

    f.write(
        f"| {'Player':<{player_width}} | {'Current Rating':>{rating_width}} | {'Target Rating':>{target_width}} | {'Games Played':>{games_width}} | {'Selection Weight':>{weight_width}} |\n"
    )
    f.write(
        f"|{'-' * (player_width + 2)}|{'-' * (rating_width + 2)}|{'-' * (target_width + 2)}|{'-' * (games_width + 2)}|{'-' * (weight_width + 2)}|\n"
    )
    for challenger in challengers:
        f.write(
            f"| {challenger['player']:<{player_width}} | {challenger['rating']:>{rating_width}.1f} | {challenger['target']:>{target_width}} | {challenger['games']:>{games_width}} | {challenger['weight']:>{weight_width}.3f} |\n"
        )
    f.write("\n")


def write_potential_opponents(f, match: Dict) -> None:
    """Write potential opponents table.

    Args:
        f: File object to write to
        match: Match data dictionary
    """
    f.write("#### Potential Opponents\n\n")

    opponents = []
    for opponent in match["potential_opponents"]:
        opponents.append(
            {
                "opponent": opponent["name"],
                "rating": opponent["rating"],
                "target": opponent["target_rating"],
                "games": opponent["games_played"],
                "gap": opponent["match_gap"],
                "prob": opponent["acceptance_probability"],
            }
        )

    opponent_width = get_max_width("Opponent", opponents, "opponent")
    rating_width = get_max_width("Current Rating", opponents, "rating")
    target_width = get_max_width("Target Rating", opponents, "target")
    games_width = get_max_width("Games Played", opponents, "games")
    gap_width = get_max_width("Match Gap", opponents, "gap")
    prob_width = get_max_width("Acceptance Probability", opponents, "prob", "{:.1f}%")

    f.write(
        f"| {'Opponent':<{opponent_width}} | {'Current Rating':>{rating_width}} | {'Target Rating':>{target_width}} | {'Games Played':>{games_width}} | {'Match Gap':>{gap_width}} | {'Acceptance Probability':>{prob_width}} |\n"
    )
    f.write(
        f"|{'-' * (opponent_width + 2)}|{'-' * (rating_width + 2)}|{'-' * (target_width + 2)}|{'-' * (games_width + 2)}|{'-' * (gap_width + 2)}|{'-' * (prob_width + 2)}|\n"
    )
    for opponent in opponents:
        f.write(
            f"| {opponent['opponent']:<{opponent_width}} | {opponent['rating']:>{rating_width}.1f} | {opponent['target']:>{target_width}} | {opponent['games']:>{games_width}} | {opponent['gap']:>{gap_width}.1f} | {opponent['prob']:>{prob_width}.1f}% |\n"
        )
    f.write("\n")


def write_match_result(f, match: Dict) -> None:
    """Write match result details.

    Args:
        f: File object to write to
        match: Match data dictionary
    """
    f.write("#### Match Result\n\n")

    # Get player data
    p1 = match["player1"]
    p2 = match["player2"]
    p1_won = match["winner"] == p1["name"]

    # Calculate rating changes
    p1_change = p1["rating_after"] - p1["rating"]
    p2_change = p2["rating_after"] - p2["rating"]

    # Write match details
    f.write("##### Match Details\n\n")
    f.write(f"Challenger: {p1['name']}\n")
    f.write(f"Opponent: {p2['name']}\n")
    f.write(f"Winner: {match['winner']}\n\n")

    # Write ratings table
    ratings = [
        {
            "player": p1["name"],
            "before": p1["rating"],
            "after": p1["rating_after"],
            "change": p1_change,
        },
        {
            "player": p2["name"],
            "before": p2["rating"],
            "after": p2["rating_after"],
            "change": p2_change,
        },
    ]

    player_width = get_max_width("Player", ratings, "player")
    before_width = get_max_width("Rating Before", ratings, "before")
    after_width = get_max_width("Rating After", ratings, "after")
    change_width = get_max_width("Rating Change", ratings, "change")

    f.write(
        f"| {'Player':<{player_width}} | {'Rating Before':>{before_width}} | {'Rating After':>{after_width}} | {'Rating Change':>{change_width}} |\n"
    )
    f.write(
        f"|{'-' * (player_width + 2)}|{'-' * (before_width + 2)}|{'-' * (after_width + 2)}|{'-' * (change_width + 2)}|\n"
    )
    for rating in ratings:
        f.write(
            f"| {rating['player']:<{player_width}} | {rating['before']:>{before_width}.1f} | {rating['after']:>{after_width}.1f} | {rating['change']:>{change_width}.1f} |\n"
        )
    f.write("\n")

    # Write match factors
    f.write("##### Match Factors\n\n")
    factors = [
        {"factor": "Win Probability", "value": match["win_probability"] * 100},
        {"factor": "Confidence Level", "value": match["confidence_level"] * 100},
        {"factor": "Variety Bonus", "value": match["variety_bonus"] * 100},
        {"factor": "Rating Change", "value": abs(p1_change)},
        {"factor": "Multiplier", "value": match["multiplier"]},
    ]

    factor_width = get_max_width("Factor", factors, "factor")
    value_width = get_max_width("Value", factors, "value")

    f.write(f"| {'Factor':<{factor_width}} | {'Value':>{value_width}} |\n")
    f.write(f"|{'-' * (factor_width + 2)}|{'-' * (value_width + 2)}|\n")
    for factor in factors:
        if factor["factor"] in ["Win Probability", "Confidence Level", "Variety Bonus"]:
            f.write(
                f"| {factor['factor']:<{factor_width}} | {factor['value']:>{value_width}.1f}% |\n"
            )
        else:
            f.write(
                f"| {factor['factor']:<{factor_width}} | {factor['value']:>{value_width}.1f} |\n"
            )
    f.write("\n")
