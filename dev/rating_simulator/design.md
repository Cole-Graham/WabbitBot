### Rating Calculator Design

## Explanation of "Scrimmage" ranked system for games

Instead of match-making, Players/Teams will manually challenge each other to matches.
This creates several problems, some of which exist to some extent in match-making systems for
low population games, but are exacerbated in 

## Incentive problems

# Problem 1: Players could get their low Elo friends to feed them Elo for free (i.e. "Shadow-boxing up the ladder")

Solution: Higher elo players get no elo from playing opponent's below their rating range.
- Only applies to the higher Elo player
- Decrease Elo adjustments to 0 over a specified range below the higher rated player's Elo
- Formula: `gap_scaling = (1.0 + cos(π * normalized_gap * 0.7)) / 2.0`
- Example scaling values:
  - 0% of max rating gap: 1.0 (full weight)
  - 25%: 0.9
  - 50%: 0.75
  - 75%: 0.5
  - 100%: 0.0 (no weight)
- The range would be 20% of the total Elo range of the entire leaderboard population, so that
the range adjusts dynamically as the leaderboard develops over a ranked season.
- Only apply this decrease to the higher Elo player if the low Elo player has 1.0 confidence

# Problem 2: High Elo players would then have no incentive to accept challenges from low Elo players.

Example: A known skilled player on a new discord account, or who joined late in the leaderboard season.

Solution: "Proven Potential" tracker.
- A system which determines if a new player had "proven potential" until they reach 1.0 confidence,
and retroactively adjusts the rating loss for established players who accepted a match from them.
- Adjustments would be triggered as the "new" player gains Elo, using 10% breakpoints over a range
between both player's ratings at the start of the match. 
- Retroactive adjustments only apply to matches where the new player was below 1.0 confidence.
- Track and display the potential Elo adjustments for players who might have their rating retroactively adjusted.

# Problem 3: Players might form leaderboard "cliques", i.e groups of high elo players who only challenge each other.

Solution: Player "Variety" score
- Calculate a score for players that reflects the variety of opponents they accept challenges from,
relative to other players.
- Only matches with opponents greater than or equal to the player's Elo will have a weight of 1.0 in calculations
used to increase the player's variety score.
- Use cosine scaling to decrease this weighting the lower the opponent's elo is, reaching 0 at a specified max gap.
  The formula is: `weight = (1 + cos(π * normalized_gap * 0.7)) / 2`
  where normalized_gap is the rating difference divided by the max gap.
- The max rating gap should be equal to 40% of the total Elo range of the entire leaderboard population,
divided by 2.
- Formula: `variety_gap = (max_rating - min_rating) * 0.40 / 2`

## Variety Bonus Calculation Details

The variety bonus system is designed to reward players who maintain a diverse set of opponents while
discouraging "shadow-boxing" with friends. The bonus is a multiplier on the Elo they gain from matches
they win, but does not reduce the Elo they lose for losses. 

The bonus calculation takes into account several factors:

1. **Entropy Difference**: 
   - Measures how much more diverse a player's opponent pool is compared to the average player
   - Uses Shannon entropy to measure the diversity of a player's opponent pool
   - Calculates weighted entropy for each player based on their opponent distribution
   - Higher entropy means more diverse opponent selection
   - Only opponents within MAX_GAP_PERCENT (20%) of the player's rating count towards the variety score
   - This ensures players are rewarded for maintaining variety within their skill range
   - Formula: `playerVarietyEntropy = -Σ(weight * log2(weight))` for each opponent
   - Formula: `entropy_difference = player_entropy - average_entropy`
   - Relative difference: `relative_diff = entropy_difference / (average_entropy == 0 ? 1 : average_entropy)`

2. **Games Played Scaling**:
   - The statistical significance of a player's variety score increases with more games played
   - The bonus is scaled based on how many games the player has played relative to the median
   - Players start with a minimum scaling factor of 0.5
   - The scaling uses quadratic growth to:
     - Start slowly, requiring more games to get initial increases
     - Accelerate as they approach the median number of games
     - Reach full scaling (1.0) at or above the median
   - Formulas: 
        `games_played_ratio = min(player_games / median_games, 1.0)`
        `scaling_factor = 0.5 + (0.5 * games_played_ratio * games_played_ratio)`
   - This rewards players who play enough to statistically prove they can perform
   across a wide range of opponents, while still giving some bonus to casual players.

3. **Bonus Clamping**:
   - The final bonus is clamped between MIN_VARIETY_BONUS and MAX_VARIETY_BONUS
   - This prevents the bonus from becoming too large or negative
   - Typical values are around 0.2 for max bonus and -0.1 for min bonus
   - Formula: `final_bonus = clamp(relative_diff * scaling_factor * MAX_VARIETY_BONUS, MIN_VARIETY_BONUS, MAX_VARIETY_BONUS)`

The formula effectively rewards players who:
- Play against a diverse set of opponents
- Have played enough games to establish a meaningful pattern

### Weighted Entropy Explanation
The weighted entropy calculation uses Shannon entropy to measure how evenly distributed a player's opponents are:
- For each opponent, we calculate a weight based on how many times we've played them
- The entropy formula `-Σ(p * log2(p))` measures the diversity of this distribution
- Higher entropy means more diverse/even distribution of opponents
- Lower entropy means less diverse/more concentrated distribution
- Example:
  - Playing 10 different opponents once each: high entropy (diverse)
  - Playing the same opponent 10 times: low entropy (not diverse)
- The weights are adjusted based on rating gaps to encourage playing against a variety of skill levels
- For lower-rated opponents, weights use cosine scaling:
  ```
  normalized_gap = (player_rating - opponent_rating) / variety_gap
  weight = (1.0 + cos(π * normalized_gap * 0.7)) / 2.0
  ```
- Example scaling values:
  - 0% of max gap: 1.0 (full weight)
  - 25%: 0.9
  - 50%: 0.75
  - 75%: 0.5
  - 100%: 0.0 (no weight)

Example calculation:
- Leaderboard range: 2047 - 1371 = 676 ELO
- Variety gap: 676 * 0.4 / 2 = 135 ELO
- For a 1800 ELO player vs 1700 ELO opponent:
  - gap = 100 ELO
  - normalized_gap = 100/135 ≈ 0.74
  - weight = `(1.0 + cos(π * normalized_gap * 0.7)) / 2.0 ≈ 0.52`

## Confidence System

The confidence system is designed to give new players larger rating changes until they've played enough games to establish their true skill level.
It is also used a control for the proven potential tracker.

1. **Confidence Calculation**:
   - Confidence increases linearly with games played
   - Reaches 1.0 at 20 games played
   - Stays at 1.0 for the rest of the season
   - Used to scale rating changes (higher confidence = smaller changes)
   - Formula: `confidence = min(games_played / 20, 1.0)`

2. **Rating Change Scaling**:
   - For new players (confidence = 0), multiplier is 2.0
   - For experienced players (confidence = 1.0), multiplier is 1.0
   - Linear interpolation between these values
   - Formula: `confidence_multiplier = 2.0 - confidence`


# Example output from simulator

# Rating Simulation Results

## Configuration

Player Target ELO: 1900
Player Win Probability: 0.95
Use Elo Scaling: True
Use Target ELO for Win Prob: True
Number of Matches: 30
Number of Opponents: 18
Starting Rating: 1500
Base Rating Change: 16.0
ELO Divisor: 400.0

## Match History

| Match | Player        | Opponent      | Result | Player Change | Opponent Change | Win Prob | Confidence | Variety Bonus | Final Mult | Player PP Adj | Opponent PP Adj |
|-------|--------------|---------------|--------|--------------|----------------|----------|------------|---------------|------------|--------------|----------------|
|       | Rating       | Rating        |        |              |                |          |            |               |            |              |                |
|-------|--------------|---------------|--------|--------------|----------------|----------|------------|---------------|------------|--------------|----------------|
| 1     | 1500->1496   | 1816->1820   | L      | -4           | +4             | 0.59     | 0.00       | 0.20          | 2.00        | 0.0         | 0.0            |
| 2     | 1496->1492   | 1806->1810   | L      | -4           | +4             | 0.61     | 0.14       | 0.20          | 2.00        | 0.0         | 0.0            |
| 3     | 1492->1520   | 1870->1842   | W      | +28           | -28           | 0.53     | 0.26       | 0.20          | 2.00        | -26.0       | +26.0           |
| 4     | 1520->1544   | 1745->1721   | W      | +24           | -24           | 0.67     | 0.36       | 0.20          | 1.97        | -19.0       | +19.0           |
| 5     | 1544->1566   | 1750->1728   | W      | +22           | -22           | 0.67     | 0.45       | 0.20          | 1.86        | -17.0       | +17.0           |
| 6     | 1566->1586   | 1740->1720   | W      | +20           | -20           | 0.68     | 0.53       | 0.20          | 1.77        | -14.0       | +14.0           |
| 7     | 1586->1605   | 1752->1733   | W      | +19           | -19           | 0.66     | 0.59       | 0.20          | 1.69        | -13.0       | +13.0           |
| 8     | 1605->1601   | 1881->1885   | L      | -4           | +4             | 0.52     | 0.65       | 0.20          | 1.62        | +1.0         | -1.0           |
| 9     | 1601->1618   | 1733->1716   | W      | +17           | -17           | 0.68     | 0.70       | 0.20          | 1.56        | -11.0       | +11.0           |
| 10    | 1618->1640   | 2020->1998   | W      | +22           | -22           | 0.39     | 0.74       | 0.20          | 1.51        | -21.0       | +21.0           |
| 11    | 1640->1634   | 1820->1826   | L      | -6           | +6             | 0.58     | 0.78       | 0.20          | 1.47        | +1.0         | -1.0           |
| 12    | 1634->1650   | 1796->1780   | W      | +16           | -16           | 0.60     | 0.81       | 0.20          | 1.43        | -11.0       | +11.0           |
| 13    | 1650->1664   | 1946->1932   | W      | +14           | -14           | 0.46     | 0.83       | -0.07         | 1.08        | -12.0       | +12.0           |
| 14    | 1664->1674   | 1726->1716   | W      | +10           | -10           | 0.66     | 0.86       | -0.06         | 1.08        | -3.0        | +3.0            |
| 15    | 1674->1686   | 1899->1887   | W      | +12           | -12           | 0.50     | 0.88       | -0.08         | 1.03        | -9.0        | +9.0            |
| 16    | 1686->1694   | 1719->1711   | W      | +8            | -8            | 0.66     | 0.89       | -0.08         | 1.02        | 0.0         | 0.0            |
| 17    | 1694->1705   | 1843->1832   | W      | +11           | -11           | 0.55     | 0.91       | -0.08         | 1.00        | -6.0        | +6.0            |
| 18    | 1705->1715   | 1823->1813   | W      | +10           | -10           | 0.56     | 0.92       | -0.08         | 0.99        | -5.0        | +5.0            |
| 19    | 1715->1726   | 1882->1871   | W      | +11           | -11           | 0.51     | 0.93       | -0.06         | 1.00        | -7.0        | +7.0            |
| 20    | 1726->1737   | 1906->1895   | W      | +11           | -11           | 0.49     | 0.94       | -0.09         | 0.96        | -7.0        | +7.0            |
| 21    | 1737->1734   | 1924->1927   | L      | -3           | +3             | 0.48     | 1.00       | -0.09         | 0.91        | -1.0        | +1.0            |
| 22    | 1734->1730   | 1880->1884   | L      | -4           | +4             | 0.51     | 1.00       | -0.10         | 0.90        | 0.0         | 0.0            |
| 23    | 1730->1738   | 1770->1762   | W      | +8            | -8            | 0.60     | 1.00       | -0.10         | 0.90        | -1.0        | +1.0            |
| 24    | 1738->1749   | 1930->1919   | W      | +11           | -11           | 0.48     | 1.00       | -0.08         | 0.92        | -8.0        | +8.0            |
| 25    | 1749->1755   | 1732->1726   | W      | +6            | -6            | 0.62     | 1.00       | -0.07         | 0.93        | +2.0         | -2.0           |
| 26    | 1755->1752   | 1982->1985   | L      | -3           | +3             | 0.45     | 1.00       | -0.07         | 0.93        | 0.0         | 0.0            |
| 27    | 1752->1747   | 1831->1836   | L      | -5           | +5             | 0.55     | 1.00       | -0.09         | 0.91        | 0.0         | 0.0            |
| 28    | 1747->1757   | 1928->1918   | W      | +10           | -10           | 0.48     | 1.00       | -0.09         | 0.91        | 0.0         | 0.0            |
| 29    | 1757->1752   | 1836->1841   | L      | -5           | +5             | 0.54     | 1.00       | -0.09         | 0.91        | 0.0         | 0.0            |
| 30    | 1752->1759   | 1767->1760   | W      | +7            | -7            | 0.59     | 1.00       | -0.09         | 0.91        | 0.0         | 0.0            |