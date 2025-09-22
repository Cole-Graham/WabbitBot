using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;

namespace WabbitBot.DiscBot.DSharpPlus.Interactions;

/// <summary>
/// Helper class for creating Discord components for scrimmage interactions
/// </summary>
public static class ScrimmageComponentBuilders
{
    /// <summary>
    /// Creates a dropdown for map ban selection (following old program pattern)
    /// </summary>
    public static DiscordSelectComponent CreateMapBansDropdown(string scrimmageId, string teamId, List<string> availableMaps, int numBans = 3)
    {
        return new DiscordSelectComponent(
            $"map_ban_{scrimmageId}_{teamId}",
            $"Select {numBans} maps to ban (in order of priority)",
            availableMaps.Select(map => new DiscordSelectComponentOption(map, map)),
            false,
            minOptions: numBans,
            maxOptions: numBans
        );
    }

    /// <summary>
    /// Creates a dropdown for game result reporting (following old program pattern)
    /// </summary>
    public static DiscordSelectComponent CreateGameResultDropdown(string scrimmageId, string team1Id, string team2Id, int gameNumber)
    {
        var winnerOptions = new List<DiscordSelectComponentOption>
        {
            new DiscordSelectComponentOption(
                $"{team1Id} wins",
                $"{team1Id}",
                $"{team1Id} is the winner of this game",
                false,
                new DiscordComponentEmoji("🏆")),
            new DiscordSelectComponentOption(
                $"{team2Id} wins",
                $"{team2Id}",
                $"{team2Id} is the winner of this game",
                false,
                new DiscordComponentEmoji("🏆")),
            new DiscordSelectComponentOption(
                "Draw (no winner)",
                "draw",
                "The game ended in a draw",
                false,
                new DiscordComponentEmoji("🤝"))
        };

        return new DiscordSelectComponent(
            $"game_winner_{scrimmageId}_{gameNumber}",
            "Select Winner",
            winnerOptions
        );
    }

    /// <summary>
    /// Creates a text input component for deck code submission
    /// </summary>
    public static DiscordTextInputComponent CreateDeckCodeInput(string customId, string label, string placeholder = "Enter your deck code here...", string? value = null)
    {
        return new DiscordTextInputComponent(
            customId,
            label,
            placeholder: placeholder,
            value: value,
            required: true,
            min_length: 10,
            max_length: 200
        );
    }
}
