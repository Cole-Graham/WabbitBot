using DSharpPlus.Entities;
using WabbitBot.Common.Attributes;
using WabbitBot.Core.Common.Models;
using WabbitBot.DiscBot.DiscBot.Interfaces;

namespace WabbitBot.DiscBot.DSharpPlus.Embeds;

[GenerateEmbedFactory]
public class MapEmbed : BaseEmbed
{
    private Map? _map;

    public void SetMap(Map map, string description)
    {
        _map = map;
        Title = map.Name;
        Description = description;
        ThumbnailUrl = map.ThumbnailUrl;

        var fields = new List<IEmbedField>
        {
            new EmbedField { Name = "Size", Value = map.Size ?? "Unknown", IsInline = true },
            new EmbedField { Name = "In Random Pool", Value = map.IsInRandomPool ? "Yes" : "No", IsInline = true },
            new EmbedField { Name = "In Tournament Pool", Value = map.IsInTournamentPool ? "Yes" : "No", IsInline = true },
        };

        Fields = fields;
    }
}