using DSharpPlus;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Events.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Models.Scrimmage;
using WabbitBot.Core.Common.Services;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Interfaces;
using WabbitBot.DiscBot.App.Renderers;
using WabbitBot.DiscBot.App.Services.DiscBot;
using WabbitBot.DiscBot.App.Utilities;

namespace WabbitBot.DiscBot.App
{
    public partial class ScrimmageApp : IScrimmageApp
    {
        #region Buttons
        /// <summary>
        /// Handles button interactions (accept/decline challenge, confirm selections).
        /// Returns Result indicating success/failure for immediate feedback.
        /// Publishes events for cross-boundary communication.
        /// </summary>
        public static async Task<Result> ProcessButtonInteractionAsync(
            DiscordClient client,
            ComponentInteractionCreatedEventArgs args
        )
        {
            var interaction = args.Interaction;
            var customId = interaction.Data.CustomId;

            try
            {
                // Accept challenge button
                if (customId.StartsWith("accept_challenge_", StringComparison.Ordinal))
                {
                    return await ProcessAcceptChallengeButtonAsync(interaction, customId);
                }

                // Decline challenge button
                if (customId.StartsWith("decline_challenge_", StringComparison.Ordinal))
                {
                    return await ProcessDeclineChallengeButtonAsync(interaction, customId);
                }

                return Result.Failure($"Unknown custom ID: {customId}");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle button interaction: {customId}",
                    nameof(ProcessButtonInteractionAsync)
                );

                // Try to respond with error - may fail if response was already sent
                try
                {
                    await interaction.CreateResponseAsync(
                        DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("An error occurred while processing your interaction. Please try again.")
                            .AsEphemeral()
                    );
                }
                catch
                {
                    // Response was already sent, ignore
                }

                return Result.Failure($"Failed to handle button interaction: {ex.Message}");
            }
        }

        private static async Task<Result> ProcessAcceptChallengeButtonAsync(
            DiscordInteraction interaction,
            string customId
        )
        {
            // Parse challenge ID from custom ID: "accept_challenge_{challengeId}"
            var challengeIdStr = customId.Replace("accept_challenge_", "", StringComparison.Ordinal);
            if (!Guid.TryParse(challengeIdStr, out var challengeId))
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Invalid challenge ID.").AsEphemeral()
                );
                return Result.Failure("Invalid challenge ID");
            }
            // Get opponent team Ids from challenge
            var getChallenge = await CoreService.ScrimmageChallenges.GetByIdAsync(
                challengeId,
                DatabaseComponent.Repository
            );
            if (!getChallenge.Success)
            {
                return Result.Failure("Challenge not found");
            }
            var Challenge = getChallenge.Data;
            if (Challenge == null || Challenge.OpponentTeamPlayers == null)
            {
                return Result.Failure("Challenge not found or opponent team players not found");
            }
            var OpponentTeamId = Challenge.OpponentTeamId;
            var getOpponentSelectedPlayerIds = await CoreService.WithDbContext(async db =>
                await db
                    .Players.Where(p => Challenge.OpponentTeamPlayers.Select(p => p.Id).Contains(p.Id))
                    .Select(p => p.Id)
                    .ToArrayAsync()
            );
            if (getOpponentSelectedPlayerIds == null)
            {
                return Result.Failure("Opponent selected players not found");
            }
            var OpponentSelectedPlayerIds = getOpponentSelectedPlayerIds;
            var getAcceptedByPlayer = await CoreService.WithDbContext(async db =>
                await db.Players.FirstOrDefaultAsync(p => p.MashinaUser.DiscordUserId == interaction.User.Id)
            );
            if (getAcceptedByPlayer == null)
            {
                return Result.Failure("Accepted by player not found");
            }
            var AcceptedByPlayerId = getAcceptedByPlayer.Id;

            // Acknowledge interaction
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            // Publish ScrimmageAccepted (Global) to Core
            await ProcessChallengeAcceptedAsync(
                Challenge,
                OpponentTeamId,
                OpponentSelectedPlayerIds,
                AcceptedByPlayerId
            );

            return Result.CreateSuccess("Challenge accepted");
        }

        private static async Task<Result> ProcessDeclineChallengeButtonAsync(
            DiscordInteraction interaction,
            string customId
        )
        {
            var challengeIdStr = customId.Replace("decline_challenge_", "", StringComparison.Ordinal);
            if (!Guid.TryParse(challengeIdStr, out var challengeId))
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Invalid challenge ID.").AsEphemeral()
                );
                return Result.Failure("Invalid challenge ID");
            }

            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            // Publish ScrimmageDeclined (Global) to Core
            // await PublishChallengeDeclinedAsync(challengeId, interaction.User.Id);

            return Result.CreateSuccess("Challenge declined");
        }
        #endregion
        public static async Task<Result> ProcessChallengeAcceptedAsync(
            ScrimmageChallenge Challenge,
            Guid OpponentTeamId,
            Guid[] OpponentSelectedPlayerIds,
            Guid AcceptedByPlayerId
        )
        {
            // Update scrimmage to accepted
            Challenge.ChallengeStatus = ScrimmageChallengeStatus.Accepted;
            var updateResult = await CoreService.ScrimmageChallenges.UpdateAsync(
                Challenge,
                DatabaseComponent.Repository
            );
            if (!updateResult.Success)
            {
                return Result.Failure("Failed to update scrimmage challenge");
            }

            var pubResult = await PublishChallengeAcceptedAsync(
                Challenge.Id,
                OpponentTeamId,
                [.. OpponentSelectedPlayerIds],
                AcceptedByPlayerId
            );
            if (!pubResult.Success)
            {
                return Result.Failure("Failed to publish challenge accepted");
            }

            return Result.CreateSuccess();
        }

        public static async Task<Result<ScrimmageThreadsResult>> CreateScrimmageThreadsAsync(
            Guid ScrimmageId,
            Guid MatchId
        )
        {
            try
            {
                var getScrimmage = await CoreService.Scrimmages.GetByIdAsync(ScrimmageId, DatabaseComponent.Repository);
                if (!getScrimmage.Success)
                {
                    return Result<ScrimmageThreadsResult>.Failure("Scrimmage not found");
                }
                var Scrimmage = getScrimmage.Data;
                if (Scrimmage == null)
                {
                    return Result<ScrimmageThreadsResult>.Failure("Scrimmage not found");
                }
                var getMatch = await CoreService.Matches.GetByIdAsync(MatchId, DatabaseComponent.Repository);
                if (!getMatch.Success)
                {
                    return Result<ScrimmageThreadsResult>.Failure("Scrimmage match not found");
                }
                if (getMatch.Data == null)
                {
                    return Result<ScrimmageThreadsResult>.Failure("Scrimmage match not found");
                }
                var Match = getMatch.Data;

                var ChallengerTeamMentions = new List<string>();
                foreach (var playerId in Match.Team1PlayerIds)
                {
                    var getChallengerPlayer = await CoreService.Players.GetByIdAsync(
                        playerId,
                        DatabaseComponent.Repository
                    );
                    if (
                        getChallengerPlayer.Success && getChallengerPlayer.Data?.MashinaUser?.DiscordMention is not null
                    )
                    {
                        ChallengerTeamMentions.Add(getChallengerPlayer.Data.MashinaUser.DiscordMention);
                    }
                }
                var OpponentTeamMentions = new List<string>();
                foreach (var playerId in Match.Team2PlayerIds)
                {
                    var getOpponentPlayer = await CoreService.Players.GetByIdAsync(
                        playerId,
                        DatabaseComponent.Repository
                    );
                    if (getOpponentPlayer.Success && getOpponentPlayer.Data?.MashinaUser?.DiscordMention is not null)
                    {
                        OpponentTeamMentions.Add(getOpponentPlayer.Data.MashinaUser.DiscordMention);
                    }
                }

                if (Scrimmage.ScrimmageChallenge == null)
                {
                    return Result<ScrimmageThreadsResult>.Failure("Scrimmage challenge not found");
                }
                if (Scrimmage.ScrimmageChallenge.ChallengerTeamPlayers == null)
                {
                    return Result<ScrimmageThreadsResult>.Failure("Challenger team players not found");
                }
                if (Scrimmage.ScrimmageChallenge.OpponentTeamPlayers == null)
                {
                    return Result<ScrimmageThreadsResult>.Failure("Opponent team players not found");
                }

                // Build messages
                var getContainers = await MatchRenderer.RenderScrimmageMatchContainersAsync(
                    Match.Id,
                    Scrimmage.ScrimmageChallenge.ChallengerTeamPlayers,
                    Scrimmage.ScrimmageChallenge.OpponentTeamPlayers
                );
                if (!getContainers.Success || getContainers.Data == null)
                {
                    return Result<ScrimmageThreadsResult>.Failure("Failed to get match containers");
                }
                var ChallengerContainer = getContainers.Data.ChallengerContainer;
                var OpponentContainer = getContainers.Data.OpponentContainer;
                if (Scrimmage.ScrimmageChallenge.ChallengerTeam == null)
                {
                    return Result<ScrimmageThreadsResult>.Failure("Challenger team not found");
                }
                if (Scrimmage.ScrimmageChallenge.OpponentTeam == null)
                {
                    return Result<ScrimmageThreadsResult>.Failure("Opponent team not found");
                }

                var ChallengerThread = await DiscBotService.ScrimmageChannel.CreateThreadAsync(
                    Scrimmage.ScrimmageChallenge.ChallengerTeam.Name,
                    DiscordAutoArchiveDuration.Day,
                    DiscordChannelType.PrivateThread
                );
                await ChallengerThread.SendMessageAsync(
                    new DiscordMessageBuilder().EnableV2Components().AddContainerComponent(ChallengerContainer)
                );
                var OpponentThread = await DiscBotService.ScrimmageChannel.CreateThreadAsync(
                    Scrimmage.ScrimmageChallenge.OpponentTeam.Name,
                    DiscordAutoArchiveDuration.Day,
                    DiscordChannelType.PrivateThread
                );
                await OpponentThread.SendMessageAsync(
                    new DiscordMessageBuilder().EnableV2Components().AddContainerComponent(OpponentContainer)
                );

                // Use a purpose-built record instead of a dictionary for clarity and type safety.
                return Result<ScrimmageThreadsResult>.CreateSuccess(
                    new ScrimmageThreadsResult(ChallengerThread, OpponentThread),
                    "Scrimmage threads created"
                );
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to create scrimmage threads",
                    nameof(CreateScrimmageThreadsAsync)
                );
                return Result<ScrimmageThreadsResult>.Failure($"Failed to create scrimmage threads: {ex.Message}");
            }
        }

        public static async Task<Result<ChallengeContainerResult>> CreateChallengeContainerAsync(
            Guid ChallengeId,
            TeamSize TeamSize,
            string ChallengerTeamName,
            string OpponentTeamName
        )
        {
            try
            {
                // Get the challenge banner image (default or custom)
                var challengeBannerPath = DiscBotService.FileSystem.GetDiscordComponentImage("challenge_banner.jpg");

                if (challengeBannerPath is null)
                {
                    return Result<ChallengeContainerResult>.Failure("Challenge banner image not found");
                }

                var challengeContainer = new DiscordContainerComponent(
                    components:
                    [
                        new DiscordMediaGalleryComponent(items: [new DiscordMediaGalleryItem(challengeBannerPath)]),
                        new DiscordTextDisplayComponent(content: $"{ChallengerTeamName} vs. {OpponentTeamName}"),
                        new DiscordButtonComponent(
                            DiscordButtonStyle.Success,
                            $"accept_challenge_{ChallengeId}",
                            "Accept Challenge"
                        ),
                        new DiscordButtonComponent(
                            DiscordButtonStyle.Secondary,
                            $"decline_challenge_{ChallengeId}",
                            "Decline Challenge"
                        ),
                    ],
                    isSpoilered: false,
                    color: EmbedStyling.GetChallengeColor()
                );
                // Get the TeamSizeRosterGroup for the TeamSize
                var teamSizeRosterGroup = TeamCore.TeamSizeRosterGrouping.GetRosterGroup(TeamSize);

                // Get opponent team to use ID for efficient filtering
                var opponentTeamResult = await CoreService.Teams.GetByNameAsync(
                    OpponentTeamName,
                    DatabaseComponent.Repository
                );

                if (!opponentTeamResult.Success || opponentTeamResult.Data is null)
                {
                    return Result<ChallengeContainerResult>.Failure("Opponent team not found");
                }

                var opponentTeamId = opponentTeamResult.Data.Id;

                // Query using TeamId (indexed FK) instead of Team.Name (string comparison)
                var allowedOpponentMentions = await CoreService.WithDbContext(async db =>
                {
                    return await db
                        .TeamMembers.Where(tm =>
                            tm.TeamRoster.TeamId == opponentTeamId
                            && tm.TeamRoster.RosterGroup == teamSizeRosterGroup
                            && tm.ReceiveScrimmagePings
                            && tm.MashinaUser != null
                        )
                        .Select(tm => tm.MashinaUser!.DiscordUserId!)
                        .ToListAsync();
                });
                var message = new DiscordMessageBuilder()
                    .EnableV2Components()
                    .AddContainerComponent(challengeContainer)
                    .WithAllowedMentions(allowedOpponentMentions.Select(id => new UserMention(id)).Cast<IMention>());

                await message.SendAsync(DiscBotService.ScrimmageChannel);
                return Result<ChallengeContainerResult>.CreateSuccess(
                    new ChallengeContainerResult(DiscBotService.ScrimmageChannel)
                );
            }
            catch (Exception ex)
            {
                return Result<ChallengeContainerResult>.Failure($"Failed to create challenge container: {ex.Message}");
            }
        }
    }

    #region Result Types
    public record ScrimmageThreadsResult(DiscordThreadChannel ChallengerThread, DiscordThreadChannel OpponentThread);

    public record ChallengeContainerResult(DiscordChannel ChallengeChannel);
    #endregion
}
