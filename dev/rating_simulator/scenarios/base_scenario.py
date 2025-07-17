"""Base scenario class for rating system."""

from abc import ABC, abstractmethod
from typing import List, Tuple


class BaseScenario(ABC):
    """Base class for rating system scenarios."""

    @abstractmethod
    def generate_players(self) -> List:
        """Generate initial players.

        Returns:
            List of player objects
        """
        pass

    @abstractmethod
    def select_matchup(self, players: List) -> Tuple:
        """Select two players to match against each other.

        Args:
            players: List of available players

        Returns:
            Tuple of (challenger, opponent)
        """
        pass

    @abstractmethod
    def calculate_win_probability(self, p1, p2) -> float:
        """Calculate probability of player 1 winning.

        Args:
            p1: Player 1
            p2: Player 2

        Returns:
            Win probability for player 1
        """
        pass

    @abstractmethod
    def calculate_variety_bonus(self, p1, p2) -> float:
        """Calculate variety bonus for the match.

        Args:
            p1: Player 1
            p2: Player 2

        Returns:
            Variety bonus (0-1)
        """
        pass

    @abstractmethod
    def calculate_multiplier(self, p1, p2) -> float:
        """Calculate rating change multiplier for the match.

        Args:
            p1: Player 1
            p2: Player 2

        Returns:
            Rating change multiplier
        """
        pass

    @abstractmethod
    def update_player_stats(self, p1, p2, p1_won: bool) -> None:
        """Update player statistics after a match.

        Args:
            p1: Player 1
            p2: Player 2
            p1_won: Whether player 1 won
        """
        pass

    @abstractmethod
    def get_scenario_name(self) -> str:
        """Get the name of the scenario."""
        pass

    @abstractmethod
    def get_scenario_description(self) -> str:
        """Get a description of the scenario."""
        pass
