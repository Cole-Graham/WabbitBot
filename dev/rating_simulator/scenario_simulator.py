import random
from typing import Dict, List, Optional
from datetime import datetime
import json
import os
import sys
from pathlib import Path
import statistics
import math
from collections import defaultdict
from bisect import bisect_left

# Add the project root to the Python path
project_root = str(Path(__file__).parent.parent.parent)
if project_root not in sys.path:
    sys.path.append(project_root)

from dev.rating_simulator.scenarios.base_scenario import BaseScenario
from dev.rating_simulator.rating_calculator import RatingCalculator
from dev.rating_simulator.scenarios.ladder_reset import LadderResetScenario, Player
from dev.rating_simulator.scenarios.output import save_ladder_reset_results
from dev.rating_simulator.simulation_config import LADDER_RESET_CONFIG


class ScenarioSimulator:
    """Simulator for rating system scenarios."""

    def __init__(self, scenario: BaseScenario):
        """Initialize simulator with a scenario.

        Args:
            scenario: Scenario to simulate
        """
        self.scenario = scenario
        self.players = []
        self.match_history = []
        self.rating_calculator = RatingCalculator()

    def simulate_match(self) -> Dict:
        """Simulate a single match.

        Returns:
            Dictionary containing match details
        """
        # Select players for the match
        p1, p2 = self.scenario.select_matchup(self.players)

        # Calculate win probability
        win_prob = self.scenario.calculate_win_probability(p1, p2)

        # Determine winner
        p1_won = random.random() < win_prob

        # Calculate variety bonuses using the rating calculator
        p1_opponent_counts = self._get_opponent_counts(p1)
        p2_opponent_counts = self._get_opponent_counts(p2)

        # Calculate rating range for variety bonus
        ratings = [p.rating for p in self.players]
        rating_range = max(ratings) - min(ratings) if ratings else 400

        # Calculate median games played
        games_played_list = [p.games_played for p in self.players]
        median_games_played = (
            statistics.median(games_played_list) if games_played_list else 0
        )

        # Handle division by zero case
        if median_games_played == 0:
            median_games_played = 1.0  # Use 1.0 as default to avoid division by zero

        # Calculate simulated average variety score (simplified - could be improved)
        simulated_avg_variety_score = (
            2.0  # Default value, could be calculated from all players
        )

        # Calculate player percentiles in current rating distribution
        current_ratings = [p.rating for p in self.players]
        current_ratings.sort()

        # Calculate percentile for each player
        p1_percentile = self._calculate_percentile(p1.rating, current_ratings)
        p2_percentile = self._calculate_percentile(p2.rating, current_ratings)

        # Calculate variety bonuses
        p1_variety_bonus = self.rating_calculator.calculate_variety_bonus(
            p1.rating,
            p2.rating,
            p1_opponent_counts,
            self.players,
            p1.games_played,
            median_games_played,
            simulated_avg_variety_score,
            p1_percentile,
        )
        p2_variety_bonus = self.rating_calculator.calculate_variety_bonus(
            p2.rating,
            p1.rating,
            p2_opponent_counts,
            self.players,
            p2.games_played,
            median_games_played,
            simulated_avg_variety_score,
            p2_percentile,
        )

        # Calculate matchmaking parameters
        target_ratings = [p.target_rating for p in self.players]
        target_highest = max(target_ratings)
        target_lowest = min(target_ratings)
        target_rating_range = target_highest - target_lowest
        bias_target_gap = target_rating_range * 0.2  # 20% of range
        max_match_gap = target_rating_range * 0.4  # 40% of range

        # Calculate challenger selection data
        challenger_selection = []
        for player in self.players:
            challenger_elo_bias = player.target_rating - bias_target_gap
            challenger_max_range = challenger_elo_bias + max_match_gap
            challenger_min_range = challenger_elo_bias - max_match_gap

            total_prob = 0
            for opponent in self.players:
                if opponent == player:
                    continue

                match_gap = abs(player.target_rating - opponent.target_rating)
                if match_gap <= max_match_gap:
                    normalized_gap = match_gap / max_match_gap
                    prob = (1.0 + math.cos(math.pi * normalized_gap)) / 2.0
                    total_prob += prob

            weight = total_prob / (len(self.players) - 1)
            challenger_selection.append(
                {
                    "name": player.name,
                    "rating": player.rating,
                    "target_rating": player.target_rating,
                    "games_played": player.games_played,
                    "selection_weight": weight,
                }
            )

        # Calculate potential opponents data
        potential_opponents = []
        challenger = p1  # p1 is always the challenger
        challenger_elo_bias = challenger.target_rating - bias_target_gap
        challenger_max_range = challenger_elo_bias + max_match_gap
        challenger_min_range = challenger_elo_bias - max_match_gap

        for player in self.players:
            if player == challenger:
                continue

            if (
                player.target_rating >= challenger_min_range
                and player.target_rating <= challenger_max_range
            ):
                match_gap = abs(challenger.target_rating - player.target_rating)
                normalized_gap = match_gap / max_match_gap
                accept_prob = (1.0 + math.cos(math.pi * normalized_gap)) / 2.0

                potential_opponents.append(
                    {
                        "name": player.name,
                        "rating": player.rating,
                        "target_rating": player.target_rating,
                        "games_played": player.games_played,
                        "match_gap": match_gap,
                        "acceptance_probability": accept_prob
                        * 100,  # Convert to percentage
                    }
                )

        # Store initial ratings before updating
        p1_rating_before = p1.rating
        p2_rating_before = p2.rating

        # Update player stats (this modifies the ratings)
        self.scenario.update_player_stats(
            p1, p2, p1_won, p1_variety_bonus, p2_variety_bonus
        )

        # Store match details with updated ratings
        match = {
            "player1": {
                "name": p1.name,
                "rating": p1_rating_before,
                "rating_after": p1.rating,
                "target_rating": p1.target_rating,
                "games_played": p1.games_played,
            },
            "player2": {
                "name": p2.name,
                "rating": p2_rating_before,
                "rating_after": p2.rating,
                "target_rating": p2.target_rating,
                "games_played": p2.games_played,
            },
            "winner": p1.name if p1_won else p2.name,
            "win_probability": win_prob,
            # Calculate individual confidence for each player
            "p1_confidence": min(1.0, p1.games_played / 20),
            "p2_confidence": min(1.0, p2.games_played / 20),
            "variety_bonus": self.scenario.calculate_variety_bonus(p1, p2),
            "p1_multiplier": self.scenario.calculate_multiplier(
                p1, p2, p1_variety_bonus, p2_variety_bonus, p1_won
            )[0],
            "p2_multiplier": self.scenario.calculate_multiplier(
                p1, p2, p1_variety_bonus, p2_variety_bonus, p1_won
            )[1],
            # Add variety bonuses from rating calculator
            "p1_variety_bonus": p1_variety_bonus,
            "p2_variety_bonus": p2_variety_bonus,
            # Add matchmaking parameters
            "bias_target_gap": bias_target_gap,
            "max_match_gap": max_match_gap,
            "target_rating_range": target_rating_range,
            "avg_games_played": sum(p.games_played for p in self.players)
            / len(self.players),
            # Add challenger selection data
            "challenger_selection": challenger_selection,
            # Add potential opponents data
            "potential_opponents": potential_opponents,
        }

        self.match_history.append(match)
        return match

    def _get_opponent_counts(self, player) -> Dict[str, int]:
        """Get opponent counts for a player from match history.

        Args:
            player: The player to get opponent counts for

        Returns:
            Dictionary mapping opponent names to count of matches
        """
        opponent_counts = defaultdict(int)
        for match in self.match_history:
            if match["player1"]["name"] == player.name:
                opponent_counts[match["player2"]["name"]] += 1
            elif match["player2"]["name"] == player.name:
                opponent_counts[match["player1"]["name"]] += 1
        return dict(opponent_counts)

    def _calculate_percentile(self, rating: float, ratings: List[float]) -> float:
        """Calculate the percentile of a player's rating in the current rating distribution.

        Args:
            rating: The player's current rating.
            ratings: A sorted list of all player ratings.

        Returns:
            The percentile (0.0 to 100.0) of the player's rating.
        """
        if not ratings:
            return 50.0  # Default to middle if no ratings

        # Find the position of this rating in the sorted list
        # Count how many ratings are less than this rating
        count_below = 0
        count_equal = 0

        for r in ratings:
            if r < rating:
                count_below += 1
            elif r == rating:
                count_equal += 1

        # Calculate percentile using the formula: (count_below + 0.5 * count_equal) / total * 100
        # This handles ties by giving them the average position
        percentile = (count_below + 0.5 * count_equal) / len(ratings) * 100

        return percentile

    def simulate(self, num_matches: int) -> List[Dict]:
        """Run the full simulation.

        Args:
            num_matches: Number of matches to simulate

        Returns:
            List of match results
        """
        # Generate initial players
        self.players = self.scenario.generate_players()

        # Reset match history
        self.match_history = []

        # Run matches
        results = []
        for _ in range(num_matches):
            match = self.simulate_match()
            results.append(match)

        return results

    def save_results(self, output_dir: str = "simulation_results") -> None:
        """Save simulation results.

        Args:
            output_dir: Directory to save output files
        """
        # Create output directory if it doesn't exist
        os.makedirs(output_dir, exist_ok=True)

        # Save results based on scenario type
        if isinstance(self.scenario, self.scenario.__class__):
            save_ladder_reset_results(self.players, self.match_history, output_dir)
        else:
            raise ValueError(f"Unknown scenario type: {type(self.scenario)}")


def main():
    """Run the simulation."""
    from dev.rating_simulator.scenarios.ladder_reset import LadderResetScenario

    # Create scenario
    scenario = LadderResetScenario()

    # Create simulator
    simulator = ScenarioSimulator(scenario)

    # Run simulation
    print("Running simulation...")
    results = simulator.simulate(num_matches=LADDER_RESET_CONFIG["num_matches"])

    # Save results
    print("Saving results...")
    simulator.save_results()

    print("Done!")


if __name__ == "__main__":
    main()
