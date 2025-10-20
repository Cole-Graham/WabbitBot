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
        // State manager for challenge configuration messages
        private static readonly InteractiveMessageStateManager<ChallengeConfigurationSelections> _challengeStateManager =
            new();

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

                // Cancel challenge button
                if (customId.StartsWith("cancel_challenge_", StringComparison.Ordinal))
                {
                    return await ProcessCancelChallengeButtonAsync(interaction, customId);
                }

                // Challenge issue button (from configuration)
                if (customId.StartsWith("challenge_issue_", StringComparison.Ordinal))
                {
                    return await ProcessIssueChallengeButtonAsync(interaction, customId);
                }

                // Challenge config cancel button
                if (customId.StartsWith("challenge_cancel_", StringComparison.Ordinal))
                {
                    return await ProcessCancelConfigurationButtonAsync(interaction, customId);
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
        #endregion

        #region Select Menus
        /// <summary>
        /// Handles select menu interactions (opponent selection, player selection, best of selection).
        /// Returns Result indicating success/failure for immediate feedback.
        /// </summary>
        public static async Task<Result> ProcessSelectMenuInteractionAsync(
            DiscordClient client,
            ComponentInteractionCreatedEventArgs args
        )
        {
            var interaction = args.Interaction;
            var customId = interaction.Data.CustomId;

            try
            {
                // Opponent team selection
                if (customId.StartsWith("challenge_opponent_", StringComparison.Ordinal))
                {
                    return await ProcessOpponentSelectionAsync(interaction, customId);
                }

                // Player selection
                if (customId.StartsWith("challenge_players_", StringComparison.Ordinal))
                {
                    return await ProcessPlayerSelectionAsync(interaction, customId);
                }

                // Best of selection
                if (customId.StartsWith("challenge_bestof_", StringComparison.Ordinal))
                {
                    return await ProcessBestOfSelectionAsync(interaction, customId);
                }

                // Teammate selection from challenge container
                if (customId.StartsWith("select_teammates_", StringComparison.Ordinal))
                {
                    return await ProcessTeammateSelectionAsync(interaction, customId);
                }

                return Result.Failure($"Unknown custom ID: {customId}");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle select menu interaction: {customId}",
                    nameof(ProcessSelectMenuInteractionAsync)
                );

                // Try to respond with error - may fail if response was already sent
                try
                {
                    await interaction.CreateResponseAsync(
                        DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("An error occurred while processing your selection. Please try again.")
                            .AsEphemeral()
                    );
                }
                catch
                {
                    // Response was already sent, ignore
                }

                return Result.Failure($"Failed to handle select menu interaction: {ex.Message}");
            }
        }
        #endregion

        #region Accept Challenge
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
            // Get challenge from database
            var getChallenge = await CoreService.ScrimmageChallenges.GetByIdAsync(
                challengeId,
                DatabaseComponent.Repository
            );
            if (!getChallenge.Success || getChallenge.Data is null)
            {
                return Result.Failure("Challenge not found");
            }
            var Challenge = getChallenge.Data;

            // Get opponent team ID and player IDs from the challenge
            var OpponentTeamId = Challenge.OpponentTeamId;

            // Check if opponent team players have been selected
            if (Challenge.OpponentTeamPlayerIds is null || Challenge.OpponentTeamPlayerIds.Count == 0)
            {
                return Result.Failure("Opponent team players not selected yet");
            }

            var OpponentSelectedPlayerIds = Challenge.OpponentTeamPlayerIds.ToArray();
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
        #endregion

        #region Decline Challenge
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

        #region Cancel Challenge
        /// <summary>
        /// Updates a challenge container message to show cancellation status.
        /// </summary>
        public static async Task<Result> UpdateChallengeContainerToCancelledAsync(
            string challengerTeamName,
            string opponentTeamName,
            DiscordUser cancelledBy,
            ulong? challengeMessageId,
            ulong? challengeChannelId
        )
        {
            try
            {
                // Check if we have message information
                if (challengeMessageId is null || challengeChannelId is null)
                {
                    return Result.Failure("Challenge message information not found");
                }

                // Get the channel
                var channel = await DiscBotService.Client.GetChannelAsync(challengeChannelId.Value);
                if (channel is null)
                {
                    return Result.Failure("Challenge channel not found");
                }

                // Get the message
                var message = await channel.GetMessageAsync(challengeMessageId.Value);
                if (message is null)
                {
                    return Result.Failure("Challenge message not found");
                }

                // Find the existing container
                var existingContainer = message.Components?.OfType<DiscordContainerComponent>().FirstOrDefault();
                if (existingContainer is null)
                {
                    return Result.Failure("Challenge container not found in message");
                }

                // Render the cancelled container
                var renderResult = ScrimmageRenderer.RenderCancelledChallengeContainer(
                    existingContainer,
                    challengerTeamName,
                    opponentTeamName,
                    cancelledBy
                );

                if (!renderResult.Success || renderResult.Data is null)
                {
                    return Result.Failure($"Failed to render cancelled container: {renderResult.ErrorMessage}");
                }

                // Update the message
                var messageBuilder = new DiscordMessageBuilder()
                    .EnableV2Components()
                    .AddContainerComponent(renderResult.Data);

                await message.ModifyAsync(messageBuilder);

                return Result.CreateSuccess("Challenge container updated");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to update challenge container for message {challengeMessageId}",
                    nameof(UpdateChallengeContainerToCancelledAsync)
                );
                return Result.Failure($"Failed to update challenge container: {ex.Message}");
            }
        }

        /// <summary>
        /// Core cancellation logic shared between button and command handlers.
        /// Validates authorization, checks status, and deletes the challenge.
        /// </summary>
        public static async Task<Result<ChallengeCancellationResult>> CancelChallengeAsync(
            Guid challengeId,
            ulong discordUserId,
            bool isModerator = false
        )
        {
            // Get the challenge using direct database query (GetByIdAsync is broken)
            Console.WriteLine($"🔍 DEBUG: CancelChallengeAsync - Looking for challenge ID: {challengeId}");
            var challenge = await CoreService.WithDbContext(async db =>
            {
                return await db.ScrimmageChallenges.FirstOrDefaultAsync(c => c.Id == challengeId);
            });

            Console.WriteLine($"🔍 DEBUG: CancelChallengeAsync - Challenge found: {challenge is not null}");
            if (challenge is not null)
            {
                Console.WriteLine($"🔍 DEBUG: CancelChallengeAsync - Challenge ID: {challenge.Id}");
                Console.WriteLine($"🔍 DEBUG: CancelChallengeAsync - Challenge Status: {challenge.ChallengeStatus}");
            }

            if (challenge is null)
            {
                return Result<ChallengeCancellationResult>.Failure("Challenge not found.");
            }

            // Check if challenge has already been accepted
            if (challenge.ChallengeStatus == ScrimmageChallengeStatus.Accepted)
            {
                return Result<ChallengeCancellationResult>.Failure(
                    "This challenge has already been accepted and cannot be cancelled."
                );
            }

            // Check if challenge has already been declined
            if (challenge.ChallengeStatus == ScrimmageChallengeStatus.Declined)
            {
                return Result<ChallengeCancellationResult>.Failure("This challenge has already been declined.");
            }

            // Get the player associated with the Discord user
            var getPlayer = await CoreService.WithDbContext(async db =>
                await db.Players.FirstOrDefaultAsync(p => p.MashinaUser.DiscordUserId == discordUserId)
            );

            if (getPlayer is null)
            {
                return Result<ChallengeCancellationResult>.Failure("You are not registered as a player.");
            }

            var player = getPlayer;

            // Get the challenger team to check captain
            var getTeam = await CoreService.Teams.GetByIdAsync(
                challenge.ChallengerTeamId,
                DatabaseComponent.Repository
            );
            if (!getTeam.Success || getTeam.Data is null)
            {
                return Result<ChallengeCancellationResult>.Failure("Challenger team not found.");
            }

            var challengerTeam = getTeam.Data;

            // Authorization check: must be admin OR the issuer OR the team captain
            var isIssuer = player.Id == challenge.IssuedByPlayerId;
            var isCaptain = player.Id == challengerTeam.TeamCaptainId;

            if (!isModerator && !isIssuer && !isCaptain)
            {
                return Result<ChallengeCancellationResult>.Failure(
                    "You are not authorized to cancel this challenge. Only the player who "
                        + "issued the challenge, the team captain, or a moderator can cancel it."
                );
            }

            // Get opponent team for display
            var getOpponentTeam = await CoreService.Teams.GetByIdAsync(
                challenge.OpponentTeamId,
                DatabaseComponent.Repository
            );
            if (!getOpponentTeam.Success || getOpponentTeam.Data is null)
            {
                return Result<ChallengeCancellationResult>.Failure("Opponent team not found.");
            }

            var opponentTeam = getOpponentTeam.Data;

            // Delete the challenge entity
            var deleteResult = await CoreService.ScrimmageChallenges.DeleteAsync(
                challenge.Id,
                DatabaseComponent.Repository
            );
            if (!deleteResult.Success)
            {
                return Result<ChallengeCancellationResult>.Failure(
                    $"Failed to delete challenge: {deleteResult.ErrorMessage}"
                );
            }

            return Result<ChallengeCancellationResult>.CreateSuccess(
                new ChallengeCancellationResult(
                    challengerTeam.Name,
                    opponentTeam.Name,
                    challenge.ChallengeMessageId,
                    challenge.ChallengeChannelId
                ),
                "Challenge cancelled successfully"
            );
        }

        private static async Task<Result> ProcessCancelChallengeButtonAsync(
            DiscordInteraction interaction,
            string customId
        )
        {
            var challengeIdStr = customId.Replace("cancel_challenge_", "", StringComparison.Ordinal);
            if (!Guid.TryParse(challengeIdStr, out var challengeId))
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Invalid challenge ID.").AsEphemeral()
                );
                return Result.Failure("Invalid challenge ID");
            }

            // Check if user is admin
            var isAdmin = false;
            if (interaction.User is DiscordMember member)
            {
                isAdmin = member.Permissions.HasFlag(DiscordPermission.ModerateMembers);
            }

            // Defer the response first
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            // Call the shared cancellation logic
            var cancelResult = await CancelChallengeAsync(challengeId, interaction.User.Id, isAdmin);

            if (!cancelResult.Success || cancelResult.Data is null)
            {
                // Send error as ephemeral follow-up
                try
                {
                    await interaction.CreateFollowupMessageAsync(
                        new DiscordFollowupMessageBuilder()
                            .WithContent(cancelResult.ErrorMessage ?? "Failed to cancel challenge.")
                            .AsEphemeral()
                    );
                }
                catch
                {
                    // Ignore if follow-up fails
                }
                return Result.Failure(cancelResult.ErrorMessage ?? "Failed to cancel challenge.");
            }

            // Update the challenge container using the shared method
            var updateResult = await UpdateChallengeContainerToCancelledAsync(
                cancelResult.Data.ChallengerTeamName,
                cancelResult.Data.OpponentTeamName,
                interaction.User,
                cancelResult.Data.ChallengeMessageId,
                cancelResult.Data.ChallengeChannelId
            );

            if (!updateResult.Success)
            {
                // Log but don't fail - the challenge was already deleted
                await DiscBotService.ErrorHandler.CaptureAsync(
                    new Exception(updateResult.ErrorMessage),
                    "Failed to update challenge container after cancellation",
                    nameof(ProcessCancelChallengeButtonAsync)
                );
            }

            return Result.CreateSuccess("Challenge cancelled");
        }
        #endregion


        #region Challenge Accepted
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
        #endregion

        #region App
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
                Match.ChannelId = DiscBotService.ScrimmageChannel.Id;

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
                    Scrimmage.ScrimmageChallenge.ChallengerTeamPlayers.ToArray(),
                    Scrimmage.ScrimmageChallenge.OpponentTeamPlayers?.ToArray()
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
                Match.Team1ThreadId = ChallengerThread.Id;
                var ChallengerContainerMsg = await ChallengerThread.SendMessageAsync(
                    new DiscordMessageBuilder().EnableV2Components().AddContainerComponent(ChallengerContainer)
                );
                Match.Team1OverviewContainerMsgId = ChallengerContainerMsg.Id;
                var OpponentThread = await DiscBotService.ScrimmageChannel.CreateThreadAsync(
                    Scrimmage.ScrimmageChallenge.OpponentTeam.Name,
                    DiscordAutoArchiveDuration.Day,
                    DiscordChannelType.PrivateThread
                );
                Match.Team2ThreadId = OpponentThread.Id;
                var OpponentContainerMsg = await OpponentThread.SendMessageAsync(
                    new DiscordMessageBuilder().EnableV2Components().AddContainerComponent(OpponentContainer)
                );
                Match.Team2OverviewContainerMsgId = OpponentContainerMsg.Id;

                // Use a purpose-built record instead of a dictionary for clarity and type safety.
                return Result<ScrimmageThreadsResult>.CreateSuccess(
                    new ScrimmageThreadsResult(ChallengerThread, OpponentThread),
                    "Scrimmage threads and initial Overview container messages created"
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

        #endregion

        #region Player Substitution
        /// <summary>
        /// Substitutes a player in an active scrimmage.
        /// Updates the scrimmage, match, and active game player lists.
        /// Resets deck code submission if the substituted player had already submitted.
        /// </summary>
        public static async Task<Result<string>> SubstitutePlayerAsync(
            Guid scrimmageId,
            Guid playerOutId,
            Guid playerInId,
            ulong requestingUserId
        )
        {
            try
            {
                // Get the scrimmage with all necessary navigation properties
                var scrimmageResult = await CoreService.Scrimmages.GetByIdAsync(
                    scrimmageId,
                    DatabaseComponent.Repository
                );
                if (!scrimmageResult.Success || scrimmageResult.Data is null)
                {
                    return Result<string>.Failure("Scrimmage not found.");
                }

                var scrimmage = scrimmageResult.Data;

                // Verify scrimmage is active
                if (scrimmage.CompletedAt.HasValue)
                {
                    return Result<string>.Failure("Cannot substitute players in a completed scrimmage.");
                }

                // Get the requesting player
                var requestingPlayer = await CoreService.WithDbContext(async db =>
                    await db.Players.FirstOrDefaultAsync(p => p.MashinaUser.DiscordUserId == requestingUserId)
                );

                if (requestingPlayer is null)
                {
                    return Result<string>.Failure("You must be registered as a player to substitute.");
                }

                // Determine which team the player to sub out is on
                bool isOnChallengerTeam = scrimmage.ChallengerTeamPlayerIds.Contains(playerOutId);
                bool isOnOpponentTeam = scrimmage.OpponentTeamPlayerIds.Contains(playerOutId);

                if (!isOnChallengerTeam && !isOnOpponentTeam)
                {
                    return Result<string>.Failure("The player to substitute out is not in this scrimmage.");
                }

                // Get team info for permission check
                var teamId = isOnChallengerTeam ? scrimmage.ChallengerTeamId : scrimmage.OpponentTeamId;
                var teamResult = await CoreService.Teams.GetByIdAsync(teamId, DatabaseComponent.Repository);
                if (!teamResult.Success || teamResult.Data is null)
                {
                    return Result<string>.Failure("Team not found.");
                }

                var team = teamResult.Data;

                // Verify permission: must be team captain, team manager, or the player being substituted
                var isTeamCaptain = team.TeamCaptainId == requestingPlayer.Id;
                var isPlayerBeingSubbed = playerOutId == requestingPlayer.Id;

                var isTeamManager = await CoreService.WithDbContext(async db =>
                {
                    var rosterGroup = TeamCore.TeamSizeRosterGrouping.GetRosterGroup(scrimmage.TeamSize);
                    return await db.TeamMembers.AnyAsync(tm =>
                        tm.TeamRoster.TeamId == teamId
                        && tm.TeamRoster.RosterGroup == rosterGroup
                        && tm.PlayerId == requestingPlayer.Id
                        && tm.IsTeamManager
                    );
                });

                if (!isTeamCaptain && !isTeamManager && !isPlayerBeingSubbed)
                {
                    return Result<string>.Failure(
                        "Only the team captain, a team manager, or the player being substituted can make substitutions."
                    );
                }

                // Verify the player to sub in is not already in the scrimmage
                if (
                    scrimmage.ChallengerTeamPlayerIds.Contains(playerInId)
                    || scrimmage.OpponentTeamPlayerIds.Contains(playerInId)
                )
                {
                    return Result<string>.Failure("The substitute player is already in this scrimmage.");
                }

                // Verify the player to sub in is on the team's roster
                var isOnRoster = await CoreService.WithDbContext(async db =>
                {
                    var rosterGroup = TeamCore.TeamSizeRosterGrouping.GetRosterGroup(scrimmage.TeamSize);
                    return await db.TeamMembers.AnyAsync(tm =>
                        tm.TeamRoster.TeamId == teamId
                        && tm.TeamRoster.RosterGroup == rosterGroup
                        && tm.PlayerId == playerInId
                        && tm.IsActive
                    );
                });

                if (!isOnRoster)
                {
                    return Result<string>.Failure(
                        "The substitute player is not on the team's roster for this game size."
                    );
                }

                // Get player names for response message
                var playerOutResult = await CoreService.Players.GetByIdAsync(playerOutId, DatabaseComponent.Repository);
                var playerInResult = await CoreService.Players.GetByIdAsync(playerInId, DatabaseComponent.Repository);

                if (!playerOutResult.Success || playerOutResult.Data is null)
                {
                    return Result<string>.Failure("Player to substitute out not found.");
                }

                if (!playerInResult.Success || playerInResult.Data is null)
                {
                    return Result<string>.Failure("Substitute player not found.");
                }

                var playerOut = playerOutResult.Data;
                var playerIn = playerInResult.Data;

                // Update scrimmage player lists
                if (isOnChallengerTeam)
                {
                    var index = scrimmage.ChallengerTeamPlayerIds.IndexOf(playerOutId);
                    scrimmage.ChallengerTeamPlayerIds[index] = playerInId;
                }
                else
                {
                    var index = scrimmage.OpponentTeamPlayerIds.IndexOf(playerOutId);
                    scrimmage.OpponentTeamPlayerIds[index] = playerInId;
                }

                // Update the scrimmage
                await CoreService.Scrimmages.UpdateAsync(scrimmage, DatabaseComponent.Repository);

                // Update the match if it exists
                if (scrimmage.Match is not null)
                {
                    var matchResult = await CoreService.Matches.GetByIdAsync(
                        scrimmage.Match.Id,
                        DatabaseComponent.Repository
                    );
                    if (matchResult.Success && matchResult.Data is not null)
                    {
                        var match = matchResult.Data;

                        // Update match player lists
                        if (isOnChallengerTeam)
                        {
                            var index = match.Team1PlayerIds.IndexOf(playerOutId);
                            match.Team1PlayerIds[index] = playerInId;
                        }
                        else
                        {
                            var index = match.Team2PlayerIds.IndexOf(playerOutId);
                            match.Team2PlayerIds[index] = playerInId;
                        }

                        await CoreService.Matches.UpdateAsync(match, DatabaseComponent.Repository);

                        // Update active games in the match
                        var activeGames = match.Games.Where(g =>
                            MatchCore.Accessors.GetCurrentSnapshot(g).CompletedAt == null
                        );

                        foreach (var game in activeGames)
                        {
                            var gameResult = await CoreService.Games.GetByIdAsync(
                                game.Id,
                                DatabaseComponent.Repository
                            );
                            if (gameResult.Success && gameResult.Data is not null)
                            {
                                var activeGame = gameResult.Data;

                                // Update game player lists
                                if (isOnChallengerTeam)
                                {
                                    var index = activeGame.Team1PlayerIds.IndexOf(playerOutId);
                                    activeGame.Team1PlayerIds[index] = playerInId;
                                }
                                else
                                {
                                    var index = activeGame.Team2PlayerIds.IndexOf(playerOutId);
                                    activeGame.Team2PlayerIds[index] = playerInId;
                                }

                                // Check if player out had submitted a deck code and clear it
                                var currentSnapshot = MatchCore.Accessors.GetCurrentSnapshot(activeGame);
                                if (currentSnapshot.PlayerDeckCodes.ContainsKey(playerOutId))
                                {
                                    // Create a new snapshot with the deck code removed
                                    var newSnapshot = MatchCore.Factory.CreateGameStateSnapshotFromOther(
                                        currentSnapshot
                                    );
                                    newSnapshot.PlayerDeckCodes.Remove(playerOutId);
                                    newSnapshot.PlayerDeckSubmittedAt.Remove(playerOutId);
                                    newSnapshot.PlayerDeckConfirmed.Remove(playerOutId);
                                    newSnapshot.PlayerDeckConfirmedAt.Remove(playerOutId);

                                    // Also update the denormalized player IDs in the snapshot
                                    if (isOnChallengerTeam)
                                    {
                                        var index = newSnapshot.Team1PlayerIds.IndexOf(playerOutId);
                                        newSnapshot.Team1PlayerIds[index] = playerInId;
                                    }
                                    else
                                    {
                                        var index = newSnapshot.Team2PlayerIds.IndexOf(playerOutId);
                                        newSnapshot.Team2PlayerIds[index] = playerInId;
                                    }

                                    activeGame.StateHistory.Add(newSnapshot);
                                }

                                await CoreService.Games.UpdateAsync(activeGame, DatabaseComponent.Repository);
                            }
                        }
                    }
                }

                var playerOutName =
                    playerOut.MashinaUser?.DiscordGlobalname
                    ?? playerOut.MashinaUser?.DiscordUsername
                    ?? playerOut.GameUsername;
                var playerInName =
                    playerIn.MashinaUser?.DiscordGlobalname
                    ?? playerIn.MashinaUser?.DiscordUsername
                    ?? playerIn.GameUsername;

                return Result<string>.CreateSuccess(
                    $"Successfully substituted **{playerOutName}** with **{playerInName}**. "
                        + (
                            scrimmage.Match?.Games.Any(g =>
                                MatchCore.Accessors.GetCurrentSnapshot(g).CompletedAt == null
                            ) == true
                                ? "The previous player's deck submission has been cleared if it existed."
                                : ""
                        )
                );
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to substitute player in scrimmage",
                    nameof(SubstitutePlayerAsync)
                );
                return Result<string>.Failure($"An error occurred while substituting the player: {ex.Message}");
            }
        }
        #endregion

        #region Challenge Configuration Handlers
        private static async Task<Result> ProcessOpponentSelectionAsync(DiscordInteraction interaction, string customId)
        {
            // Track the selection
            var selections = interaction.Data.Values.ToArray();
            var selectedOpponentTeamId = selections.Length > 0 ? selections[0] : null;

            _challengeStateManager.UpdateState(
                interaction.Message.Id,
                state =>
                {
                    state.OpponentTeamId = selectedOpponentTeamId;
                    return state;
                }
            );

            // Update button state with the interaction response
            await UpdateChallengeButtonStateAsync(interaction);

            return Result.CreateSuccess("Opponent selected");
        }

        private static async Task<Result> ProcessPlayerSelectionAsync(DiscordInteraction interaction, string customId)
        {
            // Parse issuer player ID from custom ID: "challenge_players_{teamId}_{issuerPlayerId}"
            var parts = customId.Replace("challenge_players_", "", StringComparison.Ordinal).Split('_');
            Guid? issuerPlayerId = null;
            if (parts.Length >= 2 && Guid.TryParse(parts[1], out var parsedIssuerId))
            {
                issuerPlayerId = parsedIssuerId;
            }

            // Get selected player IDs (teammates)
            var selections = interaction.Data.Values.ToArray();
            var selectedPlayerIds = selections
                .Select(s => Guid.TryParse(s, out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .ToList();

            // Auto-include the issuer
            if (issuerPlayerId.HasValue && !selectedPlayerIds.Contains(issuerPlayerId.Value))
            {
                selectedPlayerIds.Insert(0, issuerPlayerId.Value);
            }

            // Track the selection
            _challengeStateManager.UpdateState(
                interaction.Message.Id,
                state =>
                {
                    state.PlayerIds = selectedPlayerIds;
                    return state;
                }
            );

            // Update button state with the interaction response
            await UpdateChallengeButtonStateAsync(interaction);

            return Result.CreateSuccess("Players selected");
        }

        private static async Task<Result> ProcessBestOfSelectionAsync(DiscordInteraction interaction, string customId)
        {
            // Get selected best of value
            var selections = interaction.Data.Values.ToArray();
            var bestOf = selections.Length > 0 && int.TryParse(selections[0], out var bo) ? bo : 1;

            // Track the selection
            _challengeStateManager.UpdateState(
                interaction.Message.Id,
                state =>
                {
                    state.BestOf = bestOf;
                    return state;
                }
            );

            // Update button state with the interaction response (bestOf doesn't affect button, but keeps UI consistent)
            await UpdateChallengeButtonStateAsync(interaction);

            return Result.CreateSuccess("Best of selected");
        }

        private static async Task<Result> ProcessTeammateSelectionAsync(DiscordInteraction interaction, string customId)
        {
            // Parse challenge ID from custom ID: "select_teammates_{challengeId}"
            var challengeIdStr = customId.Replace("select_teammates_", "", StringComparison.Ordinal);
            if (!Guid.TryParse(challengeIdStr, out var challengeId))
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Invalid challenge ID.").AsEphemeral()
                );
                return Result.Failure("Invalid challenge ID");
            }

            // Get selected teammate IDs
            var selectedTeammateIds = interaction
                .Data.Values.Select(v => Guid.TryParse(v, out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .ToList();

            if (selectedTeammateIds.Count == 0)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("No teammates selected.").AsEphemeral()
                );
                return Result.Failure("No teammates selected");
            }

            // Get the challenge
            var challengeResult = await CoreService.ScrimmageChallenges.GetByIdAsync(
                challengeId,
                DatabaseComponent.Repository
            );

            if (!challengeResult.Success || challengeResult.Data is null)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Challenge not found.").AsEphemeral()
                );
                return Result.Failure("Challenge not found");
            }

            var challenge = challengeResult.Data;

            // Verify the user is a member of the opponent team
            var userPlayer = await CoreService.WithDbContext(async db =>
            {
                return await db
                    .Players.Where(p => p.MashinaUser.DiscordUserId == interaction.User.Id)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync();
            });

            if (userPlayer == Guid.Empty)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Player not found.").AsEphemeral()
                );
                return Result.Failure("Player not found");
            }

            // Check if the user is a member of the opponent team
            var isTeamMember = await CoreService.WithDbContext(async db =>
            {
                return await db
                    .Teams.Where(t => t.Id == challenge.OpponentTeamId)
                    .SelectMany(t => t.Rosters)
                    .Where(r => r.RosterGroup == TeamCore.TeamSizeRosterGrouping.GetRosterGroup(challenge.TeamSize))
                    .SelectMany(r => r.RosterMembers)
                    .Where(tm => tm.PlayerId == userPlayer && tm.IsActive)
                    .AnyAsync();
            });

            if (!isTeamMember)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("Only team members can select teammates.")
                        .AsEphemeral()
                );
                return Result.Failure("Unauthorized - not team member");
            }

            // Add the captain to the selected players
            var allOpponentPlayerIds = new List<Guid> { userPlayer };
            allOpponentPlayerIds.AddRange(selectedTeammateIds);

            // Update the challenge with selected opponent team players
            challenge.OpponentTeamPlayerIds = allOpponentPlayerIds;
            challenge.AcceptedByPlayerId = userPlayer;

            var updateResult = await CoreService.ScrimmageChallenges.UpdateAsync(
                challenge,
                DatabaseComponent.Repository
            );

            if (!updateResult.Success)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("Failed to update challenge with teammate selection.")
                        .AsEphemeral()
                );
                return Result.Failure("Failed to update challenge");
            }

            // Acknowledge the interaction
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            // Update the challenge container to show the selection
            // TODO: Update the container to show selected teammates and enable accept button

            return Result.CreateSuccess("Teammates selected successfully");
        }

        /// <summary>
        /// Updates the "Issue Challenge" button enabled/disabled state based on current selections
        /// </summary>
        private static async Task UpdateChallengeButtonStateAsync(DiscordInteraction interaction)
        {
            try
            {
                // Get current state
                var configState = await ExtractConfigurationStateAsync(interaction.Message);
                if (!configState.Success || configState.Data is null)
                {
                    return;
                }

                // Get tracked selections
                var selections = _challengeStateManager.GetState(interaction.Message.Id);
                if (selections is null)
                {
                    return;
                }

                // Re-render the entire container with selections preserved
                var containerResult = await Renderers.ScrimmageRenderer.RenderChallengeConfigurationAsync(
                    configState.Data.UserId,
                    configState.Data.TeamSize,
                    configState.Data.ChallengerTeam,
                    configState.Data.ChallengerRoster,
                    selections.OpponentTeamId,
                    selections.PlayerIds,
                    selections.BestOf
                );

                if (containerResult.Success && containerResult.Data is not null)
                {
                    // Use the interaction response to update the message (preserves selections)
                    await interaction.CreateResponseAsync(
                        DiscordInteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder()
                            .EnableV2Components()
                            .AddContainerComponent(containerResult.Data)
                    );
                }
            }
            catch
            {
                // Silently fail - button state update is not critical
            }
        }

        private static async Task<Result> ProcessIssueChallengeButtonAsync(
            DiscordInteraction interaction,
            string customId
        )
        {
            try
            {
                // Parse team ID and user ID from custom ID: "challenge_issue_{teamId}_{userId}"
                var parts = customId.Replace("challenge_issue_", "", StringComparison.Ordinal).Split('_');
                if (
                    parts.Length != 2
                    || !Guid.TryParse(parts[0], out var challengerTeamId)
                    || !ulong.TryParse(parts[1], out var userId)
                )
                {
                    await interaction.CreateResponseAsync(
                        DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent("Invalid team or user ID.").AsEphemeral()
                    );
                    return Result.Failure("Invalid team or user ID");
                }

                // Verify the user is the one who initiated the configuration
                if (interaction.User.Id != userId)
                {
                    await interaction.CreateResponseAsync(
                        DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("Only the user who started the challenge can issue it.")
                            .AsEphemeral()
                    );
                    return Result.Failure("Unauthorized user");
                }

                await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

                // Get tracked selections
                var selections = _challengeStateManager.GetState(interaction.Message.Id);
                if (selections is null)
                {
                    await interaction.CreateFollowupMessageAsync(
                        new DiscordFollowupMessageBuilder()
                            .WithContent("❌ Please make your selections before issuing the challenge.")
                            .AsEphemeral()
                    );
                    return Result.Failure("No selections found");
                }

                // Get current state from the message (for team size info)
                var configState = await ExtractConfigurationStateAsync(interaction.Message);
                if (!configState.Success || configState.Data is null)
                {
                    return Result.Failure("Failed to extract configuration state");
                }

                var state = configState.Data;

                // Validate we have all required selections
                if (string.IsNullOrEmpty(selections.OpponentTeamId))
                {
                    await interaction.CreateFollowupMessageAsync(
                        new DiscordFollowupMessageBuilder()
                            .WithContent("❌ Please select an opponent team first.")
                            .AsEphemeral()
                    );
                    return Result.Failure("No opponent team selected");
                }

                var requiredPlayers = state.TeamSize.GetPlayersPerTeam();
                if (selections.PlayerIds is null || selections.PlayerIds.Count != requiredPlayers)
                {
                    await interaction.CreateFollowupMessageAsync(
                        new DiscordFollowupMessageBuilder()
                            .WithContent($"❌ Please select exactly {requiredPlayers} players from your roster.")
                            .AsEphemeral()
                    );
                    return Result.Failure("Invalid player selection");
                }

                state.SelectedOpponentTeamId = selections.OpponentTeamId;
                state.SelectedPlayerIds = selections.PlayerIds;
                state.BestOf = selections.BestOf ?? 1;

                // Get the challenger and opponent teams
                var challengerTeamResult = await CoreService.Teams.GetByIdAsync(
                    challengerTeamId,
                    DatabaseComponent.Repository
                );
                var opponentTeamResult = await CoreService.Teams.GetByIdAsync(
                    Guid.Parse(state.SelectedOpponentTeamId),
                    DatabaseComponent.Repository
                );

                if (
                    !challengerTeamResult.Success
                    || challengerTeamResult.Data is null
                    || !opponentTeamResult.Success
                    || opponentTeamResult.Data is null
                )
                {
                    await interaction.CreateFollowupMessageAsync(
                        new DiscordFollowupMessageBuilder().WithContent("❌ Failed to load team data.").AsEphemeral()
                    );
                    return Result.Failure("Failed to load teams");
                }

                // Get the issuer's player ID
                var issuerPlayer = await CoreService.WithDbContext(async db =>
                    await db.Players.Where(p => p.MashinaUser.DiscordUserId == userId).FirstOrDefaultAsync()
                );

                if (issuerPlayer is null)
                {
                    await interaction.CreateFollowupMessageAsync(
                        new DiscordFollowupMessageBuilder()
                            .WithContent("❌ You must be registered as a player to issue challenges.")
                            .AsEphemeral()
                    );
                    return Result.Failure("Issuer player not found");
                }

                // Issue the challenge by publishing the event with IDs instead of names
                Console.WriteLine($"🔍 DEBUG: Publishing ChallengeRequested event:");
                Console.WriteLine($"   TeamSize: {(int)state.TeamSize}");
                Console.WriteLine($"   ChallengerTeamId: {challengerTeamResult.Data.Id}");
                Console.WriteLine($"   OpponentTeamId: {opponentTeamResult.Data.Id}");
                Console.WriteLine($"   SelectedPlayerIds: [{string.Join(", ", state.SelectedPlayerIds)}]");
                Console.WriteLine($"   IssuedByPlayerId: {issuerPlayer.Id}");
                Console.WriteLine($"   BestOf: {state.BestOf ?? 1}");

                var pubResult = await PublishChallengeRequestedAsync(
                    (int)state.TeamSize,
                    challengerTeamResult.Data.Id,
                    opponentTeamResult.Data.Id,
                    [.. state.SelectedPlayerIds],
                    issuerPlayer.Id,
                    state.BestOf ?? 1
                );

                Console.WriteLine($"📤 DEBUG: PublishChallengeRequestedAsync result: Success={pubResult.Success}");
                if (!pubResult.Success)
                {
                    Console.WriteLine($"   Error: {pubResult.ErrorMessage}");
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder().WithContent(
                            $"❌ Failed to issue challenge: {pubResult.ErrorMessage}"
                        )
                    );
                    return Result.Failure($"Failed to publish challenge: {pubResult.ErrorMessage}");
                }
                Console.WriteLine($"   Event published successfully");

                // Delete the configuration message and send confirmation
                try
                {
                    await interaction.Message.DeleteAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Warning: Could not delete configuration message: {ex.Message}");
                    // Continue anyway - message deletion is not critical
                }

                // Clean up tracked selections
                _challengeStateManager.RemoveState(interaction.Message.Id);

                return Result.CreateSuccess("Challenge issued successfully");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to issue challenge",
                    nameof(ProcessIssueChallengeButtonAsync)
                );
                return Result.Failure($"Failed to update challenge button state: {ex.Message}");
            }
        }

        private static async Task<Result> ProcessCancelConfigurationButtonAsync(
            DiscordInteraction interaction,
            string customId
        )
        {
            await interaction.CreateResponseAsync(
                DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Challenge configuration cancelled.").AsEphemeral()
            );

            // Delete the configuration message
            try
            {
                await interaction.Message.DeleteAsync();
            }
            catch
            {
                // Ignore errors if message was already deleted
            }

            // Clean up tracked selections
            _challengeStateManager.RemoveState(interaction.Message.Id);

            return Result.CreateSuccess("Configuration cancelled");
        }

        /// <summary>
        /// Extracts the current configuration state from the container message components
        /// </summary>
        private static async Task<Result<ChallengeConfigurationState>> ExtractConfigurationStateAsync(
            DiscordMessage message
        )
        {
            try
            {
                var container = message.Components?.OfType<DiscordContainerComponent>().FirstOrDefault();
                if (container is null)
                {
                    return Result<ChallengeConfigurationState>.Failure("No container found in message");
                }

                var state = new ChallengeConfigurationState();

                // Parse the components to extract state
                foreach (var component in container.Components)
                {
                    if (component is DiscordActionRowComponent actionRow)
                    {
                        foreach (var child in actionRow.Components)
                        {
                            // Extract opponent team selection (just get the challenger team ID from custom ID)
                            if (
                                child is DiscordSelectComponent select
                                && select.CustomId.Contains("challenge_opponent_")
                            )
                            {
                                var teamIdStr = select.CustomId.Replace("challenge_opponent_", "");
                                if (Guid.TryParse(teamIdStr, out var teamId))
                                {
                                    state.ChallengerTeamId = teamId;
                                }
                            }

                            // Extract user ID from issue button
                            if (child is DiscordButtonComponent button && button.CustomId.Contains("challenge_issue_"))
                            {
                                var parts = button.CustomId.Replace("challenge_issue_", "").Split('_');
                                if (parts.Length >= 2 && ulong.TryParse(parts[1], out var userId))
                                {
                                    state.UserId = userId;
                                }
                            }
                        }
                    }
                }

                // Load the team and roster data with eager loading
                if (state.ChallengerTeamId != Guid.Empty)
                {
                    state.ChallengerTeam = await CoreService.WithDbContext(async db =>
                    {
                        return await db
                            .Teams.Include(t => t.Rosters)
                            .ThenInclude(r => r.RosterMembers)
                            .ThenInclude(m => m.MashinaUser)
                            .FirstOrDefaultAsync(t => t.Id == state.ChallengerTeamId);
                    });

                    if (state.ChallengerTeam is not null)
                    {
                        // Infer team size from roster with active members
                        var rosterWithMembers = state.ChallengerTeam.Rosters.FirstOrDefault(r =>
                            r.RosterMembers.Any(m => m.IsActive)
                        );
                        if (rosterWithMembers is not null)
                        {
                            // Infer team size from roster group
                            state.TeamSize = rosterWithMembers.RosterGroup switch
                            {
                                TeamSizeRosterGroup.Solo => TeamSize.OneVOne,
                                TeamSizeRosterGroup.Duo => TeamSize.TwoVTwo,
                                TeamSizeRosterGroup.Squad => TeamSize.ThreeVThree,
                                _ => TeamSize.TwoVTwo,
                            };
                            state.ChallengerRoster = rosterWithMembers;
                        }
                    }
                }

                return Result<ChallengeConfigurationState>.CreateSuccess(state, "State extracted");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to extract configuration state",
                    nameof(ExtractConfigurationStateAsync)
                );
                return Result<ChallengeConfigurationState>.Failure($"Failed to extract state: {ex.Message}");
            }
        }

        #endregion
    }

    #region Result Types
    public record ScrimmageThreadsResult(DiscordThreadChannel ChallengerThread, DiscordThreadChannel OpponentThread);

    public record ChallengeCancellationResult(
        string ChallengerTeamName,
        string OpponentTeamName,
        ulong? ChallengeMessageId,
        ulong? ChallengeChannelId
    );

    internal class ChallengeConfigurationState
    {
        public Guid ChallengerTeamId { get; set; }
        public ulong UserId { get; set; }
        public TeamSize TeamSize { get; set; }
        public Team ChallengerTeam { get; set; } = null!;
        public TeamRoster ChallengerRoster { get; set; } = null!;
        public string? SelectedOpponentTeamId { get; set; }
        public List<Guid>? SelectedPlayerIds { get; set; }
        public int? BestOf { get; set; }
    }

    /// <summary>
    /// Tracks user selections in the challenge configuration message
    /// </summary>
    internal class ChallengeConfigurationSelections
    {
        public string? OpponentTeamId { get; set; }
        public List<Guid>? PlayerIds { get; set; }
        public int? BestOf { get; set; }
    }
    #endregion
}
