# Scrimmage Rating Calculator

## Overview
The Scrimmage Rating Calculator is a sophisticated system that determines rating changes for teams based on their performance in scrimmages. It takes into account various factors to ensure fair and accurate rating adjustments.

## Key Components

### 1. Base Rating Change
- Uses the Elo rating system formula
- Calculates expected outcome based on team ratings
- Applies a base K-factor of 32 for rating adjustments

### 2. Rating Multiplier System
The system uses a multiplier to adjust the base rating change based on several factors:

#### Confidence Rating (0.0 to 1.0)
- Measures how confident we are in a team's current rating
- Based on two factors:
  - Total matches played (0-100 matches)
  - Recent match activity (last 30 days)
- Formula: `confidence = (totalMatchesConfidence + recentMatchesConfidence) / 2`
- Higher confidence = smaller rating changes
- Lower confidence = larger rating changes
- Confidence multiplier ranges from 1.0 (high confidence) to 2.0 (low confidence)

#### Opponent Weight
- Adjusts rating changes based on the rating difference between teams
- Uses a sigmoid function to smoothly scale the impact
- Prevents unfair rating changes when playing against much lower-rated opponents
- Formula: `weight = 1 / (1 + e^(-k * (opponentRating - teamRating)))`
- Where k is a scaling factor (0.0001) to control the steepness of the curve

#### Variety Bonus
- Encourages teams to play against diverse opponents
- Uses Shannon entropy to measure opponent diversity
- Calculates team-specific variety entropy
- Compares against global average variety entropy
- Scales bonus based on games played relative to average
- Formula: `bonus = relativeDiff * (1 - gamesPlayedFactor)`
- Where:
  - `relativeDiff = (teamVarietyEntropy - averageVarietyEntropy) / averageVarietyEntropy`
  - `gamesPlayedFactor = min(teamMatchesPlayed / averageMatchesPlayed, 1.0)`

### 3. Final Rating Change
The final rating change is calculated as:
```
finalChange = baseChange * confidenceMultiplier * opponentWeight * (1 + varietyBonus)
```

Where:
- `baseChange`: Initial Elo rating change
- `confidenceMultiplier`: 1.0 to 2.0 based on team's confidence rating
- `opponentWeight`: 0.5 to 1.0 based on rating difference
- `varietyBonus`: -0.5 to 0.5 based on opponent diversity

## Implementation Details

### Confidence Calculation
```csharp
private static double CalculateConfidence(int totalMatches, int recentMatches)
{
    // Total matches confidence (0-100 matches)
    double totalMatchesConfidence = Math.Min(totalMatches / 100.0, 1.0);
    
    // Recent matches confidence (0-30 days)
    double recentMatchesConfidence = Math.Min(recentMatches / 30.0, 1.0);
    
    // Combined confidence
    return (totalMatchesConfidence + recentMatchesConfidence) / 2;
}
```

### Opponent Weight Calculation
```csharp
private static double GetOpponentWeight(int teamRating, int opponentRating, int highestRating, int lowestRating)
{
    double k = 0.0001; // Scaling factor
    return 1 / (1 + Math.Exp(-k * (opponentRating - teamRating)));
}
```

### Variety Bonus Calculation
```csharp
private static double CalculateVarietyBonus(
    double teamVarietyEntropy,
    double averageVarietyEntropy,
    int teamMatchesPlayed,
    double averageMatchesPlayed)
{
    // Calculate how far the team's variety entropy is from the average
    double entropyDifference = teamVarietyEntropy - averageVarietyEntropy;
    
    // Bonus scales proportionally with difference from average
    double relativeDiff = entropyDifference / (averageVarietyEntropy == 0 ? 1 : averageVarietyEntropy);
    
    // Scale the bonus based on games played relative to average
    double gamesPlayedFactor = Math.Min(teamMatchesPlayed / averageMatchesPlayed, 1.0);
    
    // Final bonus is proportional to entropy difference and inversely proportional to games played
    return relativeDiff * (1 - gamesPlayedFactor);
}
```

## Benefits of the New System

1. **Fairer Rating Changes**
   - Teams with established ratings see smaller changes
   - New teams can adjust their ratings more quickly
   - Prevents rating inflation from playing lower-rated teams

2. **Encourages Diverse Matchmaking**
   - Teams are rewarded for playing against different opponents
   - Prevents rating farming from playing the same teams repeatedly
   - Promotes a more dynamic and engaging competitive environment

3. **Balanced Progression**
   - New teams can climb ratings faster until their rating stabilizes
   - Established teams maintain more stable ratings
   - System adapts to team activity levels

4. **Anti-Abuse Measures**
   - Opponent weight prevents rating farming
   - Variety bonus discourages repeated matches against the same teams
   - Confidence system prevents rating manipulation

## Example Scenarios

### New Team
- Low confidence rating (0.2)
- High confidence multiplier (1.8)
- Large rating changes
- Quick rating stabilization

### Established Team
- High confidence rating (0.8)
- Low confidence multiplier (1.2)
- Smaller rating changes
- Stable rating progression

### Team Playing Diverse Opponents
- High variety entropy
- Positive variety bonus
- Larger rating changes
- Encouraged to maintain diverse matchmaking

### Team Playing Same Opponents
- Low variety entropy
- Negative variety bonus
- Smaller rating changes
- Incentivized to play different teams 