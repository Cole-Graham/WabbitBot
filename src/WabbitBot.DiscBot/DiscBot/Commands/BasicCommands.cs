using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Services;
using WabbitBot.DiscBot.Commands.Providers;

namespace WabbitBot.DiscBot.Commands;

[WabbitCommand("maps")]
[Description("Map management commands")]
public partial class BasicCommands
{
    private static readonly MapService MapService = MapService.Instance;

    [WabbitCommand("random")]
    [Description("Get a random map from the pool")]
    public async Task RandomMapAsync(CommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        var map = MapService.GetRandomMap();
        if (map == null)
        {
            await ctx.EditResponseAsync("No maps found in the random pool");
            return;
        }

        var embed = CreateMapEmbed(map, "Random map from the pool");
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    [WabbitCommand("list")]
    [Description("List all available maps")]
    public async Task ListMapsAsync(
        CommandContext ctx,
        [Description("Filter by size (e.g., 1v1 to 4v4, or 'all' for all sizes)")]
        [ChoiceProvider(typeof(MapSizeChoiceProvider))]
        string size = "all",
        [Description("Show only maps in random pool (-1=all, 0=no, 1=yes)")]
        int inRandomPoolInt = -1)
    {
        await ctx.DeferResponseAsync();

        bool? inRandomPool = inRandomPoolInt switch
        {
            0 => false,
            1 => true,
            _ => null
        };

        var maps = MapService.GetMaps(size, inRandomPool);
        if (!maps.Any())
        {
            await ctx.EditResponseAsync("No maps found matching the specified criteria.");
            return;
        }

        var embeds = CreateMapListEmbeds(maps, size, inRandomPool);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embeds[0]));

        for (int i = 1; i < embeds.Count; i++)
        {
            await ctx.FollowupAsync(new DiscordWebhookBuilder().AddEmbed(embeds[i]));
        }
    }

    [WabbitCommand("show")]
    [Description("Show details for a specific map")]
    public async Task ShowMapAsync(
        CommandContext ctx,
        [Description("Map name")]
        [AutoCompleteProvider(typeof(MapNameAutoCompleteProvider))]
        string mapName)
    {
        await ctx.DeferResponseAsync();

        var map = MapService.GetMapByName(mapName);
        if (map == null)
        {
            await ctx.EditResponseAsync($"Map '{mapName}' not found.");
            return;
        }

        var embed = CreateMapEmbed(map, $"Details for {map.Name}");
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    private static DiscordEmbed CreateMapEmbed(Map map, string description)
    {
        var embed = new DiscordEmbedBuilder()
            .WithTitle(map.Name)
            .WithDescription(description)
            .AddField("Size", map.Size ?? "Unknown", true)
            .AddField("In Random Pool", map.IsInRandomPool ? "Yes" : "No", true)
            .AddField("In Tournament Pool", map.IsInTournamentPool ? "Yes" : "No", true);

        if (!string.IsNullOrEmpty(map.Thumbnail))
        {
            embed.WithThumbnail(map.Thumbnail);
        }

        return embed;
    }

    private static List<DiscordEmbed> CreateMapListEmbeds(IEnumerable<Map> maps, string size, bool? inRandomPool)
    {
        var embeds = new List<DiscordEmbed>();
        var mapsArray = maps.ToArray();
        int mapsPerEmbed = 8;

        for (int i = 0; i < mapsArray.Length; i += mapsPerEmbed)
        {
            var embed = new DiscordEmbedBuilder();
            if (i == 0)
            {
                string title = "Maps";
                if (size != "all") title += $" ({size})";
                if (inRandomPool.HasValue)
                    title += inRandomPool.Value ? " (In Random Pool)" : " (Not In Random Pool)";
                embed.WithTitle(title);
            }

            var chunk = mapsArray.Skip(i).Take(mapsPerEmbed);
            foreach (var map in chunk)
            {
                embed.AddField("Name", map.Name, true);
                embed.AddField("Size", map.Size ?? "Unknown", true);
                embed.AddField("In Random Pool", map.IsInRandomPool ? "Yes" : "No", true);
            }

            embeds.Add(embed);
        }

        return embeds;
    }
}