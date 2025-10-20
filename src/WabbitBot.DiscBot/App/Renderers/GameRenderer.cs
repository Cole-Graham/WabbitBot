using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Models;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Services.DiscBot;
using WabbitBot.DiscBot.App.Utilities;

namespace WabbitBot.DiscBot.App.Renderers
{
    /// <summary>
    /// Result data for game container rendering
    /// </summary>
    public record GameContainerResult(DiscordMessage Team1Message, DiscordMessage Team2Message);

    /// <summary>
    /// Renderer for per-game containers and deck submission DMs.
    /// Accepts concrete Discord parameters and performs rendering logic.
    /// Does NOT subscribe to events - that's the Handler's job.
    /// </summary>
    public static class GameRenderer
    {
        /// <summary>
        /// Renders a game container with game banner, map thumbnail, player sections, and replay submission components.
        /// </summary>
        /// <param name="gameId">Game ID</param>
        /// <param name="gameNumber">Game number (1-based)</param>
        /// <param name="mapName">Name of the map for this game</param>
        /// <param name="team1Name">Team 1 name</param>
        /// <param name="team2Name">Team 2 name</param>
        /// <param name="team1PlayerIds">Team 1 player IDs</param>
        /// <param name="team2PlayerIds">Team 2 player IDs</param>
        /// <param name="team1Thread">Team 1 Discord thread</param>
        /// <param name="team2Thread">Team 2 Discord thread</param>
        /// <returns>Result with GameContainerResult data</returns>
        public static async Task<Result<GameContainerResult>> RenderGameContainerAsync(
            Guid gameId,
            int gameNumber,
            string mapName,
            string team1Name,
            string team2Name,
            List<Guid> team1PlayerIds,
            List<Guid> team2PlayerIds,
            DiscordThreadChannel team1Thread,
            DiscordThreadChannel team2Thread,
            int team1Rating = 0,
            int team2Rating = 0
        )
        {
            try
            {
                // Retrieve full player data from database
                var (team1Players, team2Players) = await Core.Common.Services.CoreService.WithDbContext(async db =>
                {
                    var allPlayerIds = team1PlayerIds.Concat(team2PlayerIds).ToList();
                    var players = await db
                        .Players.Where(p => allPlayerIds.Contains(p.Id))
                        .Include(p => p.MashinaUser)
                        .ToListAsync();

                    var team1 = players.Where(p => team1PlayerIds.Contains(p.Id)).ToList();
                    var team2 = players.Where(p => team2PlayerIds.Contains(p.Id)).ToList();

                    return (team1, team2);
                });

                // 1. Resolve game banner (CDN-first, fallback to attachment)
                var gameBannerFileName = $"game_{gameNumber}_banner.jpg";
                var (bannerCdnUrl, bannerAttachmentHint) = await DiscBotService.AssetResolver.ResolveGameBannerAsync(
                    gameBannerFileName
                );

                // 2. Resolve map thumbnail (CDN-first, fallback to attachment)
                var (mapCdnUrl, mapAttachmentHint) = await DiscBotService.AssetResolver.ResolveMapThumbnailAsync(
                    mapName
                );

                // 3. Build container components for each team separately
                var team1Components = new List<DiscordComponent>();
                var team2Components = new List<DiscordComponent>();

                // Add shared components (banner, map) to both
                if (bannerCdnUrl is not null)
                {
                    var bannerItem = new DiscordMediaGalleryItem(bannerCdnUrl);
                    team1Components.Add(new DiscordMediaGalleryComponent(items: [bannerItem]));
                    team2Components.Add(new DiscordMediaGalleryComponent(items: [bannerItem]));
                }

                team1Components.Add(new DiscordSeparatorComponent(true));
                team2Components.Add(new DiscordSeparatorComponent(true));

                if (mapCdnUrl is not null)
                {
                    var mapItem = new DiscordMediaGalleryItem(mapCdnUrl);
                    team1Components.Add(new DiscordMediaGalleryComponent(items: [mapItem]));
                    team2Components.Add(new DiscordMediaGalleryComponent(items: [mapItem]));
                }

                team1Components.Add(new DiscordSeparatorComponent(true));
                team2Components.Add(new DiscordSeparatorComponent(true));

                // Add match info section (component 4)
                var matchInfoText =
                    $"[{team1Rating}] **{team1Name}** vs. [{team2Rating}] **{team2Name}** - Game {gameNumber} - **{mapName}**";
                team1Components.Add(new DiscordSectionComponent(new DiscordTextDisplayComponent(matchInfoText), null!));
                team2Components.Add(new DiscordSectionComponent(new DiscordTextDisplayComponent(matchInfoText), null!));

                team1Components.Add(new DiscordSeparatorComponent(true));
                team2Components.Add(new DiscordSeparatorComponent(true));

                // Add player section components - only show own team
                await AddPlayerSectionComponentsAsync(
                    team1Components,
                    team1Players,
                    deckCodes: new Dictionary<Guid, string>(),
                    deckSubmitted: new HashSet<Guid>(),
                    replayFiles: new Dictionary<Guid, string>()
                );
                await AddPlayerSectionComponentsAsync(
                    team2Components,
                    team2Players,
                    deckCodes: new Dictionary<Guid, string>(),
                    deckSubmitted: new HashSet<Guid>(),
                    replayFiles: new Dictionary<Guid, string>()
                );

                // Add separator before opponent status section
                team1Components.Add(new DiscordSeparatorComponent(true));
                team2Components.Add(new DiscordSeparatorComponent(true));

                // Add opponent status section - shows opponent deck submission status (but not actual deck codes)
                await AddOpponentStatusSectionAsync(
                    team1Components,
                    team2Players,
                    team2Name,
                    team2Rating,
                    team2PlayerIds.Count,
                    deckSubmittedPlayerIds: new HashSet<Guid>()
                );
                await AddOpponentStatusSectionAsync(
                    team2Components,
                    team1Players,
                    team1Name,
                    team1Rating,
                    team1PlayerIds.Count,
                    deckSubmittedPlayerIds: new HashSet<Guid>()
                );

                // Add separator after opponent status section
                team1Components.Add(new DiscordSeparatorComponent(true));
                team2Components.Add(new DiscordSeparatorComponent(true));

                // Add final action component based on game state (initially: "Submit Deck Code" button)
                await AddFinalActionComponentAsync(
                    team1Components,
                    gameId,
                    team1Players,
                    deckSubmittedPlayerIds: new HashSet<Guid>(),
                    deckConfirmedPlayerIds: new HashSet<Guid>(),
                    replaySubmittedPlayerIds: new HashSet<Guid>()
                );
                await AddFinalActionComponentAsync(
                    team2Components,
                    gameId,
                    team2Players,
                    deckSubmittedPlayerIds: new HashSet<Guid>(),
                    deckConfirmedPlayerIds: new HashSet<Guid>(),
                    replaySubmittedPlayerIds: new HashSet<Guid>()
                );

                var team1Container = new DiscordContainerComponent(team1Components);
                var team2Container = new DiscordContainerComponent(team2Components);

                // 5. Build messages with containers and attachments
                var team1Builder = new DiscordMessageBuilder().AddContainerComponent(team1Container);
                var team2Builder = new DiscordMessageBuilder().AddContainerComponent(team2Container);

                // Add attachments to both builders if needed
                if (bannerAttachmentHint is not null)
                {
                    await AddFileAttachmentAsync(team1Builder, bannerAttachmentHint, "game banner");
                    await AddFileAttachmentAsync(team2Builder, bannerAttachmentHint, "game banner");
                }

                if (mapAttachmentHint is not null)
                {
                    await AddFileAttachmentAsync(team1Builder, mapAttachmentHint, "map thumbnail");
                    await AddFileAttachmentAsync(team2Builder, mapAttachmentHint, "map thumbnail");
                }

                // 6. Send messages to both threads
                var team1Message = await team1Thread.SendMessageAsync(team1Builder);
                var team2Message = await team2Thread.SendMessageAsync(team2Builder);

                // 7. Capture CDN URLs from attachments (both messages for redundancy)
                if (bannerAttachmentHint is not null)
                {
                    await CdnCapture.CaptureFromMessageAsync(team1Message, bannerAttachmentHint.CanonicalFileName);
                    await CdnCapture.CaptureFromMessageAsync(team2Message, bannerAttachmentHint.CanonicalFileName);
                }
                if (mapAttachmentHint is not null)
                {
                    await CdnCapture.CaptureFromMessageAsync(team1Message, mapAttachmentHint.CanonicalFileName);
                    await CdnCapture.CaptureFromMessageAsync(team2Message, mapAttachmentHint.CanonicalFileName);
                }

                return Result<GameContainerResult>.CreateSuccess(
                    new GameContainerResult(team1Message, team2Message),
                    "Game container created"
                );
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to render game container for game {gameId}, game number {gameNumber}",
                    nameof(RenderGameContainerAsync)
                );
                return Result<GameContainerResult>.Failure($"Failed to create game container: {ex.Message}");
            }
        }

        /// <summary>
        /// Refreshes the game container to show the latest opponent status (deck submission, confirmations, etc.)
        /// </summary>
        public static async Task<Result> RefreshGameContainerAsync(Guid gameId)
        {
            try
            {
                var gameResult = await Core.Common.Services.CoreService.Games.GetByIdAsync(
                    gameId,
                    Common.Data.Interfaces.DatabaseComponent.Repository
                );
                if (!gameResult.Success || gameResult.Data is null)
                {
                    return Result.Failure($"Game not found: {gameId}");
                }

                var game = gameResult.Data;
                var client = DiscBotService.Client;
                if (client is null)
                {
                    return Result.Failure("Discord client not available");
                }

                // Get players, deck status, and replay data
                var (
                    team1Players,
                    team2Players,
                    replaySubmittedPlayerIds,
                    replayFilenames,
                    playerDeckCodes,
                    playerDeckConfirmed,
                    replays
                ) = await Core.Common.Services.CoreService.WithDbContext(async db =>
                {
                    // Load players for both teams
                    var allPlayerIds = game.Match.Team1PlayerIds.Concat(game.Match.Team2PlayerIds).ToList();
                    var players = await db
                        .Players.Where(p => allPlayerIds.Contains(p.Id))
                        .Include(p => p.MashinaUser)
                        .ToListAsync();

                    var team1 = players.Where(p => game.Match.Team1PlayerIds.Contains(p.Id)).ToList();
                    var team2 = players.Where(p => game.Match.Team2PlayerIds.Contains(p.Id)).ToList();

                    // Get latest game state snapshot for deck status
                    var latestSnapshot = await db
                        .GameStateSnapshots.Where(gs => gs.GameId == gameId)
                        .OrderByDescending(gs => gs.Timestamp)
                        .FirstOrDefaultAsync();

                    var deckCodes = latestSnapshot?.PlayerDeckCodes ?? new Dictionary<Guid, string>();
                    var deckConfirmed = latestSnapshot?.PlayerDeckConfirmed ?? new HashSet<Guid>();

                    // Get replay data and determine who submitted, including filenames
                    var allReplays = await db
                        .Replays.Where(r => r.GameId == gameId)
                        .Include(r => r.Players)
                        .ToListAsync();

                    var submitted = new HashSet<Guid>();
                    var filenames = new Dictionary<Guid, string>();

                    foreach (var player in players)
                    {
                        foreach (var replay in allReplays)
                        {
                            foreach (var rp in replay.Players)
                            {
                                if (
                                    (
                                        !string.IsNullOrEmpty(rp.PlayerUserId)
                                        && player
                                            .PreviousPlatformIds.GetValueOrDefault("EugenSystems")
                                            ?.Contains(rp.PlayerUserId) == true
                                    )
                                    || (
                                        ExtractSteamId(rp.PlayerAvatar) is string steamId
                                        && player.PreviousPlatformIds.GetValueOrDefault("Steam")?.Contains(steamId)
                                            == true
                                    )
                                    || (
                                        !string.IsNullOrEmpty(rp.PlayerName)
                                        && (
                                            player.GameUsername == rp.PlayerName
                                            || player.PreviousGameUsernames.Contains(rp.PlayerName)
                                        )
                                    )
                                )
                                {
                                    submitted.Add(player.Id);
                                    filenames[player.Id] = replay.OriginalFilename ?? "replay.rpl3";
                                    break;
                                }
                            }
                        }
                    }

                    return (team1, team2, submitted, filenames, deckCodes, deckConfirmed, allReplays);
                });

                // verify that teams are loaded
                if (game.Match.Team1 is null)
                {
                    return Result.Failure("Team 1 not found");
                }
                if (game.Match.Team2 is null)
                {
                    return Result.Failure("Team 2 not found");
                }

                // Filter deck codes to only include players on each team (security: teams should not see opponent's deck codes)
                var team1PlayerIds = team1Players.Select(p => p.Id).ToHashSet();
                var team2PlayerIds = team2Players.Select(p => p.Id).ToHashSet();

                var team1DeckCodes = playerDeckCodes
                    .Where(kvp => team1PlayerIds.Contains(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                var team2DeckCodes = playerDeckCodes
                    .Where(kvp => team2PlayerIds.Contains(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                // Get team ratings for this team size
                var team1Rating = game.Match.Team1.ScrimmageTeamStats.TryGetValue(game.TeamSize, out var team1Stats)
                    ? (int)team1Stats.CurrentRating
                    : 0;
                var team2Rating = game.Match.Team2.ScrimmageTeamStats.TryGetValue(game.TeamSize, out var team2Stats)
                    ? (int)team2Stats.CurrentRating
                    : 0;

                // Update both messages - each showing only their own team with deck status
                await UpdateMessageWithPlayerSectionsAsync(
                    client,
                    game.Match.Team1ThreadId,
                    game.Team1GameContainerMsgId,
                    game.GameNumber,
                    game.Map.Name,
                    game.Match.Team1.Name,
                    team1Players,
                    replaySubmittedPlayerIds,
                    replayFilenames,
                    gameId,
                    team1DeckCodes,
                    playerDeckConfirmed,
                    replays,
                    team2Players,
                    game.Match.Team2.Name,
                    team2Rating,
                    playerDeckCodes
                );
                await UpdateMessageWithPlayerSectionsAsync(
                    client,
                    game.Match.Team2ThreadId,
                    game.Team2GameContainerMsgId,
                    game.GameNumber,
                    game.Map.Name,
                    game.Match.Team2.Name,
                    team2Players,
                    replaySubmittedPlayerIds,
                    replayFilenames,
                    gameId,
                    team2DeckCodes,
                    playerDeckConfirmed,
                    replays,
                    team1Players,
                    game.Match.Team1.Name,
                    team1Rating,
                    playerDeckCodes
                );

                return Result.CreateSuccess("Game container refreshed");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to refresh game container for game {gameId}",
                    nameof(RefreshGameContainerAsync)
                );
                return Result.Failure($"Failed to refresh game container: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the replay status in the game container messages for both teams.
        /// </summary>
        public static async Task<Result> UpdateGameContainerReplayStatusAsync(Guid gameId, Guid submitterPlayerId)
        {
            try
            {
                var gameResult = await Core.Common.Services.CoreService.Games.GetByIdAsync(
                    gameId,
                    Common.Data.Interfaces.DatabaseComponent.Repository
                );
                if (!gameResult.Success || gameResult.Data is null)
                {
                    return Result.Failure($"Game not found: {gameId}");
                }

                var game = gameResult.Data;
                var client = DiscBotService.Client;
                if (client is null)
                {
                    return Result.Failure("Discord client not available");
                }

                // Get players, deck status, and replay data
                var (
                    team1Players,
                    team2Players,
                    replaySubmittedPlayerIds,
                    replayFilenames,
                    playerDeckCodes,
                    playerDeckConfirmed,
                    replays
                ) = await Core.Common.Services.CoreService.WithDbContext(async db =>
                {
                    // Load players for both teams
                    var allPlayerIds = game.Match.Team1PlayerIds.Concat(game.Match.Team2PlayerIds).ToList();
                    var players = await db
                        .Players.Where(p => allPlayerIds.Contains(p.Id))
                        .Include(p => p.MashinaUser)
                        .ToListAsync();

                    var team1 = players.Where(p => game.Match.Team1PlayerIds.Contains(p.Id)).ToList();
                    var team2 = players.Where(p => game.Match.Team2PlayerIds.Contains(p.Id)).ToList();

                    // Get latest game state snapshot for deck status
                    var latestSnapshot = await db
                        .GameStateSnapshots.Where(gs => gs.GameId == gameId)
                        .OrderByDescending(gs => gs.Timestamp)
                        .FirstOrDefaultAsync();

                    var deckCodes = latestSnapshot?.PlayerDeckCodes ?? new Dictionary<Guid, string>();
                    var deckConfirmed = latestSnapshot?.PlayerDeckConfirmed ?? new HashSet<Guid>();

                    // Get replay data and determine who submitted, including filenames
                    var allReplays = await db
                        .Replays.Where(r => r.GameId == gameId)
                        .Include(r => r.Players)
                        .ToListAsync();

                    var submitted = new HashSet<Guid>();
                    var filenames = new Dictionary<Guid, string>();

                    foreach (var player in players)
                    {
                        foreach (var replay in allReplays)
                        {
                            foreach (var rp in replay.Players)
                            {
                                if (
                                    (
                                        !string.IsNullOrEmpty(rp.PlayerUserId)
                                        && player
                                            .PreviousPlatformIds.GetValueOrDefault("EugenSystems")
                                            ?.Contains(rp.PlayerUserId) == true
                                    )
                                    || (
                                        ExtractSteamId(rp.PlayerAvatar) is string steamId
                                        && player.PreviousPlatformIds.GetValueOrDefault("Steam")?.Contains(steamId)
                                            == true
                                    )
                                    || (
                                        !string.IsNullOrEmpty(rp.PlayerName)
                                        && (
                                            player.GameUsername == rp.PlayerName
                                            || player.PreviousGameUsernames.Contains(rp.PlayerName)
                                        )
                                    )
                                )
                                {
                                    submitted.Add(player.Id);
                                    filenames[player.Id] = replay.OriginalFilename ?? "replay.rpl3";
                                    break;
                                }
                            }
                        }
                    }

                    return (team1, team2, submitted, filenames, deckCodes, deckConfirmed, allReplays);
                });

                // verify that teams are loaded
                if (game.Match.Team1 is null)
                {
                    return Result.Failure("Team 1 not found");
                }
                if (game.Match.Team2 is null)
                {
                    return Result.Failure("Team 2 not found");
                }

                // Filter deck codes to only include players on each team (security: teams should not see opponent's deck codes)
                var team1PlayerIds = team1Players.Select(p => p.Id).ToHashSet();
                var team2PlayerIds = team2Players.Select(p => p.Id).ToHashSet();

                var team1DeckCodes = playerDeckCodes
                    .Where(kvp => team1PlayerIds.Contains(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                var team2DeckCodes = playerDeckCodes
                    .Where(kvp => team2PlayerIds.Contains(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                // Get team ratings for this team size
                var team1Rating = game.Match.Team1.ScrimmageTeamStats.TryGetValue(
                    game.TeamSize,
                    out var team1StatsReplay
                )
                    ? (int)team1StatsReplay.CurrentRating
                    : 0;
                var team2Rating = game.Match.Team2.ScrimmageTeamStats.TryGetValue(
                    game.TeamSize,
                    out var team2StatsReplay
                )
                    ? (int)team2StatsReplay.CurrentRating
                    : 0;

                // Update both messages - each showing only their own team with deck status
                await UpdateMessageWithPlayerSectionsAsync(
                    client,
                    game.Match.Team1ThreadId,
                    game.Team1GameContainerMsgId,
                    game.GameNumber,
                    game.Map.Name,
                    game.Match.Team1.Name,
                    team1Players,
                    replaySubmittedPlayerIds,
                    replayFilenames,
                    gameId,
                    team1DeckCodes,
                    playerDeckConfirmed,
                    replays,
                    team2Players,
                    game.Match.Team2.Name,
                    team2Rating,
                    playerDeckCodes
                );
                await UpdateMessageWithPlayerSectionsAsync(
                    client,
                    game.Match.Team2ThreadId,
                    game.Team2GameContainerMsgId,
                    game.GameNumber,
                    game.Map.Name,
                    game.Match.Team2.Name,
                    team2Players,
                    replaySubmittedPlayerIds,
                    replayFilenames,
                    gameId,
                    team2DeckCodes,
                    playerDeckConfirmed,
                    replays,
                    team1Players,
                    game.Match.Team1.Name,
                    team1Rating,
                    playerDeckCodes
                );

                return Result.CreateSuccess("Game container updated");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to update game container replay status for game {gameId}",
                    nameof(UpdateGameContainerReplayStatusAsync)
                );
                return Result.Failure($"Failed to update game container: {ex.Message}");
            }
        }

        private static async Task UpdateMessageWithPlayerSectionsAsync(
            DiscordClient client,
            ulong? threadId,
            ulong? messageId,
            int gameNumber,
            string mapName,
            string teamName,
            List<Core.Common.Models.Common.Player> teamPlayers,
            HashSet<Guid> replaySubmittedPlayerIds,
            Dictionary<Guid, string> replayFilenames,
            Guid gameId,
            Dictionary<Guid, string> playerDeckCodes,
            HashSet<Guid> playerDeckConfirmed,
            List<Core.Common.Models.Common.Replay> replays,
            List<Core.Common.Models.Common.Player> opponentPlayers,
            string opponentTeamName,
            int opponentTeamRating,
            Dictionary<Guid, string> allPlayerDeckCodes
        )
        {
            if (!threadId.HasValue || !messageId.HasValue)
            {
                return;
            }

            try
            {
                var thread = await client.GetChannelAsync(threadId.Value) as DiscordThreadChannel;
                if (thread is null)
                {
                    return;
                }
                var message = await thread.GetMessageAsync(messageId.Value);
                if (message is null)
                {
                    return;
                }

                var (bannerCdn, _) = await DiscBotService.AssetResolver.ResolveGameBannerAsync(
                    $"game_{gameNumber}_banner.jpg"
                );
                var (mapCdn, _) = await DiscBotService.AssetResolver.ResolveMapThumbnailAsync(mapName);

                var components = new List<DiscordComponent>();
                if (bannerCdn is not null)
                {
                    components.Add(new DiscordMediaGalleryComponent([new DiscordMediaGalleryItem(bannerCdn)]));
                }
                components.Add(new DiscordSeparatorComponent());
                if (mapCdn is not null)
                {
                    components.Add(new DiscordMediaGalleryComponent([new DiscordMediaGalleryItem(mapCdn)]));
                }
                components.Add(new DiscordSeparatorComponent());

                // Add match info section - need to get team ratings from database
                // TODO: Get actual team ratings from game/match data
                var matchInfoText = $"**{teamName}** - Game {gameNumber} - **{mapName}**";
                components.Add(new DiscordSectionComponent(new DiscordTextDisplayComponent(matchInfoText), null!));

                components.Add(new DiscordSeparatorComponent());

                // Add player section components with updated status - only for this team
                // Build HashSet of players who submitted decks (have deck codes in snapshot)
                var deckSubmittedPlayerIds = teamPlayers
                    .Where(p => playerDeckCodes.ContainsKey(p.Id))
                    .Select(p => p.Id)
                    .ToHashSet();

                await AddPlayerSectionComponentsAsync(
                    components,
                    teamPlayers,
                    deckCodes: playerDeckCodes,
                    deckSubmittedPlayerIds,
                    replayFilenames
                );

                // Add separator before opponent status section
                components.Add(new DiscordSeparatorComponent());

                // Add opponent status section - shows opponent deck submission status (but not actual deck codes)
                var opponentDeckSubmittedPlayerIds = opponentPlayers
                    .Where(p => allPlayerDeckCodes.ContainsKey(p.Id))
                    .Select(p => p.Id)
                    .ToHashSet();

                await AddOpponentStatusSectionAsync(
                    components,
                    opponentPlayers,
                    opponentTeamName,
                    opponentTeamRating,
                    opponentPlayers.Count,
                    opponentDeckSubmittedPlayerIds
                );

                // Add separator after opponent status section
                components.Add(new DiscordSeparatorComponent());

                // Check if game is completed and get winner
                var isGameCompleted = false;
                Guid? winnerTeamId = null;
                Guid? currentTeamId = null;

                // Fetch game state to check if completed
                var gameStateResult = await Core.Common.Services.CoreService.WithDbContext(async db =>
                {
                    var game = await db
                        .Games.Include(g => g.StateHistory)
                        .Include(g => g.Match)
                        .Where(g => g.Id == gameId)
                        .FirstOrDefaultAsync();

                    if (game is not null)
                    {
                        var currentSnapshot = Core.Common.Models.Common.MatchCore.Accessors.GetCurrentSnapshot(game);
                        var completed = currentSnapshot.CompletedAt.HasValue;
                        var winner = currentSnapshot.WinnerId;

                        // Determine current team ID based on which team the players belong to
                        Guid? teamId = null;
                        if (teamPlayers.Count > 0 && game.Match is not null)
                        {
                            if (game.Match.Team1PlayerIds.Contains(teamPlayers[0].Id))
                            {
                                teamId = game.Match.Team1Id;
                            }
                            else if (game.Match.Team2PlayerIds.Contains(teamPlayers[0].Id))
                            {
                                teamId = game.Match.Team2Id;
                            }
                        }

                        return (completed, winner, teamId);
                    }

                    return (false, null, null);
                });

                isGameCompleted = gameStateResult.completed;
                winnerTeamId = gameStateResult.winner;
                currentTeamId = gameStateResult.teamId;

                // Add final action component based on game state
                await AddFinalActionComponentAsync(
                    components,
                    gameId,
                    teamPlayers,
                    deckSubmittedPlayerIds,
                    playerDeckConfirmed,
                    replaySubmittedPlayerIds,
                    isGameCompleted,
                    winnerTeamId,
                    currentTeamId
                );

                await message.ModifyAsync(
                    new DiscordMessageBuilder().AddContainerComponent(new DiscordContainerComponent(components))
                );
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to update message {messageId} in thread {threadId}",
                    nameof(UpdateMessageWithPlayerSectionsAsync)
                );
            }
        }

        private static string? ExtractSteamId(string? avatarUrl)
        {
            if (string.IsNullOrEmpty(avatarUrl))
            {
                return null;
            }
            var segments = avatarUrl.Split('/');
            return
                segments.Length >= 3
                && segments[^2].Equals("SteamGamerPicture", StringComparison.OrdinalIgnoreCase)
                && long.TryParse(segments[^1], out _)
                ? segments[^1]
                : null;
        }

        /// <summary>
        /// Builds the final action row based on game state.
        /// Handles deck submission, confirmation, replay submission, and game results display.
        /// All states include a "Refresh Opponent Status" button.
        /// </summary>
        /// <param name="components">List to add components to</param>
        /// <param name="gameId">Game ID</param>
        /// <param name="teamPlayers">Players on this team</param>
        /// <param name="deckSubmittedPlayerIds">Players who have submitted decks</param>
        /// <param name="deckConfirmedPlayerIds">Players who have confirmed their decks</param>
        /// <param name="replaySubmittedPlayerIds">Players who have submitted replays</param>
        /// <param name="isGameCompleted">Whether the game has been completed</param>
        /// <param name="winnerTeamId">The team ID that won the game (if completed)</param>
        /// <param name="currentTeamId">The current team's ID (to determine if they won or lost)</param>
        private static Task AddFinalActionComponentAsync(
            List<DiscordComponent> components,
            Guid gameId,
            List<Core.Common.Models.Common.Player> teamPlayers,
            HashSet<Guid> deckSubmittedPlayerIds,
            HashSet<Guid> deckConfirmedPlayerIds,
            HashSet<Guid> replaySubmittedPlayerIds,
            bool isGameCompleted = false,
            Guid? winnerTeamId = null,
            Guid? currentTeamId = null
        )
        {
            var allPlayerIds = teamPlayers.Select(p => p.Id).ToHashSet();
            var buttons = new List<DiscordButtonComponent>();

            // State 5: Game completed -> Show game results
            if (isGameCompleted && winnerTeamId.HasValue && currentTeamId.HasValue)
            {
                var isWinner = winnerTeamId.Value == currentTeamId.Value;
                var resultText = isWinner ? "🏆 **VICTORY** 🏆" : "💔 **DEFEAT** 💔";
                var resultComponent = new DiscordSectionComponent(new DiscordTextDisplayComponent(resultText), null!);
                components.Add(resultComponent);

                // No action buttons for completed games
                return Task.CompletedTask;
            }

            // State 1: Not all players have submitted decks -> Show "Submit Deck Code" button
            if (!allPlayerIds.All(deckSubmittedPlayerIds.Contains))
            {
                var submitDeckButton = new DiscordButtonComponent(
                    DiscordButtonStyle.Primary,
                    $"open_deck_modal_{gameId}",
                    "Submit Deck Code"
                );
                buttons.Add(submitDeckButton);
            }
            // State 2: Not all players have confirmed decks -> Show "Confirm" and "Revise" buttons
            else if (!allPlayerIds.All(deckConfirmedPlayerIds.Contains))
            {
                var confirmButton = new DiscordButtonComponent(
                    DiscordButtonStyle.Success,
                    $"confirm_deck_{gameId}",
                    "Confirm Deck"
                );
                var reviseButton = new DiscordButtonComponent(
                    DiscordButtonStyle.Secondary,
                    $"revise_deck_{gameId}",
                    "Revise Deck"
                );
                buttons.Add(confirmButton);
                buttons.Add(reviseButton);
            }
            // State 3: Not all players have submitted replays -> Show "Submit Replay File" button
            else if (!allPlayerIds.All(replaySubmittedPlayerIds.Contains))
            {
                var replaySubmitButton = new DiscordButtonComponent(
                    DiscordButtonStyle.Primary,
                    $"open_replay_modal_{gameId}",
                    "Submit Replay File"
                );
                buttons.Add(replaySubmitButton);
            }
            // State 4: All replays submitted -> Button disappears (no buttons except refresh)

            // Always add "Refresh Opponent Status" button to all states (except completed)
            if (!isGameCompleted)
            {
                var refreshButton = new DiscordButtonComponent(
                    DiscordButtonStyle.Secondary,
                    $"refresh_opponent_{gameId}",
                    "Refresh Opponent Status"
                );
                buttons.Add(refreshButton);

                // Add "Forfeit Game" button (which forfeits the entire match)
                var forfeitButton = new DiscordButtonComponent(
                    DiscordButtonStyle.Danger,
                    $"forfeit_game_{gameId}",
                    "Forfeit Game"
                );
                buttons.Add(forfeitButton);
            }

            // Add action row if we have any buttons
            if (buttons.Count > 0)
            {
                components.Add(new DiscordActionRowComponent(buttons));
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Adds player section components for a team to the components list.
        /// Each player gets a SectionComponent with their deck info and division icon.
        /// Replay files are inserted immediately after each player's section.
        /// </summary>
        private static async Task AddPlayerSectionComponentsAsync(
            List<DiscordComponent> components,
            List<Core.Common.Models.Common.Player> players,
            Dictionary<Guid, string> deckCodes,
            HashSet<Guid> deckSubmitted,
            Dictionary<Guid, string> replayFiles
        )
        {
            foreach (var player in players)
            {
                var playerName =
                    player.GameUsername
                    ?? player.MashinaUser?.DiscordUsername
                    ?? player.MashinaUser?.DiscordGlobalname
                    ?? "Unknown";
                var discordMention = player.MashinaUser?.DiscordMention ?? "";
                var hasDeck = deckSubmitted.Contains(player.Id);
                var deckCode = deckCodes.GetValueOrDefault(player.Id);

                // Build player info text based on deck submission status
                string playerInfoText;
                string iconFileName;

                if (hasDeck && !string.IsNullOrEmpty(deckCode))
                {
                    // After deck submission: show name, mention, division, and deck code
                    // TODO: Get actual division name from deck code parsing
                    var divisionName = "???"; // Placeholder until deck parsing is implemented
                    playerInfoText = $"**{playerName}** {discordMention} **__{divisionName}__**\n`{deckCode}`";
                    iconFileName = "division_icon.jpg"; // TODO: Use actual division icon
                }
                else
                {
                    // Before deck submission: show name, mention, and waiting message
                    playerInfoText = $"**{playerName}** {discordMention} *__???__*\n*Waiting for deck code...*";
                    iconFileName = "division_notsubmitted.jpg";
                }

                // Resolve division icon using AssetResolver
                var (iconUrl, iconAttachment) = await DiscBotService.AssetResolver.ResolveDivisionIconAsync(
                    iconFileName
                );

                // Build the section component with thumbnail accessory
                DiscordComponent sectionComponent;
                if (iconUrl is not null)
                {
                    var thumbnailComponent = new DiscordThumbnailComponent(iconUrl);
                    sectionComponent = new DiscordSectionComponent(
                        new DiscordTextDisplayComponent(playerInfoText),
                        thumbnailComponent
                    );
                }
                else
                {
                    // Fallback without accessory
                    sectionComponent = new DiscordSectionComponent(
                        new DiscordTextDisplayComponent(playerInfoText),
                        null!
                    );
                }

                components.Add(sectionComponent);

                // Insert DiscordFileComponent right after this player if they have a replay
                if (replayFiles.TryGetValue(player.Id, out var replayFileName))
                {
                    // Use attachment:// reference for the replay file
                    var fileComponent = new DiscordFileComponent($"attachment://{replayFileName}", isSpoilered: false);
                    components.Add(fileComponent);
                }
            }

            // Add separator after all players
            components.Add(new DiscordSeparatorComponent(true));
        }

        /// <summary>
        /// Adds an opponent status section to show which opponents have submitted their decks (without showing actual deck codes).
        /// </summary>
        private static Task AddOpponentStatusSectionAsync(
            List<DiscordComponent> components,
            List<Core.Common.Models.Common.Player> opponentPlayers,
            string opponentTeamName,
            int opponentTeamRating,
            int rosterSize,
            HashSet<Guid> deckSubmittedPlayerIds
        )
        {
            // Build opponent status text
            var opponentStatusLines = new List<string>
            {
                $"Opponent Team: [{opponentTeamRating}] **{opponentTeamName}** - *{rosterSize}v{rosterSize} Roster*",
            };

            foreach (var player in opponentPlayers)
            {
                var playerName =
                    player.GameUsername
                    ?? player.MashinaUser?.DiscordUsername
                    ?? player.MashinaUser?.DiscordGlobalname
                    ?? "Unknown";
                var discordMention = player.MashinaUser?.DiscordMention ?? "";
                var hasDeck = deckSubmittedPlayerIds.Contains(player.Id);

                if (hasDeck)
                {
                    // Show that they've submitted (with division name placeholder)
                    // TODO: Parse actual division name from deck code
                    var divisionName = "???";
                    opponentStatusLines.Add($"**{playerName}** {discordMention} **__{divisionName}__**");
                }
                else
                {
                    // Show that they're still waiting
                    opponentStatusLines.Add($"**{playerName}** {discordMention} *Waiting...*");
                }
            }

            var opponentStatusText = string.Join("\n", opponentStatusLines);
            var opponentStatusSection = new DiscordSectionComponent(
                new DiscordTextDisplayComponent(opponentStatusText),
                null!
            );

            components.Add(opponentStatusSection);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Adds a file attachment to the message builder using the AttachmentHint.
        /// The AttachmentHint should have already been resolved by AssetResolver with path information.
        /// Reads the file into memory to avoid stream disposal issues.
        /// </summary>
        private static async Task AddFileAttachmentAsync(
            DiscordMessageBuilder builder,
            AttachmentHint attachmentHint,
            string fileType
        )
        {
            var filePath = !string.IsNullOrEmpty(attachmentHint.RelativePathUnderAppBase)
                ? Path.Combine(AppContext.BaseDirectory, attachmentHint.RelativePathUnderAppBase)
                : Path.Combine(AppContext.BaseDirectory, attachmentHint.CanonicalFileName);

            if (File.Exists(filePath))
            {
                // Read file into memory to avoid stream disposal issues
                byte[] fileData = await File.ReadAllBytesAsync(filePath);
                var memoryStream = new MemoryStream(fileData);
                builder.AddFile(attachmentHint.CanonicalFileName, memoryStream);
            }
            else
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    new FileNotFoundException($"Attachment file not found: {filePath}"),
                    $"Failed to attach {fileType}: {attachmentHint.CanonicalFileName}",
                    nameof(AddFileAttachmentAsync)
                );
            }
        }
    }
}
