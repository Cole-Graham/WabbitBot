# gap scaling

- This is meant to prevent people from shadow-boxing, by lowering the ELO gains from higher rated players
- It doesn't apply when the lower ELO player is a new player (confidence < 1.0 ),
  to keep some incentive to play new players still.
- Possibly too punishing for the higher rated player when playing opponents in their rating range
    #### Match 26392
    | Category                 | Details                    |
    | ------------------------ | -------------------------- |
    | Challenger               | Player_272 (1852)          |
    | Opponent                 | Player_232 (1791)          |
    | Result                   | Loss                       |
    | Challenger Rating        | 1700.9 -> 1683.8           |
    | Opponent Rating          | 1577.6 -> 1609.7           |
    | Win Probability          | 58.7%                      |
    | Challenger Confidence    | 1.00                       |
    | Opponent Confidence      | 1.00                       |
    | Challenger Variety Bonus | 0.20                       |
    | Opponent Variety Bonus   | 0.20                       |
    | Rating Changes           | Winner: 32.2, Loser: -17.1 |
    | Rating Changes after PP  | Winner: 32.2, Loser: -17.1 |
    | Multipliers              | Winner: 1.20, Loser: 0.80  |

-Also creates ELO inflation, although thats not necessarily a deal-breaker.

# Tail-distribution adjustments to variety score
- Players at rating extremes naturally have lower variety scores because there are fewer players at
  their skill level. The scaling gives them up to 2x bonus for good variety and 50% reduction in penalties 
  for poor variety, ensuring fair rating adjustments regardless of position in the distribution.

- Problem: Currently just an arbitrary/clumsy adjustment below 10th and above 90th percentile
- Solution: Normalize variety score by number of players in the player's rating range.
   - Also added config option for fine-tuning difficulty of maintaining variety score
   - TODO: The real system might will also need to normalize for recently active players
           the players rating range.
    ```py
    # Variety Bonus Parameters
    VARIETY_CONFIG = {
        # Multiplier for variety score difficulty (lower = easier to get bonuses)
        # 1.0 = use actual average, 0.75 = 25% easier (need 25% less entropy)
        "variety_difficulty_multiplier": 0.75,
    }

    """
    Applies continuous scaling based on opponent availability:
    - Uses same rating range calculation as gap scaling (40% of total range / 2)
    - Counts potential opponents within neighbor range
    - Compares to player with maximum neighbors in their range
    - Scales bonuses/penalties based on neighbor ratio (1.0 to 1.5x)
    """
    # Apply continuous scaling based on opponent availability
    # Use the same rating range calculation as gap scaling (40% of total range / 2)
    rating_range = (
        max(o.rating for o in opponents) - min(o.rating for o in opponents)
        if opponents
        else 0
    )
    neighbor_range = (
        rating_range * 0.4 / 2
    )  # Same formula as max_gap in gap scaling
    # Find this player's opponent count within neighbor range
    player_neighbors = sum(
        1 for opp in opponents if abs(opp.rating - player_rating) <= neighbor_range
    )
    # Find maximum neighbors any player has
    max_neighbors = (
        max(
            sum(
                1
                for other in opponents
                if abs(other.rating - opp.rating) <= neighbor_range
            )
            for opp in opponents
        )
        if opponents
        else 1
    )
    # Calculate scaling factor based on neighbor ratio
    if max_neighbors > 0:
        neighbor_ratio = player_neighbors / max_neighbors
        # Scale variety bonus based on opponent availability
        # Players with more neighbors get standard bonuses
        # Players with fewer neighbors get amplified bonuses/penalties
        availability_factor = 1.0 + (1.0 - neighbor_ratio) * 0.5  # 1.0 to 1.5 range
        base_bonus *= availability_factor
    ```

- Problem: Simulator was using an arbitrary value of 2.0 for the average variety score to calculate the bonus
  ```py
  simulated_avg_variety_score = (
      2.0  # Default value, could be calculated from all players
  )
  ````
- Solution: Calculate the actual average variety scores in simulation `simulated_avg_variety_score = self._calculate_avg_variety_score()`