using DSharpPlus.Entities;
using WabbitBot.Common.Models;
using WabbitBot.Common.Attributes;

namespace WabbitBot.DiscBot.DSharpPlus.Embeds;

/// <summary>
/// Embed for displaying configuration information
/// </summary>
[GenerateEmbedFactory]
public class ConfigurationEmbed : BaseEmbed
{
    public void SetConfiguration(BotOptions config, string title = "Bot Configuration")
    {
        Title = title;
        Color = DiscordColor.Blue;

        var fields = new List<IEmbedField>
        {
            new EmbedField { Name = "Server ID", Value = config.ServerId?.ToString() ?? "Not set", IsInline = true },
            new EmbedField { Name = "Bot Channel", Value = config.Channels.BotChannel?.ToString() ?? "Not set", IsInline = true },
            new EmbedField { Name = "Replay Channel", Value = config.Channels.ReplayChannel?.ToString() ?? "Not set", IsInline = true },
            new EmbedField { Name = "Deck Channel", Value = config.Channels.DeckChannel?.ToString() ?? "Not set", IsInline = true },
            new EmbedField { Name = "Signup Channel", Value = config.Channels.SignupChannel?.ToString() ?? "Not set", IsInline = true },
            new EmbedField { Name = "Standings Channel", Value = config.Channels.StandingsChannel?.ToString() ?? "Not set", IsInline = true },
            new EmbedField { Name = "Scrimmage Channel", Value = config.Channels.ScrimmageChannel?.ToString() ?? "Not set", IsInline = true },
            new EmbedField { Name = "Whitelisted Role", Value = config.Roles.Whitelisted?.ToString() ?? "Not set", IsInline = true },
            new EmbedField { Name = "Admin Role", Value = config.Roles.Admin?.ToString() ?? "Not set", IsInline = true },
            new EmbedField { Name = "Moderator Role", Value = config.Roles.Moderator?.ToString() ?? "Not set", IsInline = true },
            new EmbedField { Name = "Scrimmage Max Concurrent", Value = config.Scrimmage.MaxConcurrentScrimmages.ToString(), IsInline = true },
            new EmbedField { Name = "Tournament Bracket Size", Value = config.Tournament.BracketSize.ToString(), IsInline = true },
            new EmbedField { Name = "Leaderboard Display Top N", Value = config.Leaderboard.DisplayTopN.ToString(), IsInline = true },
            new EmbedField { Name = "Maps Count", Value = config.Maps.Maps.Count.ToString(), IsInline = true },
        };

        Fields = fields;
    }
}
