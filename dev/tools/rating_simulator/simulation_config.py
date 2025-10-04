# ============================================================================
# Simulation Configuration
# ============================================================================

# Simulation Parameters
SIMULATION_CONFIG = {
    # Number of matches to simulate
    "num_matches": 30,
    # Number of opponents to generate
    "num_opponents": 18,
    # Output directory for results
    "output_dir": "simulation_results",
    # Scenario to use (None for default behavior)
    "scenario": "ladder_reset",
}

# Opponent Generation Parameters
OPPONENT_CONFIG = {
    # Minimum rating for generated opponents
    "min_opponent_rating": 1700,
    # Maximum rating for generated opponents
    "max_opponent_rating": 2100,
    # Percentage of opponents at the minimum rating tail
    "min_tail_distribution": 0.15,
    # Percentage of opponents at the maximum rating tail
    "max_tail_distribution": 0.10,
    # ELO rating where player has maximum bias for accepting matches
    "player_bias_elo": 1800,
    # Initial games played range for opponents
    "initial_games_range": (20, 50),
    # Initial recent games range for opponents
    "initial_recent_games_range": (5, 15),
}

# Match Simulation Parameters
MATCH_CONFIG = {
    # Max win probability for the player
    "player_win_probability": 0.95,
    # Target ELO rating for the player
    "player_target_elo": 1900,
    # Whether to decrease win probability as player approaches target ELO
    "use_elo_scaling": True,
    # Whether to use target ELO instead of current ELO for win probability calculations
    "use_target_elo_for_win_prob": True,
}

# Variety Bonus Parameters
VARIETY_CONFIG = {
    # Multiplier for variety score difficulty (lower = easier to get bonuses)
    # 1.0 = use actual average, 0.75 = 25% easier (need 25% less entropy)
    "variety_difficulty_multiplier": 0.75,
}

# Ladder Reset Scenario Parameters
LADDER_RESET_CONFIG = {
    # Number of players in the ladder
    "num_players": 100,
    # Number of matches to simulate
    "num_matches": 4400,
    # Target rating distribution (rating, percentage)
    "target_rating_distribution": [
        (1300, 0.1),  # 10% of players target 1300
        (1500, 0.3),  # 30% of players target 1500
        (1700, 0.3),  # 30% of players target 1700
        (1900, 0.2),  # 20% of players target 1900
        (2100, 0.1),  # 10% of players target 2100
    ],
    # Whether to focus on current ratings early in the simulation
    "focus_on_current_ratings": False,
    # Late joiners configuration for proven potential testing
    "late_joiners": {
        "enabled": True,
        "late_joiner_percentage": 0.50,  # 50% of total players join later
        "join_after_matches": 25,  # Join after this many matches
        "join_interval": 25,  # Join every N matches
        "curved_join": True,  # Use curved (exponential decay) distribution for join times, most join early and taper off
    },
    # Minimum games played for established player (conf >=1.0) for a match to be considered for PP adjustments
    # Invalidates PP if fewer than this at match time
    "min_established_games_for_pp": 40,
    # Catch-up bonus configuration
    "catch_up_bonus_config": {
        "enabled": True,  # Enable catch-up bonus for low-rated players
        "apply_to_loser": False,  # Apply to losers below target (if enabled)
        "target_rating": 1700,  # Intended average rating for convergence
        "convergence_threshold": 50,  # Minimum distance below target to trigger bonus
        "max_bonus": 1.0,  # Maximum multiplier from bonus (additive to other multipliers)
    },
}
