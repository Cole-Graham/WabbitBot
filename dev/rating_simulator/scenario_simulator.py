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
        self.late_joiners = []  # Players who will join later
        self.match_history = []
        self.rating_calculator = RatingCalculator()
        self.last_results = []  # Store the results from the last simulation run

        # Track which proven potential adjustments have been applied to current ratings
        # Key: (previous_match_number, opponent_id, threshold_applied)
        self.applied_proven_potential_adjustments = set()

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
        rating_range = (
            max(ratings) - min(ratings) + 400 if ratings else 400
        )  # Add buffer to prevent division by zero

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
        # For ladder reset scenario, use target ratings for percentile calculation
        # since tail scaling should be based on intended skill level
        if (
            hasattr(self.scenario, "get_scenario_name")
            and self.scenario.get_scenario_name() == "Ladder Reset"
        ):
            # Use target ratings for percentile calculation in ladder reset
            current_ratings = [p.target_rating for p in self.players]
        else:
            # Use current ratings for other scenarios
            current_ratings = [p.rating for p in self.players]
        current_ratings.sort()

        # Calculate percentile for each player
        p1_percentile = self._calculate_percentile(
            (
                p1.rating
                if not hasattr(self.scenario, "get_scenario_name")
                or self.scenario.get_scenario_name() != "Ladder Reset"
                else p1.target_rating
            ),
            current_ratings,
        )
        p2_percentile = self._calculate_percentile(
            (
                p2.rating
                if not hasattr(self.scenario, "get_scenario_name")
                or self.scenario.get_scenario_name() != "Ladder Reset"
                else p2.target_rating
            ),
            current_ratings,
        )

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

        # Calculate confidence for each player using logarithmic scaling
        p1_confidence = self.rating_calculator.calculate_confidence(p1.games_played)
        p2_confidence = self.rating_calculator.calculate_confidence(p2.games_played)

        # Determine winner/loser parameters
        if p1_won:
            winner_rating = p1_rating_before
            loser_rating = p2_rating_before
            winner_confidence = p1_confidence
            loser_confidence = p2_confidence
            winner_variety_bonus = p1_variety_bonus
            loser_variety_bonus = p2_variety_bonus
            winner_games_played = p1.games_played
            loser_games_played = p2.games_played
        else:
            winner_rating = p2_rating_before
            loser_rating = p1_rating_before
            winner_confidence = p2_confidence
            loser_confidence = p1_confidence
            winner_variety_bonus = p2_variety_bonus
            loser_variety_bonus = p1_variety_bonus
            winner_games_played = p2.games_played
            loser_games_played = p1.games_played

        # Get scenario-specific parameters
        variety_bonus_games_threshold = getattr(
            self.scenario, "variety_bonus_games_threshold", 0
        )
        catch_up_bonus_config = getattr(self.scenario, "catch_up_bonus_config", None)

        # Calculate rating changes using the scenario-specific rating calculator
        winner_change, loser_change, winner_multiplier, loser_multiplier = (
            self.rating_calculator.calculate_rating_change_for_scenario(
                winner_rating,
                loser_rating,
                winner_confidence,
                loser_confidence,
                winner_variety_bonus,
                loser_variety_bonus,
                rating_range,
                winner_games_played,
                loser_games_played,
                variety_bonus_games_threshold,
                catch_up_bonus_config,
            )
        )

        # Apply rating changes
        if p1_won:
            p1.rating += winner_change
            p2.rating += loser_change
            p1_multiplier = winner_multiplier
            p2_multiplier = loser_multiplier
        else:
            p1.rating += loser_change
            p2.rating += winner_change
            p1_multiplier = loser_multiplier
            p2_multiplier = winner_multiplier

        # Update games played
        p1.games_played += 1
        p2.games_played += 1

        # Create match record for proven potential checking
        # Use the actual simulation match number (passed from simulate method)
        match_number = getattr(
            self, "current_match_number", len(self.match_history) // 2 + 1
        )

        # Create match data for proven potential checking
        current_match = {
            "match_number": match_number,
            "player_id": p1.name,
            "opponent_id": p2.name,
            "player_rating_before": p1_rating_before,
            "opponent_rating_before": p2_rating_before,
            "player_rating_after": p1.rating,
            "opponent_rating_after": p2.rating,
            "player_confidence": p1_confidence,
            "opponent_confidence": p2_confidence,
            "player_won": p1_won,
            "rating_change": winner_change if p1_won else loser_change,
            "player_rating_change": winner_change if p1_won else loser_change,
            "opponent_rating_change": loser_change if p1_won else winner_change,
        }

        # Create opponent match record for proven potential
        opponent_match = {
            "match_number": match_number,
            "player_id": p2.name,
            "opponent_id": p1.name,
            "player_rating_before": p2_rating_before,
            "opponent_rating_before": p1_rating_before,
            "player_rating_after": p2.rating,
            "opponent_rating_after": p1.rating,
            "player_confidence": p2_confidence,
            "opponent_confidence": p1_confidence,
            "player_won": not p1_won,
            "rating_change": winner_change if not p1_won else loser_change,
            "player_rating_change": winner_change if not p1_won else loser_change,
            "opponent_rating_change": loser_change if not p1_won else winner_change,
        }

        # Add to match history
        self.match_history.append(current_match)
        self.match_history.append(opponent_match)

        # Check proven potential for both players AFTER adding to history
        # Both players could qualify for proven potential compensation
        self.rating_calculator.check_proven_potential(current_match, self.match_history)
        self.rating_calculator.check_proven_potential(
            opponent_match, self.match_history
        )

        # Apply proven potential adjustments to current player ratings
        self._apply_proven_potential_adjustments(current_match)
        self._apply_proven_potential_adjustments(opponent_match)

        # Store match details with original ratings (before proven potential adjustments)
        # Calculate the original ratings after the match but before any proven potential adjustments
        p1_rating_after_match = p1_rating_before + (
            winner_change if p1_won else loser_change
        )
        p2_rating_after_match = p2_rating_before + (
            loser_change if p1_won else winner_change
        )

        match = {
            "match_number": match_number,
            "player_id": p1.name,
            "opponent_id": p2.name,
            "player_rating_before": p1_rating_before,
            "opponent_rating_before": p2_rating_before,
            "player_rating_after": p1_rating_after_match,
            "opponent_rating_after": p2_rating_after_match,
            "player_confidence": p1_confidence,
            "opponent_confidence": p2_confidence,
            "player_won": p1_won,
            "rating_change": winner_change if p1_won else loser_change,
            "winner": p1.name if p1_won else p2.name,
            "win_probability": win_prob,
            "p1_variety_bonus": p1_variety_bonus,
            "p2_variety_bonus": p2_variety_bonus,
            "p1_multiplier": p1_multiplier,
            "p2_multiplier": p2_multiplier,
            "challenger_selection": challenger_selection,
            "potential_opponents": potential_opponents,
            "proven_potential_details": current_match.get(
                "proven_potential_details", []
            ),
            "opponent_proven_potential_details": opponent_match.get(
                "proven_potential_details", []
            ),
        }

        return match

    def _apply_proven_potential_adjustments(self, match_data: Dict) -> None:
        """Apply proven potential adjustments to current player ratings.

        Args:
            match_data: The match data containing proven potential details
        """
        if "proven_potential_details" not in match_data:
            return

        for detail in match_data["proven_potential_details"]:
            # Find the original match first
            original_match = next(
                m
                for m in self.match_history
                if m["match_number"] == detail["previous_match_number"]
            )

            # Create a unique key for this adjustment
            # Use the highest threshold applied to ensure uniqueness
            highest_threshold = (
                max(detail["thresholds_applied"]) if detail["thresholds_applied"] else 0
            )
            adjustment_key = (
                detail["previous_match_number"],
                original_match["opponent_id"],
                highest_threshold,
            )

            # Skip if we've already applied this adjustment
            if adjustment_key in self.applied_proven_potential_adjustments:
                continue

            # Get the opponent from the original match (this is the key fix!)
            original_opponent_name = original_match["opponent_id"]

            # Find the opponent player object from the original match
            opponent_player = next(
                (p for p in self.players if p.name == original_opponent_name), None
            )

            if opponent_player is None:
                continue

            # Get the original player from the match
            original_player_name = original_match["player_id"]
            original_player = next(
                (p for p in self.players if p.name == original_player_name), None
            )

            if original_player is not None:
                # Apply adjustment to the original player (who proved their potential)
                original_player.rating += detail["player_adjustment"]

            # Apply adjustment to the opponent from the original match
            opponent_player.rating += detail["opponent_adjustment"]

            # Mark this adjustment as applied
            self.applied_proven_potential_adjustments.add(adjustment_key)

    def _get_opponent_counts(self, player) -> Dict[str, int]:
        """Get opponent counts for a player from match history.

        Args:
            player: The player to get opponent counts for

        Returns:
            Dictionary mapping opponent names to count of matches
        """
        opponent_counts = defaultdict(int)
        for match in self.match_history:
            if match["player_id"] == player.name:
                opponent_counts[match["opponent_id"]] += 1
        return dict(opponent_counts)

    def _generate_late_joiners(self) -> None:
        """Generate late joiners for proven potential testing."""
        from dev.rating_simulator.simulation_config import LADDER_RESET_CONFIG

        if not LADDER_RESET_CONFIG.get("late_joiners", {}).get("enabled", False):
            return

        late_joiner_config = LADDER_RESET_CONFIG["late_joiners"]
        late_joiner_percentage = late_joiner_config["late_joiner_percentage"]
        num_late_joiners = int(
            LADDER_RESET_CONFIG["num_players"] * late_joiner_percentage
        )

        # Generate late joiners with similar logic to regular players
        for i in range(num_late_joiners):
            # Generate target rating using normal distribution
            mean_rating = 1700
            std_dev = 300
            target_rating = random.gauss(mean_rating, std_dev)
            target_rating = max(1000, min(2400, int(round(target_rating))))

            # Create late joiner player
            from dev.rating_simulator.scenarios.ladder_reset import Player

            late_joiner = Player(
                name=f"LateJoiner_{i+1}",
                rating=1000,  # Start at 1000 like everyone else
                target_rating=target_rating,
                games_played=0,  # Start with 0 games (low confidence)
                activity_multiplier=1.0,
            )
            self.late_joiners.append(late_joiner)

    def _check_late_joiners(self, match_num: int) -> None:
        """Check if late joiners should join the ladder."""
        from dev.rating_simulator.simulation_config import LADDER_RESET_CONFIG

        if not LADDER_RESET_CONFIG.get("late_joiners", {}).get("enabled", False):
            return

        late_joiner_config = LADDER_RESET_CONFIG["late_joiners"]
        join_after_matches = late_joiner_config["join_after_matches"]
        join_interval = late_joiner_config["join_interval"]

        # Check if it's time for late joiners to join
        if (
            match_num >= join_after_matches
            and (match_num - join_after_matches) % join_interval == 0
        ):
            # Add a late joiner to the active players
            if self.late_joiners:
                late_joiner = self.late_joiners.pop(0)
                self.players.append(late_joiner)
                print(f"Late joiner {late_joiner.name} joined at match {match_num}")

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

    def _calculate_initial_players(self) -> int:
        """Calculate the number of initial players based on late joiner configuration.

        Returns:
            Number of initial players to generate
        """
        from dev.rating_simulator.simulation_config import LADDER_RESET_CONFIG

        if not LADDER_RESET_CONFIG.get("late_joiners", {}).get("enabled", False):
            # If late joiners disabled, use full player count
            return LADDER_RESET_CONFIG["num_players"]

        late_joiner_config = LADDER_RESET_CONFIG["late_joiners"]
        late_joiner_percentage = late_joiner_config["late_joiner_percentage"]
        total_players = LADDER_RESET_CONFIG["num_players"]

        # Calculate initial players: total - late joiners
        initial_players = int(total_players * (1 - late_joiner_percentage))

        return initial_players

    def simulate(self, num_matches: int) -> List[Dict]:
        """Run the full simulation.

        Args:
            num_matches: Number of matches to simulate

        Returns:
            List of match results
        """
        # Calculate initial players based on late joiner percentage
        initial_players = self._calculate_initial_players()

        # Generate initial players
        self.players = self.scenario.generate_players(initial_players)

        # Generate late joiners if enabled
        self._generate_late_joiners()

        # Reset match history and applied adjustments
        self.match_history = []
        self.applied_proven_potential_adjustments = set()

        # Run matches
        results = []
        for match_num in range(num_matches):
            # Check if late joiners should join
            self._check_late_joiners(match_num)

            # Set the current match number for proven potential tracking
            self.current_match_number = match_num + 1

            match = self.simulate_match()
            results.append(match)

        self.last_results = results  # Store the results
        return results

    def save_results(self, output_dir: str = "simulation_results") -> None:
        """Save simulation results.

        Args:
            output_dir: Directory to save output files
        """
        # Create output directory if it doesn't exist
        os.makedirs(output_dir, exist_ok=True)

        # Use the results from the last simulation run
        results = self.last_results

        # Save results based on scenario type
        if isinstance(self.scenario, self.scenario.__class__):
            save_ladder_reset_results(self.players, results, output_dir)
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
