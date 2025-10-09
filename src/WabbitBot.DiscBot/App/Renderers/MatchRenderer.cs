using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Services;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Services.DiscBot;
using WabbitBot.DiscBot.App.Utilities;

namespace WabbitBot.DiscBot.App.Renderers
{
    /// <summary>
    /// Renderer for match-related Discord operations (threads, containers).
    /// Accepts concrete Discord parameters and performs rendering logic.
    /// Does NOT subscribe to events - that's the Handler's job.
    /// </summary>
    public static class MatchRenderer
    {
        /// <summary>
        /// Renders a match container in the specified threads.
        /// </summary>
        /// <param name="client">Discord client</param>
        /// <param name="channel">Channel to post container in</param>
        /// <param name="matchId">Match ID</param>
        /// <returns>Result indicating success/failure</returns>
        public static async Task<Result> RenderMatchContainerAsync(
            DiscordClient client,
            DiscordChannel channel,
            DiscordMessageBuilder challengerBuilder,
            DiscordMessageBuilder opponentBuilder,
            List<Player> selectedTeam1Players,
            List<Player> selectedTeam2Players,
            Guid matchId
        )
        {
            try
            {
                // Fetch match data
                var matchResult = await CoreService.Matches.GetByIdAsync(matchId, DatabaseComponent.Repository);
                if (!matchResult.Success)
                {
                    return Result.Failure("Match not found");
                }
                var match = matchResult.Data!;

                // Fetch team1 data
                var challengerTeamResult = await CoreService.Teams.GetByIdAsync(
                    match.Team1Id,
                    DatabaseComponent.Repository
                );
                if (!challengerTeamResult.Success)
                {
                    return Result.Failure("Challenger team not found");
                }
                var challengerTeam = challengerTeamResult.Data!;
                var challengerRating = (int)Math.Round(challengerTeam.ScrimmageTeamStats[match.TeamSize].CurrentRating);
                var challengerTeamInfo = new Dictionary<ulong, Dictionary<string, string>>();
                for (int i1 = 0; i1 < selectedTeam1Players.Count; i1++)
                {
                    var team1Player = selectedTeam1Players.ElementAt(i1);
                    var team1User = team1Player.MashinaUser;
                    challengerTeamInfo[team1User.DiscordUserId] = new Dictionary<string, string>
                    {
                        { "DiscordMention", team1User.DiscordMention ?? "<@unknown>" },
                        { "DiscordUsername", team1User.DiscordUsername ?? "Unknown" },
                        { "DiscordUserId", team1User.DiscordUserId.ToString() },
                    };
                }
                var challengerMentions = new List<string>();
                foreach (var info in challengerTeamInfo)
                {
                    challengerMentions.Add(info.Value["DiscordMention"]);
                }

                // Fetch team2 data
                var opponentTeamResult = await CoreService.Teams.GetByIdAsync(
                    match.Team2Id,
                    DatabaseComponent.Repository
                );
                if (!opponentTeamResult.Success)
                {
                    return Result.Failure("Opponent team not found");
                }
                var opponentTeam = opponentTeamResult.Data!;
                var opponentRating = (int)Math.Round(opponentTeam.ScrimmageTeamStats[match.TeamSize].CurrentRating);
                var opponentTeamInfo = new Dictionary<ulong, Dictionary<string, string>>();
                for (int i2 = 0; i2 < selectedTeam2Players.Count; i2++)
                {
                    var team2Player = selectedTeam2Players.ElementAt(i2);
                    var team2User = team2Player.MashinaUser;
                    opponentTeamInfo[team2User.DiscordUserId] = new Dictionary<string, string>
                    {
                        { "DiscordMention", team2User.DiscordMention ?? "<@unknown>" },
                        { "DiscordUsername", team2User.DiscordUsername ?? "Unknown" },
                        { "DiscordUserId", team2User.DiscordUserId.ToString() },
                    };
                }
                var opponentMentions = new List<string>();
                foreach (var info in opponentTeamInfo)
                {
                    opponentMentions.Add(info.Value["DiscordMention"]);
                }

                // Placeholder for map pool (use match.AvailableMaps or fetch)
                var mapPool =
                    match.AvailableMaps
                    ?? new List<string> { "Echeneis", "Glittering Lagoon", "Silent Sanctum", "Thornwood" }; // Placeholder
                var teamSize = match.TeamSize;
                var matchLength = match.BestOf != 0 ? match.BestOf : 3; // Assume bo3, placeholder - adjust property name

                // Placeholder for map ban config retrieval
                // var banConfig = await GetMapBanConfigAsync(teamSize, matchLength); // Placeholder
                var guaranteedBans = 2; // Placeholder
                var coinflipBans = 1; // Placeholder

                // Map status emojis (placeholder)
                var statusEmoji = "🟢"; // Available, change based on bans

                // Build map pool status text
                var mapPoolText = "**Map Pool:**\n";
                foreach (var map in mapPool)
                {
                    mapPoolText += $" - {statusEmoji} {map},\n";
                }
                mapPoolText += "**Ban status:**\n";
                mapPoolText += $"{challengerTeam.Name} In Progress\n";
                mapPoolText += $"{opponentTeam.Name} In Progress\n";

                // Placeholder for ban instructions
                var banInstructions =
                    $"Please select your map bans in order of priority. You will have a chance to preview\n"
                    + $"your selections before confirming. You have {guaranteedBans} guaranteed bans, and \n"
                    + $"{coinflipBans} coinflip bans. Coinflip bans come into play depending on the number of\n"
                    + $"games played."; // Placeholder

                // Build components
                var challengerContainerComponents = new List<DiscordComponent>();
                var opponentContainerComponents = new List<DiscordComponent>();

                // 1. Top banner - placeholder for MediaGalleryBuilderComponent
                // var topBanner = new MediaGalleryBuilderComponent("match_banner"); // Placeholder
                // challengerContainerComponents.Add(topBanner);
                // opponentContainerComponents.Add(topBanner);

                // 2. Match info text
                var challengerMatchInfoText =
                    $"## {challengerTeam.Name} ({challengerRating})\n vs. {opponentTeam.Name} ({opponentRating})\n"
                    + $"{string.Join(" ", challengerMentions)}\n"
                    + $"**Best of {matchLength}**";
                challengerContainerComponents.Add(new DiscordTextDisplayComponent(challengerMatchInfoText));
                var opponentMatchInfoText =
                    $"## {opponentTeam.Name} ({opponentRating})\n vs. {challengerTeam.Name} ({challengerRating})\n"
                    + $"{string.Join(" ", opponentMentions)}\n"
                    + $"**Best of {matchLength}**";
                opponentContainerComponents.Add(new DiscordTextDisplayComponent(opponentMatchInfoText));

                // 3. Map ban banner - placeholder
                // var mapBanBanner = new MediaGalleryBuilderComponent("map_ban_banner"); // Placeholder
                // challengerContainerComponents.Add(mapBanBanner);
                // opponentContainerComponents.Add(mapBanBanner);

                // 4. Map pool status text
                challengerContainerComponents.Add(new DiscordTextDisplayComponent(mapPoolText));
                opponentContainerComponents.Add(new DiscordTextDisplayComponent(mapPoolText));

                // 5. Map thumbnails gallery - placeholder for DiscordMediaGalleryBuilderComponent
                // var mapThumbnails = new DiscordMediaGalleryBuilderComponent(mapPool); // Placeholder
                // challengerContainerComponents.Add(mapThumbnails);
                // opponentContainerComponents.Add(mapThumbnails);

                // 6. Instructions text
                var instructionsText =
                    $"## Map bans - {challengerTeam.Name} vs." + $"{opponentTeam.Name}\n\n{banInstructions}";
                challengerContainerComponents.Add(new DiscordTextDisplayComponent(instructionsText));
                opponentContainerComponents.Add(new DiscordTextDisplayComponent(instructionsText));

                // 7. Select menu for bans (custom id based on team thread to distinguish - but since same content, use generic for now, handler will distinguish)
                var selectId = $"ban_select_{matchId}_{challengerTeam.Id}"; // Customize per thread
                var selectOptions = mapPool
                    .Select(map => new DiscordSelectComponentOption(map, map, isDefault: false))
                    .ToList();
                var banSelect = new DiscordSelectComponent(
                    selectId,
                    "Select maps to ban",
                    selectOptions,
                    minOptions: 1,
                    maxOptions: guaranteedBans + coinflipBans
                );

                var actionRow = new DiscordActionRowComponent(new DiscordComponent[] { banSelect });
                challengerContainerComponents.Add(actionRow);
                opponentContainerComponents.Add(actionRow);

                // Add refresh button for other team view
                var refreshButton = new DiscordButtonComponent(
                    DiscordButtonStyle.Secondary,
                    $"refresh_bans_{matchId}",
                    "Refresh"
                );
                challengerContainerComponents.Add(refreshButton);
                opponentContainerComponents.Add(refreshButton);

                // Add start and cancel buttons (keep for now)
                var startButton = new DiscordButtonComponent(
                    DiscordButtonStyle.Primary,
                    $"start_match_{matchId}",
                    "Start Match"
                );
                var cancelButton = new DiscordButtonComponent(
                    DiscordButtonStyle.Danger,
                    $"cancel_match_{matchId}",
                    "Cancel Match"
                );
                challengerContainerComponents.Add(startButton);
                opponentContainerComponents.Add(startButton);
                challengerContainerComponents.Add(cancelButton);
                opponentContainerComponents.Add(cancelButton);

                var challengerContainer = new DiscordContainerComponent(challengerContainerComponents);
                var opponentContainer = new DiscordContainerComponent(opponentContainerComponents);

                // Send to both team threads (same content for now, refresh will update status)
                challengerBuilder.AddContainerComponent(challengerContainer);
                opponentBuilder.AddContainerComponent(opponentContainer);

                // Publish MatchProvisioned (update event to include both threads if needed)
                // await DiscBotService.PublishAsync(new MatchProvisioned(
                //     matchId,
                //     ));

                return Result.CreateSuccess("Match container created");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to render match container for {matchId}",
                    nameof(RenderMatchContainerAsync)
                );
                return Result.Failure($"Failed to create match container: {ex.Message}");
            }
        }

        // Old DM rendering removed - map bans now handled in threads
    }
}
