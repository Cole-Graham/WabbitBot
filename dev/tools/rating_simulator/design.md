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
- Retroactive adjustments only apply to matches with "new players", i.e. players below 1.0 confidence.
- Track and display the potential Elo adjustments for players who might have their rating retroactively adjusted.
- `NEW:` To avoid cascade effects of one PP adjustment effecting another, PP adjuments are applied all at once
  after the new player has reached 1.0 confidence
- Eligible matches being tracked for PP adjustments have a limited number of matches for which they can be
  tracked. Currently its set to track PP adjustments for `16 matches` (played by the New Player).

- Hypothetical example:
    1. On a new players 3rd match they play an established player that triggers tracking for PP adjustments
    2. On their 10th match, they play another established player triggering tracking for that match as well. 
       Now the the PP adjustments from their 3rd match can't be applied until PP adjustments are finalized for 
       their 10th match. 
    3. They reach 1.0 confidence on their 20th match, having not played any other established players since their 
       10th match. Now that they're 1.0 confidence, their matches are no longer eligible for PP adjustments.
    4. On completing their 26th match (10 + 16), the PP adjustments are finalized for the 3rd and 10th match,
       and finally applied. 

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

- Difficulty of maintaining variety score can be adjusted by a final `variety_difficulty` modifier.

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


# Example output from simulator (2025-10-04)

# Ladder Reset Simulation Results

Simulates a ladder reset where all players start at 1000 rating. Players have target ratings they will tend towards over time, with matchmaking favoring closer matches and players who have played fewer games.

## Overall Statistics

| Metric                    |  Value |
| ------------------------- | ------ |
| Total Players             |  100.0 |
| Total Matches             | 4400.0 |
| Average Final Rating      | 1540.8 |
| Median Final Rating       | 1555.0 |
| Rating Standard Deviation |  188.6 |
| Average Rating Change     |   40.8 |
| Median Rating Change      |   55.0 |

## Match Statistics

| Metric                 | Value |
| ---------------------- | ----- |
| Average Matches Played |  88.0 |
| Median Matches Played  |  78.0 |
| Maximum Matches Played | 191.0 |
| Minimum Matches Played |   6.0 |

## Target Rating Achievement

| Target Rating | Players | Achieved | % Achieved | Avg Final Rating |
| ------------- | ------- | -------- | ---------- | ---------------- |
| 1100-1199     |       6 |        6 |     100.0% |           1284.2 |
| 1200-1299     |       6 |        4 |      66.7% |           1294.5 |
| 1300-1399     |       8 |        4 |      50.0% |           1332.5 |
| 1400-1499     |       7 |        3 |      42.9% |           1405.1 |
| 1500-1599     |      10 |        3 |      30.0% |           1517.4 |
| 1600-1699     |      10 |        0 |       0.0% |           1560.4 |
| 1700-1799     |      12 |        0 |       0.0% |           1552.6 |
| 1800-1899     |       7 |        0 |       0.0% |           1562.3 |
| 1900-1999     |       9 |        0 |       0.0% |           1571.6 |
| 2000-2099     |      12 |        0 |       0.0% |           1665.9 |
| 2100-2199     |       6 |        0 |       0.0% |           1756.7 |
| 2200-2299     |       6 |        0 |       0.0% |           1850.1 |
| 2300-2399     |       1 |        0 |       0.0% |           1993.1 |

## Rating Distribution

| Rating Range | Players | % of Total |
| ------------ | ------- | ---------- |
| 0-1400       |      23 |      23.0% |
| 1400-1600    |      40 |      40.0% |
| 1600-1800    |      27 |      27.0% |
| 1800-2000    |      10 |      10.0% |
| 2000-∞       |       0 |       0.0% |

## Win Rate Analysis

| Rating Range | Avg Win Rate | Games per Player |
| ------------ | ------------ | ---------------- |
| 0-1400       |        39.4% |             40.8 |
| 1400-1600    |        47.7% |             83.4 |
| 1600-1800    |        50.7% |            106.2 |
| 1800-2000    |        61.4% |            165.7 |

## Top 100 Players

| Rank | Player        | Final Rating | Target Rating | Win Rate | Games Played | Variety Bonus |
| ---- | ------------- | ------------ | ------------- | -------- | ------------ | ------------- |
| 1    | Player_41     |       1993.1 |          2319 |    69.9% |          166 |          0.07 |
| 2    | Player_15     |       1907.1 |          2151 |    55.9% |          179 |          0.11 |
| 3    | Player_3      |       1885.9 |          2273 |    61.8% |          191 |          0.08 |
| 4    | Player_25     |       1868.0 |          2277 |    63.1% |          160 |          0.07 |
| 5    | Player_4      |       1864.0 |          2212 |    57.7% |          163 |          0.09 |
| 6    | Player_2      |       1862.2 |          2270 |    65.4% |          162 |          0.08 |
| 7    | Player_32     |       1835.7 |          2046 |    56.0% |          168 |          0.11 |
| 8    | Player_5      |       1822.6 |          2277 |    68.4% |          155 |          0.06 |
| 9    | Player_26     |       1814.7 |          2060 |    56.7% |          157 |          0.10 |
| 10   | Player_38     |       1810.7 |          2169 |    59.0% |          156 |          0.09 |
| 11   | Player_1      |       1797.8 |          2241 |    62.2% |          180 |          0.07 |
| 12   | LateJoiner_12 |       1766.3 |          2164 |    57.6% |           99 |          0.08 |
| 13   | Player_49     |       1739.5 |          2004 |    54.8% |          155 |          0.10 |
| 14   | LateJoiner_1  |       1725.4 |          2064 |    56.8% |           74 |          0.06 |
| 15   | Player_11     |       1717.9 |          1963 |    55.3% |          114 |          0.11 |
| 16   | Player_34     |       1717.8 |          1942 |    47.8% |          138 |          0.11 |
| 17   | LateJoiner_38 |       1717.0 |          2102 |    49.2% |           65 |          0.06 |
| 18   | LateJoiner_34 |       1712.9 |          2037 |    50.5% |           99 |          0.07 |
| 19   | LateJoiner_45 |       1696.5 |          2063 |    55.3% |           76 |          0.08 |
| 20   | Player_19     |       1694.5 |          1799 |    51.2% |          121 |          0.10 |
| 21   | LateJoiner_40 |       1685.1 |          2048 |    47.1% |          102 |          0.08 |
| 22   | Player_42     |       1678.2 |          2152 |    51.5% |          169 |          0.08 |
| 23   | LateJoiner_24 |       1661.0 |          2150 |    49.2% |           65 |          0.04 |
| 24   | LateJoiner_41 |       1656.0 |          2038 |    47.3% |           55 |          0.05 |
| 25   | LateJoiner_11 |       1652.0 |          2003 |    53.3% |          105 |          0.09 |
| 26   | Player_23     |       1631.8 |          1712 |    38.8% |          134 |          0.11 |
| 27   | LateJoiner_30 |       1624.6 |          1644 |    54.1% |           85 |          0.10 |
| 28   | LateJoiner_31 |       1620.0 |          1806 |    52.8% |           72 |          0.09 |
| 29   | Player_40     |       1619.1 |          1994 |    44.7% |          150 |          0.10 |
| 30   | Player_14     |       1618.7 |          1726 |    48.7% |          156 |          0.11 |
| 31   | Player_29     |       1618.4 |          1832 |    48.2% |          141 |          0.11 |
| 32   | LateJoiner_4  |       1616.6 |          1861 |    45.3% |          106 |          0.09 |
| 33   | LateJoiner_32 |       1616.1 |          1761 |    46.1% |           76 |          0.08 |
| 34   | LateJoiner_37 |       1612.5 |          1955 |    46.4% |          112 |          0.09 |
| 35   | LateJoiner_44 |       1609.9 |          1793 |    48.4% |           91 |          0.09 |
| 36   | LateJoiner_6  |       1602.2 |          1944 |    56.7% |           30 |          0.03 |
| 37   | Player_31     |       1600.8 |          1625 |    51.0% |           98 |          0.09 |
| 38   | LateJoiner_25 |       1598.3 |          1965 |    54.9% |           82 |          0.09 |
| 39   | Player_27     |       1591.7 |          1604 |    46.6% |          103 |          0.09 |
| 40   | LateJoiner_16 |       1591.1 |          1799 |    51.5% |           68 |          0.07 |
| 41   | Player_12     |       1584.8 |          1506 |    48.4% |           91 |          0.09 |
| 42   | Player_44     |       1584.3 |          2006 |    46.4% |          140 |          0.10 |
| 43   | Player_16     |       1582.7 |          1603 |    52.9% |          102 |          0.10 |
| 44   | Player_45     |       1581.8 |          1836 |    51.8% |          137 |          0.10 |
| 45   | Player_43     |       1581.5 |          1740 |    46.5% |           99 |          0.09 |
| 46   | Player_13     |       1579.7 |          1930 |    51.7% |          145 |          0.10 |
| 47   | LateJoiner_47 |       1572.3 |          1429 |    50.0% |           72 |          0.07 |
| 48   | Player_20     |       1557.8 |          1679 |    52.8% |          123 |          0.10 |
| 49   | LateJoiner_36 |       1556.4 |          1594 |    46.4% |           69 |          0.08 |
| 50   | LateJoiner_26 |       1556.0 |          1554 |    54.5% |           44 |          0.04 |
| 51   | Player_22     |       1554.0 |          1783 |    46.8% |           94 |          0.09 |
| 52   | LateJoiner_10 |       1552.6 |          1746 |    56.7% |           67 |          0.08 |
| 53   | Player_21     |       1551.7 |          1800 |    45.7% |          127 |          0.11 |
| 54   | Player_28     |       1549.5 |          1837 |    48.4% |          126 |          0.11 |
| 55   | LateJoiner_7  |       1536.6 |          1509 |    52.7% |           55 |          0.06 |
| 56   | LateJoiner_46 |       1536.2 |          1600 |    45.7% |           46 |          0.04 |
| 57   | Player_47     |       1533.9 |          1596 |    47.1% |          102 |          0.10 |
| 58   | LateJoiner_21 |       1533.1 |          1601 |    36.9% |           84 |          0.10 |
| 59   | LateJoiner_48 |       1528.5 |          1689 |    36.4% |           77 |          0.08 |
| 60   | Player_35     |       1527.8 |          1698 |    41.9% |          124 |          0.11 |
| 61   | LateJoiner_50 |       1526.2 |          2018 |    46.2% |           26 |          0.02 |
| 62   | Player_39     |       1523.1 |          1406 |    48.8% |           84 |          0.09 |
| 63   | Player_24     |       1521.1 |          1623 |    45.5% |          132 |          0.10 |
| 64   | LateJoiner_28 |       1518.9 |          1763 |    45.5% |           77 |          0.09 |
| 65   | LateJoiner_43 |       1517.7 |          1524 |    42.4% |           66 |          0.06 |
| 66   | LateJoiner_29 |       1517.4 |          1551 |    44.2% |           77 |          0.08 |
| 67   | LateJoiner_20 |       1507.9 |          1572 |    46.5% |           71 |          0.07 |
| 68   | Player_46     |       1498.8 |          1563 |    51.3% |          113 |          0.10 |
| 69   | LateJoiner_19 |       1497.5 |          1714 |    51.5% |           33 |          0.03 |
| 70   | LateJoiner_5  |       1494.3 |          1961 |    50.0% |           38 |          0.04 |
| 71   | Player_30     |       1482.0 |          1470 |    43.6% |           78 |          0.08 |
| 72   | LateJoiner_2  |       1470.4 |          1326 |    52.9% |           51 |          0.04 |
| 73   | Player_33     |       1459.9 |          1466 |    39.7% |           78 |          0.09 |
| 74   | LateJoiner_17 |       1432.3 |          1271 |    45.0% |           60 |          0.06 |
| 75   | LateJoiner_35 |       1432.1 |          1451 |    40.0% |           80 |          0.11 |
| 76   | LateJoiner_14 |       1412.2 |          1314 |    52.4% |           42 |          0.04 |
| 77   | Player_9      |       1401.0 |          1170 |    50.9% |           53 |          0.03 |
| 78   | LateJoiner_22 |       1398.4 |          1881 |    38.5% |           13 |         -0.01 |
| 79   | Player_48     |       1385.4 |          1318 |    46.6% |           58 |          0.05 |
| 80   | Player_18     |       1370.1 |          1348 |    43.6% |           78 |          0.09 |
| 81   | Player_17     |       1364.3 |          1543 |    36.6% |           71 |          0.07 |
| 82   | LateJoiner_3  |       1362.0 |          2000 |    44.4% |            9 |         -0.02 |
| 83   | Player_7      |       1331.8 |          1112 |    22.4% |           58 |          0.03 |
| 84   | Player_50     |       1323.9 |          1362 |    30.0% |           90 |          0.09 |
| 85   | LateJoiner_15 |       1320.2 |          1338 |    40.4% |           47 |          0.06 |
| 86   | LateJoiner_49 |       1310.4 |          1221 |    47.4% |           19 |          0.00 |
| 87   | Player_37     |       1304.8 |          1268 |    32.4% |           71 |          0.08 |
| 88   | LateJoiner_39 |       1298.3 |          1291 |    37.0% |           46 |          0.06 |
| 89   | LateJoiner_13 |       1297.6 |          1498 |    57.1% |            7 |         -0.05 |
| 90   | Player_8      |       1282.1 |          1121 |    31.6% |           57 |          0.04 |
| 91   | LateJoiner_8  |       1275.4 |          1167 |    43.5% |           23 |         -0.01 |
| 92   | LateJoiner_42 |       1258.3 |          1398 |    50.0% |            6 |         -0.05 |
| 93   | Player_6      |       1239.0 |          1198 |    33.3% |           54 |          0.04 |
| 94   | LateJoiner_9  |       1230.9 |          1262 |    28.6% |           42 |          0.03 |
| 95   | LateJoiner_27 |       1203.1 |          1998 |    52.2% |           23 |          0.01 |
| 96   | Player_36     |       1190.2 |          1202 |    34.5% |           58 |          0.04 |
| 97   | Player_10     |       1175.8 |          1129 |    36.0% |           50 |          0.02 |
| 98   | LateJoiner_33 |       1164.8 |          1705 |    52.2% |           23 |          0.02 |
| 99   | LateJoiner_23 |       1119.8 |          1318 |    18.8% |           16 |          0.01 |
| 100  | LateJoiner_18 |       1068.4 |          1436 |    50.0% |           20 |          0.01 |

## Proven Potential Statistics

| Metric                             | Value |
| ---------------------------------- | ----- |
| Total Proven Potential Adjustments |     0 |
| Matches with PP Adjustments        |     0 |
| PP Match Percentage                |  0.0% |
| Avg Adjustments per PP Match       |   0.0 |
| Players with PP Adjustments        |     0 |

## 10 Largest Batched Proven Potential Adjustments for New Players

These show the total PP adjustments applied in batches (sum across multiple triggers for the same new player, applied all at once after final tracking). Includes rating before/after the entire batch at application time.

Top 10 largest batched PP adjustments:

### #1: New Player LateJoiner_45 (Batch Applied at Match 2103, Total Impact: 498.7)

**New Player Total Batch Adjustment:** Δ-498.7 (from 18 triggers at matches [1369, 1479, 1488, 1516, 1521, 1536, 1589, 1702, 1753, 1845, 1876, 1912, 1921, 1966, 1973, 1975, 2003, 2065])
**New Player Ratings:** Before Batch: 1538.5 → After Batch: 1039.8 (Δ-498.7)
**Established Players Total Adjustments:**
- Player_2 (True Skill: 2270): Δ+1.1
- Player_38 (True Skill: 2169): Δ+3.7
- Player_49 (True Skill: 2004): Δ+26.3
- Player_41 (True Skill: 2319): Δ+1.6
- Player_34 (True Skill: 1942): Δ+5.4
- Player_21 (True Skill: 1800): Δ+29.0
- Player_11 (True Skill: 1963): Δ+50.3
- Player_25 (True Skill: 2277): Δ+9.8
- Player_3 (True Skill: 2273): Δ+15.8
- LateJoiner_40 (True Skill: 2048): Δ+23.1
- Player_14 (True Skill: 1726): Δ+18.1
- Player_5 (True Skill: 2277): Δ+3.0
- Player_26 (True Skill: 2060): Δ+18.9
- Player_1 (True Skill: 2241): Δ+19.2
- Player_13 (True Skill: 1930): Δ+2.5
**New Player True Skill:** 2063

### #2: New Player LateJoiner_5 (Batch Applied at Match 3249, Total Impact: 488.3)

**New Player Total Batch Adjustment:** Δ-488.3 (from 18 triggers at matches [2304, 2362, 2473, 2501, 2522, 2626, 2639, 2648, 2655, 2682, 2788, 2823, 2874, 3025, 3060, 3065, 3155, 3231])
**New Player Ratings:** Before Batch: 1538.1 → After Batch: 1049.8 (Δ-488.3)
**Established Players Total Adjustments:**
- Player_31 (True Skill: 1625): Δ+33.8
- Player_4 (True Skill: 2212): Δ+23.6
- Player_2 (True Skill: 2270): Δ+4.8
- Player_20 (True Skill: 1679): Δ+26.8
- Player_40 (True Skill: 1994): Δ+18.3
- LateJoiner_21 (True Skill: 1601): Δ+8.8
- Player_5 (True Skill: 2277): Δ+7.7
- Player_25 (True Skill: 2277): Δ+3.6
- Player_43 (True Skill: 1740): Δ+16.4
- Player_28 (True Skill: 1837): Δ+19.3
- Player_11 (True Skill: 1963): Δ+19.2
- Player_34 (True Skill: 1942): Δ+18.0
- Player_23 (True Skill: 1712): Δ+18.7
**New Player True Skill:** 1961

### #3: New Player LateJoiner_33 (Batch Applied at Match 4068, Total Impact: 465.7)

**New Player Total Batch Adjustment:** Δ-465.7 (from 18 triggers at matches [2860, 2893, 3002, 3084, 3108, 3379, 3451, 3456, 3471, 3475, 3511, 3539, 3638, 3645, 3668, 3966, 4013, 4048])
**New Player Ratings:** Before Batch: 1479.1 → After Batch: 1013.5 (Δ-465.7)
**Established Players Total Adjustments:**
- Player_11 (True Skill: 1963): Δ+2.1
- LateJoiner_44 (True Skill: 1793): Δ+28.0
- LateJoiner_4 (True Skill: 1861): Δ+23.7
- Player_26 (True Skill: 2060): Δ+1.7
- Player_27 (True Skill: 1604): Δ+28.1
- Player_12 (True Skill: 1506): Δ+25.1
- Player_29 (True Skill: 1832): Δ+6.4
- Player_34 (True Skill: 1942): Δ+26.1
- Player_46 (True Skill: 1563): Δ+13.4
- Player_16 (True Skill: 1603): Δ+20.9
- Player_20 (True Skill: 1679): Δ+21.4
**New Player True Skill:** 1705

etc...


## Detailed Individual Match Information

### First 100 Matches

#### Match 1

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_40 (1994)           |
| Opponent (0 Games)       | Player_28 (1837)           |
| Result                   | Win                        |
| Challenger Rating        | 1000.0 -> 1060.0           |
| Opponent Rating          | 1000.0 -> 960.0            |
| Win Probability          | 71.2%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.00                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | 0.20                       |
| Rating Changes           | Winner: 60.0, Loser: -40.0 |
| Rating Changes after PP  | Winner: 60.0, Loser: -40.0 |
| Multipliers              | Winner: 2.00, Loser: 2.00  |

#### Match 2

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_31 (1625)           |
| Opponent (0 Games)       | Player_46 (1563)           |
| Result                   | Loss                       |
| Challenger Rating        | 1000.0 -> 960.0            |
| Opponent Rating          | 1000.0 -> 1060.0           |
| Win Probability          | 58.8%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.00                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | 0.20                       |
| Rating Changes           | Winner: 60.0, Loser: -40.0 |
| Rating Changes after PP  | Winner: 60.0, Loser: -40.0 |
| Multipliers              | Winner: 2.00, Loser: 2.00  |

#### Match 3

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_19 (1799)           |
| Opponent (0 Games)       | Player_14 (1726)           |
| Result                   | Win                        |
| Challenger Rating        | 1000.0 -> 1060.0           |
| Opponent Rating          | 1000.0 -> 960.0            |
| Win Probability          | 60.4%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.00                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | 0.20                       |
| Rating Changes           | Winner: 60.0, Loser: -40.0 |
| Rating Changes after PP  | Winner: 60.0, Loser: -40.0 |
| Multipliers              | Winner: 2.00, Loser: 2.00  |

#### Match 4

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_41 (2319)           |
| Opponent (0 Games)       | Player_25 (2277)           |
| Result                   | Loss                       |
| Challenger Rating        | 1000.0 -> 960.0            |
| Opponent Rating          | 1000.0 -> 1060.0           |
| Win Probability          | 56.0%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.00                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | 0.20                       |
| Rating Changes           | Winner: 60.0, Loser: -40.0 |
| Rating Changes after PP  | Winner: 60.0, Loser: -40.0 |
| Multipliers              | Winner: 2.00, Loser: 2.00  |

#### Match 5

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_42 (2152)           |
| Opponent (1 Games)       | Player_40 (1994)           |
| Result                   | Win                        |
| Challenger Rating        | 1000.0 -> 1070.2           |
| Opponent Rating          | 1060.0 -> 1016.4           |
| Win Probability          | 71.3%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.14                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | 0.00                       |
| Rating Changes           | Winner: 70.2, Loser: -43.6 |
| Rating Changes after PP  | Winner: 70.2, Loser: -43.6 |
| Multipliers              | Winner: 2.00, Loser: 1.86  |

#### Match 6

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_6 (1198)            |
| Opponent (0 Games)       | Player_37 (1268)           |
| Result                   | Win                        |
| Challenger Rating        | 1000.0 -> 1060.0           |
| Opponent Rating          | 1000.0 -> 960.0            |
| Win Probability          | 40.1%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.00                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | 0.20                       |
| Rating Changes           | Winner: 60.0, Loser: -40.0 |
| Rating Changes after PP  | Winner: 60.0, Loser: -40.0 |
| Multipliers              | Winner: 2.00, Loser: 2.00  |

#### Match 7

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_5 (2277)            |
| Opponent (0 Games)       | Player_26 (2060)           |
| Result                   | Loss                       |
| Challenger Rating        | 1000.0 -> 960.0            |
| Opponent Rating          | 1000.0 -> 1060.0           |
| Win Probability          | 77.7%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.00                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | 0.20                       |
| Rating Changes           | Winner: 60.0, Loser: -40.0 |
| Rating Changes after PP  | Winner: 60.0, Loser: -40.0 |
| Multipliers              | Winner: 2.00, Loser: 2.00  |

#### Match 8

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_35 (1698)           |
| Opponent (0 Games)       | Player_43 (1740)           |
| Result                   | Loss                       |
| Challenger Rating        | 1000.0 -> 960.0            |
| Opponent Rating          | 1000.0 -> 1060.0           |
| Win Probability          | 44.0%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.00                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | 0.20                       |
| Rating Changes           | Winner: 60.0, Loser: -40.0 |
| Rating Changes after PP  | Winner: 60.0, Loser: -40.0 |
| Multipliers              | Winner: 2.00, Loser: 2.00  |

#### Match 9

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (1 Games)     | Player_26 (2060)           |
| Opponent (1 Games)       | Player_14 (1726)           |
| Result                   | Win                        |
| Challenger Rating        | 1060.0 -> 1101.1           |
| Opponent Rating          | 960.0 -> 933.2             |
| Win Probability          | 87.2%                      |
| Challenger Confidence    | 0.14                       |
| Opponent Confidence      | 0.14                       |
| Challenger Variety Bonus | -0.10                      |
| Opponent Variety Bonus   | -0.10                      |
| Rating Changes           | Winner: 41.2, Loser: -26.8 |
| Rating Changes after PP  | Winner: 41.2, Loser: -26.8 |
| Multipliers              | Winner: 1.86, Loser: 1.86  |

#### Match 10

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | LateJoiner_4 (1861)        |
| Opponent (0 Games)       | Player_13 (1930)           |
| Result                   | Loss                       |
| Challenger Rating        | 1000.0 -> 960.0            |
| Opponent Rating          | 1000.0 -> 1060.0           |
| Win Probability          | 40.2%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.00                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | 0.20                       |
| Rating Changes           | Winner: 60.0, Loser: -40.0 |
| Rating Changes after PP  | Winner: 60.0, Loser: -40.0 |
| Multipliers              | Winner: 2.00, Loser: 2.00  |

#### Match 11

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_20 (1679)           |
| Opponent (1 Games)       | Player_46 (1563)           |
| Result                   | Win                        |
| Challenger Rating        | 1000.0 -> 1070.2           |
| Opponent Rating          | 1060.0 -> 1016.4           |
| Win Probability          | 66.1%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.14                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | -0.10                      |
| Rating Changes           | Winner: 70.2, Loser: -43.6 |
| Rating Changes after PP  | Winner: 70.2, Loser: -43.6 |
| Multipliers              | Winner: 2.00, Loser: 1.86  |

#### Match 12

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_23 (1712)           |
| Opponent (1 Games)       | LateJoiner_4 (1861)        |
| Result                   | Loss                       |
| Challenger Rating        | 1000.0 -> 955.4            |
| Opponent Rating          | 960.0 -> 1023.8            |
| Win Probability          | 29.8%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.14                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | -0.10                      |
| Rating Changes           | Winner: 63.8, Loser: -44.6 |
| Rating Changes after PP  | Winner: 63.8, Loser: -44.6 |
| Multipliers              | Winner: 1.86, Loser: 2.00  |

#### Match 13

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (1 Games)     | Player_23 (1712)           |
| Opponent (0 Games)       | Player_34 (1942)           |
| Result                   | Win                        |
| Challenger Rating        | 955.4 -> 1019.9            |
| Opponent Rating          | 1000.0 -> 954.9            |
| Win Probability          | 21.0%                      |
| Challenger Confidence    | 0.14                       |
| Opponent Confidence      | 0.00                       |
| Challenger Variety Bonus | -0.10                      |
| Opponent Variety Bonus   | 0.20                       |
| Rating Changes           | Winner: 64.5, Loser: -45.1 |
| Rating Changes after PP  | Winner: 64.5, Loser: -45.1 |
| Multipliers              | Winner: 1.86, Loser: 2.00  |

#### Match 14

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (1 Games)     | Player_41 (2319)           |
| Opponent (2 Games)       | Player_40 (1994)           |
| Result                   | Win                        |
| Challenger Rating        | 960.0 -> 1026.4            |
| Opponent Rating          | 1016.4 -> 976.0            |
| Win Probability          | 86.7%                      |
| Challenger Confidence    | 0.14                       |
| Opponent Confidence      | 0.26                       |
| Challenger Variety Bonus | -0.10                      |
| Opponent Variety Bonus   | 0.20                       |
| Rating Changes           | Winner: 66.4, Loser: -40.4 |
| Rating Changes after PP  | Winner: 66.4, Loser: -40.4 |
| Multipliers              | Winner: 1.86, Loser: 1.74  |

#### Match 15

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_47 (1596)           |
| Opponent (0 Games)       | Player_29 (1832)           |
| Result                   | Loss                       |
| Challenger Rating        | 1000.0 -> 960.0            |
| Opponent Rating          | 1000.0 -> 1060.0           |
| Win Probability          | 20.4%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.00                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | 0.20                       |
| Rating Changes           | Winner: 60.0, Loser: -40.0 |
| Rating Changes after PP  | Winner: 60.0, Loser: -40.0 |
| Multipliers              | Winner: 2.00, Loser: 2.00  |

#### Match 16

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (1 Games)     | Player_34 (1942)           |
| Opponent (1 Games)       | Player_20 (1679)           |
| Result                   | Win                        |
| Challenger Rating        | 954.9 -> 1030.4            |
| Opponent Rating          | 1070.2 -> 1021.1           |
| Win Probability          | 82.0%                      |
| Challenger Confidence    | 0.14                       |
| Opponent Confidence      | 0.14                       |
| Challenger Variety Bonus | -0.10                      |
| Opponent Variety Bonus   | -0.10                      |
| Rating Changes           | Winner: 75.5, Loser: -49.1 |
| Rating Changes after PP  | Winner: 75.5, Loser: -49.1 |
| Multipliers              | Winner: 1.86, Loser: 1.86  |

#### Match 17

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_49 (2004)           |
| Opponent (0 Games)       | Player_38 (2169)           |
| Result                   | Loss                       |
| Challenger Rating        | 1000.0 -> 960.0            |
| Opponent Rating          | 1000.0 -> 1060.0           |
| Win Probability          | 27.9%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.00                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | 0.20                       |
| Rating Changes           | Winner: 60.0, Loser: -40.0 |
| Rating Changes after PP  | Winner: 60.0, Loser: -40.0 |
| Multipliers              | Winner: 2.00, Loser: 2.00  |

#### Match 18

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_1 (2241)            |
| Opponent (2 Games)       | Player_26 (2060)           |
| Result                   | Loss                       |
| Challenger Rating        | 1000.0 -> 971.3            |
| Opponent Rating          | 1101.1 -> 1140.4           |
| Win Probability          | 73.9%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.26                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | 0.20                       |
| Rating Changes           | Winner: 39.3, Loser: -28.7 |
| Rating Changes after PP  | Winner: 39.3, Loser: -28.7 |
| Multipliers              | Winner: 1.74, Loser: 2.00  |

#### Match 19

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (2 Games)     | Player_34 (1942)           |
| Opponent (3 Games)       | Player_40 (1994)           |
| Result                   | Loss                       |
| Challenger Rating        | 1030.4 -> 990.2            |
| Opponent Rating          | 976.0 -> 1036.9            |
| Win Probability          | 42.6%                      |
| Challenger Confidence    | 0.26                       |
| Opponent Confidence      | 0.36                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | 0.20                       |
| Rating Changes           | Winner: 60.9, Loser: -40.2 |
| Rating Changes after PP  | Winner: 60.9, Loser: -40.2 |
| Multipliers              | Winner: 1.64, Loser: 1.74  |

#### Match 20

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_44 (2006)           |
| Opponent (3 Games)       | Player_26 (2060)           |
| Result                   | Loss                       |
| Challenger Rating        | 1000.0 -> 975.3            |
| Opponent Rating          | 1140.4 -> 1172.9           |
| Win Probability          | 42.3%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.36                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | 0.20                       |
| Rating Changes           | Winner: 32.5, Loser: -24.7 |
| Rating Changes after PP  | Winner: 32.5, Loser: -24.7 |
| Multipliers              | Winner: 1.64, Loser: 2.00  |

#### Match 21

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_11 (1963)           |
| Opponent (0 Games)       | Player_4 (2212)            |
| Result                   | Loss                       |
| Challenger Rating        | 1000.0 -> 960.0            |
| Opponent Rating          | 1000.0 -> 1060.0           |
| Win Probability          | 19.3%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.00                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | 0.20                       |
| Rating Changes           | Winner: 60.0, Loser: -40.0 |
| Rating Changes after PP  | Winner: 60.0, Loser: -40.0 |
| Multipliers              | Winner: 2.00, Loser: 2.00  |

#### Match 22

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (3 Games)     | Player_34 (1942)           |
| Opponent (0 Games)       | Player_32 (2046)           |
| Result                   | Loss                       |
| Challenger Rating        | 990.2 -> 958.4             |
| Opponent Rating          | 1000.0 -> 1058.3           |
| Win Probability          | 35.5%                      |
| Challenger Confidence    | 0.36                       |
| Opponent Confidence      | 0.00                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | 0.20                       |
| Rating Changes           | Winner: 58.3, Loser: -31.8 |
| Rating Changes after PP  | Winner: 58.3, Loser: -31.8 |
| Multipliers              | Winner: 2.00, Loser: 1.64  |

#### Match 23

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_15 (2151)           |
| Opponent (1 Games)       | Player_32 (2046)           |
| Result                   | Win                        |
| Challenger Rating        | 1000.0 -> 1070.0           |
| Opponent Rating          | 1058.3 -> 1014.9           |
| Win Probability          | 64.7%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.14                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | -0.10                      |
| Rating Changes           | Winner: 70.0, Loser: -43.4 |
| Rating Changes after PP  | Winner: 70.0, Loser: -43.4 |
| Multipliers              | Winner: 2.00, Loser: 1.86  |

#### Match 24

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (2 Games)     | Player_41 (2319)           |
| Opponent (1 Games)       | Player_42 (2152)           |
| Result                   | Loss                       |
| Challenger Rating        | 1026.4 -> 996.0            |
| Opponent Rating          | 1070.2 -> 1120.2           |
| Win Probability          | 72.3%                      |
| Challenger Confidence    | 0.26                       |
| Opponent Confidence      | 0.14                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | -0.10                      |
| Rating Changes           | Winner: 50.0, Loser: -30.4 |
| Rating Changes after PP  | Winner: 50.0, Loser: -30.4 |
| Multipliers              | Winner: 1.86, Loser: 1.74  |

#### Match 25

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_27 (1604)           |
| Opponent (0 Games)       | Player_48 (1318)           |
| Result                   | Loss                       |
| Challenger Rating        | 1000.0 -> 960.0            |
| Opponent Rating          | 1000.0 -> 1060.0           |
| Win Probability          | 83.8%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.00                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | 0.20                       |
| Rating Changes           | Winner: 60.0, Loser: -40.0 |
| Rating Changes after PP  | Winner: 60.0, Loser: -40.0 |
| Multipliers              | Winner: 2.00, Loser: 2.00  |

#### Match 26

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_50 (1362)           |
| Opponent (0 Games)       | Player_12 (1506)           |
| Result                   | Loss                       |
| Challenger Rating        | 1000.0 -> 960.0            |
| Opponent Rating          | 1000.0 -> 1060.0           |
| Win Probability          | 30.4%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.00                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | 0.20                       |
| Rating Changes           | Winner: 60.0, Loser: -40.0 |
| Rating Changes after PP  | Winner: 60.0, Loser: -40.0 |
| Multipliers              | Winner: 2.00, Loser: 2.00  |

#### Match 27

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (1 Games)     | Player_28 (1837)           |
| Opponent (1 Games)       | Player_43 (1740)           |
| Result                   | Win                        |
| Challenger Rating        | 960.0 -> 1033.2            |
| Opponent Rating          | 1060.0 -> 1012.3           |
| Win Probability          | 63.6%                      |
| Challenger Confidence    | 0.14                       |
| Opponent Confidence      | 0.14                       |
| Challenger Variety Bonus | -0.10                      |
| Opponent Variety Bonus   | -0.10                      |
| Rating Changes           | Winner: 73.2, Loser: -47.6 |
| Rating Changes after PP  | Winner: 73.2, Loser: -47.6 |
| Multipliers              | Winner: 1.86, Loser: 1.86  |

#### Match 28

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_24 (1623)           |
| Opponent (1 Games)       | Player_29 (1832)           |
| Result                   | Loss                       |
| Challenger Rating        | 1000.0 -> 966.8            |
| Opponent Rating          | 1060.0 -> 1107.4           |
| Win Probability          | 23.1%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.14                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | -0.10                      |
| Rating Changes           | Winner: 47.4, Loser: -33.2 |
| Rating Changes after PP  | Winner: 47.4, Loser: -33.2 |
| Multipliers              | Winner: 1.86, Loser: 2.00  |

#### Match 29

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_3 (2273)            |
| Opponent (1 Games)       | Player_49 (2004)           |
| Result                   | Loss                       |
| Challenger Rating        | 1000.0 -> 955.4            |
| Opponent Rating          | 960.0 -> 1023.8            |
| Win Probability          | 82.5%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.14                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | -0.10                      |
| Rating Changes           | Winner: 63.8, Loser: -44.6 |
| Rating Changes after PP  | Winner: 63.8, Loser: -44.6 |
| Multipliers              | Winner: 1.86, Loser: 2.00  |

#### Match 30

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (0 Games)     | Player_18 (1348)           |
| Opponent (0 Games)       | Player_8 (1121)            |
| Result                   | Loss                       |
| Challenger Rating        | 1000.0 -> 960.0            |
| Opponent Rating          | 1000.0 -> 1060.0           |
| Win Probability          | 78.7%                      |
| Challenger Confidence    | 0.00                       |
| Opponent Confidence      | 0.00                       |
| Challenger Variety Bonus | 0.20                       |
| Opponent Variety Bonus   | 0.20                       |
| Rating Changes           | Winner: 60.0, Loser: -40.0 |
| Rating Changes after PP  | Winner: 60.0, Loser: -40.0 |
| Multipliers              | Winner: 2.00, Loser: 2.00  |

etc...

### Last 10 Matches

#### Match 4391

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (97 Games)    | LateJoiner_12 (2164)       |
| Opponent (98 Games)      | LateJoiner_34 (2037)       |
| Result                   | Win                        |
| Challenger Rating        | 1765.0 -> 1783.3           |
| Opponent Rating          | 1729.6 -> 1712.9           |
| Win Probability          | 67.5%                      |
| Challenger Confidence    | 1.00                       |
| Opponent Confidence      | 1.00                       |
| Challenger Variety Bonus | 0.07                       |
| Opponent Variety Bonus   | 0.07                       |
| Rating Changes           | Winner: 18.3, Loser: -16.6 |
| Rating Changes after PP  | Winner: 18.3, Loser: -16.6 |
| Multipliers              | Winner: 1.07, Loser: 0.93  |

#### Match 4392

| Category                 | Details                   |
| ------------------------ | ------------------------- |
| Challenger (75 Games)    | LateJoiner_45 (2063)      |
| Opponent (124 Games)     | Player_28 (1837)          |
| Result                   | Win                       |
| Challenger Rating        | 1687.7 -> 1696.5          |
| Opponent Rating          | 1577.1 -> 1564.8          |
| Win Probability          | 78.6%                     |
| Challenger Confidence    | 1.00                      |
| Opponent Confidence      | 1.00                      |
| Challenger Variety Bonus | 0.08                      |
| Opponent Variety Bonus   | 0.11                      |
| Rating Changes           | Winner: 8.7, Loser: -12.3 |
| Rating Changes after PP  | Winner: 8.7, Loser: -12.3 |
| Multipliers              | Winner: 1.08, Loser: 0.89 |

#### Match 4393

| Category                 | Details                   |
| ------------------------ | ------------------------- |
| Challenger (102 Games)   | Player_27 (1604)          |
| Opponent (89 Games)      | Player_50 (1362)          |
| Result                   | Win                       |
| Challenger Rating        | 1583.5 -> 1591.7          |
| Opponent Rating          | 1330.8 -> 1323.9          |
| Win Probability          | 80.1%                     |
| Challenger Confidence    | 1.00                      |
| Opponent Confidence      | 1.00                      |
| Challenger Variety Bonus | 0.09                      |
| Opponent Variety Bonus   | 0.09                      |
| Rating Changes           | Winner: 8.3, Loser: -6.9  |
| Rating Changes after PP  | Winner: 8.3, Loser: -6.9  |
| Multipliers              | Winner: 1.09, Loser: 0.91 |

#### Match 4394

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (98 Games)    | LateJoiner_12 (2164)       |
| Opponent (73 Games)      | LateJoiner_1 (2064)        |
| Result                   | Loss                       |
| Challenger Rating        | 1783.3 -> 1766.3           |
| Opponent Rating          | 1699.1 -> 1725.4           |
| Win Probability          | 64.0%                      |
| Challenger Confidence    | 1.00                       |
| Opponent Confidence      | 1.00                       |
| Challenger Variety Bonus | 0.08                       |
| Opponent Variety Bonus   | 0.06                       |
| Rating Changes           | Winner: 26.3, Loser: -17.0 |
| Rating Changes after PP  | Winner: 26.3, Loser: -17.0 |
| Multipliers              | Winner: 1.06, Loser: 0.92  |

#### Match 4395

| Category                 | Details                   |
| ------------------------ | ------------------------- |
| Challenger (165 Games)   | Player_41 (2319)          |
| Opponent (111 Games)     | LateJoiner_37 (1955)      |
| Result                   | Win                       |
| Challenger Rating        | 1988.6 -> 1993.1          |
| Opponent Rating          | 1616.3 -> 1612.5          |
| Win Probability          | 89.0%                     |
| Challenger Confidence    | 1.00                      |
| Opponent Confidence      | 1.00                      |
| Challenger Variety Bonus | 0.07                      |
| Opponent Variety Bonus   | 0.09                      |
| Rating Changes           | Winner: 4.5, Loser: -3.8  |
| Rating Changes after PP  | Winner: 4.5, Loser: -3.8  |
| Multipliers              | Winner: 1.07, Loser: 0.91 |

#### Match 4396

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (133 Games)   | Player_23 (1712)           |
| Opponent (125 Games)     | Player_28 (1837)           |
| Result                   | Win                        |
| Challenger Rating        | 1614.5 -> 1631.8           |
| Opponent Rating          | 1564.8 -> 1549.5           |
| Win Probability          | 32.7%                      |
| Challenger Confidence    | 1.00                       |
| Opponent Confidence      | 1.00                       |
| Challenger Variety Bonus | 0.11                       |
| Opponent Variety Bonus   | 0.11                       |
| Rating Changes           | Winner: 17.3, Loser: -15.3 |
| Rating Changes after PP  | Winner: 17.3, Loser: -15.3 |
| Multipliers              | Winner: 1.11, Loser: 0.89  |

#### Match 4397

| Category                 | Details                    |
| ------------------------ | -------------------------- |
| Challenger (136 Games)   | Player_45 (1836)           |
| Opponent (140 Games)     | Player_29 (1832)           |
| Result                   | Loss                       |
| Challenger Rating        | 1599.9 -> 1581.8           |
| Opponent Rating          | 1596.0 -> 1618.4           |
| Win Probability          | 50.6%                      |
| Challenger Confidence    | 1.00                       |
| Opponent Confidence      | 1.00                       |
| Challenger Variety Bonus | 0.10                       |
| Opponent Variety Bonus   | 0.11                       |
| Rating Changes           | Winner: 22.4, Loser: -18.1 |
| Rating Changes after PP  | Winner: 22.4, Loser: -18.1 |
| Multipliers              | Winner: 1.11, Loser: 0.90  |

#### Match 4398

| Category                 | Details                   |
| ------------------------ | ------------------------- |
| Challenger (156 Games)   | Player_26 (2060)          |
| Opponent (68 Games)      | LateJoiner_36 (1594)      |
| Result                   | Win                       |
| Challenger Rating        | 1805.9 -> 1814.7          |
| Opponent Rating          | 1563.7 -> 1556.4          |
| Win Probability          | 93.6%                     |
| Challenger Confidence    | 1.00                      |
| Opponent Confidence      | 1.00                      |
| Challenger Variety Bonus | 0.10                      |
| Opponent Variety Bonus   | 0.08                      |
| Rating Changes           | Winner: 8.8, Loser: -7.3  |
| Rating Changes after PP  | Winner: 8.8, Loser: -7.3  |
| Multipliers              | Winner: 1.10, Loser: 0.92 |

#### Match 4399

| Category                 | Details                   |
| ------------------------ | ------------------------- |
| Challenger (167 Games)   | Player_32 (2046)          |
| Opponent (67 Games)      | LateJoiner_16 (1799)      |
| Result                   | Win                       |
| Challenger Rating        | 1826.3 -> 1835.7          |
| Opponent Rating          | 1598.1 -> 1590.3          |
| Win Probability          | 80.6%                     |
| Challenger Confidence    | 1.00                      |
| Opponent Confidence      | 1.00                      |
| Challenger Variety Bonus | 0.11                      |
| Opponent Variety Bonus   | 0.07                      |
| Rating Changes           | Winner: 9.5, Loser: -7.9  |
| Rating Changes after PP  | Winner: 9.5, Loser: -7.9  |
| Multipliers              | Winner: 1.11, Loser: 0.93 |

#### Match 4400

| Category                 | Details                   |
| ------------------------ | ------------------------- |
| Challenger (155 Games)   | Player_38 (2169)          |
| Opponent (93 Games)      | Player_22 (1783)          |
| Result                   | Win                       |
| Challenger Rating        | 1802.0 -> 1810.7          |
| Opponent Rating          | 1561.3 -> 1554.0          |
| Win Probability          | 90.2%                     |
| Challenger Confidence    | 1.00                      |
| Opponent Confidence      | 1.00                      |
| Challenger Variety Bonus | 0.09                      |
| Opponent Variety Bonus   | 0.09                      |
| Rating Changes           | Winner: 8.7, Loser: -7.3  |
| Rating Changes after PP  | Winner: 8.7, Loser: -7.3  |
| Multipliers              | Winner: 1.09, Loser: 0.91 |
