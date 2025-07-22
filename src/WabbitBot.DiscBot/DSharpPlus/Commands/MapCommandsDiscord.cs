using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using System.Text.Json;
using System.ComponentModel;
using WabbitBot.DiscBot.DiscBot.Commands;
using WabbitBot.DiscBot.DSharpPlus.Embeds;
using WabbitBot.DiscBot.DSharpPlus.Generated;

namespace WabbitBot.DiscBot.DSharpPlus.Commands
{
    /// <summary>
    /// Discord integration for map commands - handles Discord-specific code and calls business logic
    /// </summary>
    [Command("Maps")]
    [Description("Map management commands")]
    public partial class MapCommandsDiscord
    {
        private static readonly MapCommands MapCommands = new();

        #region Discord Command Handlers

        [Command("list")]
        [Description("List all available maps")]
        public async Task ListMapsAsync(
            CommandContext ctx,
            [Description("Filter by size (e.g., 1v1 to 4v4, or 'all' for all sizes)")]
            [SlashChoiceProvider(typeof(MapSizeChoiceProvider))]
            string size = "all",
            [Description("Show only maps in random pool (-1=all, 0=no, 1=yes)")]
            int inRandomPoolInt = -1)
        {
            await ctx.DeferResponseAsync();

            // Call business logic
            var result = MapCommands.ListMaps(size, inRandomPoolInt);

            if (!result.HasMaps)
            {
                await ctx.EditResponseAsync("No maps found matching the specified criteria.");
                return;
            }

            var mapsArray = result.Maps.ToArray();
            var embeds = new List<DiscordEmbed>();

            for (int i = 0; i < mapsArray.Length; i += MapListEmbed.MapsPerEmbed)
            {
                var chunk = mapsArray.Skip(i).Take(MapListEmbed.MapsPerEmbed);
                var embed = EmbedFactories.CreateEmbed<MapListEmbed>();
                embed.SetMaps(chunk, result.Size, result.InRandomPool, i == 0);
                embeds.Add(embed.ToEmbedBuilder());
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embeds[0]));

            for (int i = 1; i < embeds.Count; i++)
            {
                await ctx.FollowupAsync(new DiscordWebhookBuilder().AddEmbed(embeds[i]));
            }
        }

        [Command("random")]
        [Description("Get a random map from the pool")]
        public async Task RandomMapAsync(CommandContext ctx)
        {
            await ctx.DeferResponseAsync();

            // Call business logic
            var result = MapCommands.GetRandomMap();

            if (!result.Success)
            {
                await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to get random map");
                return;
            }

            var embed = EmbedFactories.CreateEmbed<MapEmbed>();
            embed.SetMap(result.Map!, "Random map from the pool");
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed.ToEmbedBuilder()));
        }

        [Command("display")]
        [Description("Show details for a specific map")]
        public async Task DisplayMapAsync(
            CommandContext ctx,
            [Description("Map name")]
            [SlashAutoCompleteProvider(typeof(MapNameAutoCompleteProvider))]
            string mapName)
        {
            await ctx.DeferResponseAsync();

            // Call business logic
            var result = MapCommands.GetMapByName(mapName);

            if (!result.Success)
            {
                await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to get map");
                return;
            }

            var embed = EmbedFactories.CreateEmbed<MapEmbed>();
            embed.SetMap(result.Map!, $"Details for {result.Map?.Name ?? mapName}");
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed.ToEmbedBuilder()));
        }

        [Command("add")]
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

            // Call business logic
            var result = MapCommands.AddOrUpdateMap(name, size, thumbnail, inRandomPool, inTournamentPool);

            var embed = EmbedFactories.CreateEmbed<MapEmbed>();
            embed.SetMap(result.Map!, result.Message ?? "Map updated successfully");
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed.ToEmbedBuilder()));
        }

        [Command("export")]
        [Description("Export current maps configuration")]
        [RequirePermissions(DiscordPermission.Administrator)]
        public async Task ExportMapsAsync(CommandContext ctx)
        {
            await ctx.DeferResponseAsync();

            // Call business logic
            var json = MapCommands.ExportMaps();

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

        [Command("import")]
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

                // Call business logic
                var result = await MapCommands.ImportMapsAsync(json);

                await ctx.EditResponseAsync(result.Success ? (result.Message ?? "Maps imported successfully") : (result.ErrorMessage ?? "Failed to import maps"));
            }
            catch (Exception ex)
            {
                await ctx.EditResponseAsync($"Error importing maps: {ex.Message}");
            }
        }

        [Command("remove")]
        [Description("Remove a map")]
        [RequirePermissions(DiscordPermission.Administrator)]
        public async Task RemoveMapAsync(
            CommandContext ctx,
            [Parameter("name"), Description("Map name")]
            [SlashAutoCompleteProvider(typeof(MapNameAutoCompleteProvider))]
            string name)
        {
            await ctx.DeferResponseAsync();

            // Call business logic
            var result = MapCommands.RemoveMap(name);

            await ctx.EditResponseAsync(result.Success ? (result.Message ?? "Map removed successfully") : (result.ErrorMessage ?? "Failed to remove map"));
        }

        #endregion
    }
}