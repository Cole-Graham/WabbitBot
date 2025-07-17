import math
from typing import Dict, List, Tuple

# ============================================================================
# Rating System Configuration
# ============================================================================

# Rating System Parameters
RATING_CONFIG = {
    # Base rating for new players/teams
    "starting_rating": 1500,
    # Minimum rating possible
    "minimum_rating": 1300,
    # Base K-factor for ELO calculations
    "base_rating_change": 16.0,
    # ELO rating divisor (default 400)
    "elo_divisor": 400.0,
    # Maximum matches to look back for proven potential
    "max_matches_for_proven_potential": 10,
    # Maximum confidence to consider for proven potential
    "max_confidence_for_proven_potential": 1.0,  # Only consider matches where player had low confidence
    # Gap closure threshold for proven potential (e.g. 0.1 means every 10% of gap closed)
    "proven_potential_gap_threshold": 0.1,
}

# Confidence System Parameters
CONFIDENCE_CONFIG = {
    # Games needed for maximum confidence (1.0)
    "max_confidence_games": 20,
    # Maximum confidence
    "max_confidence": 1.0,
}

# Variety Bonus Parameters
VARIETY_CONFIG = {
    # Maximum variety bonus (positive)
    "max_variety_bonus": 0.2,
    # Minimum variety bonus (negative)
    "min_variety_bonus": -0.1,
    # Maximum percentage difference before opponent weight is zeroed
    "max_gap_percent": 0.4,
    # Minimum scaling factor for games played
    "min_scaling_factor": 0.5,
}

# Multiplier Parameters
MULTIPLIER_CONFIG = {
    # Minimum possible multiplier
    "min_multiplier": 0.5,
    # Maximum possible multiplier
    "max_multiplier": 2.0,
}


class RatingCalculator:
    def __init__(
        self,
        starting_rating: int = RATING_CONFIG["starting_rating"],
        base_rating_change: float = RATING_CONFIG["base_rating_change"],
        max_variety_bonus: float = VARIETY_CONFIG["max_variety_bonus"],
        min_variety_bonus: float = VARIETY_CONFIG["min_variety_bonus"],
        max_gap_percent: float = VARIETY_CONFIG["max_gap_percent"],
    ):
        self.starting_rating = starting_rating
        self.base_rating_change = base_rating_change
        self.max_variety_bonus = max_variety_bonus
        self.min_variety_bonus = min_variety_bonus
        self.max_gap_percent = max_gap_percent

    def calculate_confidence(self, games_played: int) -> float:
        """Calculate confidence based on games played.

        Uses a logarithmic scaling that:
        - Starts quickly (high initial growth)
        - Levels out as games played increases
        - Reaches max_confidence at max_confidence_games
        - Stays at max_confidence for the rest of the season

        Formula:
        confidence = min(
            max_confidence,
            max_confidence * (1 - e^(-k * games_played / max_confidence_games))
        )
        where k is a scaling factor that controls how quickly confidence grows
        """
        if games_played >= CONFIDENCE_CONFIG["max_confidence_games"]:
            return CONFIDENCE_CONFIG["max_confidence"]

        # Calculate how far along we are in the confidence growth (0 to 1)
        progress = games_played / CONFIDENCE_CONFIG["max_confidence_games"]

        # Use exponential decay formula: 1 - e^(-k * x)
        # k = 3.0 gives a good balance of quick early growth and smooth leveling
        k = 3.0
        confidence = CONFIDENCE_CONFIG["max_confidence"] * (1 - math.exp(-k * progress))

        return confidence

    def calculate_variety_bonus(
        self,
        player_rating: int,
        opponent_rating: int,
        opponent_counts: Dict[str, int],
        opponents: List[Dict],
        player_games_played: int,
        median_games_played: float,
        simulated_avg_variety_score: float,
        player_percentile: float = None,  # Player's percentile in current rating distribution (0-100)
    ) -> float:
        """Calculate variety bonus based on opponent distribution.

        Matches the C# implementation:
        - Uses Shannon entropy to measure opponent diversity
        - Weights opponents based on rating gap
        - Normalizes entropy to get variety bonus
        - Scales between MIN_VARIETY_BONUS and MAX_VARIETY_BONUS
        - Applies quadratic scaling based on games played relative to median
        - Only counts opponents within MAX_GAP_PERCENT (40%) of player's rating
        - Gives full weight (1.0) to higher-rated opponents
        - Uses cosine scaling for lower-rated opponents:
          - 0% gap: 1.0
          - 25% gap: 0.9
          - 50% gap: 0.75
          - 75% gap: 0.5
          - 100% gap: 0.0

        Applies distribution scaling for players at extremes:
        - Top 10%: 90th-99th percentile get boosted variety bonuses
        - Bottom 10%: 1st-10th percentile get boosted variety bonuses
        - Scaling formula based on distance from 10th/90th percentile
        """
        if not opponent_counts:
            return self.max_variety_bonus

        # Calculate weighted opponent distribution
        total_weight = 0
        weighted_counts = {}
        rating_range = max(o.rating for o in opponents) - min(
            o.rating for o in opponents
        )
        # rating_range = highest elo player - lowest elo player
        variety_gap = rating_range * 0.4 / 2

        for name, count in opponent_counts.items():
            opponent = next(o for o in opponents if o.name == name)
            # Calculate opponent weight based on rating gap
            gap = abs(player_rating - opponent.rating)

            # Only count opponents within variety gap
            if gap <= variety_gap:
                # For higher-rated opponents, use full weight
                if opponent.rating >= player_rating:
                    weight = 1.0
                else:
                    # For lower-rated opponents, use cosine scaling
                    # normalized_gap is gap normalized to 1.0 (0.0 to 1.0)
                    normalized_gap = gap / variety_gap
                    weight = (1.0 + math.cos(math.pi * normalized_gap * 0.7)) / 2.0

                weighted_counts[name] = count * weight
                total_weight += count * weight

        # If no valid opponents, return max bonus
        if not weighted_counts:
            return self.max_variety_bonus

        # Calculate player's variety entropy
        player_variety_entropy = 0.0
        for count in weighted_counts.values():
            if count > 0:
                p = count / total_weight
                player_variety_entropy -= p * math.log2(p)

        # Calculate entropy difference from simulated average variety score
        entropy_difference = player_variety_entropy - simulated_avg_variety_score
        relative_diff = entropy_difference / (
            abs(simulated_avg_variety_score)
            if simulated_avg_variety_score != 0
            else 1.0
        )

        # Calculate games played scaling factor
        games_played_ratio = min(player_games_played / median_games_played, 1.0)
        scaling_factor = VARIETY_CONFIG["min_scaling_factor"] + (
            (1.0 - VARIETY_CONFIG["min_scaling_factor"])
            * games_played_ratio
            * games_played_ratio
        )

        # Apply scaling factor to relative difference
        scaled_diff = relative_diff * scaling_factor

        # Calculate base bonus
        base_bonus = scaled_diff * self.max_variety_bonus

        # Apply distribution scaling for players at the extremes
        # This helps players at the top and bottom 10% who have fewer opponents at their skill level
        if player_percentile is not None:
            # Calculate distance from the middle (50th percentile)
            # 0 = at 50th percentile, 1 = at 0th or 100th percentile
            distance_from_middle = abs(player_percentile - 50.0) / 50.0

            # Only apply scaling to top and bottom 10% (distance > 0.8)
            if distance_from_middle > 0.8:
                # Use sigmoid-based scaling for smooth curve
                if player_percentile >= 90:
                    # Top 10%: use sigmoid curve from 90-100 percentile
                    bonus_multiplier = self._scaled_bonus_curve_percentile(
                        int(player_percentile)
                    )
                else:
                    # Bottom 10%: mirror the top 10% scaling
                    # Map 10th percentile to 90th, 1st to 99th, etc.
                    mirrored_percentile = 100 - player_percentile
                    bonus_multiplier = self._scaled_bonus_curve_percentile(
                        int(mirrored_percentile)
                    )

                # Apply the bonus multiplier
                # The scaling should always INCREASE variety bonuses for players at extremes
                # to compensate for naturally lower variety due to fewer opponents
                # Convert sigmoid output (0.1 to 1.0) to increase factor (1.1 to 2.0)
                increase_factor = 1.0 + bonus_multiplier

                # Apply symmetric scaling: both positive and negative get the same relative improvement
                # e.g., if +5% becomes +10% (100% increase), then -5% should become 0% (100% improvement)
                if base_bonus >= 0:
                    # Positive bonus: increase by the increase factor
                    # e.g., 0.05 (5% bonus) with 100% scaling becomes 0.1 (10% bonus)
                    base_bonus *= increase_factor
                else:
                    # Negative penalty: move toward 0 by the same relative amount
                    # e.g., -0.05 (5% penalty) with 100% scaling becomes 0.0 (0% penalty)
                    # This gives the same relative improvement as positive bonuses
                    base_bonus = base_bonus * (2.0 - increase_factor)

        # Calculate final bonus with clamping
        return max(
            self.min_variety_bonus,
            min(self.max_variety_bonus, base_bonus),
        )

    def _scaled_bonus_curve_percentile(self, p: int) -> float:
        """
        Input: percentile from 90 to 100
        Output: bonus multiplier from 0.1 to 1.0 (smooth sigmoid)
        """
        if not 90 <= p <= 100:
            raise ValueError("Percentile must be between 90 and 100")

        # Map percentile to x in range [1, 10]
        x = p - 89

        k = 1.1  # Growth rate
        x0 = 8.0  # Midpoint (same as original)

        raw = 1 / (1 + math.exp(-k * (x - x0)))
        min_raw = 1 / (1 + math.exp(-k * (1 - x0)))
        max_raw = 1 / (1 + math.exp(-k * (10 - x0)))

        scaled = (raw - min_raw) / (max_raw - min_raw)  # Normalize to [0, 1]
        return 0.1 + scaled * 0.9  # Scale to [0.1, 1.0]

    def calculate_rating_change(
        self,
        winner_rating: int,
        loser_rating: int,
        winner_confidence: float,
        loser_confidence: float,
        winner_variety_bonus: float,
        loser_variety_bonus: float,
        rating_range: float,  # Total range of leaderboard
    ) -> Tuple[int, int]:
        """Calculate rating change for a match.

        Matches the C# implementation:
        - Calculate expected score using ELO formula
        - Calculate base rating change as K * (actual - expected)
        - Apply multiplier to final change
        - Use cosine scaling to decrease adjustments for large rating gaps to prevent shadow-boxing
        - Max gap is 20% of total leaderboard range (40% / 2) on either side of player's rating
        - Only apply gap scaling to higher-rated player if lower-rated player has 1.0 confidence
        - Cosine scaling values:
          - 0% gap: 1.0 (full weight)
          - 25% gap: 0.9
          - 50% gap: 0.75
          - 75% gap: 0.5
          - 100% gap: 0.0 (no weight)

        Returns:
            Tuple[int, int]: (winner_rating_change, loser_rating_change)
        """
        # Calculate expected score using ELO formula
        expected_score = 1.0 / (
            1.0
            + math.pow(
                10, (loser_rating - winner_rating) / RATING_CONFIG["elo_divisor"]
            )
        )

        # Calculate base rating change (K * (actual - expected))
        base_change = self.base_rating_change * (1 - expected_score)

        # Calculate confidence multipliers for each player (1.0 to 2.0 based on confidence)
        winner_confidence_multiplier = 2.0 - winner_confidence
        loser_confidence_multiplier = 2.0 - loser_confidence

        # Calculate rating gap scaling to prevent shadow-boxing
        # rating_range is total leaderboard range, calculate 20% of it (40% / 2)
        max_gap = rating_range * 0.4 / 2
        rating_gap = abs(winner_rating - loser_rating)

        # Initialize gap scaling (no effect by default)
        gap_scaling = 1.0

        if rating_gap <= max_gap:
            # normalized_gap is rating_gap normalized to 1.0 (0.0 to 1.0)
            normalized_gap = rating_gap / max_gap
            # Use cosine scaling: (1 + cos(π * normalized_gap * 0.7)) / 2
            cosine_scaling = (1.0 + math.cos(math.pi * normalized_gap * 0.7)) / 2.0

            # Only apply gap scaling to higher-rated player if lower-rated player has 1.0 confidence
            if winner_rating > loser_rating and loser_confidence >= 1.0:
                # Winner is higher rated, apply scaling to their change
                gap_scaling = cosine_scaling
            elif loser_rating > winner_rating and winner_confidence >= 1.0:
                # Loser is higher rated, apply scaling to their change
                gap_scaling = cosine_scaling

        # Calculate final multipliers for each player
        # Both players use the same formula: (1.0 + variety_bonus) * confidence_multiplier
        winner_multiplier = (1.0 + winner_variety_bonus) * winner_confidence_multiplier
        loser_multiplier = (1.0 + loser_variety_bonus) * loser_confidence_multiplier

        # Clamp multipliers between min and max values
        winner_multiplier = max(
            MULTIPLIER_CONFIG["min_multiplier"],
            min(MULTIPLIER_CONFIG["max_multiplier"], winner_multiplier),
        )
        loser_multiplier = max(
            MULTIPLIER_CONFIG["min_multiplier"],
            min(MULTIPLIER_CONFIG["max_multiplier"], loser_multiplier),
        )

        # Calculate final rating changes
        if winner_rating > loser_rating:
            # Winner is higher rated, apply gap scaling to their change
            winner_change = int(base_change * winner_multiplier * gap_scaling)
            loser_change = int(-base_change * loser_multiplier)
        else:
            # Loser is higher rated, apply gap scaling to their change
            winner_change = int(base_change * winner_multiplier)
            loser_change = int(-base_change * loser_multiplier * gap_scaling)

        # Store the final multiplier in the match data for reporting
        return winner_change, loser_change, winner_multiplier, loser_multiplier

    def check_proven_potential(self, current_match: Dict, match_history: List[Dict]):
        """Check if player has proven their potential against previous opponents."""
        player_id = current_match["player_id"]
        current_rating = current_match["player_rating_after"]

        # Get recent matches for this player
        player_matches = [
            m
            for m in match_history
            if m["player_id"] == player_id
            and m["match_number"] < current_match["match_number"]
            and m["match_number"]
            >= current_match["match_number"]
            - RATING_CONFIG["max_matches_for_proven_potential"]
        ]

        # Initialize proven potential details list for current match
        if "proven_potential_details" not in current_match:
            current_match["proven_potential_details"] = []

        # For each previous match, check if player has proven their potential
        for match in player_matches:
            opponent_rating_at_time = match["opponent_rating_before"]
            player_rating_at_time = match["player_rating_before"]
            player_confidence_at_time = match["player_confidence"]

            # Only consider matches where the player had low confidence
            if (
                player_confidence_at_time
                > RATING_CONFIG["max_confidence_for_proven_potential"]
            ):
                continue

            # Calculate original rating gap
            original_gap = abs(opponent_rating_at_time - player_rating_at_time)
            if original_gap == 0:
                continue

            # Calculate how much of the gap has been closed
            current_gap = abs(opponent_rating_at_time - current_rating)
            gap_closed = original_gap - current_gap
            gap_closure_percent = gap_closed / original_gap

            # Initialize applied thresholds set if not exists
            if "applied_thresholds" not in match:
                match["applied_thresholds"] = set()

            # Calculate the next threshold to check (in multiples of 10%)
            next_threshold = (
                math.floor(
                    max(match["applied_thresholds"], default=0)
                    / RATING_CONFIG["proven_potential_gap_threshold"]
                )
                + 1
            ) * RATING_CONFIG["proven_potential_gap_threshold"]

            # Only apply adjustment if we've crossed the next threshold
            if gap_closure_percent < next_threshold:
                continue

            # Skip if we've already applied this threshold
            if next_threshold in match["applied_thresholds"]:
                continue

            # Recalculate the ELO change using current ratings
            # The expected score should be higher when the player's rating is higher
            expected_score = 1.0 / (
                1.0
                + math.pow(
                    10,
                    (player_rating_at_time - opponent_rating_at_time)
                    / RATING_CONFIG["elo_divisor"],
                )
            )

            # Calculate base rating change
            base_change = self.base_rating_change * (1 - expected_score)

            # Calculate confidence multiplier
            confidence_multiplier = 1.0 + (1.0 - player_confidence_at_time)

            # Calculate final rating change
            new_rating_change = int(base_change * confidence_multiplier)

            # Calculate the difference between original and new rating change
            original_change = match["rating_change"]
            rating_adjustment = (
                original_change - new_rating_change
            )  # Reversed to get correct sign

            # Apply the rating adjustment to both players
            if match["player_won"]:
                # Player was stronger than their rating suggested, so opponent gets points back
                match["opponent_rating_after"] += rating_adjustment
                # Store the rating adjustment in the match data for reporting
                if "proven_potential_adjustment" not in match:
                    match["proven_potential_adjustment"] = 0
                match["proven_potential_adjustment"] = (
                    rating_adjustment  # Positive because opponent gains points
                )
            else:
                # Player was weaker than their rating suggested, so opponent loses points
                match["opponent_rating_after"] -= rating_adjustment
                # Store the rating adjustment in the match data for reporting
                if "proven_potential_adjustment" not in match:
                    match["proven_potential_adjustment"] = 0
                match["proven_potential_adjustment"] = (
                    -rating_adjustment  # Negative because opponent loses points
                )

            # Mark this threshold as applied
            match["applied_thresholds"].add(next_threshold)

            # Create the complete detail record
            detail_record = {
                "previous_match_number": match["match_number"],
                "original_gap": original_gap,
                "current_gap": current_gap,
                "gap_closed": gap_closed,
                "gap_closure_percent": gap_closure_percent,
                "original_rating_change": original_change,
                "new_rating_change": new_rating_change,
                "rating_adjustment": rating_adjustment,
                "applied_to_both": True,
                "threshold_applied": next_threshold,
            }

            # Store detailed calculations in the current match
            current_match["proven_potential_details"].append(detail_record)
