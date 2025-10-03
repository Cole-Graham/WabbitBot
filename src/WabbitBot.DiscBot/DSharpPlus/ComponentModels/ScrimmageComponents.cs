using DSharpPlus.Entities;
using WabbitBot.Common.Attributes;

namespace WabbitBot.DiscBot.DSharpPlus.ComponentModels;

// ============================================================================
// COMPONENT MODEL ORGANIZATION
// ============================================================================
// This file contains all scrimmage-related visual component models.
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
/// POCO container model for scrimmage challenge display.
/// Contains only data needed for rendering - no DSharpPlus dependencies.
/// </summary>
[GenerateComponentFactory(Theme = "Info")]
public class ChallengeContainer
{
    /// <summary>
    /// The component type.
    /// </summary>
    public required DiscordContainerComponent ComponentType { get; init; }

    /// <summary>
    /// The challenge ID.
    /// </summary>
    public required Guid ChallengeId { get; init; }

    /// <summary>
    /// Name of the challenging team.
    /// </summary>
    public required string ChallengerTeamName { get; init; }

    /// <summary>
    /// Name of the opponent team.
    /// </summary>
    public required string OpponentTeamName { get; init; }

    /// <summary>
    /// Game size (e.g., "1v1", "2v2").
    /// </summary>
    public required string GameSize { get; init; }

    /// <summary>
    /// When the challenge was created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Optional message or description.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Color theme for the embed/container.
    /// </summary>
    public string Color { get; init; } = "Info";

    /// <summary>
    /// Whether to show accept/decline buttons.
    /// </summary>
    public bool ShowActionButtons { get; init; } = true;
}

