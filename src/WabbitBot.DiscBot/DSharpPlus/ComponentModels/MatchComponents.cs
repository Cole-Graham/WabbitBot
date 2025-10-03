using DSharpPlus.Entities;
using WabbitBot.Common.Attributes;

namespace WabbitBot.DiscBot.DSharpPlus.ComponentModels;

// ============================================================================
// COMPONENT MODEL ORGANIZATION
// ============================================================================
// This file contains all match-related visual component models.
// 
// ARCHITECTURE PATTERN:
// - Component models are POCO classes with no inheritance
// - Organized by domain (Scrimmage, Match, Game) in separate files
// - Each model is marked with [GenerateComponentFactory] for auto-generation
// - Models contain only data needed for rendering (view models)
// - Minimal DSharpPlus dependencies (only DiscordContainerComponent)
//
// PRIMARY UI PATTERN: DiscordContainerComponent (modern, rich displays)
// - All current models use Container pattern
// - Containers support interactive elements, layouts, and theming
// - Models must expose: `DiscordContainerComponent ComponentType { get; init; }`
//
// FUTURE PATTERN: DiscordEmbed (simple interaction responses)
// - Reserved for simple, legacy-style displays per Discord best practices
// - Not currently in use; containers are preferred for all displays
// - Would expose: `DiscordEmbed Embed { get; init; }`
// ============================================================================

/// <summary>
/// POCO container model for match display.
/// Contains only data needed for rendering - no DSharpPlus dependencies.
/// </summary>
[GenerateComponentFactory(Theme = "Info")]
public class MatchContainer
{
    /// <summary>
    /// The component type.
    /// </summary>
    public required DiscordContainerComponent ComponentType { get; init; }

    /// <summary>
    /// The match ID.
    /// </summary>
    public required Guid MatchId { get; init; }

    /// <summary>
    /// Name of team 1.
    /// </summary>
    public required string Team1Name { get; init; }

    /// <summary>
    /// Name of team 2.
    /// </summary>
    public required string Team2Name { get; init; }

    /// <summary>
    /// Match format (e.g., "Best of 1", "Best of 3").
    /// </summary>
    public required string Format { get; init; }

    /// <summary>
    /// Current match status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Current score (e.g., "Team1: 1 - Team2: 0").
    /// </summary>
    public string? CurrentScore { get; init; }

    /// <summary>
    /// List of completed games with results.
    /// </summary>
    public List<GameSummary> CompletedGames { get; init; } = [];

    /// <summary>
    /// Color theme for the embed/container.
    /// </summary>
    public string Color { get; init; } = "Info";

    /// <summary>
    /// Whether to show start match button.
    /// </summary>
    public bool ShowStartButton { get; init; } = true;

    /// <summary>
    /// Whether to show cancel match button.
    /// </summary>
    public bool ShowCancelButton { get; init; } = true;
}

/// <summary>
/// Summary of a single game within a match.
/// </summary>
public class GameSummary
{
    /// <summary>
    /// Game number in the series.
    /// </summary>
    public required int GameNumber { get; init; }

    /// <summary>
    /// Map played.
    /// </summary>
    public required string Map { get; init; }

    /// <summary>
    /// Winner team name.
    /// </summary>
    public required string WinnerTeamName { get; init; }

    /// <summary>
    /// Game duration or additional info.
    /// </summary>
    public string? AdditionalInfo { get; init; }
}

