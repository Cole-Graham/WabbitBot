using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;
using System.ComponentModel;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Commands;
using WabbitBot.Core.Common.Services;
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
        private static readonly WabbitBot.Core.Common.Services.FileSystemService FileSystemService = new();

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

            if (!result.Success || result.Data?.Count == 0)
            {
                await ctx.EditResponseAsync("No maps found matching the specified criteria.");
                return;
            }

            var mapsArray = result.Data!.ToArray();
            var embeds = new List<DiscordEmbed>();

            for (int i = 0; i < mapsArray.Length; i += MapListEmbed.MapsPerEmbed)
            {
                var chunk = mapsArray.Skip(i).Take(MapListEmbed.MapsPerEmbed);
                var embed = EmbedFactories.CreateEmbed<MapListEmbed>();
                var inRandomPoolValue = result.Metadata?.GetValueOrDefault("InRandomPool")?.ToString();
                bool? inRandomPool = inRandomPoolValue switch
                {
                    "True" => true,
                    "False" => false,
                    _ => null
                };

                embed.SetMaps(chunk,
                    result.Metadata?.GetValueOrDefault("Size")?.ToString() ?? "all",
                    inRandomPool,
                    i == 0);
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
            embed.SetMap(result.Data!, "Random map from the pool");
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
            embed.SetMap(result.Data!, $"Details for {result.Data?.Name ?? mapName}");
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
            var result = await MapCommands.AddOrUpdateMapAsync(name, size, thumbnail, inRandomPool, inTournamentPool);

            var embed = EmbedFactories.CreateEmbed<MapEmbed>();
            embed.SetMap(result.Data!, "Map updated successfully");
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed.ToEmbedBuilder()));
        }

        [Command("export")]
        [Description("Export current maps configuration")]
        [RequirePermissions(DiscordPermission.Administrator)]
        public async Task ExportMapsAsync(CommandContext ctx)
        {
            await ctx.DeferResponseAsync();

            // Call business logic
            var result = MapCommands.ExportMaps();

            if (!result.Success)
            {
                await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to export maps");
                return;
            }

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            await writer.WriteAsync(result.Data!);
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

            await ctx.EditResponseAsync(result.Success ? "Map removed successfully" : (result.ErrorMessage ?? "Failed to remove map"));
        }

        [Command("upload-thumbnail")]
        [Description("Upload a thumbnail for a map")]
        [RequirePermissions(DiscordPermission.Administrator)]
        public async Task UploadThumbnailAsync(
            CommandContext ctx,
            [Description("Name of the map to add thumbnail to")]
            [SlashAutoCompleteProvider(typeof(MapNameAutoCompleteProvider))]
            string mapName,
            [Description("Image file to upload as thumbnail")]
            DiscordAttachment imageFile)
        {
            await ctx.DeferResponseAsync();

            try
            {
                // Validate map exists
                var mapResult = MapCommands.GetMapByName(mapName);
                if (!mapResult.Success || mapResult.Data == null)
                {
                    await ctx.EditResponseAsync($"Map '{mapName}' not found.");
                    return;
                }

                // Download and validate the file
                using var httpClient = new HttpClient();
                using var fileStream = await httpClient.GetStreamAsync(imageFile.Url);
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Validate and save the image
                var secureFileName = await FileSystemService.ValidateAndSaveImageAsync(
                    memoryStream,
                    imageFile.FileName ?? "unknown",
                    imageFile.MediaType ?? "application/octet-stream");

                if (secureFileName == null)
                {
                    await ctx.EditResponseAsync("❌ Failed to upload image. Please ensure it's a valid image file (JPG, PNG, GIF, WebP) under 1MB.");
                    return;
                }

                // Update map with new thumbnail using business logic
                var updateResult = await MapCommands.AddOrUpdateMapAsync(mapName, mapResult.Data!.Size ?? "unknown", secureFileName, mapResult.Data.IsInRandomPool, mapResult.Data.IsInTournamentPool);

                if (updateResult.Success)
                {
                    var embed = EmbedFactories.CreateEmbed<MapEmbed>();
                    embed.SetMap(updateResult.Data!, $"✅ Thumbnail uploaded successfully for map '{mapName}'!");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed.ToEmbedBuilder()));
                }
                else
                {
                    await ctx.EditResponseAsync($"❌ Error updating map: {updateResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                await ctx.EditResponseAsync($"❌ Error uploading thumbnail: {ex.Message}");
            }
        }

        [Command("remove-thumbnail")]
        [Description("Remove thumbnail from a map")]
        [RequirePermissions(DiscordPermission.Administrator)]
        public async Task RemoveThumbnailAsync(
            CommandContext ctx,
            [Description("Name of the map to remove thumbnail from")]
            [SlashAutoCompleteProvider(typeof(MapNameAutoCompleteProvider))]
            string mapName)
        {
            await ctx.DeferResponseAsync();

            try
            {
                // Validate map exists
                var mapResult = MapCommands.GetMapByName(mapName);
                if (!mapResult.Success || mapResult.Data == null)
                {
                    await ctx.EditResponseAsync($"Map '{mapName}' not found.");
                    return;
                }

                if (string.IsNullOrEmpty(mapResult.Data.ThumbnailFilename))
                {
                    await ctx.EditResponseAsync($"Map '{mapName}' doesn't have a thumbnail.");
                    return;
                }

                // Update map to remove thumbnail using business logic
                var updateResult = await MapCommands.AddOrUpdateMapAsync(mapName, mapResult.Data.Size ?? "unknown", null, mapResult.Data.IsInRandomPool, mapResult.Data.IsInTournamentPool);

                if (updateResult.Success)
                {
                    // Delete the file
                    await FileSystemService.DeleteThumbnailAsync(mapResult.Data.ThumbnailFilename);

                    var embed = EmbedFactories.CreateEmbed<MapEmbed>();
                    embed.SetMap(updateResult.Data!, $"✅ Thumbnail removed from map '{mapName}'!");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed.ToEmbedBuilder()));
                }
                else
                {
                    await ctx.EditResponseAsync($"❌ Error updating map: {updateResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                await ctx.EditResponseAsync($"❌ Error removing thumbnail: {ex.Message}");
            }
        }

        [Command("list-thumbnails")]
        [Description("List all maps with their thumbnails")]
        public async Task ListThumbnailsAsync(CommandContext ctx)
        {
            await ctx.DeferResponseAsync();

            try
            {
                // Call business logic
                var result = MapCommands.ListMaps("all", -1);
                var mapsWithThumbnails = result.Data?.Where(m => !string.IsNullOrEmpty(m.ThumbnailFilename)).ToList() ?? new List<Map>();

                if (!mapsWithThumbnails.Any())
                {
                    await ctx.EditResponseAsync("No maps have thumbnails configured.");
                    return;
                }

                var embed = new DiscordEmbedBuilder()
                    .WithTitle("Maps with Thumbnails")
                    .WithColor(DiscordColor.Blue);

                foreach (var map in mapsWithThumbnails)
                {
                    embed.AddField(map.Name, $"Size: {map.Size}\nThumbnail: {map.ThumbnailFilename}", true);
                }

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception ex)
            {
                await ctx.EditResponseAsync($"❌ Error listing thumbnails: {ex.Message}");
            }
        }

        #endregion
    }
}