# WabbitBot Configuration Guide

## Overview

WabbitBot uses the modern .NET configuration system with `IConfiguration` and the Options pattern. This provides a flexible, secure, and maintainable way to configure the bot.

## Configuration Files

### Primary Configuration
- **`appsettings.json`** - Main configuration file with all bot settings
- **`appsettings.Development.json`** - Development-specific overrides
- **`appsettings.Production.json`** - Production-specific overrides

### Environment Variables
For sensitive data and deployment flexibility, use environment variables:

```bash
# Required
WABBITBOT_TOKEN=your-discord-bot-token-here

# Optional
WABBITBOT_DATABASE_PATH=data/wabbitbot.db
ASPNETCORE_ENVIRONMENT=Development
```

## Configuration Structure

The configuration is organized into logical sections:

```json
{
  "Bot": {
    "Token": "your-bot-token",
    "LogLevel": "Information",
    "ServerId": null,
    "Database": { ... },
    "Channels": { ... },
    "Roles": { ... },
    "Activity": { ... },
    "Scrimmage": { ... },
    "Tournament": { ... },
    "Match": { ... },
    "Leaderboard": { ... }
  }
}
```

## Feature-Specific Configuration

### Scrimmage Settings
- `MaxConcurrentScrimmages` - Maximum number of simultaneous scrimmages
- `RatingSystem` - Rating algorithm ("elo")
- `InitialRating` - Starting rating for new teams
- `KFactor` - ELO sensitivity factor
- `MatchTimeoutMinutes` - Time limit for matches
- `MapBanCount` - Number of maps each team can ban
- `BestOf` - Number of games per scrimmage

### Tournament Settings
- `DefaultFormat` - Tournament format ("single-elimination")
- `BracketSize` - Number of teams in tournament
- `BestOf` - Games per match
- `AllowSpectators` - Whether spectators are allowed
- `MatchTimeoutMinutes` - Time limit for matches
- `MaxTournamentsPerDay` - Daily tournament limit

### Match Settings
- `MaxGamesPerMatch` - Maximum games in a single match
- `DefaultBestOf` - Default games per match
- `DeckCodeRequired` - Whether deck codes are mandatory
- `AllowDuplicateDecks` - Whether duplicate decks are allowed
- `ResultTimeoutMinutes` - Time limit for result submission

### Leaderboard Settings
- `DisplayTopN` - Number of teams to show in leaderboard
- `RankingAlgorithm` - Ranking method ("elo")
- `SeasonalResets` - Whether to reset ratings seasonally
- `SeasonLengthDays` - Length of each season
- `TournamentWeight` - Weight multiplier for tournament results
- `ScrimmageWeight` - Weight multiplier for scrimmage results

## Environment Variable Overrides

You can override any configuration value using environment variables:

```bash
# Override specific settings
WABBITBOT_SCRIMMAGE_MAXCONCURRENTSCRIMMAGES=15
WABBITBOT_TOURNAMENT_BRACKETSIZE=32
WABBITBOT_LEADERBOARD_DISPLAYTOPN=20
```

## Configuration Validation

The system automatically validates configuration on startup:
- Required fields are present
- Numeric values are within valid ranges
- Logical constraints are satisfied

## Security Best Practices

1. **Never commit sensitive data** to version control
2. **Use environment variables** for tokens and API keys
3. **Use different configs** for different environments
4. **Restrict file permissions** on configuration files

## Configuration Commands

The bot provides Discord commands for configuration management:
- `/config get` - View current configuration
- `/config set-server` - Set server ID
- `/config set-channel` - Configure channels
- `/config set-role` - Configure roles
- `/config export` - Export configuration as JSON
- `/config import` - Import configuration from JSON

## Migration from Old System

If migrating from the old configuration system:

1. **Backup** your existing `config.json`
2. **Convert** settings to the new `appsettings.json` format
3. **Set environment variables** for sensitive data
4. **Test** configuration in development environment
5. **Deploy** with new configuration system

## Troubleshooting

### Common Issues

**Configuration not loading:**
- Check file paths and permissions
- Verify JSON syntax is valid
- Ensure required fields are present

**Environment variables not working:**
- Check variable names (case-sensitive)
- Verify environment is set correctly
- Restart application after changes

**Validation errors:**
- Check numeric ranges
- Verify required fields
- Review logical constraints

### Getting Help

For configuration issues:
1. Check the application logs
2. Verify configuration syntax
3. Test with minimal configuration
4. Review this documentation
