using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Services;
using WabbitBot.DiscBot.App.Services.DiscBot;
using WabbitBot.DiscBot.App.Utilities;

namespace WabbitBot.DiscBot.App.Renderers
{
    public class ChallengerPlayerInfo
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public ulong DiscordUserId { get; set; }
    }

    public static class ScrimmageRenderer
    {
        /// <summary>
        /// Renders a challenge container and sends it to the scrimmage channel.
        /// </summary>
        /// <param name="challengeId">The challenge ID</param>
        /// <param name="teamSize">Team size for roster group filtering</param>
        /// <param name="challengerTeamName">Name of the challenger team</param>
        /// <param name="opponentTeamName">Name of the opponent team</param>
        /// <returns>Result containing the channel and message</returns>
        public static async Task<Result<ChallengeContainerResult>> RenderChallengeContainerAsync(
            Guid challengeId,
            TeamSize teamSize,
            string challengerTeamName,
            string opponentTeamName,
            Team opponentTeam
        )
        {
            try
            {
                Console.WriteLine($"🔍 DEBUG: RenderChallengeContainerAsync called with:");
                Console.WriteLine($"   ChallengeId: {challengeId}");
                Console.WriteLine($"   TeamSize: {teamSize}");
                Console.WriteLine($"   ChallengerTeamName: {challengerTeamName}");
                Console.WriteLine($"   OpponentTeamName: {opponentTeamName}");
                Console.WriteLine($"   OpponentTeam.Id: {opponentTeam.Id}");

                Console.WriteLine($"🔍 DEBUG: About to resolve challenge banner image...");
                Console.WriteLine($"🔍 DEBUG: AppContext.BaseDirectory: {AppContext.BaseDirectory}");
                Console.WriteLine($"🔍 DEBUG: Current working directory: {Environment.CurrentDirectory}");

                // Get the challenge banner image (default or custom)
                var (challengeBannerUrl, challengeBannerHint, isCdnUrl) =
                    DiscBotService.AssetResolver.ResolveDiscordComponentImage("challenge_banner.jpg");
                Console.WriteLine(
                    $"🔍 DEBUG: Challenge banner resolved: Url={challengeBannerUrl}, Hint={challengeBannerHint}, IsCdn={isCdnUrl}"
                );

                if (challengeBannerUrl is null && challengeBannerHint is null)
                {
                    return Result<ChallengeContainerResult>.Failure("Challenge banner image not found");
                }

                // Create media gallery item - use the boolean to determine type
                DiscordMediaGalleryItem? mediaItem = null;
                if (!string.IsNullOrEmpty(challengeBannerUrl))
                {
                    if (isCdnUrl)
                    {
                        // It's a CDN URL, use it directly
                        mediaItem = new DiscordMediaGalleryItem(challengeBannerUrl);
                    }
                    else if (challengeBannerHint is not null)
                    {
                        // It's a local file, reference it as attachment
                        mediaItem = new DiscordMediaGalleryItem(
                            "attachment://" + challengeBannerHint.CanonicalFileName
                        );
                    }
                }

                // Get team IDs from the challenge
                var challengerTeamId = await CoreService.WithDbContext(async db =>
                {
                    return await db
                        .ScrimmageChallenges.Where(c => c.Id == challengeId)
                        .Select(c => c.ChallengerTeamId)
                        .FirstOrDefaultAsync();
                });

                var opponentTeamId = opponentTeam.Id;

                // Get team captains and ratings
                var challengerCaptain = await CoreService.WithDbContext(async db =>
                {
                    return await db
                        .Teams.Where(t => t.Id == challengerTeamId)
                        .Select(t => new { t.TeamMajorId, t.Name })
                        .FirstOrDefaultAsync();
                });

                var opponentCaptain = await CoreService.WithDbContext(async db =>
                {
                    return await db
                        .Teams.Where(t => t.Id == opponentTeamId)
                        .Select(t => new { t.TeamMajorId, t.Name })
                        .FirstOrDefaultAsync();
                });

                // Get captain Discord mentions
                var challengerCaptainMention = await CoreService.WithDbContext(async db =>
                {
                    if (challengerCaptain?.TeamMajorId is null)
                        return "Unknown Captain";
                    var captain = await db
                        .Players.Include(p => p.MashinaUser)
                        .FirstOrDefaultAsync(p => p.Id == challengerCaptain.TeamMajorId);
                    return captain?.MashinaUser?.DiscordUserId is not null
                        ? $"<@{captain.MashinaUser.DiscordMention}>"
                        : "Unknown Captain";
                });

                var opponentCaptainMention = await CoreService.WithDbContext(async db =>
                {
                    if (opponentCaptain?.TeamMajorId is null)
                        return "Unknown Captain";
                    var captain = await db
                        .Players.Include(p => p.MashinaUser)
                        .FirstOrDefaultAsync(p => p.Id == opponentCaptain.TeamMajorId);
                    return captain?.MashinaUser?.DiscordUserId is not null
                        ? $"<@{captain.MashinaUser.DiscordMention}>"
                        : "Unknown Captain";
                });

                // Get team ratings (placeholder for now - would need ScrimmageTeamStats)
                var challengerRating = "1500"; // TODO: Get from ScrimmageTeamStats
                var opponentRating = "1500"; // TODO: Get from ScrimmageTeamStats

                // Get challenger team players who are actually playing
                var challengerPlayers = await CoreService.WithDbContext(async db =>
                {
                    var challenge = await db.ScrimmageChallenges.FirstOrDefaultAsync(c => c.Id == challengeId);

                    if (challenge == null)
                        return new List<ChallengerPlayerInfo>();

                    // Construct full player list: issuer + teammates
                    var playerIds = new List<Guid> { challenge.IssuedByPlayerId };
                    playerIds.AddRange(challenge.ChallengerTeammateIds);

                    return await db
                        .Players.Where(p => playerIds.Contains(p.Id))
                        .Include(p => p.MashinaUser)
                        .Select(p => new ChallengerPlayerInfo
                        {
                            Id = p.Id,
                            DisplayName = string.IsNullOrEmpty(p.MashinaUser!.DiscordGlobalname)
                                ? p.MashinaUser.DiscordUsername ?? "Unknown"
                                : p.MashinaUser.DiscordGlobalname,
                            DiscordUserId = p.MashinaUser.DiscordUserId,
                        })
                        .ToListAsync();
                });

                // Get opponent team roster for teammate selection (include all active members)
                var opponentRoster = await CoreService.WithDbContext(async db =>
                {
                    return await db
                        .Teams.Where(t => t.Id == opponentTeamId)
                        .SelectMany(t => t.Rosters)
                        .Where(r => r.RosterGroup == TeamCore.TeamSizeRosterGrouping.GetRosterGroup(teamSize))
                        .SelectMany(r => r.RosterMembers)
                        .Where(tm => tm.ValidTo == null)
                        .Include(tm => tm.MashinaUser)
                        .Select(tm => new
                        {
                            tm.PlayerId,
                            DisplayName = string.IsNullOrEmpty(tm.MashinaUser!.DiscordGlobalname)
                                ? tm.MashinaUser.DiscordUsername ?? "Unknown"
                                : tm.MashinaUser.DiscordGlobalname,
                            DiscordUserId = tm.MashinaUser.DiscordUserId,
                        })
                        .ToListAsync();
                });

                // Create teammate selection options
                var teammateOptions = opponentRoster
                    .Select(member => new DiscordSelectComponentOption(
                        member.DisplayName,
                        member.PlayerId.ToString(),
                        description: $"Select {member.DisplayName} as teammate"
                    ))
                    .ToList();

                // Create the challenge container with proper structure
                var challengeContainer = new DiscordContainerComponent(
                    components:
                    [
                        // 1. Media gallery for banner
                        mediaItem != null
                            ? new DiscordMediaGalleryComponent(items: [mediaItem])
                            : new DiscordTextDisplayComponent(content: "🎮 Challenge"),
                        // 2. Separator
                        new DiscordSeparatorComponent(true),
                        // 3. Challenge information
                        new DiscordTextDisplayComponent(
                            content: $"## {challengerTeamName} ({challengerRating})\n\n"
                                + $"**Challengers:** {string.Join(", ", challengerPlayers.Select(p => $"<@{p.DiscordUserId}>"))}\n\n"
                                + $"{opponentCaptainMention} {opponentTeamName} ({opponentRating}) has been challenged to a Scrimmage Match."
                        ),
                        // 4. Separator
                        new DiscordSeparatorComponent(true),
                        // 5. Teammate selection (if not 1v1)
                        teamSize != TeamSize.OneVOne
                            ? new DiscordActionRowComponent(
                                [
                                    new DiscordSelectComponent(
                                        $"select_teammates_{challengeId}",
                                        "Select Your Team Players",
                                        teammateOptions,
                                        minOptions: teamSize.GetPlayersPerTeam(),
                                        maxOptions: teamSize.GetPlayersPerTeam()
                                    ),
                                ]
                            )
                            : new DiscordTextDisplayComponent(
                                content: $"**1v1 Match** - {opponentCaptainMention} will play solo"
                            ),
                        // 6. Action buttons
                        new DiscordActionRowComponent(
                            [
                                new DiscordButtonComponent(
                                    DiscordButtonStyle.Success,
                                    $"accept_challenge_{challengeId}",
                                    "Accept Challenge"
                                ),
                                new DiscordButtonComponent(
                                    DiscordButtonStyle.Secondary,
                                    $"decline_challenge_{challengeId}",
                                    "Decline Challenge"
                                ),
                                new DiscordButtonComponent(
                                    DiscordButtonStyle.Danger,
                                    $"cancel_challenge_{challengeId}",
                                    "Cancel Challenge"
                                ),
                            ]
                        ),
                    ],
                    isSpoilered: false,
                    color: EmbedStyling.GetChallengeColor()
                );

                // Get the TeamSizeRosterGroup for the TeamSize
                var teamSizeRosterGroup = TeamCore.TeamSizeRosterGrouping.GetRosterGroup(teamSize);

                // Query for opponent team members to get Discord user IDs for mentions
                var allowedOpponentMentions = await CoreService.WithDbContext(async db =>
                {
                    return await db
                        .TeamMembers.Where(tm =>
                            tm.TeamRoster.TeamId == opponentTeam.Id
                            && tm.TeamRoster.RosterGroup == teamSizeRosterGroup
                            && tm.ReceiveScrimmagePings
                            && tm.MashinaUser != null
                        )
                        .Select(tm => tm.MashinaUser!.DiscordUserId)
                        .ToListAsync();
                });

                var message = new DiscordMessageBuilder()
                    .EnableV2Components()
                    .AddContainerComponent(challengeContainer)
                    .WithAllowedMentions(allowedOpponentMentions.Select(id => new UserMention(id)).Cast<IMention>());

                // Add the image file as an attachment if we have a local file
                if (challengeBannerHint is not null && !string.IsNullOrEmpty(challengeBannerUrl) && !isCdnUrl)
                {
                    message.AddFile(challengeBannerHint.CanonicalFileName, File.OpenRead(challengeBannerUrl));
                }

                Console.WriteLine(
                    $"🔍 DEBUG: About to send challenge container to channel: {DiscBotService.ChallengeFeedChannel?.Id}"
                );
                // if (DiscBotService.ChallengeFeedChannel is null || DiscBotService.ChallengeFeedChannel.Id == 0)
                // {
                //     return Result<ChallengeContainerResult>.Failure("Challenge feed channel not found");
                // }
                var sentMessage = await message.SendAsync(DiscBotService.ChallengeFeedChannel!);
                Console.WriteLine($"🔍 DEBUG: Challenge container sent successfully, message ID: {sentMessage.Id}");
                return Result<ChallengeContainerResult>.CreateSuccess(
                    new ChallengeContainerResult(DiscBotService.ChallengeFeedChannel!, sentMessage)
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔍 DEBUG: Exception in RenderChallengeContainerAsync: {ex.Message}");
                Console.WriteLine($"🔍 DEBUG: Exception type: {ex.GetType().Name}");
                Console.WriteLine($"🔍 DEBUG: Stack trace: {ex.StackTrace}");
                return Result<ChallengeContainerResult>.Failure($"Failed to render challenge container: {ex.Message}");
            }
        }

        /// <summary>
        /// Renders a cancelled challenge container from an existing challenge container.
        /// Keeps the banner image, replaces text with cancellation message, and removes all buttons.
        /// </summary>
        /// <param name="existingContainer">The original challenge container</param>
        /// <param name="challengerTeamName">Name of the challenger team</param>
        /// <param name="opponentTeamName">Name of the opponent team</param>
        /// <param name="cancelledBy">User who cancelled the challenge</param>
        /// <returns>Result containing the cancelled container</returns>
        public static Result<DiscordContainerComponent> RenderCancelledChallengeContainer(
            DiscordContainerComponent existingContainer,
            string challengerTeamName,
            string opponentTeamName,
            DiscordUser cancelledBy
        )
        {
            try
            {
                // Build a new container from the existing one, removing buttons and updating text
                var updatedComponents = new List<DiscordComponent>();

                foreach (var component in existingContainer.Components)
                {
                    // Keep media gallery (banner image)
                    if (component is DiscordMediaGalleryComponent gallery)
                    {
                        updatedComponents.Add(gallery);
                    }
                    // Replace text display with cancellation message
                    else if (component is DiscordTextDisplayComponent)
                    {
                        updatedComponents.Add(
                            new DiscordTextDisplayComponent(
                                content: $"~~{challengerTeamName} vs. {opponentTeamName}~~\n\n"
                                    + $"**This challenge has been cancelled by {cancelledBy.Mention}.**"
                            )
                        );
                    }
                    // Skip buttons - they won't be added to updatedComponents
                }

                // Create the updated container with red error color
                var cancelledContainer = new DiscordContainerComponent(
                    updatedComponents,
                    isSpoilered: false,
                    color: EmbedStyling.GetErrorColor()
                );

                return Result<DiscordContainerComponent>.CreateSuccess(
                    cancelledContainer,
                    "Cancelled challenge container rendered"
                );
            }
            catch (Exception ex)
            {
                return Result<DiscordContainerComponent>.Failure(
                    $"Failed to render cancelled challenge container: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Renders an interactive challenge configuration container for user to configure challenge details.
        /// </summary>
        public static async Task<Result<DiscordContainerComponent>> RenderChallengeConfigurationAsync(
            ulong discordUserId,
            TeamSize teamSize,
            Guid challengerTeamId,
            TeamSizeRosterGroup challengerRosterGroup,
            Guid? selectedOpponentTeamId = null,
            List<Guid>? selectedPlayerIds = null,
            int? bestOf = null
        )
        {
            try
            {
                var components = new List<DiscordComponent>();
                var rosterGroup = TeamCore.TeamSizeRosterGrouping.GetRosterGroup(teamSize);

                // Load team and roster fresh to avoid lazy-loading after context disposal
                var loadResult = await CoreService.WithDbContext(async db =>
                {
                    var team = await db
                        .Teams.AsNoTracking()
                        .Include(t => t.Rosters)
                        .ThenInclude(r => r.RosterMembers)
                        .ThenInclude(rm => rm.MashinaUser)
                        .FirstOrDefaultAsync(t => t.Id == challengerTeamId);

                    var roster = team?.Rosters.FirstOrDefault(r => r.RosterGroup == challengerRosterGroup);
                    return new { team, roster };
                });

                var challengerTeam = loadResult.team;
                var challengerRoster = loadResult.roster;

                if (challengerTeam is null || challengerRoster is null)
                {
                    return Result<DiscordContainerComponent>.Failure("Failed to load team/roster for configuration");
                }

                // Header text
                var headerText =
                    $"# Configure Scrimmage Challenge\n\n"
                    + $"**Team Size:** {teamSize.ToSizeString()}\n"
                    + $"**Your Team:** {challengerTeam.Name}";
                components.Add(new DiscordTextDisplayComponent(headerText));

                // Load available opponent teams
                var opponentTeamsResult = await CoreService.WithDbContext(async db =>
                {
                    var userTeamIds =
                        await db
                            .Players.Where(p => p.MashinaUser.DiscordUserId == discordUserId)
                            .Select(p => p.TeamIds)
                            .FirstOrDefaultAsync() ?? [];

                    return await db
                        .Teams.Where(t => t.Rosters.Any(r => r.RosterGroup == rosterGroup))
                        .Where(t => !userTeamIds.Contains(t.Id))
                        .Where(t => !t.Matches.Any(m => m.CompletedAt == null))
                        .OrderBy(t => t.Name)
                        .Select(t => new { t.Id, t.Name })
                        .Take(25)
                        .ToListAsync();
                });

                // Opponent team select menu
                var opponentOptions = opponentTeamsResult
                    .Select(t => new DiscordSelectComponentOption(
                        t.Name,
                        t.Id.ToString(),
                        isDefault: selectedOpponentTeamId == t.Id
                    ))
                    .ToList();

                if (opponentOptions.Count == 0)
                {
                    components.Add(
                        new DiscordTextDisplayComponent("⚠️ No available opponent teams found for this game size.")
                    );
                }
                else
                {
                    var opponentSelect = new DiscordSelectComponent(
                        $"challenge_opponent_{challengerTeam.Id}",
                        "Select Opponent Team",
                        opponentOptions,
                        minOptions: 0,
                        maxOptions: 1
                    );
                    components.Add(new DiscordActionRowComponent([opponentSelect]));
                }

                // Player selection (from challenger roster)
                // The challenge issuer is automatically included, so they only need to select teammates
                var activeRosterMembers = challengerRoster
                    .RosterMembers.Where(m => m.ValidTo == null && m.MashinaUser is not null)
                    .OrderByDescending(m => m.Role == RosterRole.Captain)
                    .ThenBy(m => m.MashinaUser!.DiscordUsername)
                    .ToList();

                // Find the challenge issuer
                var issuerMember = activeRosterMembers.FirstOrDefault(m =>
                    m.MashinaUser!.DiscordUserId == discordUserId
                );
                var issuerPlayerId = issuerMember?.PlayerId;

                var requiredPlayers = teamSize.GetPlayersPerTeam();
                var teammatesRequired = requiredPlayers - 1; // Issuer is auto-included

                // Filter out the issuer from selectable options if this is not a 1v1
                var selectableMembers =
                    teammatesRequired > 0
                        ? activeRosterMembers.Where(m => m.MashinaUser!.DiscordUserId != discordUserId).ToList()
                        : activeRosterMembers;

                var playerOptions = selectableMembers
                    .Select(m => new DiscordSelectComponentOption(
                        m.MashinaUser!.DiscordGlobalname ?? m.MashinaUser.DiscordUsername ?? "Unknown",
                        m.PlayerId.ToString(),
                        isDefault: selectedPlayerIds?.Contains(m.PlayerId) ?? false
                    ))
                    .ToList();

                if (teammatesRequired > 0)
                {
                    var playerSelect = new DiscordSelectComponent(
                        $"challenge_players_{challengerTeam.Id}_{issuerPlayerId}",
                        $"Select Your Teammates ({teammatesRequired} required)",
                        playerOptions,
                        minOptions: teammatesRequired,
                        maxOptions: teammatesRequired
                    );
                    components.Add(new DiscordActionRowComponent([playerSelect]));
                }
                else
                {
                    // For 1v1, no selection needed - issuer is the only player
                    components.Add(
                        new DiscordTextDisplayComponent($"**Player:** <@{discordUserId}> (you) - auto-selected")
                    );
                }

                // Best of selection - only render if configuration allows BO3
                var config = ConfigurationProvider.GetConfigurationService();
                var scrimmageOptions = config.GetSection<ScrimmageOptions>("Bot:Scrimmage");
                var maxBestOf = scrimmageOptions.BestOf;

                // Only show best-of selector if BO3 is enabled (scrimmages max at BO3, never BO5)
                if (maxBestOf >= 3)
                {
                    var bestOfOptions = new List<DiscordSelectComponentOption>
                    {
                        new("Best of 1", "1", isDefault: bestOf == 1 || bestOf is null),
                        new("Best of 3", "3", isDefault: bestOf == 3),
                    };

                    var bestOfSelect = new DiscordSelectComponent(
                        $"challenge_bestof_{challengerTeam.Id}",
                        "Select Match Length",
                        bestOfOptions,
                        minOptions: 1,
                        maxOptions: 1
                    );
                    components.Add(new DiscordActionRowComponent([bestOfSelect]));
                }

                components.Add(new DiscordSeparatorComponent(true));

                // Use WithDbContext to access navigation properties within the same context
                var opponentInfoText = await CoreService.WithDbContext(async dbContext =>
                {
                    if (selectedOpponentTeamId is null)
                    {
                        return string.Empty;
                    }
                    var opponentTeam = await dbContext
                        .Teams.Include(t => t.Rosters)
                        .ThenInclude(r => r.RosterMembers)
                        .ThenInclude(rm => rm.MashinaUser)
                        .FirstOrDefaultAsync(t => t.Id == selectedOpponentTeamId.Value);

                    if (opponentTeam is null)
                    {
                        // This should never happen if the UI is working correctly
                        // Log the error and return a generic message
                        Console.WriteLine(
                            $"ERROR: Selected opponent team {selectedOpponentTeamId} not found in database"
                        );
                        return "⚠️ Selected team is no longer available. Please refresh and try again.";
                    }

                    var opponentRoster = opponentTeam.Rosters.FirstOrDefault(r => r.RosterGroup == rosterGroup);
                    if (opponentRoster is null)
                        return $"# {opponentTeam.Name}";

                    // Get captain info
                    var captain = opponentRoster.RosterMembers.FirstOrDefault(m =>
                        m.Role == RosterRole.Captain && m.ValidTo == null
                    );

                    var infoText = $"\n# {opponentTeam.Name}";
                    if (captain?.MashinaUser is not null)
                    {
                        infoText += $"\n✪ {captain.MashinaUser.DiscordMention}";
                    }

                    // Show active roster members
                    var activeMembers = opponentRoster
                        .RosterMembers.Where(m =>
                            m.ValidTo == null && m.MashinaUser is not null && m.PlayerId != issuerPlayerId
                        )
                        .ToList();

                    if (activeMembers.Any())
                    {
                        foreach (var member in activeMembers)
                        {
                            if (member.Role != RosterRole.Captain)
                                infoText += $"\n     {member.MashinaUser!.DiscordMention}";
                        }
                    }

                    return infoText;
                });

                if (!string.IsNullOrEmpty(opponentInfoText))
                {
                    components.Add(new DiscordTextDisplayComponent(opponentInfoText));
                }

                // Action buttons
                // Note: selectedPlayerIds includes the issuer, so check against requiredPlayers not teammatesRequired
                var issueButton = new DiscordButtonComponent(
                    DiscordButtonStyle.Success,
                    $"challenge_issue_{challengerTeam.Id}_{discordUserId}",
                    "Issue Challenge",
                    disabled: selectedOpponentTeamId is null
                        || selectedPlayerIds is null
                        || selectedPlayerIds.Count != requiredPlayers
                );

                var cancelButton = new DiscordButtonComponent(
                    DiscordButtonStyle.Secondary,
                    $"challenge_cancel_{challengerTeam.Id}_{discordUserId}",
                    "Cancel"
                );

                components.Add(new DiscordActionRowComponent([issueButton, cancelButton]));

                return Result<DiscordContainerComponent>.CreateSuccess(
                    new DiscordContainerComponent(components),
                    "Challenge configuration created"
                );
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to create challenge configuration",
                    nameof(RenderChallengeConfigurationAsync)
                );
                return Result<DiscordContainerComponent>.Failure($"Failed to create configuration: {ex.Message}");
            }
        }
    }

    #region Result Types
    public record ChallengeContainerResult(DiscordChannel ChallengeChannel, DiscordMessage ChallengeMessage);
    #endregion
}
