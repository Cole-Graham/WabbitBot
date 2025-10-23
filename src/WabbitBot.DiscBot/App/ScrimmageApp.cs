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
        private static readonly MessageStateManager<ChallengeConfigurationSelections> _challengeStateManager = new();

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

            Console.WriteLine($"[DEBUG] ProcessSelectMenuInteractionAsync - CustomId: {customId}");
            Console.WriteLine($"[DEBUG] User: {interaction.User.Username} ({interaction.User.Id})");
            Console.WriteLine($"[DEBUG] Values count: {interaction.Data.Values?.Count() ?? 0}");

            try
            {
                // Opponent team selection
                if (customId.StartsWith("challenge_opponent_", StringComparison.Ordinal))
                {
                    Console.WriteLine("[DEBUG] Routing to ProcessOpponentSelectionAsync");
                    return await ProcessOpponentSelectionAsync(interaction, customId);
                }

                // Player selection
                if (customId.StartsWith("challenge_players_", StringComparison.Ordinal))
                {
                    Console.WriteLine("[DEBUG] Routing to ProcessPlayerSelectionAsync");
                    return await ProcessPlayerSelectionAsync(interaction, customId);
                }

                // Best of selection
                if (customId.StartsWith("challenge_bestof_", StringComparison.Ordinal))
                {
                    Console.WriteLine("[DEBUG] Routing to ProcessBestOfSelectionAsync");
                    return await ProcessBestOfSelectionAsync(interaction, customId);
                }

                // Teammate selection from challenge container
                if (customId.StartsWith("select_teammates_", StringComparison.Ordinal))
                {
                    Console.WriteLine("[DEBUG] Routing to ProcessTeammateSelectionAsync");
                    return await ProcessTeammateSelectionAsync(interaction, customId);
                }

                Console.WriteLine($"[ERROR] Unknown custom ID: {customId}");
                return Result.Failure($"Unknown custom ID: {customId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ProcessSelectMenuInteractionAsync failed: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");

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
                            .WithContent($"An error occurred while processing your selection: {ex.Message}")
                            .AsEphemeral()
                    );
                }
                catch (Exception responseEx)
                {
                    Console.WriteLine($"[ERROR] Failed to send error response: {responseEx.Message}");
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

            // Get opponent team ID from the challenge
            var OpponentTeamId = Challenge.OpponentTeamId;

            // Check if opponent team players have been selected
            if (
                Challenge.OpponentTeammateIds is null
                || Challenge.OpponentTeammateIds.Count == 0
                || !Challenge.AcceptedByPlayerId.HasValue
            )
            {
                return Result.Failure("Opponent team players not selected yet");
            }

            // OpponentTeammateIds now contains the complete list of selected players
            var opponentPlayerIds = Challenge.OpponentTeammateIds.ToList();

            var OpponentSelectedPlayers = await CoreService.WithDbContext(async db =>
                await db.Players.Where(p => opponentPlayerIds.Contains(p.Id)).ToListAsync()
            );

            var AcceptedByPlayerId = Challenge.AcceptedByPlayerId.Value;

            // Acknowledge interaction
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            // Publish ScrimmageAccepted (Global) to Core
            await ProcessChallengeAcceptedAsync(Challenge, OpponentTeamId, OpponentSelectedPlayers, AcceptedByPlayerId);

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
            var isCaptain = player.Id == challengerTeam.TeamMajorId;

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
            List<Player> OpponentSelectedPlayers,
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
                [.. OpponentSelectedPlayers.Select(p => p.Id)],
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

                if (Match.Team1Players is null)
                {
                    return Result<ScrimmageThreadsResult>.Failure("Team 1 players not found");
                }
                if (Match.Team2Players is null)
                {
                    return Result<ScrimmageThreadsResult>.Failure("Team 2 players not found");
                }

                var ChallengerTeamMentions = new List<string>();
                foreach (var player in Match.Team1Players)
                {
                    var getChallengerPlayer = await CoreService.Players.GetByIdAsync(
                        player.Id,
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
                foreach (var player in Match.Team2Players)
                {
                    var getOpponentPlayer = await CoreService.Players.GetByIdAsync(
                        player.Id,
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

                // Construct challenger team players: issuer + teammates
                var challengerPlayerIds = new List<Guid> { Scrimmage.ScrimmageChallenge.IssuedByPlayerId };
                challengerPlayerIds.AddRange(Scrimmage.ScrimmageChallenge.ChallengerTeammateIds);
                var challengerPlayers = await CoreService.WithDbContext(async db =>
                    await db.Players.Where(p => challengerPlayerIds.Contains(p.Id)).ToListAsync()
                );

                // Construct opponent team players: acceptor + teammates
                if (!Scrimmage.ScrimmageChallenge.AcceptedByPlayerId.HasValue)
                {
                    return Result<ScrimmageThreadsResult>.Failure("Challenge not yet accepted");
                }
                if (
                    Scrimmage.ScrimmageChallenge.OpponentTeammateIds is null
                    || Scrimmage.ScrimmageChallenge.OpponentTeammateIds.Count == 0
                )
                {
                    return Result<ScrimmageThreadsResult>.Failure("Opponent teammates not selected");
                }
                // OpponentTeammateIds now contains the complete list of selected players
                var opponentPlayerIds = Scrimmage.ScrimmageChallenge.OpponentTeammateIds.ToList();
                var opponentPlayers = await CoreService.WithDbContext(async db =>
                    await db.Players.Where(p => opponentPlayerIds.Contains(p.Id)).ToListAsync()
                );

                // Build messages
                var getContainers = await MatchRenderer.RenderScrimmageMatchContainersAsync(
                    Match.Id,
                    [.. challengerPlayers],
                    [.. opponentPlayers]
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
                // Single consolidated database query for all required data
                var validationData = await CoreService.WithDbContext(async db =>
                {
                    var scrimmage = await db.Scrimmages.FirstOrDefaultAsync(s => s.Id == scrimmageId);
                    if (scrimmage is null || scrimmage.CompletedAt.HasValue)
                    {
                        return null;
                    }

                    var requestingPlayer = await db.Players.FirstOrDefaultAsync(p =>
                        p.MashinaUser.DiscordUserId == requestingUserId
                    );
                    if (requestingPlayer is null)
                    {
                        return null;
                    }

                    // Determine team and check if player is on that team
                    var isOnChallengerTeam = scrimmage.ChallengerTeamPlayers.Any(p => p.Id == playerOutId);
                    var isOnOpponentTeam = scrimmage.OpponentTeamPlayers.Any(p => p.Id == playerOutId);
                    if (!isOnChallengerTeam && !isOnOpponentTeam)
                    {
                        return null;
                    }

                    var teamId = isOnChallengerTeam ? scrimmage.ChallengerTeamId : scrimmage.OpponentTeamId;
                    var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == teamId);
                    if (team is null)
                    {
                        return null;
                    }

                    var rosterGroup = TeamCore.TeamSizeRosterGrouping.GetRosterGroup(scrimmage.TeamSize);

                    // Check permissions and roster membership in single query
                    var IsRosterManager = await db.TeamMembers.AnyAsync(tm =>
                        tm.TeamRoster.TeamId == teamId
                        && tm.TeamRoster.RosterGroup == rosterGroup
                        && tm.PlayerId == requestingPlayer.Id
                        && tm.IsRosterManager
                    );

                    var isOnRoster = await db.TeamMembers.AnyAsync(tm =>
                        tm.TeamRoster.TeamId == teamId
                        && tm.TeamRoster.RosterGroup == rosterGroup
                        && tm.PlayerId == playerInId
                        && tm.ValidTo == null
                    );

                    // Get both players in one go
                    var playerIds = new[] { playerOutId, playerInId };
                    var players = await db.Players.Where(p => playerIds.Contains(p.Id)).ToListAsync();
                    var playerOut = players.FirstOrDefault(p => p.Id == playerOutId);
                    var playerIn = players.FirstOrDefault(p => p.Id == playerInId);

                    return new
                    {
                        Scrimmage = scrimmage,
                        RequestingPlayer = requestingPlayer,
                        Team = team,
                        IsOnChallengerTeam = isOnChallengerTeam,
                        IsTeamCaptain = team.TeamMajorId == requestingPlayer.Id,
                        IsPlayerBeingSubbed = playerOutId == requestingPlayer.Id,
                        IsRosterManager = IsRosterManager,
                        IsOnRoster = isOnRoster,
                        PlayerOut = playerOut,
                        PlayerIn = playerIn,
                        AlreadyInScrimmage = scrimmage.ChallengerTeamPlayers.Any(p => p.Id == playerInId)
                            || scrimmage.OpponentTeamPlayers.Any(p => p.Id == playerInId),
                    };
                });

                // Validation checks
                if (validationData is null)
                {
                    return Result<string>.Failure(
                        "Invalid substitution request: scrimmage not found, completed, or player not in scrimmage."
                    );
                }

                if (
                    !validationData.IsTeamCaptain
                    && !validationData.IsRosterManager
                    && !validationData.IsPlayerBeingSubbed
                )
                {
                    return Result<string>.Failure(
                        "Only the team captain, a team manager, or the player being substituted can make substitutions."
                    );
                }

                if (validationData.AlreadyInScrimmage)
                {
                    return Result<string>.Failure("The substitute player is already in this scrimmage.");
                }

                if (!validationData.IsOnRoster)
                {
                    return Result<string>.Failure(
                        "The substitute player is not on the team's roster for this game size."
                    );
                }

                if (validationData.PlayerOut is null || validationData.PlayerIn is null)
                {
                    return Result<string>.Failure("One or both players not found.");
                }

                var scrimmage = validationData.Scrimmage;
                var playerOut = validationData.PlayerOut;
                var playerIn = validationData.PlayerIn;
                var isOnChallengerTeam = validationData.IsOnChallengerTeam;

                // Helper to replace player in collection
                void ReplacePlayerInList(ICollection<Player> collection, Player oldPlayer, Player newPlayer)
                {
                    collection.Remove(oldPlayer);
                    collection.Add(newPlayer);
                }

                // Simulate substitution and validate lineup per TeamRules before committing
                var simulatedChallenger = scrimmage.ChallengerTeamPlayers.Select(p => p.Id).ToList();
                var simulatedOpponent = scrimmage.OpponentTeamPlayers.Select(p => p.Id).ToList();
                if (isOnChallengerTeam)
                {
                    var idx = simulatedChallenger.IndexOf(playerOut.Id);
                    if (idx >= 0)
                        simulatedChallenger[idx] = playerIn.Id;
                }
                else
                {
                    var idx = simulatedOpponent.IndexOf(playerOut.Id);
                    if (idx >= 0)
                        simulatedOpponent[idx] = playerIn.Id;
                }

                var teamIdForValidation = isOnChallengerTeam ? scrimmage.ChallengerTeamId : scrimmage.OpponentTeamId;
                var lineupIds = isOnChallengerTeam ? simulatedChallenger : simulatedOpponent;
                var lineupValidation = await TeamCore.Validation.ValidateLineupForMatch(
                    teamIdForValidation,
                    scrimmage.TeamSize,
                    lineupIds
                );

                if (!lineupValidation.Success)
                {
                    return Result<string>.Failure(lineupValidation.ErrorMessage ?? "Substitution violates team rules.");
                }

                // Update scrimmage player lists (full combined lists)
                if (isOnChallengerTeam)
                {
                    ReplacePlayerInList(scrimmage.ChallengerTeamPlayers, playerOut, playerIn);
                }
                else
                {
                    ReplacePlayerInList(scrimmage.OpponentTeamPlayers, playerOut, playerIn);
                }

                await CoreService.Scrimmages.UpdateAsync(scrimmage, DatabaseComponent.Repository);

                // Update match and games if they exist
                bool hasActiveGames = false;
                if (scrimmage.MatchId.HasValue)
                {
                    var matchResult = await CoreService.Matches.GetByIdAsync(
                        scrimmage.MatchId.Value,
                        DatabaseComponent.Repository
                    );

                    if (matchResult.Success && matchResult.Data is not null)
                    {
                        var match = matchResult.Data;

                        if (match.Team1Players is null || match.Team2Players is null)
                        {
                            return Result<string>.Failure("Match player data is incomplete.");
                        }

                        // Update match player lists
                        if (isOnChallengerTeam)
                        {
                            ReplacePlayerInList(match.Team1Players, playerOut, playerIn);
                        }
                        else
                        {
                            ReplacePlayerInList(match.Team2Players, playerOut, playerIn);
                        }

                        await CoreService.Matches.UpdateAsync(match, DatabaseComponent.Repository);

                        // Update active games - batch process them
                        var activeGameIds = match
                            .Games.Where(g => MatchCore.Accessors.GetCurrentSnapshot(g).CompletedAt == null)
                            .Select(g => g.Id)
                            .ToList();

                        if (activeGameIds.Any())
                        {
                            hasActiveGames = true;
                            await UpdateActiveGamesForSubstitution(
                                activeGameIds,
                                playerOutId,
                                playerInId,
                                isOnChallengerTeam
                            );
                        }
                    }
                }

                // Build response message
                var playerOutName =
                    playerOut.MashinaUser?.DiscordGlobalname
                    ?? playerOut.MashinaUser?.DiscordUsername
                    ?? playerOut.CurrentSteamUsername;
                var playerInName =
                    playerIn.MashinaUser?.DiscordGlobalname
                    ?? playerIn.MashinaUser?.DiscordUsername
                    ?? playerIn.CurrentSteamUsername;

                var message = $"Successfully substituted **{playerOutName}** with **{playerInName}**.";
                if (hasActiveGames)
                {
                    message += " The previous player's deck submission has been cleared if it existed.";
                }

                return Result<string>.CreateSuccess(message);
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

        /// <summary>
        /// Updates active games with player substitution and clears any deck submissions from the substituted player.
        /// </summary>
        private static async Task UpdateActiveGamesForSubstitution(
            List<Guid> activeGameIds,
            Guid playerOutId,
            Guid playerInId,
            bool isOnChallengerTeam
        )
        {
            foreach (var gameId in activeGameIds)
            {
                var gameResult = await CoreService.Games.GetByIdAsync(gameId, DatabaseComponent.Repository);
                if (!gameResult.Success || gameResult.Data is null)
                {
                    continue;
                }

                var game = gameResult.Data;

                // Update game player ID lists
                var playerList = isOnChallengerTeam ? game.Team1PlayerIds : game.Team2PlayerIds;
                var index = playerList.IndexOf(playerOutId);
                if (index >= 0)
                {
                    playerList[index] = playerInId;
                }

                // Handle deck submission cleanup if player had submitted
                var currentSnapshot = MatchCore.Accessors.GetCurrentSnapshot(game);
                if (currentSnapshot.PlayerDeckCodes.ContainsKey(playerOutId))
                {
                    var newSnapshot = MatchCore.Factory.CreateGameStateSnapshotFromOther(currentSnapshot);

                    // Remove player out's deck submission
                    newSnapshot.PlayerDeckCodes.Remove(playerOutId);
                    newSnapshot.PlayerDeckSubmittedAt.Remove(playerOutId);
                    newSnapshot.PlayerDeckConfirmed.Remove(playerOutId);
                    newSnapshot.PlayerDeckConfirmedAt.Remove(playerOutId);

                    // Update player IDs in snapshot
                    var snapshotPlayerList = isOnChallengerTeam
                        ? newSnapshot.Team1PlayerIds
                        : newSnapshot.Team2PlayerIds;
                    var snapshotIndex = snapshotPlayerList.IndexOf(playerOutId);
                    if (snapshotIndex >= 0)
                    {
                        snapshotPlayerList[snapshotIndex] = playerInId;
                    }

                    game.StateHistory.Add(newSnapshot);
                }

                await CoreService.Games.UpdateAsync(game, DatabaseComponent.Repository);
            }
        }
        #endregion

        #region Challenge Configuration Handlers
        private static async Task<Result> ProcessOpponentSelectionAsync(DiscordInteraction interaction, string customId)
        {
            try
            {
                Console.WriteLine($"[DEBUG] ProcessOpponentSelectionAsync started - CustomId: {customId}");

                // Track the selection
                var selections = interaction.Data.Values.ToArray();
                var selectedOpponentTeamId = selections.Length > 0 ? selections[0] : null;

                Console.WriteLine($"[DEBUG] Selections count: {selections.Length}");
                Console.WriteLine($"[DEBUG] Selected opponent team ID: {selectedOpponentTeamId}");

                if (interaction.Message is null)
                {
                    Console.WriteLine("[ERROR] Message not found in ProcessOpponentSelectionAsync");
                    return Result.Failure("Message not found in ProcessOpponentSelectionAsync");
                }

                Console.WriteLine($"[DEBUG] Message ID: {interaction.Message.Id}");

                // Handle case where no selection was made (user deselected or didn't select)
                if (string.IsNullOrEmpty(selectedOpponentTeamId))
                {
                    Console.WriteLine("[DEBUG] No opponent team selected - reverting to initial state");

                    // Clear the opponent team selection in state
                    _challengeStateManager.UpdateState(
                        interaction.Message.Id,
                        state =>
                        {
                            state.OpponentTeamId = null;
                            return state;
                        }
                    );

                    // Update button state to reflect no selection
                    await UpdateChallengeButtonStateAsync(interaction);
                    return Result.CreateSuccess("Opponent selection cleared");
                }

                // Validate that the selected ID is a valid GUID
                if (!Guid.TryParse(selectedOpponentTeamId, out var selectedOpponentTeamGuid))
                {
                    Console.WriteLine($"[ERROR] Invalid opponent team ID format: {selectedOpponentTeamId}");
                    return Result.Failure(
                        $"Invalid opponent team ID format: {selectedOpponentTeamId}. Please try selecting a team again."
                    );
                }

                Console.WriteLine($"[DEBUG] Parsed opponent team GUID: {selectedOpponentTeamGuid}");

                _challengeStateManager.UpdateState(
                    interaction.Message.Id,
                    state =>
                    {
                        Console.WriteLine($"[DEBUG] Updating state - OpponentTeamId: {selectedOpponentTeamGuid}");
                        state.OpponentTeamId = selectedOpponentTeamGuid;
                        return state;
                    }
                );

                Console.WriteLine("[DEBUG] About to call UpdateChallengeButtonStateAsync");

                // Update button state with the interaction response
                await UpdateChallengeButtonStateAsync(interaction);

                Console.WriteLine("[DEBUG] ProcessOpponentSelectionAsync completed successfully");
                return Result.CreateSuccess("Opponent selected");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ProcessOpponentSelectionAsync failed: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");

                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to process opponent selection: {customId}",
                    nameof(ProcessOpponentSelectionAsync)
                );

                return Result.Failure($"Failed to process opponent selection: {ex.Message}");
            }
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

            if (interaction.Message is null)
            {
                return Result.Failure("Message not found in ProcessOpponentSelectionAsync");
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

            if (interaction.Message is null)
            {
                return Result.Failure("Message not found in ProcessBestOfSelectionAsync");
            }

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

            // Get the challenge to check team size requirements
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
            var requiredPlayers = challenge.TeamSize.GetPlayersPerTeam();

            if (selectedTeammateIds.Count != requiredPlayers)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent(
                            $"Please select exactly {requiredPlayers} players for a {challenge.TeamSize} match."
                        )
                        .AsEphemeral()
                );
                return Result.Failure(
                    $"Incorrect number of players selected. Required: {requiredPlayers}, Selected: {selectedTeammateIds.Count}"
                );
            }

            if (interaction.Message is null)
            {
                return Result.Failure("Message not found in ProcessTeammateSelectionAsync");
            }

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
                    .Where(tm => tm.PlayerId == userPlayer && tm.ValidTo == null)
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

            // Store opponent teammate IDs (complete list of selected players)
            challenge.OpponentTeammateIds = selectedTeammateIds.ToList();
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
                Console.WriteLine("[DEBUG] UpdateChallengeButtonStateAsync started");

                if (interaction.Message is null)
                {
                    Console.WriteLine("[ERROR] Message not found in UpdateChallengeButtonStateAsync");
                    throw new InvalidOperationException("Message not found in UpdateChallengeButtonStateAsync");
                }

                Console.WriteLine($"[DEBUG] Message ID: {interaction.Message.Id}");

                // Get current state
                Console.WriteLine("[DEBUG] Extracting configuration state...");
                var configState = await ExtractConfigurationStateAsync(interaction.Message);

                Console.WriteLine(
                    $"[DEBUG] Config state success: {configState.Success}, Has data: {configState.Data is not null}"
                );

                if (!configState.Success || configState.Data is null)
                {
                    Console.WriteLine($"[ERROR] Failed to extract config state: {configState.ErrorMessage}");
                    return;
                }

                Console.WriteLine(
                    $"[DEBUG] Config state - DiscordUserId: {configState.Data.DiscordUserId},"
                        + $" TeamSize: {configState.Data.TeamSize}"
                );
                Console.WriteLine($"[DEBUG] Config state - ChallengerTeam ID: {configState.Data.ChallengerTeam?.Id}");

                // Get tracked selections
                Console.WriteLine("[DEBUG] Getting tracked selections...");
                var selections = _challengeStateManager.GetState(interaction.Message.Id);

                if (selections is null)
                {
                    Console.WriteLine("[ERROR] No selections found in state manager");
                    return;
                }

                Console.WriteLine($"[DEBUG] Selections - OpponentTeamId: {selections.OpponentTeamId}");
                Console.WriteLine($"[DEBUG] Selections - PlayerIds count: {selections.PlayerIds?.Count ?? 0}");
                Console.WriteLine($"[DEBUG] Selections - BestOf: {selections.BestOf}");

                if (configState.Data.ChallengerRoster is null)
                {
                    Console.WriteLine("[ERROR] Challenger roster not found in UpdateChallengeButtonStateAsync");
                    throw new InvalidOperationException(
                        "Challenger roster not found in UpdateChallengeButtonStateAsync"
                    );
                }

                Console.WriteLine(
                    $"[DEBUG] ChallengerRoster count: {configState.Data.ChallengerRoster.RosterMembers.Count}"
                );

                Console.WriteLine("[DEBUG] About to render challenge configuration...");
                Console.WriteLine(
                    $"[DEBUG] Rendering with - OpponentTeamId: {selections.OpponentTeamId}, PlayerIds:"
                        + $"{selections.PlayerIds?.Count ?? 0}, BestOf: {selections.BestOf ?? 1}"
                );

                if (configState.Data.ChallengerTeam is null)
                {
                    Console.WriteLine("[ERROR] Challenger team not found in UpdateChallengeButtonStateAsync");
                    return;
                }

                // Re-render the entire container with selections preserved
                // OpponentTeamId, PlayerIds, and BestOf can all be null
                var containerResult = await ScrimmageRenderer.RenderChallengeConfigurationAsync(
                    configState.Data.DiscordUserId,
                    configState.Data.TeamSize,
                    configState.Data.ChallengerTeam,
                    configState.Data.ChallengerRoster,
                    selections.OpponentTeamId,
                    selections.PlayerIds,
                    selections.BestOf ?? 1
                );

                Console.WriteLine(
                    $"[DEBUG] Container render result - Success: {containerResult.Success},"
                        + $" Has data: {containerResult.Data is not null}"
                );

                if (containerResult.Success && containerResult.Data is not null)
                {
                    Console.WriteLine("[DEBUG] Creating interaction response to update message...");

                    // Use the interaction response to update the message (preserves selections)
                    await interaction.CreateResponseAsync(
                        DiscordInteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder()
                            .EnableV2Components()
                            .AddContainerComponent(containerResult.Data)
                    );

                    Console.WriteLine("[DEBUG] Interaction response created successfully");
                }
                else
                {
                    Console.WriteLine($"[ERROR] Container render failed: {containerResult.ErrorMessage}");
                }

                Console.WriteLine("[DEBUG] UpdateChallengeButtonStateAsync completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] UpdateChallengeButtonStateAsync failed: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");

                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to update challenge button state",
                    nameof(UpdateChallengeButtonStateAsync)
                );

                // Try to send an error response if we haven't responded yet
                try
                {
                    if (interaction.ResponseState == DiscordInteractionResponseState.Unacknowledged)
                    {
                        Console.WriteLine("[DEBUG] Attempting to send error response to user...");
                        await interaction.CreateResponseAsync(
                            DiscordInteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                                .WithContent($"Failed to update challenge configuration: {ex.Message}")
                                .AsEphemeral()
                        );
                    }
                }
                catch (Exception responseEx)
                {
                    Console.WriteLine($"[ERROR] Failed to send error response: {responseEx.Message}");
                }
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

                if (interaction.Message is null)
                {
                    throw new InvalidOperationException("Message not found in ProcessIssueChallengeButtonAsync");
                }

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
                if (selections.OpponentTeamId is null)
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

                if (state.SelectedOpponentTeamId is null)
                {
                    throw new InvalidOperationException(
                        "Opponent team not selected in ProcessIssueChallengeButtonAsync"
                    );
                }

                var opponentTeamResult = await CoreService.Teams.GetByIdAsync(
                    state.SelectedOpponentTeamId.Value,
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

                // Delete the temporary thread if this message is in one
                try
                {
                    if (interaction.Channel is DiscordThreadChannel threadChannel)
                    {
                        Console.WriteLine($"🔍 DEBUG: Deleting temporary thread: {threadChannel.Name}");
                        await threadChannel.DeleteAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Warning: Could not delete temporary thread: {ex.Message}");
                    // Continue anyway - thread deletion is not critical
                }

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

            if (interaction.Message is null)
            {
                return Result.Failure("Message not found in ProcessCancelConfigurationButtonAsync");
            }

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

            // Delete the thread if this message is in one
            try
            {
                if (interaction.Channel is DiscordThreadChannel threadChannel)
                {
                    Console.WriteLine($"[DEBUG] Deleting cancelled challenge setup thread: {threadChannel.Name}");
                    await threadChannel.DeleteAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Failed to delete cancelled challenge thread: {ex.Message}");
                // Continue anyway - thread deletion is not critical
            }

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
                Console.WriteLine("[DEBUG] ExtractConfigurationStateAsync started");
                Console.WriteLine($"[DEBUG] Message has components: {message.Components is not null}");

                var container = message.Components?.OfType<DiscordContainerComponent>().FirstOrDefault();
                if (container is null)
                {
                    Console.WriteLine("[ERROR] No container found in message");
                    return Result<ChallengeConfigurationState>.Failure("No container found in message");
                }

                Console.WriteLine($"[DEBUG] Container has {container.Components.Count} components");

                ChallengeConfigurationState? state = null;
                Guid? challengerTeamId = null;
                ulong? discordUserId = null;

                foreach (var component in container.Components)
                {
                    Console.WriteLine($"[DEBUG] Component type: {component.GetType().Name}");

                    if (component is DiscordActionRowComponent actionRow)
                    {
                        Console.WriteLine($"[DEBUG] ActionRow has {actionRow.Components.Count} child components");

                        foreach (var child in actionRow.Components)
                        {
                            Console.WriteLine($"[DEBUG] Child component type: {child.GetType().Name}");

                            // Extract opponent team selection (just get the challenger team ID from custom ID)
                            if (child is DiscordSelectComponent select)
                            {
                                Console.WriteLine($"[DEBUG] SelectComponent CustomId: {select.CustomId}");

                                if (select.CustomId.Contains("challenge_opponent_", StringComparison.Ordinal))
                                {
                                    var teamIdStr = select.CustomId.Replace(
                                        "challenge_opponent_",
                                        "",
                                        StringComparison.Ordinal
                                    );
                                    Console.WriteLine($"[DEBUG] Extracted team ID string: {teamIdStr}");

                                    if (Guid.TryParse(teamIdStr, out var teamId))
                                    {
                                        challengerTeamId = teamId;
                                        Console.WriteLine($"[DEBUG] Parsed challenger team ID: {challengerTeamId}");
                                    }
                                }
                            }

                            // Extract user ID from issue button
                            if (child is DiscordButtonComponent button)
                            {
                                Console.WriteLine($"[DEBUG] ButtonComponent CustomId: {button.CustomId}");

                                if (button.CustomId.Contains("challenge_issue_", StringComparison.Ordinal))
                                {
                                    var parts = button
                                        .CustomId.Replace("challenge_issue_", "", StringComparison.Ordinal)
                                        .Split('_');
                                    Console.WriteLine($"[DEBUG] Button ID parts count: {parts.Length}");

                                    if (parts.Length >= 2)
                                    {
                                        Console.WriteLine(
                                            $"[DEBUG] Team ID part: {parts[0]}, User ID part: {parts[1]}"
                                        );

                                        if (ulong.TryParse(parts[1], out var userId))
                                        {
                                            discordUserId = userId;
                                            Console.WriteLine($"[DEBUG] Parsed Discord user ID: {discordUserId}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                Console.WriteLine(
                    $"[DEBUG] Final extraction - ChallengerTeamId: {challengerTeamId}, DiscordUserId: {discordUserId}"
                );

                if (challengerTeamId is not null && discordUserId is not null)
                {
                    Console.WriteLine("[DEBUG] Creating ChallengeConfigurationState");
                    state = new ChallengeConfigurationState
                    {
                        ChallengerTeamId = challengerTeamId.Value,
                        DiscordUserId = discordUserId.Value,
                    };
                }
                else
                {
                    Console.WriteLine(
                        $"[ERROR] Invalid challenger team or user ID - TeamId null: {challengerTeamId is null},"
                            + $" UserId null: {discordUserId is null}"
                    );
                    return Result<ChallengeConfigurationState>.Failure("Invalid challenger team or user ID");
                }

                // Load the team and roster data with eager loading
                if (state is not null)
                {
                    Console.WriteLine(
                        $"[DEBUG] Loading team from database - ChallengerTeamId: {state.ChallengerTeamId}"
                    );

                    var teamResult = await CoreService.TryWithDbContext(
                        async db =>
                        {
                            var team = await db
                                .Teams.Include(t => t.Rosters)
                                .ThenInclude(r => r.RosterMembers)
                                .ThenInclude(m => m.MashinaUser)
                                .FirstOrDefaultAsync(t => t.Id == state.ChallengerTeamId);

                            if (team is null)
                            {
                                throw new InvalidOperationException($"Team not found: {state.ChallengerTeamId}");
                            }

                            return team;
                        },
                        "Load challenger team for challenge configuration"
                    );

                    if (!teamResult.Success || teamResult.Data is null)
                    {
                        Console.WriteLine($"[ERROR] Failed to load challenger team: {teamResult.ErrorMessage}");
                        return Result<ChallengeConfigurationState>.Failure(
                            $"Failed to load challenger team: {teamResult.ErrorMessage}"
                        );
                    }

                    state.ChallengerTeam = teamResult.Data;
                    Console.WriteLine(
                        $"[DEBUG] Team loaded - Name: {state.ChallengerTeam.Name},"
                            + $" Rosters count: {state.ChallengerTeam.Rosters.Count}"
                    );

                    // Infer team size from roster with active members
                    var rosterWithMembers = state.ChallengerTeam.Rosters.FirstOrDefault(r =>
                        r.RosterMembers.Any(m => m.ValidTo == null)
                    );

                    if (rosterWithMembers is not null)
                    {
                        Console.WriteLine(
                            $"[DEBUG] Found roster with active members - Group: {rosterWithMembers.RosterGroup}"
                        );

                        // Infer team size from roster group
                        state.TeamSize = rosterWithMembers.RosterGroup switch
                        {
                            TeamSizeRosterGroup.Solo => TeamSize.OneVOne,
                            TeamSizeRosterGroup.Duo => TeamSize.TwoVTwo,
                            TeamSizeRosterGroup.Squad => TeamSize.ThreeVThree,
                            _ => TeamSize.TwoVTwo,
                        };
                        state.ChallengerRoster = rosterWithMembers;

                        Console.WriteLine($"[DEBUG] Team size set to: {state.TeamSize}");
                    }
                    else
                    {
                        Console.WriteLine("[ERROR] No roster with active members found");
                        return Result<ChallengeConfigurationState>.Failure("No roster with active members found");
                    }
                }
                else
                {
                    Console.WriteLine("[ERROR] Failed to extract configuration state - state is null");
                    return Result<ChallengeConfigurationState>.Failure("Failed to extract configuration state");
                }

                Console.WriteLine("[DEBUG] State extracted successfully");
                return Result<ChallengeConfigurationState>.CreateSuccess(state, "State extracted");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ExtractConfigurationStateAsync exception: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");

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
        public required Guid ChallengerTeamId { get; set; }
        public virtual Team ChallengerTeam { get; set; } = null!;
        public required ulong DiscordUserId { get; set; }
        public TeamSize TeamSize { get; set; }
        public TeamRoster? ChallengerRoster { get; set; }
        public Guid? SelectedOpponentTeamId { get; set; }
        public List<Guid>? SelectedPlayerIds { get; set; }
        public int? BestOf { get; set; }
    }

    /// <summary>
    /// Tracks user selections in the challenge configuration message
    /// </summary>
    internal class ChallengeConfigurationSelections
    {
        public Guid? OpponentTeamId { get; set; }
        public List<Guid>? PlayerIds { get; set; }
        public int? BestOf { get; set; }
    }
    #endregion
}
