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

from scenarios.base_scenario import BaseScenario
from rating_calculator import RatingCalculator, RATING_CONFIG
from simulation_config import LADDER_RESET_CONFIG, VARIETY_CONFIG
from scenarios.ladder_reset import LadderResetScenario, Player
from scenarios.output import save_ladder_reset_results


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
        self.pending_pp = defaultdict(list)
        self.pp_finalizations = []  # Track batch finalization events

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
        rating_range = max(ratings) - min(ratings) if ratings else 0

        # Calculate median games played
        games_played_list = [p.games_played for p in self.players]
        median_games_played = (
            statistics.median(games_played_list) if games_played_list else 0
        )

        # Handle division by zero case
        if median_games_played == 0:
            median_games_played = 1.0  # Use 1.0 as default to avoid division by zero

        # Calculate actual average variety score from all players
        simulated_avg_variety_score = self._calculate_avg_variety_score()

        # Calculate player percentiles in current rating distribution
        # For ladder reset scenario, use target ratings for percentile calculation
        # since tail scaling should be based on intended skill level
        if (
            hasattr(self.scenario, "get_scenario_name")
            and self.scenario.get_scenario_name() == "Ladder Reset (1000 Start)"
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
                or self.scenario.get_scenario_name() != "Ladder Reset (1000 Start)"
                else p1.target_rating
            ),
            current_ratings,
        )
        p2_percentile = self._calculate_percentile(
            (
                p2.rating
                if not hasattr(self.scenario, "get_scenario_name")
                or self.scenario.get_scenario_name() != "Ladder Reset (1000 Start)"
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

        # Store pre-match games played
        p1_games_before = p1.games_played
        p2_games_before = p2.games_played

        # Determine winner/loser parameters
        if p1_won:
            winner_rating = p1_rating_before
            loser_rating = p2_rating_before
            winner_confidence = p1_confidence
            loser_confidence = p2_confidence
            winner_variety_bonus = p1_variety_bonus
            loser_variety_bonus = p2_variety_bonus
            winner_games_played = p1_games_before
            loser_games_played = p2_games_before
        else:
            winner_rating = p2_rating_before
            loser_rating = p1_rating_before
            winner_confidence = p2_confidence
            loser_confidence = p1_confidence
            winner_variety_bonus = p2_variety_bonus
            loser_variety_bonus = p1_variety_bonus
            winner_games_played = p2_games_before
            loser_games_played = p1_games_before

        # Get scenario-specific parameters

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

        # Create match record (without PP)
        match_number = getattr(
            self, "current_match_number", len(self.match_history) // 2 + 1
        )

        # Calculate original ratings after the match (before any PP)
        p1_rating_after_match = p1_rating_before + (
            winner_change if p1_won else loser_change
        )
        p2_rating_after_match = p2_rating_before + (
            loser_change if p1_won else winner_change
        )

        # Check for PP trigger
        gap = abs(p1_rating_before - p2_rating_before)
        min_games = LADDER_RESET_CONFIG["min_established_games_for_pp"]
        new_player = None
        est_player = None
        est_change = 0.0
        new_change = 0.0
        est_won = False
        initial_new_after = 0.0
        if gap > 0 and (
            (
                p1_confidence >= 1.0
                and p2_confidence < 1.0
                and p1_games_before >= min_games
            )
            or (
                p2_confidence >= 1.0
                and p1_confidence < 1.0
                and p2_games_before >= min_games
            )
        ):
            if (
                p1_confidence >= 1.0
                and p2_confidence < 1.0
                and p1_games_before >= min_games
            ):
                new_player = p2
                est_player = p1
                new_before = p2_rating_before
                est_before = p1_rating_before
                new_change = winner_change if not p1_won else loser_change
                est_change = winner_change if p1_won else loser_change
                est_won = p1_won
            else:
                new_player = p1
                est_player = p2
                new_before = p1_rating_before
                est_before = p2_rating_before
                new_change = winner_change if p1_won else loser_change
                est_change = loser_change if p1_won else winner_change
                est_won = not p1_won
            initial_new_after = new_player.rating
            trigger = {
                "match_number": match_number,
                "est_player_name": est_player.name,
                "est_before": est_before,
                "new_before": new_before,
                "initial_new_after": initial_new_after,
                "est_change": est_change,
                "new_change": new_change,
                "est_won": est_won,
                "tracking_end": match_number
                + RATING_CONFIG["max_matches_for_proven_potential"],
                "subsequent_afters": [],
                "applied": False,
            }
            if new_player.name not in self.pending_pp:
                self.pending_pp[new_player.name] = []
            self.pending_pp[new_player.name].append(trigger)

        match = {
            "match_number": match_number,
            "player_id": p1.name,
            "opponent_id": p2.name,
            "player_rating_before": p1_rating_before,
            "opponent_rating_before": p2_rating_before,
            "player_rating_after": p1_rating_after_match,
            "opponent_rating_after": p2_rating_after_match,
            "player_games_played_before": p1_games_before,
            "opponent_games_played_before": p2_games_before,
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
        }

        # Add to match history (both perspectives, without PP details)
        current_match = match.copy()
        current_match.update(
            {
                "match_number": match_number,
                "player_id": p1.name,
                "opponent_id": p2.name,
                "player_rating_before": p1_rating_before,
                "opponent_rating_before": p2_rating_before,
                "player_rating_after": p1_rating_after_match,
                "opponent_rating_after": p2_rating_after_match,
                "player_games_played_before": p1_games_before,
                "opponent_games_played_before": p2_games_before,
                "player_confidence": p1_confidence,
                "opponent_confidence": p2_confidence,
                "player_won": p1_won,
                "rating_change": winner_change if p1_won else loser_change,
                "player_rating_change": winner_change if p1_won else loser_change,
                "opponent_rating_change": loser_change if p1_won else winner_change,
            }
        )
        self.match_history.append(current_match)

        opponent_match = match.copy()
        opponent_match.update(
            {
                "match_number": match_number,
                "player_id": p2.name,
                "opponent_id": p1.name,
                "player_rating_before": p2_rating_before,
                "opponent_rating_before": p1_rating_before,
                "player_rating_after": p2_rating_after_match,
                "opponent_rating_after": p1_rating_after_match,
                "player_games_played_before": p2_games_before,
                "opponent_games_played_before": p1_games_before,
                "player_confidence": p2_confidence,
                "opponent_confidence": p1_confidence,
                "player_won": not p1_won,
                "rating_change": winner_change if not p1_won else loser_change,
                "player_rating_change": winner_change if not p1_won else loser_change,
                "opponent_rating_change": loser_change if not p1_won else winner_change,
            }
        )
        self.match_history.append(opponent_match)

        return match

    def post_process_proven_potential(self):
        """Post-process all matches to calculate deferred Proven Potential adjustments."""
        for match in self.last_results:
            p1_won = match["player_won"]
            player_change = match["player_rating_after"] - match["player_rating_before"]
            opponent_change = (
                match["opponent_rating_after"] - match["opponent_rating_before"]
            )
            player_conf = match["player_confidence"]
            opponent_conf = match["opponent_confidence"]
            player_before = match["player_rating_before"]
            opponent_before = match["opponent_rating_before"]

            # Identify winner and loser for original changes
            if p1_won:
                winner_original_change = player_change
                loser_original_change = opponent_change
            else:
                winner_original_change = opponent_change
                loser_original_change = player_change

            match["original_winner_change"] = winner_original_change
            match["original_loser_change"] = loser_original_change

            # Default: no PP
            match["pp_applicable"] = False
            match["pp_scaling"] = 1.0
            match["pp_crossed_thresholds"] = 0
            match["player_adjusted_change"] = player_change
            match["opponent_adjusted_change"] = opponent_change

            # Check for PP applicability based on confidence difference
            if player_conf >= 1.0 and opponent_conf < 1.0:
                est_change = player_change
                new_change = opponent_change
                est_before = player_before
                new_before = opponent_before
                est_name = match["player_id"]
                new_name = match["opponent_id"]
                est_is_p1 = True
                est_games = match["player_games_played_before"]
            elif opponent_conf >= 1.0 and player_conf < 1.0:
                est_change = opponent_change
                new_change = player_change
                est_before = opponent_before
                new_before = player_before
                est_name = match["opponent_id"]
                new_name = match["player_id"]
                est_is_p1 = False
                est_games = match["opponent_games_played_before"]
            else:
                # Both same confidence level: no PP
                continue

            # Check minimum games for established player
            if est_games < LADDER_RESET_CONFIG["min_established_games_for_pp"]:
                continue

            # Compute gap; skip if no meaningful gap
            gap = abs(est_before - new_before)
            if gap == 0:
                continue

            # PP applicable
            match["pp_applicable"] = True

            threshold_increment = gap * RATING_CONFIG["proven_potential_gap_threshold"]
            initial_new_after = new_before + new_change

            # Find new player's next 16 participations (where new is player_id)
            subsequent_match_nums = set(
                m["match_number"]
                for m in self.match_history
                if m["match_number"] > match["match_number"]
                and m["player_id"] == new_name
            )
            sorted_sub_nums = sorted(list(subsequent_match_nums))[
                : RATING_CONFIG["max_matches_for_proven_potential"]
            ]

            next_16_afters = []
            for num in sorted_sub_nums:
                sub_entry = next(
                    (
                        m
                        for m in self.match_history
                        if m["match_number"] == num and m["player_id"] == new_name
                    ),
                    None,
                )
                if sub_entry:
                    next_16_afters.append(sub_entry["player_rating_after"])

            # Compute max_reached
            all_new_afters = [initial_new_after] + next_16_afters
            max_reached = max(all_new_afters) if all_new_afters else initial_new_after

            # Count crossed thresholds
            crossed_count = 0
            for i in range(1, 11):  # Up to 100%
                th = initial_new_after + i * threshold_increment
                if th <= max_reached:
                    crossed_count += 1
                else:
                    break

            closure_fraction = (
                crossed_count * RATING_CONFIG["proven_potential_gap_threshold"]
            )

            # Determine if established won
            est_won = p1_won if est_is_p1 else not p1_won

            # Compute adjusted changes
            if est_won:
                adjusted_est = est_change * (1 + closure_fraction)
                adjusted_new = new_change * (1 + closure_fraction)
                pp_scaling = 1 + closure_fraction
            else:
                adjusted_est = est_change * (1 - closure_fraction)
                adjusted_new = new_change * (1 - closure_fraction)
                pp_scaling = 1 - closure_fraction

            match["pp_scaling"] = pp_scaling
            match["pp_crossed_thresholds"] = crossed_count

            # Store adjusted changes based on p1/p2
            if est_is_p1:
                match["player_adjusted_change"] = adjusted_est
                match["opponent_adjusted_change"] = adjusted_new
            else:
                match["player_adjusted_change"] = adjusted_new
                match["opponent_adjusted_change"] = adjusted_est

            # For output: compute adjusted winner/loser changes
            if p1_won:
                match["adjusted_winner_change"] = match["player_adjusted_change"]
                match["adjusted_loser_change"] = match["opponent_adjusted_change"]
            else:
                match["adjusted_winner_change"] = match["opponent_adjusted_change"]
                match["adjusted_loser_change"] = match["player_adjusted_change"]

    def _update_pp_tracking(self, current_match: int, match: dict):
        """Update PP tracking after a match."""
        p1_name = match["player_id"]
        p2_name = match["opponent_id"]
        p1_obj = next((p for p in self.players if p.name == p1_name), None)
        p2_obj = next((p for p in self.players if p.name == p2_name), None)
        for player_name, player_obj in [(p1_name, p1_obj), (p2_name, p2_obj)]:
            if player_obj is None or player_name not in self.pending_pp:
                continue
            for trigger in self.pending_pp[player_name]:
                if (
                    not trigger.get("applied", False)
                    and current_match > trigger["match_number"]
                    and len(trigger["subsequent_afters"])
                    < RATING_CONFIG["max_matches_for_proven_potential"]
                ):
                    trigger["subsequent_afters"].append(player_obj.rating)
            # Check for finalization
            pending_triggers = [
                t for t in self.pending_pp[player_name] if not t.get("applied", False)
            ]
            if pending_triggers:
                max_end = max(t["tracking_end"] for t in pending_triggers)
                player_conf = self.rating_calculator.calculate_confidence(
                    player_obj.games_played
                )
                if current_match >= max_end and player_conf >= 1.0:
                    # LOGGING: Trace mid-simulation PP application
                    trigger_matches = [t["match_number"] for t in pending_triggers]
                    print(
                        f"MID-SIM APPLYING PP for {player_name} after match {current_match}: scaling for triggers {trigger_matches}"
                    )
                    # Track batch finalization BEFORE applying
                    before_rating = player_obj.rating
                    for trigger in pending_triggers:
                        self._finalize_pp_trigger(trigger, player_obj)
                    # Now sum after adjustments are calculated/applied
                    total_adj_new = sum(
                        trigger.get("adj_new", 0.0) for trigger in pending_triggers
                    )
                    after_rating = player_obj.rating  # Updated in finalizer
                    est_adjustments = {}
                    for trigger in pending_triggers:
                        est_name = trigger["est_player_name"]
                        adj_est = trigger.get("adj_est", 0.0)
                        if est_name not in est_adjustments:
                            est_adjustments[est_name] = 0.0
                        est_adjustments[est_name] += adj_est
                    self.pp_finalizations.append(
                        {
                            "new_player": player_name,
                            "before_rating": before_rating,
                            "total_adj_new": total_adj_new,
                            "after_rating": after_rating,
                            "applied_at_match": current_match,
                            "num_triggers": len(pending_triggers),
                            "trigger_matches": trigger_matches,
                            "est_adjustments": est_adjustments,
                        }
                    )
                    self.pending_pp[player_name] = [
                        t
                        for t in self.pending_pp[player_name]
                        if not t.get("applied", False)
                    ]
                    if not self.pending_pp[player_name]:
                        del self.pending_pp[player_name]

    def _finalize_pp_trigger(self, trigger: dict, new_player_obj):
        """Finalize and apply a single PP trigger adjustment."""
        all_new_afters = [trigger["initial_new_after"]] + trigger["subsequent_afters"]
        max_reached = (
            max(all_new_afters) if all_new_afters else trigger["initial_new_after"]
        )
        gap = abs(trigger["est_before"] - trigger["new_before"])
        if gap == 0:
            trigger["applied"] = True
            return
        threshold_increment = gap * RATING_CONFIG["proven_potential_gap_threshold"]
        initial_after = trigger["initial_new_after"]
        crossed_count = 0
        for i in range(1, 11):
            th = initial_after + i * threshold_increment
            if th <= max_reached:
                crossed_count += 1
            else:
                break
        closure_fraction = (
            crossed_count * RATING_CONFIG["proven_potential_gap_threshold"]
        )
        est_won = trigger["est_won"]
        if est_won:
            scaling = 1 + closure_fraction
        else:
            scaling = 1 - closure_fraction
        new_change = trigger["new_change"]
        est_change = trigger["est_change"]
        adj_new = (scaling - 1) * new_change
        adj_est = (scaling - 1) * est_change
        new_player_obj.rating += adj_new
        try:
            est_player_obj = next(
                p for p in self.players if p.name == trigger["est_player_name"]
            )
            est_player_obj.rating += adj_est
        except StopIteration:
            pass  # Est player no longer in simulation
        trigger["applied"] = True
        trigger["scaling"] = scaling
        trigger["closure_fraction"] = closure_fraction
        trigger["crossed_count"] = crossed_count
        trigger["adj_new"] = adj_new
        trigger["adj_est"] = adj_est
        # Update the corresponding match record
        for m in self.match_history:
            if (
                m["match_number"] == trigger["match_number"]
                and m["player_id"] == new_player_obj.name
                and m["opponent_id"] == trigger["est_player_name"]
            ):
                m["pp_applicable"] = True
                m["pp_scaling"] = scaling
                m["pp_crossed_thresholds"] = crossed_count
                m["player_adjusted_change"] = new_change * scaling
                m["opponent_adjusted_change"] = est_change * scaling
                m["adjusted_winner_change"] = (
                    new_change * scaling if new_change > 0 else est_change * scaling
                )
                m["adjusted_loser_change"] = (
                    est_change * scaling if new_change > 0 else new_change * scaling
                )
                break

    def _calculate_avg_variety_score(self) -> float:
        """Calculate the adjusted average variety entropy score across all players.

        This calculates the actual average entropy of all players' matchup diversity,
        then applies a difficulty multiplier to make variety bonuses easier or harder
        to achieve.

        Returns:
            Adjusted average variety entropy score (lower = easier bonuses)
        """
        if not self.players:
            return 2.0  # Fallback default

        total_entropy = 0.0
        player_count = 0

        for player in self.players:
            # Get this player's opponent counts
            opponent_counts = self._get_opponent_counts(player)

            if opponent_counts:
                # Calculate entropy for this player
                total_weight = sum(opponent_counts.values())
                player_entropy = 0.0

                for count in opponent_counts.values():
                    if count > 0:
                        p = count / total_weight
                        player_entropy -= p * math.log2(p)

                total_entropy += player_entropy
                player_count += 1

        if player_count == 0:
            return 2.0  # Fallback if no players have opponent data

        avg_entropy = total_entropy / player_count

        # Apply difficulty multiplier to make variety bonuses easier/harder to achieve
        # Lower multiplier = easier (players need less entropy for bonuses)
        difficulty_multiplier = VARIETY_CONFIG["variety_difficulty_multiplier"]
        adjusted_avg_entropy = avg_entropy * difficulty_multiplier

        return adjusted_avg_entropy

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
        from simulation_config import LADDER_RESET_CONFIG

        if not LADDER_RESET_CONFIG.get("late_joiners", {}).get("enabled", False):
            return

        late_joiner_config = LADDER_RESET_CONFIG["late_joiners"]
        late_joiner_percentage = late_joiner_config["late_joiner_percentage"]
        total_players = LADDER_RESET_CONFIG["num_players"]

        # Override num_late_joiners based on clamped initial count to respect disabling
        num_late_joiners = max(0, total_players - self.initial_player_count)

        if num_late_joiners == 0:
            print("No late joiners to generate (disabled due to small player count).")
            return

        curved_join = late_joiner_config.get("curved_join", False)

        if curved_join:
            total_matches = LADDER_RESET_CONFIG["num_matches"]
            buffer_end = (
                total_matches * 0.9
            )  # 10% buffer: all join by 90% of simulation
            lambda_val = (
                3.0 / buffer_end if buffer_end > 0 else 1.0
            )  # Mean join ~ buffer_end/3
            joiners = []
            for i in range(num_late_joiners):
                # Generate target rating using normal distribution
                mean_rating = 1700
                std_dev = 300
                target_rating = random.gauss(mean_rating, std_dev)
                target_rating = max(1000, min(2400, int(round(target_rating))))

                # Create late joiner player
                from scenarios.ladder_reset import Player

                late_joiner = Player(
                    name=f"LateJoiner_{i+1}",
                    rating=1000,  # Start at 1000 like everyone else
                    target_rating=target_rating,
                    games_played=0,  # Start with 0 games (low confidence)
                    activity_multiplier=1.0,
                )

                # Generate exponential join time
                u = random.random()
                join_time = -math.log(1 - u) / lambda_val if lambda_val > 0 else 0
                join_time = min(join_time, buffer_end)  # Cap at buffer end (90% point)

                joiners.append((late_joiner, join_time))
            # Sort by join_time for sequential addition
            self.late_joiners = sorted(joiners, key=lambda x: x[1])
        else:
            # Original linear generation
            for i in range(num_late_joiners):
                # Generate target rating using normal distribution
                mean_rating = 1700
                std_dev = 300
                target_rating = random.gauss(mean_rating, std_dev)
                target_rating = max(1000, min(2400, int(round(target_rating))))

                # Create late joiner player
                from scenarios.ladder_reset import Player

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
        from simulation_config import LADDER_RESET_CONFIG

        if not LADDER_RESET_CONFIG.get("late_joiners", {}).get("enabled", False):
            return

        late_joiner_config = LADDER_RESET_CONFIG["late_joiners"]
        curved_join = late_joiner_config.get("curved_join", False)

        if curved_join:
            while self.late_joiners and self.late_joiners[0][1] <= match_num:
                late_joiner, join_time = self.late_joiners.pop(0)
                self.players.append(late_joiner)
                print(
                    f"Curved late joiner {late_joiner.name} joined at match {match_num} (planned: {int(join_time)})"
                )
        else:
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
                    print(
                        f"Linear late joiner {late_joiner.name} joined at match {match_num}"
                    )

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
        from simulation_config import LADDER_RESET_CONFIG

        if not LADDER_RESET_CONFIG.get("late_joiners", {}).get("enabled", False):
            # If late joiners disabled, use full player count
            return LADDER_RESET_CONFIG["num_players"]

        late_joiner_config = LADDER_RESET_CONFIG["late_joiners"]
        late_joiner_percentage = late_joiner_config["late_joiner_percentage"]
        total_players = LADDER_RESET_CONFIG["num_players"]

        # Calculate initial players: total - late joiners, but ensure at least 2 starters
        naive_initial = int(total_players * (1 - late_joiner_percentage))
        initial_players = max(2, naive_initial)

        if initial_players >= total_players:
            # Can't have late joiners with small total; will disable in _generate_late_joiners
            print(
                f"Warning: Small total_players ({total_players}). Late joiners will be disabled to ensure at least 2 initial players."
            )

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
        self.initial_player_count = initial_players  # Store for late joiner calculation

        # Generate initial players
        self.players = self.scenario.generate_players(initial_players)

        # Generate late joiners if enabled
        self._generate_late_joiners()

        # Reset match history
        self.match_history = []

        # Run matches
        results = []
        for match_num in range(num_matches):
            # Progress output every 5000 matches
            if (match_num + 1) % 2500 == 0:
                print(f"Completed {match_num + 1}/{num_matches} matches")

            # Check if late joiners should join
            self._check_late_joiners(match_num)

            # Set the current match number for tracking
            self.current_match_number = match_num + 1

            match = self.simulate_match()
            results.append(match)
            self._update_pp_tracking(self.current_match_number, match)

        self.last_results = results  # Store the results
        # Finalize any remaining PP adjustments at simulation end
        self._finalize_pending_pp_at_end()
        self.post_process_proven_potential()
        print(f"Simulation completed: {num_matches}/{num_matches} matches")
        return results

    def _finalize_pending_pp_at_end(self):
        """Apply remaining PP adjustments for players who have reached max confidence at simulation end."""
        for player_name, triggers in list(self.pending_pp.items()):
            player_obj = next((p for p in self.players if p.name == player_name), None)
            if player_obj is None:
                continue
            player_conf = self.rating_calculator.calculate_confidence(
                player_obj.games_played
            )
            if player_conf >= 1.0:
                # LOGGING: Trace end-of-simulation PP application
                pending_triggers = [t for t in triggers if not t.get("applied", False)]
                trigger_matches = [t["match_number"] for t in pending_triggers]
                print(
                    f"END-OF-SIM APPLYING PP for {player_name}: {len(pending_triggers)} triggers at matches {trigger_matches}"
                )
                # Track batch finalization BEFORE applying
                before_rating = player_obj.rating
                for trigger in pending_triggers:
                    self._finalize_pp_trigger(trigger, player_obj)
                # Now sum after adjustments are calculated/applied
                total_adj_new = sum(
                    trigger.get("adj_new", 0.0) for trigger in pending_triggers
                )
                after_rating = player_obj.rating  # Updated
                est_adjustments = {}
                for trigger in pending_triggers:
                    est_name = trigger["est_player_name"]
                    adj_est = trigger.get("adj_est", 0.0)
                    if est_name not in est_adjustments:
                        est_adjustments[est_name] = 0.0
                    est_adjustments[est_name] += adj_est
                self.pp_finalizations.append(
                    {
                        "new_player": player_name,
                        "before_rating": before_rating,
                        "total_adj_new": total_adj_new,
                        "after_rating": after_rating,
                        "applied_at_match": "END",
                        "num_triggers": len(pending_triggers),
                        "trigger_matches": trigger_matches,
                        "est_adjustments": est_adjustments,
                    }
                )
                # Remove applied triggers
                self.pending_pp[player_name] = [
                    t for t in triggers if not t.get("applied", False)
                ]
                if not self.pending_pp[player_name]:
                    del self.pending_pp[player_name]

    def save_results(self, output_dir: str = "simulation_results") -> None:
        """Save simulation results.

        Args:
            output_dir: Directory to save output files
        """
        # Use the results from the last simulation run
        results = self.last_results

        # Save results based on scenario type
        if isinstance(self.scenario, LadderResetScenario):
            save_ladder_reset_results(
                self.players, results, self.pp_finalizations, output_dir
            )
        else:
            raise ValueError(f"Unknown scenario type: {type(self.scenario)}")


def main():
    """Run the simulation."""
    from scenarios.ladder_reset import LadderResetScenario

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
