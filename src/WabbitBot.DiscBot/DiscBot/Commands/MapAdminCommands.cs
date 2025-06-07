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
using WabbitBot.DiscBot.Commands;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.DiscBot.Commands;

[WabbitCommand("maps")]
[RequirePermissions(DiscordPermission.Administrator)]
public class MapAdminCommands
{
    private static readonly MapService MapService = MapService.Instance;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [WabbitCommand("export")]
    [Description("Export current maps configuration")]
    public async Task ExportMapsAsync(CommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        var maps = MapService.GetMaps();
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
            await MapService.ImportMapsAsync(json);

            await ctx.EditResponseAsync("Maps configuration imported successfully.");
        }
        catch (Exception ex)
        {
            await ctx.EditResponseAsync($"Error importing maps: {ex.Message}");
        }
    }

    [WabbitCommand("add")]
    [Description("Add or update a map")]
    public async Task AddMapAsync(
        CommandContext ctx,
        [Description("Map name")] string name,
        [Description("Map size (e.g., 1v1)")] string size,
        [Description("Map ID")] string id,
        [Description("Thumbnail URL (optional)")] string? thumbnail = null,
        [Description("Include in random pool")] bool inRandomPool = true,
        [Description("Include in tournament pool")] bool inTournamentPool = true)
    {
        await ctx.DeferResponseAsync();

        var map = new Map
        {
            Name = name,
            Id = id,
            Size = size,
            Thumbnail = thumbnail,
            IsInRandomPool = inRandomPool,
            IsInTournamentPool = inTournamentPool
        };

        await MapService.AddOrUpdateMapAsync(map);

        var embed = new DiscordEmbedBuilder()
            .WithTitle("Map Added/Updated")
            .WithDescription($"Map '{name}' has been added/updated successfully.")
            .AddField("Size", size, true)
            .AddField("In Random Pool", inRandomPool ? "Yes" : "No", true)
            .AddField("In Tournament Pool", inTournamentPool ? "Yes" : "No", true);

        if (!string.IsNullOrEmpty(thumbnail))
        {
            embed.WithThumbnail(thumbnail);
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    [WabbitCommand("remove")]
    [Description("Remove a map")]
    public async Task RemoveMapAsync(
        CommandContext ctx,
        [Parameter("name"), Description("Map name")]
        [SlashAutoCompleteProvider(typeof(MapNameAutocompleteProvider))]
        string name)
    {
        await ctx.DeferResponseAsync();

        var map = MapService.GetMapByName(name);
        if (map is null)
        {
            await ctx.EditResponseAsync($"Map '{name}' not found.");
            return;
        }

        await MapService.RemoveMapAsync(name);
        await ctx.EditResponseAsync($"Map '{name}' has been removed.");
    }
}