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
        public static async Task<Result<MatchContainersResult>> RenderScrimmageMatchContainersAsync(
            Guid matchId,
            Player[] ChallengerTeamPlayers,
            Player[] OpponentTeamPlayers
        )
        {
            try
            {
                var getMatch = await CoreService.Matches.GetByIdAsync(matchId, DatabaseComponent.Repository);
                if (!getMatch.Success)
                {
                    return Result<MatchContainersResult>.Failure("Match not found");
                }
                var Match = getMatch.Data!;
                var getChallengerTeam = await CoreService.Teams.GetByIdAsync(
                    Match.Team1Id,
                    DatabaseComponent.Repository
                );
                if (!getChallengerTeam.Success || getChallengerTeam.Data is null)
                {
                    return Result<MatchContainersResult>.Failure("Challenger team not found or null");
                }
                var ChallengerTeam = getChallengerTeam.Data;
                var challengerRating = (int)Math.Round(ChallengerTeam.ScrimmageTeamStats[Match.TeamSize].CurrentRating);
                var challengerTeamInfo = new Dictionary<ulong, Dictionary<string, string>>();
                for (int i1 = 0; i1 < ChallengerTeamPlayers.Length; i1++)
                {
                    var team1Player = ChallengerTeamPlayers.ElementAt(i1);
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
                var getOpponentTeam = await CoreService.Teams.GetByIdAsync(Match.Team2Id, DatabaseComponent.Repository);
                if (!getOpponentTeam.Success || getOpponentTeam.Data is null)
                {
                    return Result<MatchContainersResult>.Failure("Opponent team not found or null");
                }
                var OpponentTeam = getOpponentTeam.Data;
                var opponentRating = (int)Math.Round(OpponentTeam.ScrimmageTeamStats[Match.TeamSize].CurrentRating);
                var opponentTeamInfo = new Dictionary<ulong, Dictionary<string, string>>();
                for (int i2 = 0; i2 < OpponentTeamPlayers.Length; i2++)
                {
                    var team2Player = OpponentTeamPlayers.ElementAt(i2);
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
                    Match.AvailableMaps
                    ?? new List<string> { "Echeneis", "Glittering Lagoon", "Silent Sanctum", "Thornwood" }; // Placeholder
                var teamSize = Match.TeamSize;
                var matchLength = Match.BestOf != 0 ? Match.BestOf : 3; // Assume bo3, placeholder - adjust property name

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
                mapPoolText += $"{ChallengerTeam.Name} In Progress\n";
                mapPoolText += $"{OpponentTeam.Name} In Progress\n";

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
                    $"## {ChallengerTeam.Name} ({challengerRating})\n vs. {OpponentTeam.Name} ({opponentRating})\n"
                    + $"{string.Join(" ", challengerMentions)}\n"
                    + $"**Best of {matchLength}**";
                challengerContainerComponents.Add(new DiscordTextDisplayComponent(challengerMatchInfoText));
                var opponentMatchInfoText =
                    $"## {OpponentTeam.Name} ({opponentRating})\n vs. {ChallengerTeam.Name} ({challengerRating})\n"
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
                    $"## Map bans - {ChallengerTeam.Name} vs." + $"{OpponentTeam.Name}\n\n{banInstructions}";
                challengerContainerComponents.Add(new DiscordTextDisplayComponent(instructionsText));
                opponentContainerComponents.Add(new DiscordTextDisplayComponent(instructionsText));

                // 7. Select menu for bans (custom id based on team thread to distinguish - but since same content, use generic for now, handler will distinguish)
                var selectId = $"ban_select_{matchId}_{ChallengerTeam.Id}"; // Customize per thread
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

                return Result<MatchContainersResult>.CreateSuccess(
                    new MatchContainersResult(challengerContainer, opponentContainer),
                    "Match containers created"
                );
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to render match container for {matchId}",
                    nameof(RenderScrimmageMatchContainersAsync)
                );
                return Result<MatchContainersResult>.Failure($"Failed to create match container: {ex.Message}");
            }
        }

        // Old DM rendering removed - map bans now handled in threads
    }

    #region Result Types
    public record MatchContainersResult(
        DiscordContainerComponent ChallengerContainer,
        DiscordContainerComponent OpponentContainer
    );
    #endregion
}
