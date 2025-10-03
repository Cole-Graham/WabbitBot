using DSharpPlus.Entities;
using WabbitBot.Common.Attributes;

namespace WabbitBot.DiscBot.DSharpPlus.ComponentModels;

// ============================================================================
// COMPONENT MODEL ORGANIZATION
// ============================================================================
// This file contains all game-related visual component models.
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
/// POCO container model for per-game display in match threads.
/// Contains only data needed for rendering - no DSharpPlus dependencies.
/// </summary>
[GenerateComponentFactory(Theme = "Info")]
public class GameContainer
{
    /// <summary>
    /// The component type.
    /// </summary>
    public required DiscordContainerComponent ComponentType { get; init; }

    /// <summary>
    /// The match ID this game belongs to.
    /// </summary>
    public required Guid MatchId { get; init; }

    /// <summary>
    /// Game number in the series.
    /// </summary>
    public required int GameNumber { get; init; }

    /// <summary>
    /// Selected map for this game.
    /// </summary>
    public required string SelectedMap { get; init; }

    /// <summary>
    /// Name of team 1.
    /// </summary>
    public required string Team1Name { get; init; }

    /// <summary>
    /// Name of team 2.
    /// </summary>
    public required string Team2Name { get; init; }

    /// <summary>
    /// Current game status (e.g., "In Progress", "Completed", "Waiting for Replay").
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Winner team name (if game is completed).
    /// </summary>
    public string? WinnerTeamName { get; init; }

    /// <summary>
    /// URL or canonical filename of the map thumbnail image.
    /// Preferred: CDN URL if available; otherwise canonical filename for attachment flow.
    /// </summary>
    public string? MapThumbnailUrl { get; init; }

    /// <summary>
    /// Instructions for players.
    /// </summary>
    public string Instructions { get; init; } = "Play the game and upload the replay when finished.";

    /// <summary>
    /// Color theme for the embed/container.
    /// </summary>
    public string Color { get; init; } = "Info";

    /// <summary>
    /// Whether to show replay upload button.
    /// </summary>
    public bool ShowUploadButton { get; init; } = true;
}

