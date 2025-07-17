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

# Ladder Reset Scenario Parameters
LADDER_RESET_CONFIG = {
    # Number of players in the ladder
    "num_players": 100,
    # Number of matches to simulate
    "num_matches": 32000,
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
}
