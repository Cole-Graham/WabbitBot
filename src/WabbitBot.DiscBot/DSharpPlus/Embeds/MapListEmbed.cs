using DSharpPlus.Entities;
using WabbitBot.Common.Attributes;
using WabbitBot.Core.Common.Models;
using WabbitBot.DiscBot.DiscBot.Interfaces;

namespace WabbitBot.DiscBot.DSharpPlus.Embeds;

[GenerateEmbedFactory]
public class MapListEmbed : BaseEmbed
{
    public const int MapsPerEmbed = 8;

    public void SetMaps(IEnumerable<Map> maps, string size, bool? inRandomPool, bool isFirstPage)
    {
        if (isFirstPage)
        {
            string title = "Maps";
            if (size != "all") title += $" ({size})";
            if (inRandomPool.HasValue)
                title += inRandomPool.Value ? " (In Random Pool)" : " (Not In Random Pool)";
            Title = title;
        }

        var fields = new List<IEmbedField>();
        foreach (var map in maps)
        {
            fields.Add(new EmbedField { Name = "Name", Value = map.Name, IsInline = true });
            fields.Add(new EmbedField { Name = "Size", Value = map.Size ?? "Unknown", IsInline = true });
            fields.Add(new EmbedField { Name = "In Random Pool", Value = map.IsInRandomPool ? "Yes" : "No", IsInline = true });
        }

        Fields = fields;
    }
}