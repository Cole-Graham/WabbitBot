"""Ladder reset scenario for rating system."""

import random
from typing import Dict, List, Tuple
import statistics
import math

from .base_scenario import BaseScenario
from dev.tools.rating_simulator.rating_calculator import RatingCalculator


class LadderResetScenario(BaseScenario):
    """Scenario simulating a ladder reset where all players start at 1000."""

    def __init__(
        self,
        num_players: int = 100,
        target_rating_distribution: Dict[int, int] = None,
    ):
        """Initialize ladder reset scenario.

        Args:
            num_players: Number of players to simulate
            target_rating_distribution: Dictionary mapping target ratings to number of players
        """
        self.num_players = num_players
        self.target_rating_distribution = target_rating_distribution or {
            1400: 20,
            1600: 20,
            1800: 20,
            2000: 20,
            2200: 20,
        }
        self.rating_calculator = RatingCalculator()

        # Catch-up bonus configuration for ladder reset
        self.convergence_threshold = 200  # Rating difference threshold for convergence
        self.intended_average = (
            1500  # The average rating we want players to converge to
        )

        # Scenario-specific rating parameters
        self.catch_up_bonus_config = {
            "enabled": True,
            "target_rating": self.intended_average,
            "threshold": self.convergence_threshold,
            "max_bonus": 1.0,
        }

    def get_scenario_name(self) -> str:
        """Get the name of the scenario.

        Returns:
            Name of the scenario
        """
        return "Ladder Reset (1000 Start)"

    def get_scenario_description(self) -> str:
        """Get the description of the scenario.

        Returns:
            Description of the scenario
        """
        return (
            "Simulates a ladder reset where all players start at 1000 rating. "
            "Players have target ratings they will tend towards over time, "
            "with matchmaking favoring closer matches and players who have "
            "played fewer games. Uses catch-up bonus to accelerate convergence "
            "to intended average of 1500."
        )

    def generate_players(self, num_players: int = None) -> List:
        """Generate initial players with a curved distribution of target ratings.

        Args:
            num_players: Number of players to generate (defaults to self.num_players)

        Returns:
            List of player objects
        """
        players = []

        # Use provided num_players or default to self.num_players
        player_count = num_players if num_players is not None else self.num_players

        # Generate target ratings using a normal distribution
        # Center around 1700 (middle of 1000-2400 range)
        # Use standard deviation of 300 to create a wide bell curve
        mean_rating = 1700
        std_dev = 300

        # Generate target ratings for all players
        target_ratings = []
        for _ in range(player_count):
            # Generate rating from normal distribution
            rating = random.gauss(mean_rating, std_dev)

            # Clamp to valid range (1000-2400)
            rating = max(1000, min(2400, rating))

            # Round to nearest integer
            target_ratings.append(int(round(rating)))

        # Ensure we have some players at the extremes by manually allocating a few
        num_extreme_players = 10  # 10% of players at extremes

        # Add some very high-rated players (2200-2400)
        for i in range(num_extreme_players // 2):
            if i < len(target_ratings):
                target_ratings[i] = random.randint(2200, 2400)

        # Add some very low-rated players (1000-1200)
        for i in range(num_extreme_players // 2):
            if i + num_extreme_players // 2 < len(target_ratings):
                target_ratings[i + num_extreme_players // 2] = random.randint(
                    1000, 1200
                )

        # Create players with the generated target ratings and activity levels
        for i, target_rating in enumerate(target_ratings):
            # Calculate activity level based on skill (target rating)
            # Higher skill = higher activity, with some randomness for outliers

            # Normalize target rating to 0-1 scale (1000-2400 range)
            normalized_skill = (target_rating - 1000) / (2400 - 1000)

            # Base activity multiplier: much stronger correlation with skill
            # 0.5x for lowest skill, 1.0x for average skill, 3.0x for highest skill
            base_activity = 0.5 + (normalized_skill * 2.5)  # 0.5 to 3.0

            # Add randomness for outliers with more extreme variation
            # Use a distribution that allows for more extreme outliers
            # 70% of players get normal variation (±20%), 30% get extreme variation (±50%)
            if random.random() < 0.7:
                # Normal variation
                random_factor = random.uniform(0.8, 1.2)
            else:
                # Extreme variation - allows low/average skill players to be very active
                random_factor = random.uniform(0.5, 1.5)

            activity_multiplier = base_activity * random_factor

            # Ensure some minimum and maximum bounds
            activity_multiplier = max(0.3, min(4.5, activity_multiplier))

            player = Player(
                name=f"Player_{i+1}",
                rating=1000,  # All players start at 1000
                target_rating=target_rating,
                games_played=0,
                activity_multiplier=activity_multiplier,
            )
            players.append(player)

        return players

    def select_matchup(self, players: List) -> Tuple:
        """Select two players to match against each other.

        Args:
            players: List of available players

        Returns:
            Tuple of (challenger, opponent)
        """
        # Calculate target rating range
        target_ratings = [p.target_rating for p in players]
        target_highest = max(target_ratings)
        target_lowest = min(target_ratings)
        target_rating_range = target_highest - target_lowest

        # Calculate matchmaking parameters
        bias_target_gap = target_rating_range * 0.2  # 20% of range
        max_match_gap = target_rating_range * 0.4  # 40% of range

        # Select challenger with bias toward more active players
        # Use activity multipliers as weights for selection
        activity_weights = [p.activity_multiplier for p in players]
        challenger = random.choices(players, weights=activity_weights, k=1)[0]

        # Find potential opponents
        potential_opponents = []
        for opponent in players:
            if opponent != challenger:
                match_gap = abs(challenger.target_rating - opponent.target_rating)
                if match_gap <= max_match_gap:
                    # Calculate acceptance probability
                    acceptance_prob = 1.0 - (match_gap / max_match_gap)
                    potential_opponents.append((opponent, acceptance_prob))

        # Select opponent based on acceptance probability
        if potential_opponents:
            opponents, probs = zip(*potential_opponents)
            selected_opponent = random.choices(opponents, weights=probs, k=1)[0]
        else:
            # If no valid opponents, select random player
            selected_opponent = random.choice([p for p in players if p != challenger])

        return challenger, selected_opponent

    def calculate_win_probability(self, p1, p2) -> float:
        """Calculate probability of player 1 winning.

        Args:
            p1: Player 1
            p2: Player 2

        Returns:
            Win probability for player 1
        """
        # Base probability on target ratings
        rating_diff = p1.target_rating - p2.target_rating
        win_prob = 1 / (1 + 10 ** (-rating_diff / 400))
        return win_prob

    def calculate_variety_bonus(self, p1, p2) -> float:
        """Calculate variety bonus for the match.

        This method is kept for compatibility but the actual variety bonus
        calculation is now done in the scenario simulator using the RatingCalculator.

        Args:
            p1: Player 1
            p2: Player 2

        Returns:
            Variety bonus (0-1) - simplified fallback
        """
        # This is a simplified fallback - the actual calculation is done in the simulator
        # Base variety on rating difference
        rating_diff = abs(p1.target_rating - p2.target_rating)
        variety = min(1.0, rating_diff / 400)  # Max variety at 400 rating difference
        return variety

    def calculate_multiplier(self, p1, p2) -> float:
        """Calculate rating change multiplier for the match.

        This is a minimal implementation since the actual multiplier calculation
        is now handled by the rating calculator in the scenario simulator.

        Args:
            p1: Player 1
            p2: Player 2

        Returns:
            Default multiplier of 1.0 (actual calculation done by rating calculator)
        """
        return 1.0

    def update_player_stats(self, p1, p2, p1_won: bool) -> None:
        """Update player statistics after a match.

        This is a minimal implementation since the actual rating updates
        are now handled by the rating calculator in the scenario simulator.

        Args:
            p1: Player 1
            p2: Player 2
            p1_won: Whether player 1 won
        """
        # The actual rating updates are handled by the rating calculator
        # This method is just here to satisfy the abstract base class
        pass


class Player:
    """Player class for ladder reset scenario."""

    def __init__(
        self,
        name: str,
        rating: float,
        target_rating: int,
        games_played: int,
        activity_multiplier: float = 1.0,
    ):
        """Initialize player.

        Args:
            name: Player name
            rating: Current rating
            target_rating: Target rating
            games_played: Number of games played
            activity_multiplier: Multiplier for matchmaking probability (higher = more likely to be selected)
        """
        self.name = name
        self.rating = rating
        self.target_rating = target_rating
        self.games_played = games_played
        self.activity_multiplier = activity_multiplier

    def get_scenario_name(self) -> str:
        return "Ladder Reset (1000 Start)"

    def get_scenario_description(self) -> str:
        return (
            "Simulates a ladder reset where all players start at 1000 rating. "
            "Players have target ratings they will tend towards over time, "
            "with matchmaking favoring closer matches and players who have "
            "played fewer games. Uses catch-up bonus to accelerate convergence "
            "to intended average of 1500."
        )
