using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using WabbitBot.Common.Configuration;
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
        // Map ban status emojis
        private const string AvailableEmoji = "🟢";
        private const string GuaranteedBanEmoji = "🔴";
        private const string CoinflipBanEmoji = "🟠";

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

                // Get map pool from match or use a default pool
                var mapPool =
                    Match.AvailableMaps
                    ?? new List<string> { "Echeneis", "Glittering Lagoon", "Silent Sanctum", "Thornwood" }; // Placeholder
                var teamSize = Match.TeamSize;
                var matchLength = Match.BestOf != 0 ? Match.BestOf : 3; // Assume bo3, placeholder - adjust property name

                // Retrieve map ban configuration from appsettings.json
                var mapBansConfig = ConfigurationProvider.GetSection<MapBansOptions>(MapBansOptions.SectionName);
                var banConfig = mapBansConfig.GetBanConfig(matchLength, mapPool.Count);
                var guaranteedBans = banConfig.GuaranteedBans;
                var coinflipBans = banConfig.CoinflipBans;

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

                // 1. Top banner (match_banner.jpg)
                var (matchBannerUrl, matchBannerHint, isCdnUrl) =
                    DiscBotService.AssetResolver.ResolveDiscordComponentImage("match_banner.jpg");
                if (matchBannerUrl is not null)
                {
                    challengerContainerComponents.Add(
                        new DiscordMediaGalleryComponent(items: [new DiscordMediaGalleryItem(matchBannerUrl)])
                    );
                    opponentContainerComponents.Add(
                        new DiscordMediaGalleryComponent(items: [new DiscordMediaGalleryItem(matchBannerUrl)])
                    );
                }

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

                // 3. Map ban banner (map_ban_banner.jpg)
                var (mapBanBannerUrl, mapBanBannerHint, isMapBanCdnUrl) =
                    DiscBotService.AssetResolver.ResolveDiscordComponentImage("map_ban_banner.jpg");
                if (mapBanBannerUrl is not null)
                {
                    challengerContainerComponents.Add(
                        new DiscordMediaGalleryComponent(items: [new DiscordMediaGalleryItem(mapBanBannerUrl)])
                    );
                    opponentContainerComponents.Add(
                        new DiscordMediaGalleryComponent(items: [new DiscordMediaGalleryItem(mapBanBannerUrl)])
                    );
                }

                // 4. Map pool status text
                challengerContainerComponents.Add(new DiscordTextDisplayComponent(mapPoolText));
                opponentContainerComponents.Add(new DiscordTextDisplayComponent(mapPoolText));

                // 5. Map thumbnails gallery
                var mapThumbnailItems = new List<DiscordMediaGalleryItem>();
                foreach (var mapName in mapPool)
                {
                    // Resolve map thumbnail (CDN URL or local path)
                    var (thumbnailUrl, thumbnailHint) = await DiscBotService.AssetResolver.ResolveMapThumbnailAsync(
                        mapName
                    );

                    if (thumbnailUrl is not null)
                    {
                        mapThumbnailItems.Add(new DiscordMediaGalleryItem(thumbnailUrl));
                    }
                }

                // Add gallery if we have thumbnails
                if (mapThumbnailItems.Count > 0)
                {
                    var mapThumbnailsGallery = new DiscordMediaGalleryComponent(items: mapThumbnailItems);
                    challengerContainerComponents.Add(mapThumbnailsGallery);
                    opponentContainerComponents.Add(mapThumbnailsGallery);
                }

                // 6. Instructions text
                var instructionsText =
                    $"## Map bans - {ChallengerTeam.Name} vs." + $"{OpponentTeam.Name}\n\n{banInstructions}";
                challengerContainerComponents.Add(new DiscordTextDisplayComponent(instructionsText));
                opponentContainerComponents.Add(new DiscordTextDisplayComponent(instructionsText));

                // 7. Select menu for bans and refresh button in the same action row (per team)
                var selectOptions = mapPool
                    .Select(map => new DiscordSelectComponentOption(map, map, isDefault: false))
                    .ToList();

                // Challenger team select and action row
                var challengerSelectId = $"ban_select_{matchId}_{ChallengerTeam.Id}";
                var challengerBanSelect = new DiscordSelectComponent(
                    challengerSelectId,
                    "Select maps to ban",
                    selectOptions,
                    minOptions: guaranteedBans + coinflipBans,
                    maxOptions: guaranteedBans + coinflipBans
                );
                var challengerRefreshButton = new DiscordButtonComponent(
                    DiscordButtonStyle.Secondary,
                    $"refresh_bans_{matchId}_{ChallengerTeam.Id}",
                    "Refresh"
                );
                var challengerForfeitButton = new DiscordButtonComponent(
                    DiscordButtonStyle.Danger,
                    $"forfeit_match_{matchId}_{ChallengerTeam.Id}",
                    "Forfeit Match"
                );
                var challengerActionRow = new DiscordActionRowComponent(
                    [challengerBanSelect, challengerRefreshButton, challengerForfeitButton]
                );
                challengerContainerComponents.Add(challengerActionRow);

                // Opponent team select and action row
                var opponentSelectId = $"ban_select_{matchId}_{OpponentTeam.Id}";
                var opponentBanSelect = new DiscordSelectComponent(
                    opponentSelectId,
                    "Select maps to ban",
                    selectOptions,
                    minOptions: guaranteedBans + coinflipBans,
                    maxOptions: guaranteedBans + coinflipBans
                );
                var opponentRefreshButton = new DiscordButtonComponent(
                    DiscordButtonStyle.Secondary,
                    $"refresh_bans_{matchId}_{OpponentTeam.Id}",
                    "Refresh"
                );
                var opponentForfeitButton = new DiscordButtonComponent(
                    DiscordButtonStyle.Danger,
                    $"forfeit_match_{matchId}_{OpponentTeam.Id}",
                    "Forfeit Match"
                );
                var opponentActionRow = new DiscordActionRowComponent(
                    [opponentBanSelect, opponentRefreshButton, opponentForfeitButton]
                );
                opponentContainerComponents.Add(opponentActionRow);

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

        /// <summary>
        /// Updates a match container with provisional ban selections by modifying only changed components.
        /// Efficiently reuses existing components (banners, galleries) and only updates text and buttons.
        /// </summary>
        public static async Task<Result<DiscordContainerComponent>> UpdateMatchContainerWithBansAsync(
            DiscordMessage existingMessage,
            Match match,
            Guid teamId,
            string[] selections
        )
        {
            try
            {
                if (existingMessage.Components is null)
                {
                    return Result<DiscordContainerComponent>.Failure("Message components not found");
                }

                // Extract existing container from the message
                var existingContainer = existingMessage.Components.OfType<DiscordContainerComponent>().FirstOrDefault();
                if (existingContainer is null)
                {
                    return Result<DiscordContainerComponent>.Failure("No container found in message");
                }

                // Copy existing components to modify only what changed
                var containerComponents = new List<DiscordComponent>(existingContainer.Components);

                // Determine which team is making selections
                bool isChallengerTeam = teamId == match.Team1Id;

                // Get team names for display
                var getUpdatingTeam = await CoreService.Teams.GetByIdAsync(teamId, DatabaseComponent.Repository);
                if (!getUpdatingTeam.Success || getUpdatingTeam.Data is null)
                {
                    return Result<DiscordContainerComponent>.Failure("Updating team not found");
                }
                var UpdatingTeam = getUpdatingTeam.Data;

                var opposingTeamId = isChallengerTeam ? match.Team2Id : match.Team1Id;
                var getOpposingTeam = await CoreService.Teams.GetByIdAsync(
                    opposingTeamId,
                    DatabaseComponent.Repository
                );
                if (!getOpposingTeam.Success || getOpposingTeam.Data is null)
                {
                    return Result<DiscordContainerComponent>.Failure("Opposing team not found");
                }
                var OpposingTeam = getOpposingTeam.Data;

                // Get map pool and ban configuration
                var mapPool =
                    match.AvailableMaps
                    ?? new List<string> { "Echeneis", "Glittering Lagoon", "Silent Sanctum", "Thornwood" };
                var matchLength = match.BestOf != 0 ? match.BestOf : 3;
                var mapBansConfig = ConfigurationProvider.GetSection<MapBansOptions>(MapBansOptions.SectionName);
                var banConfig = mapBansConfig.GetBanConfig(matchLength, mapPool.Count);
                var guaranteedBans = banConfig.GuaranteedBans;
                var coinflipBans = banConfig.CoinflipBans;

                // Find and replace the map pool status text component
                int mapPoolTextIndex = -1;
                for (int i = 0; i < containerComponents.Count; i++)
                {
                    if (
                        containerComponents[i] is DiscordTextDisplayComponent textComp
                        && textComp.Content.Contains("**Map Pool:**", StringComparison.Ordinal)
                    )
                    {
                        mapPoolTextIndex = i;
                        break;
                    }
                }

                if (mapPoolTextIndex != -1)
                {
                    // Build updated map pool text with new emojis
                    var mapPoolText = "**Map Pool:**\n";
                    for (int i = 0; i < mapPool.Count; i++)
                    {
                        var map = mapPool[i];
                        var selectionIndex = Array.IndexOf(selections, map);
                        string emoji =
                            selectionIndex == -1 ? AvailableEmoji
                            : selectionIndex < guaranteedBans ? GuaranteedBanEmoji
                            : CoinflipBanEmoji;

                        mapPoolText += $" - {emoji} {map},\n";
                    }
                    mapPoolText += "**Ban status:**\n";
                    mapPoolText += $"{UpdatingTeam.Name} Reviewing\n";
                    mapPoolText += $"{OpposingTeam.Name} In Progress\n";

                    containerComponents[mapPoolTextIndex] = new DiscordTextDisplayComponent(mapPoolText);
                }

                // Find and replace the instructions text component
                int instructionsTextIndex = -1;
                for (int i = 0; i < containerComponents.Count; i++)
                {
                    if (
                        containerComponents[i] is DiscordTextDisplayComponent textComp
                        && textComp.Content.Contains("## Map bans", StringComparison.Ordinal)
                    )
                    {
                        instructionsTextIndex = i;
                        break;
                    }
                }

                if (instructionsTextIndex != -1)
                {
                    var banInstructions =
                        "**Your selections have been recorded.**\n\n"
                        + $"You have selected:\n"
                        + $" - {GuaranteedBanEmoji} **{guaranteedBans} Guaranteed bans** (always applied)\n"
                        + $" - {CoinflipBanEmoji} **{coinflipBans} Coinflip bans** (conditionally applied)\n\n"
                        + "Please review your selections and choose:\n"
                        + " - **Confirm** to lock in your bans\n"
                        + " - **Revise** to make changes";
                    var instructionsText =
                        $"## Map bans - {UpdatingTeam.Name} vs. {OpposingTeam.Name}\n\n{banInstructions}";
                    containerComponents[instructionsTextIndex] = new DiscordTextDisplayComponent(instructionsText);
                }

                // Find and replace the action row (select menu -> confirm/revise buttons)
                int actionRowIndex = -1;
                for (int i = containerComponents.Count - 1; i >= 0; i--)
                {
                    if (containerComponents[i] is DiscordActionRowComponent)
                    {
                        actionRowIndex = i;
                        break;
                    }
                }

                if (actionRowIndex != -1)
                {
                    var confirmButton = new DiscordButtonComponent(
                        DiscordButtonStyle.Success,
                        $"confirm_mapban_{match.Id}_{teamId}",
                        "Confirm Bans"
                    );
                    var reviseButton = new DiscordButtonComponent(
                        DiscordButtonStyle.Secondary,
                        $"revise_mapban_{match.Id}_{teamId}",
                        "Revise Selection"
                    );
                    containerComponents[actionRowIndex] = new DiscordActionRowComponent([confirmButton, reviseButton]);
                }

                var updatedContainer = new DiscordContainerComponent(containerComponents);
                return Result<DiscordContainerComponent>.CreateSuccess(updatedContainer, "Container updated with bans");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to update match container for match {match.Id}, team {teamId}",
                    nameof(UpdateMatchContainerWithBansAsync)
                );
                return Result<DiscordContainerComponent>.Failure($"Failed to update container: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates a match container after bans are confirmed.
        /// Removes confirm/revise buttons and restores the refresh button.
        /// </summary>
        public static async Task<Result<DiscordContainerComponent>> UpdateMatchContainerConfirmedAsync(
            DiscordMessage existingMessage,
            Match match,
            Guid teamId,
            string[] selections
        )
        {
            try
            {
                if (existingMessage.Components is null)
                {
                    return Result<DiscordContainerComponent>.Failure("Message components not found");
                }

                // Extract existing container from the message
                var existingContainer = existingMessage.Components.OfType<DiscordContainerComponent>().FirstOrDefault();
                if (existingContainer is null)
                {
                    return Result<DiscordContainerComponent>.Failure("No container found in message");
                }

                // Copy existing components to modify only what changed
                var containerComponents = new List<DiscordComponent>(existingContainer.Components);

                // Determine which team confirmed
                bool isChallengerTeam = teamId == match.Team1Id;

                // Get team names for display
                var getUpdatingTeam = await CoreService.Teams.GetByIdAsync(teamId, DatabaseComponent.Repository);
                if (!getUpdatingTeam.Success || getUpdatingTeam.Data is null)
                {
                    return Result<DiscordContainerComponent>.Failure("Updating team not found");
                }
                var UpdatingTeam = getUpdatingTeam.Data;

                var opposingTeamId = isChallengerTeam ? match.Team2Id : match.Team1Id;
                var getOpposingTeam = await CoreService.Teams.GetByIdAsync(
                    opposingTeamId,
                    DatabaseComponent.Repository
                );
                if (!getOpposingTeam.Success || getOpposingTeam.Data is null)
                {
                    return Result<DiscordContainerComponent>.Failure("Opposing team not found");
                }
                var OpposingTeam = getOpposingTeam.Data;

                // Get map pool and ban configuration
                var mapPool =
                    match.AvailableMaps
                    ?? new List<string> { "Echeneis", "Glittering Lagoon", "Silent Sanctum", "Thornwood" };
                var matchLength = match.BestOf != 0 ? match.BestOf : 3;
                var mapBansConfig = ConfigurationProvider.GetSection<MapBansOptions>(MapBansOptions.SectionName);
                var banConfig = mapBansConfig.GetBanConfig(matchLength, mapPool.Count);
                var guaranteedBans = banConfig.GuaranteedBans;
                var coinflipBans = banConfig.CoinflipBans;

                // Find and replace the map pool status text component
                int mapPoolTextIndex = -1;
                for (int i = 0; i < containerComponents.Count; i++)
                {
                    if (
                        containerComponents[i] is DiscordTextDisplayComponent textComp
                        && textComp.Content.Contains("**Map Pool:**", StringComparison.Ordinal)
                    )
                    {
                        mapPoolTextIndex = i;
                        break;
                    }
                }

                if (mapPoolTextIndex != -1)
                {
                    // Build updated map pool text with confirmed status
                    var mapPoolText = "**Map Pool:**\n";
                    for (int i = 0; i < mapPool.Count; i++)
                    {
                        var map = mapPool[i];
                        var selectionIndex = Array.IndexOf(selections, map);
                        string emoji =
                            selectionIndex == -1 ? AvailableEmoji
                            : selectionIndex < guaranteedBans ? GuaranteedBanEmoji
                            : CoinflipBanEmoji;

                        mapPoolText += $" - {emoji} {map},\n";
                    }
                    mapPoolText += "**Ban status:**\n";
                    mapPoolText += $"{UpdatingTeam.Name} ✅ Confirmed\n";
                    mapPoolText += $"{OpposingTeam.Name} In Progress\n";

                    containerComponents[mapPoolTextIndex] = new DiscordTextDisplayComponent(mapPoolText);
                }

                // Find and replace the instructions text component
                int instructionsTextIndex = -1;
                for (int i = 0; i < containerComponents.Count; i++)
                {
                    if (
                        containerComponents[i] is DiscordTextDisplayComponent textComp
                        && textComp.Content.Contains("## Map bans", StringComparison.Ordinal)
                    )
                    {
                        instructionsTextIndex = i;
                        break;
                    }
                }

                if (instructionsTextIndex != -1)
                {
                    var confirmationMessage =
                        "**Your ban selections have been confirmed! ✅**\n\n"
                        + $"You selected:\n"
                        + $" - {GuaranteedBanEmoji} **{guaranteedBans} Guaranteed bans** (always applied)\n"
                        + $" - {CoinflipBanEmoji} **{coinflipBans} Coinflip bans** (conditionally applied)\n\n"
                        + "Your selections are now locked. Waiting for the opposing team to confirm their bans.";
                    var instructionsText =
                        $"## Map bans - {UpdatingTeam.Name} vs. {OpposingTeam.Name}\n\n{confirmationMessage}";
                    containerComponents[instructionsTextIndex] = new DiscordTextDisplayComponent(instructionsText);
                }

                // Find and replace the action row (confirm/revise buttons -> just refresh button)
                int actionRowIndex = -1;
                for (int i = containerComponents.Count - 1; i >= 0; i--)
                {
                    if (containerComponents[i] is DiscordActionRowComponent)
                    {
                        actionRowIndex = i;
                        break;
                    }
                }

                if (actionRowIndex != -1)
                {
                    var refreshButton = new DiscordButtonComponent(
                        DiscordButtonStyle.Secondary,
                        $"refresh_bans_{match.Id}_{teamId}",
                        "Refresh"
                    );
                    containerComponents[actionRowIndex] = new DiscordActionRowComponent([refreshButton]);
                }

                var updatedContainer = new DiscordContainerComponent(containerComponents);
                return Result<DiscordContainerComponent>.CreateSuccess(
                    updatedContainer,
                    "Container updated with confirmed bans"
                );
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to update match container after confirmation for match {match.Id}, team {teamId}",
                    nameof(UpdateMatchContainerConfirmedAsync)
                );
                return Result<DiscordContainerComponent>.Failure($"Failed to update container: {ex.Message}");
            }
        }

        /// <summary>
        /// Renders a match complete container showing final results and ratings.
        /// Posts the container to both team threads.
        /// </summary>
        public static async Task<Result> RenderMatchCompleteContainerAsync(
            Guid matchId,
            Guid winnerTeamId,
            DiscordThreadChannel team1Thread,
            DiscordThreadChannel team2Thread
        )
        {
            try
            {
                // Fetch the match
                var matchResult = await CoreService.Matches.GetByIdAsync(matchId, DatabaseComponent.Repository);
                if (!matchResult.Success || matchResult.Data is null)
                {
                    return Result.Failure("Match not found");
                }
                var match = matchResult.Data;

                // Fetch teams
                var getTeam1 = await CoreService.Teams.GetByIdAsync(match.Team1Id, DatabaseComponent.Repository);
                var getTeam2 = await CoreService.Teams.GetByIdAsync(match.Team2Id, DatabaseComponent.Repository);

                if (!getTeam1.Success || getTeam1.Data is null || !getTeam2.Success || getTeam2.Data is null)
                {
                    return Result.Failure("Teams not found");
                }

                var team1 = getTeam1.Data;
                var team2 = getTeam2.Data;

                // Determine winner and loser
                var winnerTeam = winnerTeamId == team1.Id ? team1 : team2;
                var loserTeam = winnerTeamId == team1.Id ? team2 : team1;

                // Get current ratings
                var winnerRating = (int)Math.Round(winnerTeam.ScrimmageTeamStats[match.TeamSize].CurrentRating);
                var loserRating = (int)Math.Round(loserTeam.ScrimmageTeamStats[match.TeamSize].CurrentRating);

                // Get rating changes
                var winnerRatingChange = winnerTeam.ScrimmageTeamStats[match.TeamSize].RecentRatingChange;
                var loserRatingChange = loserTeam.ScrimmageTeamStats[match.TeamSize].RecentRatingChange;

                // Use match.Games navigation property to get all games
                var games = match.Games.ToList();

                // Calculate scores (wins, losses, draws)
                int team1Wins = 0,
                    team2Wins = 0,
                    draws = 0;
                foreach (var game in games)
                {
                    var latestState = game.StateHistory.OrderByDescending(s => s.Timestamp).FirstOrDefault();
                    if (latestState?.WinnerId == match.Team1Id)
                    {
                        team1Wins++;
                    }
                    else if (latestState?.WinnerId == match.Team2Id)
                    {
                        team2Wins++;
                    }
                    else if (latestState?.Status == GameStatus.Completed)
                    {
                        draws++;
                    }
                }

                int winnerWins = winnerTeamId == team1.Id ? team1Wins : team2Wins;
                int loserWins = winnerTeamId == team1.Id ? team2Wins : team1Wins;

                // Fetch player data for both teams
                var team1Players = new List<Player>();
                foreach (var playerId in match.Team1PlayerIds)
                {
                    var playerResult = await CoreService.Players.GetByIdAsync(playerId, DatabaseComponent.Repository);
                    if (playerResult.Success && playerResult.Data is not null)
                    {
                        team1Players.Add(playerResult.Data);
                    }
                }

                var team2Players = new List<Player>();
                foreach (var playerId in match.Team2PlayerIds)
                {
                    var playerResult = await CoreService.Players.GetByIdAsync(playerId, DatabaseComponent.Repository);
                    if (playerResult.Success && playerResult.Data is not null)
                    {
                        team2Players.Add(playerResult.Data);
                    }
                }

                var winnerPlayers = winnerTeamId == team1.Id ? team1Players : team2Players;
                var loserPlayers = winnerTeamId == team1.Id ? team2Players : team1Players;

                // Create player name lists
                var winnerPlayerNames = string.Join(
                    ", ",
                    winnerPlayers.Select(p => p.MashinaUser?.DiscordUsername ?? "Unknown")
                );
                var loserPlayerNames = string.Join(
                    ", ",
                    loserPlayers.Select(p => p.MashinaUser?.DiscordUsername ?? "Unknown")
                );

                // Create zip file of all replays
                var zipFileResult = await CreateReplayZipFileAsync(
                    matchId,
                    games,
                    team1.Name,
                    team2.Name,
                    match.TeamSize
                );
                string? zipFilePath = zipFileResult.Success ? zipFileResult.Data : null;

                // Build components for both teams
                var components = new List<DiscordComponent>();

                // 1. Top banner (match_complete_banner.jpg)
                var (bannerUrl, bannerHint, isCompleteBannerCdnUrl) =
                    DiscBotService.AssetResolver.ResolveDiscordComponentImage("match_complete_banner.jpg");
                if (bannerUrl is not null)
                {
                    components.Add(new DiscordMediaGalleryComponent(items: [new DiscordMediaGalleryItem(bannerUrl)]));
                }

                // 2. Separator
                components.Add(new DiscordSeparatorComponent(true));

                // 3. Winner announcement
                var scoreText =
                    draws > 0
                        ? $"# Winner: {winnerTeam.Name} {winnerWins} - {loserWins} - {draws}"
                        : $"# Winner: {winnerTeam.Name} {winnerWins} - {loserWins}";
                components.Add(new DiscordSectionComponent(new DiscordTextDisplayComponent(scoreText), null!));

                // 4. Separator
                components.Add(new DiscordSeparatorComponent(true));

                // 5. Rating changes section
                var ratingSign = winnerRatingChange >= 0 ? "+" : "";
                var loserRatingSign = loserRatingChange >= 0 ? "+" : "";

                var ratingText =
                    $"**{winnerTeam.Name}** [{winnerRating}] {ratingSign}{winnerRatingChange:F0} 🌲\n"
                    + $"{winnerPlayerNames}\n\n"
                    + $"**{loserTeam.Name}** [{loserRating}] {loserRatingSign}{loserRatingChange:F0} 🔻\n"
                    + $"{loserPlayerNames}";
                components.Add(new DiscordSectionComponent(new DiscordTextDisplayComponent(ratingText), null!));

                // 6. Separator
                components.Add(new DiscordSeparatorComponent(true));

                // 7. Replay file attachment (if available)
                if (zipFilePath is not null && File.Exists(zipFilePath))
                {
                    var fileName = Path.GetFileName(zipFilePath);
                    components.Add(new DiscordFileComponent($"attachment://{fileName}", isSpoilered: false));
                }

                // 8. Separator
                components.Add(new DiscordSeparatorComponent(true));

                // 9. Rematch button
                var rematchButton = new DiscordButtonComponent(
                    DiscordButtonStyle.Primary,
                    $"rematch_request_{matchId}",
                    "Request Rematch"
                );
                var rematchActionRow = new DiscordActionRowComponent([rematchButton]);
                components.Add(rematchActionRow);

                // Create container
                var container = new DiscordContainerComponent(components);

                // Build message builder with file attachment
                var messageBuilder = new DiscordMessageBuilder().AddContainerComponent(container);

                if (zipFilePath is not null && File.Exists(zipFilePath))
                {
                    var fileName = Path.GetFileName(zipFilePath);

                    // Read file into memory to avoid stream disposal issues and allow reuse
                    byte[] fileData = await File.ReadAllBytesAsync(zipFilePath);

                    // Create separate streams for each message send
                    using var stream1 = new MemoryStream(fileData);
                    using var stream2 = new MemoryStream(fileData);

                    // Send to team1 thread
                    var builder1 = new DiscordMessageBuilder().AddContainerComponent(container);
                    builder1.AddFile(fileName, stream1);
                    var team1Message = await team1Thread.SendMessageAsync(builder1);

                    // Send to team2 thread
                    var builder2 = new DiscordMessageBuilder().AddContainerComponent(container);
                    builder2.AddFile(fileName, stream2);
                    var team2Message = await team2Thread.SendMessageAsync(builder2);

                    // Capture CDN URLs from both messages for caching
                    await Utilities.CdnCapture.CaptureFromMessageAsync(team1Message, fileName);
                    await Utilities.CdnCapture.CaptureFromMessageAsync(team2Message, fileName);
                }
                else
                {
                    // Send without file if zip creation failed
                    await team1Thread.SendMessageAsync(messageBuilder);
                    await team2Thread.SendMessageAsync(messageBuilder);
                }

                // Clean up temporary zip file
                if (zipFilePath is not null && File.Exists(zipFilePath))
                {
                    try
                    {
                        File.Delete(zipFilePath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }

                return Result.CreateSuccess("Match complete container rendered");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to render match complete container for {matchId}",
                    nameof(RenderMatchCompleteContainerAsync)
                );
                return Result.Failure($"Failed to render match complete container: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a zip file containing all replays from a match.
        /// Returns the path to the temporary zip file.
        /// </summary>
        private static async Task<Result<string>> CreateReplayZipFileAsync(
            Guid matchId,
            List<Game> games,
            string team1Name,
            string team2Name,
            TeamSize teamSize
        )
        {
            try
            {
                // Create temp directory for zip file
                var tempDir = Path.Combine(Path.GetTempPath(), "WabbitBot", "MatchReplays");
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                // Generate zip filename: "3v3-GOATFOAR-vs-Wolverines-2025-10-18_12-10-11.zip"
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
                var gameSizeStr = teamSize switch
                {
                    TeamSize.OneVOne => "1v1",
                    TeamSize.TwoVTwo => "2v2",
                    TeamSize.ThreeVThree => "3v3",
                    TeamSize.FourVFour => "4v4",
                    _ => "custom",
                };
                var zipFileName = $"{gameSizeStr}-{team1Name}-vs-{team2Name}-{timestamp}.zip";
                var zipFilePath = Path.Combine(tempDir, zipFileName);

                // Create zip file
                using (
                    var zipArchive = System.IO.Compression.ZipFile.Open(
                        zipFilePath,
                        System.IO.Compression.ZipArchiveMode.Create
                    )
                )
                {
                    // Add all replays from all games
                    foreach (var game in games)
                    {
                        // Get replays from game navigation property
                        if (game.Replays is not null && game.Replays.Any())
                        {
                            foreach (var replay in game.Replays)
                            {
                                if (string.IsNullOrEmpty(replay.FilePath))
                                {
                                    continue;
                                }

                                // Read replay file (which may be a zip containing the replay)
                                var replayFileBytes = await CoreService.FileSystem.ReadReplayFileAsync(replay.FilePath);
                                if (replayFileBytes is null)
                                {
                                    continue;
                                }

                                // Get player name for filename
                                var replayPlayer = replay.Players.FirstOrDefault();
                                var playerName = replayPlayer?.PlayerName ?? "Unknown";

                                // Get map name
                                var mapName = game.Map?.Name ?? "Unknown";

                                // Get division name if available
                                var divisionName = game.Team1Division?.Name ?? "Unknown";

                                // Check if the stored file is a zip file (individual replay zips)
                                // If so, extract the .rpl3 from it; otherwise use the bytes directly
                                byte[] replayBytes;
                                string replayFileName;

                                if (replay.FilePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Extract .rpl3 from the individual replay zip
                                    try
                                    {
                                        using var zipStream = new MemoryStream(replayFileBytes);
                                        using var individualZip = new System.IO.Compression.ZipArchive(
                                            zipStream,
                                            System.IO.Compression.ZipArchiveMode.Read
                                        );

                                        // Find the .rpl3 entry
                                        var rpl3Entry = individualZip.Entries.FirstOrDefault(e =>
                                            e.Name.EndsWith(".rpl3", StringComparison.OrdinalIgnoreCase)
                                        );

                                        if (rpl3Entry is null)
                                        {
                                            // No .rpl3 found, skip this replay
                                            continue;
                                        }

                                        // Read the .rpl3 file from the zip
                                        using var rpl3Stream = rpl3Entry.Open();
                                        using var memStream = new MemoryStream();
                                        await rpl3Stream.CopyToAsync(memStream);
                                        replayBytes = memStream.ToArray();

                                        // Use the original .rpl3 filename from the zip
                                        var originalFileName = rpl3Entry.Name;
                                        replayFileName =
                                            $"{playerName}-Game{game.GameNumber}-{divisionName}-{mapName}-{timestamp}.rpl3";
                                    }
                                    catch
                                    {
                                        // If extraction fails, skip this replay
                                        continue;
                                    }
                                }
                                else
                                {
                                    // File is already a raw .rpl3
                                    replayBytes = replayFileBytes;
                                    replayFileName =
                                        $"{playerName}-Game{game.GameNumber}-{divisionName}-{mapName}-{timestamp}.rpl3";
                                }

                                // Sanitize filename (remove invalid characters)
                                replayFileName = string.Join("_", replayFileName.Split(Path.GetInvalidFileNameChars()));

                                // Add to match zip
                                var entry = zipArchive.CreateEntry(replayFileName);
                                using var entryStream = entry.Open();
                                await entryStream.WriteAsync(replayBytes, 0, replayBytes.Length);
                            }
                        }
                    }
                }

                return Result<string>.CreateSuccess(zipFilePath, "Replay zip created");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to create replay zip for match {matchId}",
                    nameof(CreateReplayZipFileAsync)
                );
                return Result<string>.Failure($"Failed to create replay zip: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the match complete container to show a rematch was requested by one team.
        /// </summary>
        public static async Task<Result> RenderMatchCompleteContainerWithRematchRequestAsync(
            Guid matchId,
            Guid requestingTeamId
        )
        {
            try
            {
                var getMatch = await CoreService.Matches.GetByIdAsync(matchId, DatabaseComponent.Repository);
                if (!getMatch.Success || getMatch.Data is null)
                {
                    return Result.Failure("Match not found");
                }

                var match = getMatch.Data;

                // Get team names
                var getTeam1 = await CoreService.Teams.GetByIdAsync(match.Team1Id, DatabaseComponent.Repository);
                var getTeam2 = await CoreService.Teams.GetByIdAsync(match.Team2Id, DatabaseComponent.Repository);

                if (!getTeam1.Success || getTeam1.Data is null || !getTeam2.Success || getTeam2.Data is null)
                {
                    return Result.Failure("Failed to get team data");
                }

                var requestingTeamName = requestingTeamId == match.Team1Id ? getTeam1.Data.Name : getTeam2.Data.Name;

                // Get both thread channels
                if (match.Team1ThreadId is null || match.Team2ThreadId is null)
                {
                    return Result.Failure("Thread IDs not found");
                }

                var team1Thread = await DiscBotService.Client.GetChannelAsync(match.Team1ThreadId.Value);
                var team2Thread = await DiscBotService.Client.GetChannelAsync(match.Team2ThreadId.Value);

                // Find the match complete messages in both threads
                // We'll need to search for messages with the rematch button
                var team1Messages = new List<DiscordMessage>();
                await foreach (var msg in team1Thread.GetMessagesAsync(50))
                {
                    team1Messages.Add(msg);
                }
                var team2Messages = new List<DiscordMessage>();
                await foreach (var msg in team2Thread.GetMessagesAsync(50))
                {
                    team2Messages.Add(msg);
                }

                DiscordMessage? team1CompleteMsg = null;
                DiscordMessage? team2CompleteMsg = null;

                foreach (var msg in team1Messages)
                {
                    var container = msg.Components?.OfType<DiscordContainerComponent>().FirstOrDefault();
                    if (
                        container?.Components?.Any(c =>
                            c is DiscordActionRowComponent row
                            && row.Components.Any(b =>
                                b is DiscordButtonComponent btn && btn.CustomId == $"rematch_request_{matchId}"
                            )
                        ) ?? false
                    )
                    {
                        team1CompleteMsg = msg;
                        break;
                    }
                }

                foreach (var msg in team2Messages)
                {
                    var container = msg.Components?.OfType<DiscordContainerComponent>().FirstOrDefault();
                    if (
                        container?.Components?.Any(c =>
                            c is DiscordActionRowComponent row
                            && row.Components.Any(b =>
                                b is DiscordButtonComponent btn && btn.CustomId == $"rematch_request_{matchId}"
                            )
                        ) ?? false
                    )
                    {
                        team2CompleteMsg = msg;
                        break;
                    }
                }

                if (team1CompleteMsg is null || team2CompleteMsg is null)
                {
                    return Result.Failure("Match complete messages not found");
                }

                // Get existing containers
                var team1Container = team1CompleteMsg.Components?.OfType<DiscordContainerComponent>().FirstOrDefault();
                var team2Container = team2CompleteMsg.Components?.OfType<DiscordContainerComponent>().FirstOrDefault();

                if (team1Container is null || team2Container is null)
                {
                    return Result.Failure("Containers not found");
                }

                // Update the containers: replace the rematch request button with accept/decline buttons or a disabled message
                var team1UpdatedComponents = new List<DiscordComponent>(team1Container.Components);
                var team2UpdatedComponents = new List<DiscordComponent>(team2Container.Components);

                // Find and replace the action row in both containers
                for (int i = 0; i < team1UpdatedComponents.Count; i++)
                {
                    if (team1UpdatedComponents[i] is DiscordActionRowComponent)
                    {
                        // For the requesting team, show a disabled message
                        if (requestingTeamId == match.Team1Id)
                        {
                            team1UpdatedComponents[i] = new DiscordActionRowComponent(
                                [
                                    new DiscordButtonComponent(
                                        DiscordButtonStyle.Secondary,
                                        $"rematch_requested_{matchId}",
                                        "Rematch Requested",
                                        disabled: true
                                    ),
                                ]
                            );
                        }
                        else
                        {
                            // For the other team, show accept/decline buttons
                            team1UpdatedComponents[i] = new DiscordActionRowComponent(
                                [
                                    new DiscordButtonComponent(
                                        DiscordButtonStyle.Success,
                                        $"accept_rematch_{matchId}",
                                        "Accept Rematch"
                                    ),
                                    new DiscordButtonComponent(
                                        DiscordButtonStyle.Danger,
                                        $"decline_rematch_{matchId}",
                                        "Decline Rematch"
                                    ),
                                ]
                            );
                        }
                        break;
                    }
                }

                for (int i = 0; i < team2UpdatedComponents.Count; i++)
                {
                    if (team2UpdatedComponents[i] is DiscordActionRowComponent)
                    {
                        // For the requesting team, show a disabled message
                        if (requestingTeamId == match.Team2Id)
                        {
                            team2UpdatedComponents[i] = new DiscordActionRowComponent(
                                [
                                    new DiscordButtonComponent(
                                        DiscordButtonStyle.Secondary,
                                        $"rematch_requested_{matchId}",
                                        "Rematch Requested",
                                        disabled: true
                                    ),
                                ]
                            );
                        }
                        else
                        {
                            // For the other team, show accept/decline buttons
                            team2UpdatedComponents[i] = new DiscordActionRowComponent(
                                [
                                    new DiscordButtonComponent(
                                        DiscordButtonStyle.Success,
                                        $"accept_rematch_{matchId}",
                                        "Accept Rematch"
                                    ),
                                    new DiscordButtonComponent(
                                        DiscordButtonStyle.Danger,
                                        $"decline_rematch_{matchId}",
                                        "Decline Rematch"
                                    ),
                                ]
                            );
                        }
                        break;
                    }
                }

                // Update both messages
                await team1CompleteMsg.ModifyAsync(
                    new DiscordMessageBuilder()
                        .EnableV2Components()
                        .AddContainerComponent(new DiscordContainerComponent(team1UpdatedComponents))
                );
                await team2CompleteMsg.ModifyAsync(
                    new DiscordMessageBuilder()
                        .EnableV2Components()
                        .AddContainerComponent(new DiscordContainerComponent(team2UpdatedComponents))
                );

                return Result.CreateSuccess("Rematch request rendered");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to render rematch request for match {matchId}",
                    nameof(RenderMatchCompleteContainerWithRematchRequestAsync)
                );
                return Result.Failure($"Failed to render rematch request: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the match complete container to show a rematch was accepted.
        /// </summary>
        public static async Task<Result> RenderMatchCompleteContainerWithRematchAcceptedAsync(Guid matchId)
        {
            try
            {
                var getMatch = await CoreService.Matches.GetByIdAsync(matchId, DatabaseComponent.Repository);
                if (!getMatch.Success || getMatch.Data is null)
                {
                    return Result.Failure("Match not found");
                }

                var match = getMatch.Data;

                // Get both thread channels
                if (match.Team1ThreadId is null || match.Team2ThreadId is null)
                {
                    return Result.Failure("Thread IDs not found");
                }

                var team1Thread = await DiscBotService.Client.GetChannelAsync(match.Team1ThreadId.Value);
                var team2Thread = await DiscBotService.Client.GetChannelAsync(match.Team2ThreadId.Value);

                // Find the match complete messages
                var team1Messages = new List<DiscordMessage>();
                await foreach (var msg in team1Thread.GetMessagesAsync(50))
                {
                    team1Messages.Add(msg);
                }
                var team2Messages = new List<DiscordMessage>();
                await foreach (var msg in team2Thread.GetMessagesAsync(50))
                {
                    team2Messages.Add(msg);
                }

                DiscordMessage? team1CompleteMsg = null;
                DiscordMessage? team2CompleteMsg = null;

                foreach (var msg in team1Messages)
                {
                    var container = msg.Components?.OfType<DiscordContainerComponent>().FirstOrDefault();
                    if (container?.Components?.Any(c => c is DiscordActionRowComponent) ?? false)
                    {
                        team1CompleteMsg = msg;
                        break;
                    }
                }

                foreach (var msg in team2Messages)
                {
                    var container = msg.Components?.OfType<DiscordContainerComponent>().FirstOrDefault();
                    if (container?.Components?.Any(c => c is DiscordActionRowComponent) ?? false)
                    {
                        team2CompleteMsg = msg;
                        break;
                    }
                }

                if (team1CompleteMsg is null || team2CompleteMsg is null)
                {
                    return Result.Failure("Match complete messages not found");
                }

                // Get existing containers
                var team1Container = team1CompleteMsg.Components?.OfType<DiscordContainerComponent>().FirstOrDefault();
                var team2Container = team2CompleteMsg.Components?.OfType<DiscordContainerComponent>().FirstOrDefault();

                if (team1Container is null || team2Container is null)
                {
                    return Result.Failure("Containers not found");
                }

                // Update containers: remove action row, add text saying rematch accepted
                var team1UpdatedComponents = new List<DiscordComponent>(team1Container.Components);
                var team2UpdatedComponents = new List<DiscordComponent>(team2Container.Components);

                // Remove action rows and add success message
                team1UpdatedComponents.RemoveAll(c => c is DiscordActionRowComponent);
                team2UpdatedComponents.RemoveAll(c => c is DiscordActionRowComponent);

                team1UpdatedComponents.Add(
                    new DiscordSectionComponent(
                        new DiscordTextDisplayComponent("✅ **Rematch Accepted** - New match starting!"),
                        null!
                    )
                );
                team2UpdatedComponents.Add(
                    new DiscordSectionComponent(
                        new DiscordTextDisplayComponent("✅ **Rematch Accepted** - New match starting!"),
                        null!
                    )
                );

                // Update both messages
                await team1CompleteMsg.ModifyAsync(
                    new DiscordMessageBuilder()
                        .EnableV2Components()
                        .AddContainerComponent(new DiscordContainerComponent(team1UpdatedComponents))
                );
                await team2CompleteMsg.ModifyAsync(
                    new DiscordMessageBuilder()
                        .EnableV2Components()
                        .AddContainerComponent(new DiscordContainerComponent(team2UpdatedComponents))
                );

                return Result.CreateSuccess("Rematch accepted rendered");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to render rematch accepted for match {matchId}",
                    nameof(RenderMatchCompleteContainerWithRematchAcceptedAsync)
                );
                return Result.Failure($"Failed to render rematch accepted: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the match complete container to show a rematch was declined.
        /// </summary>
        public static async Task<Result> RenderMatchCompleteContainerWithRematchDeclinedAsync(Guid matchId)
        {
            try
            {
                var getMatch = await CoreService.Matches.GetByIdAsync(matchId, DatabaseComponent.Repository);
                if (!getMatch.Success || getMatch.Data is null)
                {
                    return Result.Failure("Match not found");
                }

                var match = getMatch.Data;

                // Get both thread channels
                if (match.Team1ThreadId is null || match.Team2ThreadId is null)
                {
                    return Result.Failure("Thread IDs not found");
                }

                var team1Thread = await DiscBotService.Client.GetChannelAsync(match.Team1ThreadId.Value);
                var team2Thread = await DiscBotService.Client.GetChannelAsync(match.Team2ThreadId.Value);

                // Find the match complete messages
                var team1Messages = new List<DiscordMessage>();
                await foreach (var msg in team1Thread.GetMessagesAsync(50))
                {
                    team1Messages.Add(msg);
                }
                var team2Messages = new List<DiscordMessage>();
                await foreach (var msg in team2Thread.GetMessagesAsync(50))
                {
                    team2Messages.Add(msg);
                }

                DiscordMessage? team1CompleteMsg = null;
                DiscordMessage? team2CompleteMsg = null;

                foreach (var msg in team1Messages)
                {
                    var container = msg.Components?.OfType<DiscordContainerComponent>().FirstOrDefault();
                    if (container?.Components?.Any(c => c is DiscordActionRowComponent) ?? false)
                    {
                        team1CompleteMsg = msg;
                        break;
                    }
                }

                foreach (var msg in team2Messages)
                {
                    var container = msg.Components?.OfType<DiscordContainerComponent>().FirstOrDefault();
                    if (container?.Components?.Any(c => c is DiscordActionRowComponent) ?? false)
                    {
                        team2CompleteMsg = msg;
                        break;
                    }
                }

                if (team1CompleteMsg is null || team2CompleteMsg is null)
                {
                    return Result.Failure("Match complete messages not found");
                }

                // Get existing containers
                var team1Container = team1CompleteMsg.Components?.OfType<DiscordContainerComponent>().FirstOrDefault();
                var team2Container = team2CompleteMsg.Components?.OfType<DiscordContainerComponent>().FirstOrDefault();

                if (team1Container is null || team2Container is null)
                {
                    return Result.Failure("Containers not found");
                }

                // Update containers: remove action rows, add declined message
                var team1UpdatedComponents = new List<DiscordComponent>(team1Container.Components);
                var team2UpdatedComponents = new List<DiscordComponent>(team2Container.Components);

                // Remove action rows and add declined message
                team1UpdatedComponents.RemoveAll(c => c is DiscordActionRowComponent);
                team2UpdatedComponents.RemoveAll(c => c is DiscordActionRowComponent);

                team1UpdatedComponents.Add(
                    new DiscordSectionComponent(new DiscordTextDisplayComponent("❌ **Rematch Declined**"), null!)
                );
                team2UpdatedComponents.Add(
                    new DiscordSectionComponent(new DiscordTextDisplayComponent("❌ **Rematch Declined**"), null!)
                );

                // Update both messages
                await team1CompleteMsg.ModifyAsync(
                    new DiscordMessageBuilder()
                        .EnableV2Components()
                        .AddContainerComponent(new DiscordContainerComponent(team1UpdatedComponents))
                );
                await team2CompleteMsg.ModifyAsync(
                    new DiscordMessageBuilder()
                        .EnableV2Components()
                        .AddContainerComponent(new DiscordContainerComponent(team2UpdatedComponents))
                );

                return Result.CreateSuccess("Rematch declined rendered");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to render rematch declined for match {matchId}",
                    nameof(RenderMatchCompleteContainerWithRematchDeclinedAsync)
                );
                return Result.Failure($"Failed to render rematch declined: {ex.Message}");
            }
        }
    }

    #region Result Types
    public record MatchContainersResult(
        DiscordContainerComponent ChallengerContainer,
        DiscordContainerComponent OpponentContainer
    );
    #endregion
}
