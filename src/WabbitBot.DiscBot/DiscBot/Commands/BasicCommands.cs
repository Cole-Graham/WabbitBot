using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using WabbitBot.Common.Attributes;
using WabbitBot.Core.Common.Handlers;
using WabbitBot.Core.Common.Models;
using WabbitBot.DiscBot.Commands.Providers;
using WabbitBot.DiscBot.DSharpPlus.Embeds;
using WabbitBot.DiscBot.DSharpPlus.Generated;

namespace WabbitBot.DiscBot.Commands;

[WabbitCommand("maps")]
[Description("Map management commands")]
public partial class BasicCommands
{
    private static readonly MapHandler MapHandler = MapHandler.Instance;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    #region Basic Map Commands

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
            _ => null,
        };

        var maps = MapHandler.GetMaps(size, inRandomPool);
        if (!maps.Any())
        {
            await ctx.EditResponseAsync("No maps found matching the specified criteria.");
            return;
        }

        var mapsArray = maps.ToArray();
        var embeds = new List<DiscordEmbed>();

        for (int i = 0; i < mapsArray.Length; i += MapListEmbed.MapsPerEmbed)
        {
            var chunk = mapsArray.Skip(i).Take(MapListEmbed.MapsPerEmbed);
            var embed = EmbedFactories.CreateEmbed<MapListEmbed>();
            embed.SetMaps(chunk, size, inRandomPool, i == 0);
            embeds.Add(embed.ToEmbedBuilder());
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embeds[0]));

        for (int i = 1; i < embeds.Count; i++)
        {
            await ctx.FollowupAsync(new DiscordWebhookBuilder().AddEmbed(embeds[i]));
        }
    }

    [WabbitCommand("random")]
    [Description("Get a random map from the pool")]
    public async Task RandomMapAsync(CommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        var map = MapHandler.GetRandomMap();
        if (map == null)
        {
            await ctx.EditResponseAsync("No maps found in the random pool");
            return;
        }

        var embed = EmbedFactories.CreateEmbed<MapEmbed>();
        embed.SetMap(map, "Random map from the pool");
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed.ToEmbedBuilder()));
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

        var map = MapHandler.GetMapByName(mapName);
        if (map == null)
        {
            await ctx.EditResponseAsync($"Map '{mapName}' not found.");
            return;
        }

        var embed = EmbedFactories.CreateEmbed<MapEmbed>();
        embed.SetMap(map, $"Details for {map.Name}");
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed.ToEmbedBuilder()));
    }

    #endregion

    #region Admin Map Commands

    [WabbitCommand("add")]
    [Description("Add or update a map")]
    [RequirePermissions(DiscordPermission.Administrator)]
    public async Task AddMapAsync(
        CommandContext ctx,
        [Description("Map name")] string name,
        [Description("Map size (e.g., 1v1)")] string size,
        [Description("Thumbnail URL (optional)")] string? thumbnail = null,
        [Description("Include in random pool")] bool inRandomPool = true,
        [Description("Include in tournament pool")] bool inTournamentPool = true)
    {
        await ctx.DeferResponseAsync();

        var map = new Map
        {
            Name = name,
            Size = size,
            ThumbnailFilename = thumbnail,
            IsInRandomPool = inRandomPool,
            IsInTournamentPool = inTournamentPool,
        };

        await MapHandler.AddOrUpdateMapAsync(map);

        var embed = EmbedFactories.CreateEmbed<MapEmbed>();
        embed.SetMap(map, $"Map '{name}' has been added/updated successfully.");
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed.ToEmbedBuilder()));
    }

    [WabbitCommand("export")]
    [Description("Export current maps configuration")]
    [RequirePermissions(DiscordPermission.Administrator)]
    public async Task ExportMapsAsync(CommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        var maps = MapHandler.GetMaps();
        var json = JsonSerializer.Serialize(maps, JsonOptions);

        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(json);
        await writer.FlushAsync();
        stream.Position = 0;

        var message = new DiscordWebhookBuilder()
            .WithContent("Current maps configuration:")
            .AddFile("maps.json", stream);

        await ctx.EditResponseAsync(message);
    }

    [WabbitCommand("import")]
    [Description("Import maps configuration from a JSON file")]
    [RequirePermissions(DiscordPermission.Administrator)]
    public async Task ImportMapsAsync(
        CommandContext ctx,
        [Description("The maps.json file to import")]
        DiscordAttachment attachment)
    {
        await ctx.DeferResponseAsync();

        try
        {
            using var client = new HttpClient();
            var json = await client.GetStringAsync(attachment.Url);
            await MapHandler.ImportMapsAsync(json);

            await ctx.EditResponseAsync("Maps configuration imported successfully.");
        }
        catch (Exception ex)
        {
            await ctx.EditResponseAsync($"Error importing maps: {ex.Message}");
        }
    }

    [WabbitCommand("remove")]
    [Description("Remove a map")]
    [RequirePermissions(DiscordPermission.Administrator)]
    public async Task RemoveMapAsync(
        CommandContext ctx,
        [Parameter("name"), Description("Map name")]
        [SlashAutoCompleteProvider(typeof(MapNameAutoCompleteProvider))]
        string name)
    {
        await ctx.DeferResponseAsync();

        var map = MapHandler.GetMapByName(name);
        if (map is null)
        {
            await ctx.EditResponseAsync($"Map '{name}' not found.");
            return;
        }

        await MapHandler.RemoveMapAsync(name);
        await ctx.EditResponseAsync($"Map '{name}' has been removed.");
    }

    #endregion
}