using System.Security.Cryptography.X509Certificates;
using DSharpPlus;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Models.Scrimmage;
using WabbitBot.Core.Common.Services;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Interfaces;
using WabbitBot.DiscBot.App.Services.DiscBot;

namespace WabbitBot.DiscBot.App
{
    public partial class MatchApp : IMatchApp
    {
        #region Modals
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
                // Map ban confirmation button
                if (customId.StartsWith("confirm_mapban_", StringComparison.Ordinal))
                {
                    return await ConfirmMapBanAsync(interaction, customId);
                }

                // Map ban revision button
                if (customId.StartsWith("revise_mapban_", StringComparison.Ordinal))
                {
                    return await ReviseMapBanAsync(interaction, customId);
                }

                // Rematch request button
                if (customId.StartsWith("rematch_request_", StringComparison.Ordinal))
                {
                    return await ProcessRequestRematchAsync(interaction, customId);
                }

                // Rematch accept button
                if (customId.StartsWith("accept_rematch_", StringComparison.Ordinal))
                {
                    return await ProcessAcceptRematchAsync(interaction, customId);
                }

                // Rematch decline button
                if (customId.StartsWith("decline_rematch_", StringComparison.Ordinal))
                {
                    return await ProcessDeclineRematchAsync(interaction, customId);
                }

                // Forfeit match button
                if (customId.StartsWith("forfeit_match_", StringComparison.Ordinal))
                {
                    return await ProcessForfeitMatchRequestAsync(interaction, customId);
                }

                // Confirm forfeit match button
                if (customId.StartsWith("confirm_forfeit_match_", StringComparison.Ordinal))
                {
                    return await ProcessConfirmForfeitMatchAsync(interaction, customId);
                }

                // Cancel forfeit match button
                if (customId.StartsWith("cancel_forfeit_match_", StringComparison.Ordinal))
                {
                    return await ProcessCancelForfeitMatchAsync(interaction, customId);
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

        /// <summary>
        /// Handles string select dropdown interactions (map ban selections).
        /// Returns Result indicating success/failure for immediate feedback.
        /// Publishes events for cross-boundary communication.
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
                // Map ban selection dropdown
                if (customId.StartsWith("ban_select_", StringComparison.Ordinal))
                {
                    return await ProcessMapBanSelectionAsync(interaction, customId);
                }

                // Refresh button (optional - could reload the container)
                if (customId.StartsWith("refresh_bans_", StringComparison.Ordinal))
                {
                    // Just defer the update - no action needed
                    await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                    return Result.CreateSuccess("Refresh acknowledged");
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

        /// <summary>
        /// Handles modal submissions (deck code input).
        /// Returns Result indicating success/failure for immediate feedback.
        /// </summary>
        public static async Task<Result> ProcessModalSubmitAsync(DiscordClient client, ModalSubmittedEventArgs args)
        {
            var interaction = args.Interaction;
            var customId = interaction.Data.CustomId;

            try
            {
                // Placeholder, add modal interactions here
                return Result.CreateSuccess("No modal handlers registered");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle modal submission: {customId}",
                    nameof(ProcessModalSubmitAsync)
                );

                // Try to respond with error - may fail if response was already sent
                try
                {
                    await interaction.CreateResponseAsync(
                        DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("An error occurred while processing your submission. Please try again.")
                            .AsEphemeral()
                    );
                }
                catch
                {
                    // Response was already sent, ignore
                }

                return Result.Failure($"Failed to handle modal submission: {ex.Message}");
            }
        }
        #endregion

        #region Map Bans
        private static async Task<Result> ProcessMapBanSelectionAsync(DiscordInteraction interaction, string customId)
        {
            // Parse match ID and team ID from custom ID: "ban_select_{matchId}_{teamId}"
            var parts = customId.Replace("ban_select_", "", StringComparison.Ordinal).Split('_');
            if (
                parts.Length != 2
                || !Guid.TryParse(parts[0], out var matchId)
                || !Guid.TryParse(parts[1], out var teamId)
            )
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Invalid match or team ID.").AsEphemeral()
                );
                return Result.Failure("Invalid match or team ID");
            }

            // Get selected values from dropdown
            var selections = interaction.Data.Values.ToArray();

            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            // Get match to retrieve container message ID
            var getMatch = await CoreService.Matches.GetByIdAsync(matchId, DatabaseComponent.Repository);
            if (!getMatch.Success || getMatch.Data is null)
            {
                return Result.Failure("Match not found");
            }
            var match = getMatch.Data;

            // Get user who triggered this interaction
            var player = await CoreService.WithDbContext(async db =>
                await db.Players.FirstOrDefaultAsync(p => p.MashinaUser.DiscordUserId == interaction.User.Id)
            );
            if (player is null)
            {
                return Result.Failure("Player not found");
            }

            // Create a new state snapshot with the provisional selections
            var currentSnapshot = MatchCore.Accessors.GetCurrentSnapshot(match);
            var newSnapshot = MatchCore.Factory.CreateMatchStateSnapshotFromOther(currentSnapshot);
            newSnapshot.TriggeredByUserId = player.MashinaUserId;
            newSnapshot.TriggeredByUserName = player.MashinaUser.DiscordUsername ?? "Unknown";

            // Determine which team is making selections and update the appropriate fields
            bool isChallengerTeam = teamId == match.Team1Id;
            if (isChallengerTeam)
            {
                newSnapshot.Team1MapBans = [.. selections];
                newSnapshot.Team1BansSubmitted = true;
                newSnapshot.Team1BansConfirmed = false; // Reset confirmation when resubmitting
            }
            else
            {
                newSnapshot.Team2MapBans = [.. selections];
                newSnapshot.Team2BansSubmitted = true;
                newSnapshot.Team2BansConfirmed = false; // Reset confirmation when resubmitting
            }

            // Persist the snapshot
            match.StateHistory.Add(newSnapshot);
            var updateMatch = await CoreService.Matches.UpdateAsync(match, DatabaseComponent.Repository);
            if (!updateMatch.Success)
            {
                return Result.Failure($"Failed to persist map ban selections: {updateMatch.ErrorMessage}");
            }
            var containerMsgId = isChallengerTeam
                ? match.Team1OverviewContainerMsgId
                : match.Team2OverviewContainerMsgId;
            var threadId = isChallengerTeam ? match.Team1ThreadId : match.Team2ThreadId;

            if (containerMsgId is null || threadId is null)
            {
                return Result.Failure("Container or thread not found");
            }

            // Get the thread and existing message
            var thread = await DiscBotService.Client.GetChannelAsync(threadId.Value);
            var containerMessage = await thread.GetMessageAsync(containerMsgId.Value);

            // Update the container with the new selections (reusing existing components)
            var updateResult = await Renderers.MatchRenderer.UpdateMatchContainerWithBansAsync(
                containerMessage,
                match,
                teamId,
                selections
            );
            if (!updateResult.Success || updateResult.Data is null)
            {
                return Result.Failure($"Failed to update container: {updateResult.ErrorMessage}");
            }

            // Send the updated container
            await containerMessage.ModifyAsync(
                new DiscordMessageBuilder().EnableV2Components().AddContainerComponent(updateResult.Data)
            );

            return Result.CreateSuccess("Map ban selection recorded and container updated");
        }

        private static async Task<Result> ConfirmMapBanAsync(DiscordInteraction interaction, string customId)
        {
            // Parse match ID and team ID from custom ID: "confirm_mapban_{matchId}_{teamId}"
            var parts = customId.Replace("confirm_mapban_", "", StringComparison.Ordinal).Split('_');
            if (
                parts.Length != 2
                || !Guid.TryParse(parts[0], out var matchId)
                || !Guid.TryParse(parts[1], out var teamId)
            )
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Invalid match or team ID.").AsEphemeral()
                );
                return Result.Failure("Invalid match or team ID");
            }

            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            // Get match to retrieve current snapshot
            var getMatch = await CoreService.Matches.GetByIdAsync(matchId, DatabaseComponent.Repository);
            if (!getMatch.Success || getMatch.Data is null)
            {
                return Result.Failure("Match not found");
            }
            var match = getMatch.Data;

            // Get user who triggered this interaction
            var player = await CoreService.WithDbContext(async db =>
                await db.Players.FirstOrDefaultAsync(p => p.MashinaUser.DiscordUserId == interaction.User.Id)
            );
            if (player is null)
            {
                return Result.Failure("Player not found");
            }

            // Get current snapshot to retrieve submitted bans
            var currentSnapshot = MatchCore.Accessors.GetCurrentSnapshot(match);
            bool isChallengerTeam = teamId == match.Team1Id;

            // Verify that bans were submitted before confirming
            bool bansSubmitted = isChallengerTeam
                ? currentSnapshot.Team1BansSubmitted
                : currentSnapshot.Team2BansSubmitted;
            if (!bansSubmitted)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("No selections found. Please make your selections first.")
                        .AsEphemeral()
                );
                return Result.Failure("No selections found");
            }

            var selections = isChallengerTeam
                ? currentSnapshot.Team1MapBans.ToArray()
                : [.. currentSnapshot.Team2MapBans];

            // Create a new state snapshot with confirmed flag
            var newSnapshot = MatchCore.Factory.CreateMatchStateSnapshotFromOther(currentSnapshot);
            newSnapshot.TriggeredByUserId = player.MashinaUserId;
            newSnapshot.TriggeredByUserName = player.MashinaUser.DiscordUsername ?? "Unknown";

            if (isChallengerTeam)
            {
                newSnapshot.Team1BansConfirmed = true;
                match.Team1MapBansConfirmedAt = DateTime.UtcNow;
            }
            else
            {
                newSnapshot.Team2BansConfirmed = true;
                match.Team2MapBansConfirmedAt = DateTime.UtcNow;
            }

            // Note: FinalMapPool and AvailableMaps will be computed by Core event handler
            // when both teams confirm (HandleScrimmageMapBansConfirmedAsync)

            // Persist the snapshot
            match.StateHistory.Add(newSnapshot);
            var updateMatch = await CoreService.Matches.UpdateAsync(match, DatabaseComponent.Repository);
            if (!updateMatch.Success)
            {
                return Result.Failure($"Failed to persist map ban confirmation: {updateMatch.ErrorMessage}");
            }

            // Update the container to show confirmed state (remove confirm/revise buttons, restore refresh button)
            var containerMsgId = isChallengerTeam
                ? match.Team1OverviewContainerMsgId
                : match.Team2OverviewContainerMsgId;
            var threadId = isChallengerTeam ? match.Team1ThreadId : match.Team2ThreadId;

            if (containerMsgId is not null && threadId is not null)
            {
                try
                {
                    var thread = await DiscBotService.Client.GetChannelAsync(threadId.Value);
                    var containerMessage = await thread.GetMessageAsync(containerMsgId.Value);

                    var updateResult = await Renderers.MatchRenderer.UpdateMatchContainerConfirmedAsync(
                        containerMessage,
                        match,
                        teamId,
                        selections
                    );

                    if (updateResult.Success && updateResult.Data is not null)
                    {
                        await containerMessage.ModifyAsync(
                            new DiscordMessageBuilder().EnableV2Components().AddContainerComponent(updateResult.Data)
                        );
                    }
                }
                catch (Exception ex)
                {
                    await DiscBotService.ErrorHandler.CaptureAsync(
                        ex,
                        $"Failed to update container after ban confirmation for match {matchId}, team {teamId}",
                        nameof(ConfirmMapBanAsync)
                    );
                    // Don't fail the whole operation if container update fails
                }
            }

            if (match.ParentId is null)
            {
                return Result.Failure("Parent scrimmage Id not found");
            }

            if (newSnapshot.Team1BansConfirmed && newSnapshot.Team2BansConfirmed)
            {
                await PublishScrimmageMapBansConfirmedAsync(match.ParentId.Value, matchId);
                return Result.CreateSuccess("Both team's map bans have been confirmed");
            }
            return Result.CreateSuccess("Map ban confirmed");
        }

        private static async Task<Result> ReviseMapBanAsync(DiscordInteraction interaction, string customId)
        {
            // Parse match ID and team ID from custom ID: "revise_mapban_{matchId}_{teamId}"
            var parts = customId.Replace("revise_mapban_", "", StringComparison.Ordinal).Split('_');
            if (
                parts.Length != 2
                || !Guid.TryParse(parts[0], out var matchId)
                || !Guid.TryParse(parts[1], out var teamId)
            )
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Invalid match or team ID.").AsEphemeral()
                );
                return Result.Failure("Invalid match or team ID");
            }

            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            // Get match to retrieve container message ID
            var getMatch = await CoreService.Matches.GetByIdAsync(matchId, DatabaseComponent.Repository);
            if (!getMatch.Success || getMatch.Data is null)
            {
                return Result.Failure("Match not found");
            }
            var match = getMatch.Data;

            // Get user who triggered this interaction
            var reviser = await CoreService.WithDbContext(async db =>
                await db.Players.FirstOrDefaultAsync(p => p.MashinaUser.DiscordUserId == interaction.User.Id)
            );
            if (reviser is null)
            {
                return Result.Failure("Player not found");
            }

            // Create a new state snapshot clearing the submitted flag and bans
            var currentSnapshot = MatchCore.Accessors.GetCurrentSnapshot(match);
            var newSnapshot = MatchCore.Factory.CreateMatchStateSnapshotFromOther(currentSnapshot);
            newSnapshot.TriggeredByUserId = reviser.MashinaUserId;
            newSnapshot.TriggeredByUserName = reviser.MashinaUser.DiscordUsername ?? "Unknown";

            bool isChallengerTeam = teamId == match.Team1Id;
            if (isChallengerTeam)
            {
                newSnapshot.Team1MapBans = [];
                newSnapshot.Team1BansSubmitted = false;
                newSnapshot.Team1BansConfirmed = false;
            }
            else
            {
                newSnapshot.Team2MapBans = [];
                newSnapshot.Team2BansSubmitted = false;
                newSnapshot.Team2BansConfirmed = false;
            }

            // Persist the snapshot
            match.StateHistory.Add(newSnapshot);
            var updateMatch = await CoreService.Matches.UpdateAsync(match, DatabaseComponent.Repository);
            if (!updateMatch.Success)
            {
                return Result.Failure($"Failed to persist map ban revision: {updateMatch.ErrorMessage}");
            }

            // Determine which container to update based on team ID
            var containerMsgId = isChallengerTeam
                ? match.Team1OverviewContainerMsgId
                : match.Team2OverviewContainerMsgId;
            var threadId = isChallengerTeam ? match.Team1ThreadId : match.Team2ThreadId;

            if (containerMsgId is null || threadId is null)
            {
                return Result.Failure("Container or thread not found");
            }

            if (match.Team1Players is null)
            {
                return Result.Failure("Team 1 players not found");
            }
            if (match.Team2Players is null)
            {
                return Result.Failure("Team 2 players not found");
            }

            // Restore the original select menu interface
            // We'll need to get the player data to recreate the container
            var playerIds = isChallengerTeam
                ? match.Team1Players.Select(p => p.Id).ToList()
                : match.Team2Players.Select(p => p.Id).ToList();
            var players = new List<Player>();
            foreach (var playerId in playerIds)
            {
                var getPlayer = await CoreService.Players.GetByIdAsync(playerId, DatabaseComponent.Repository);
                if (getPlayer.Success && getPlayer.Data is not null)
                {
                    players.Add(getPlayer.Data);
                }
            }

            // Re-render the original container with the select menu
            var renderResult = await Renderers.MatchRenderer.RenderScrimmageMatchContainersAsync(
                matchId,
                isChallengerTeam ? [.. players] : [],
                isChallengerTeam ? [] : [.. players]
            );

            if (!renderResult.Success || renderResult.Data is null)
            {
                return Result.Failure($"Failed to restore container: {renderResult.ErrorMessage}");
            }

            var restoredContainer = isChallengerTeam
                ? renderResult.Data.ChallengerContainer
                : renderResult.Data.OpponentContainer;

            // Get the thread and update the message
            var thread = await DiscBotService.Client.GetChannelAsync(threadId.Value);
            var containerMessage = await thread.GetMessageAsync(containerMsgId.Value);
            await containerMessage.ModifyAsync(
                new DiscordMessageBuilder().EnableV2Components().AddContainerComponent(restoredContainer)
            );

            return Result.CreateSuccess("Ban selection reset");
        }
        #endregion

        #region Rematch
        private static async Task<Result> ProcessRequestRematchAsync(DiscordInteraction interaction, string customId)
        {
            // Parse match ID from custom ID: "rematch_request_{matchId}"
            var matchIdStr = customId.Replace("rematch_request_", "", StringComparison.Ordinal);
            if (!Guid.TryParse(matchIdStr, out var matchId))
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Invalid match ID.").AsEphemeral()
                );
                return Result.Failure("Invalid match ID");
            }

            // Get the match
            var getMatch = await CoreService.Matches.GetByIdAsync(matchId, DatabaseComponent.Repository);
            if (!getMatch.Success || getMatch.Data is null)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Match not found.").AsEphemeral()
                );
                return Result.Failure("Match not found");
            }

            var match = getMatch.Data;

            // Get the requesting player
            var getRequestingPlayer = await CoreService.WithDbContext(async db =>
                await db.Players.FirstOrDefaultAsync(p => p.MashinaUser.DiscordUserId == interaction.User.Id)
            );

            if (getRequestingPlayer is null)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Player not found.").AsEphemeral()
                );
                return Result.Failure("Player not found");
            }

            if (match.Team1Players is null)
            {
                return Result.Failure("Team 1 players not found");
            }
            if (match.Team2Players is null)
            {
                return Result.Failure("Team 2 players not found");
            }

            // Determine which team requested
            bool team1Requested = match.Team1Players.Any(p => p.Id == getRequestingPlayer.Id);
            bool team2Requested = match.Team2Players.Any(p => p.Id == getRequestingPlayer.Id);

            if (!team1Requested && !team2Requested)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You are not part of this match.").AsEphemeral()
                );
                return Result.Failure("Player not part of match");
            }

            // Acknowledge interaction
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            // Update match complete containers to show rematch was requested
            var renderResult = await Renderers.MatchRenderer.RenderMatchCompleteContainerWithRematchRequestAsync(
                matchId,
                team1Requested ? match.Team1Id : match.Team2Id
            );

            if (!renderResult.Success)
            {
                return Result.Failure($"Failed to update rematch request: {renderResult.ErrorMessage}");
            }

            return Result.CreateSuccess("Rematch requested");
        }

        private static async Task<Result> ProcessAcceptRematchAsync(DiscordInteraction interaction, string customId)
        {
            // Parse match ID from custom ID: "accept_rematch_{matchId}"
            var matchIdStr = customId.Replace("accept_rematch_", "", StringComparison.Ordinal);
            if (!Guid.TryParse(matchIdStr, out var matchId))
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Invalid match ID.").AsEphemeral()
                );
                return Result.Failure("Invalid match ID");
            }

            // Get the match
            var getMatch = await CoreService.Matches.GetByIdAsync(matchId, DatabaseComponent.Repository);
            if (!getMatch.Success || getMatch.Data is null)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Match not found.").AsEphemeral()
                );
                return Result.Failure("Match not found");
            }

            var match = getMatch.Data;

            // Get the accepting player
            var getAcceptingPlayer = await CoreService.WithDbContext(async db =>
                await db.Players.FirstOrDefaultAsync(p => p.MashinaUser.DiscordUserId == interaction.User.Id)
            );

            if (getAcceptingPlayer is null)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Player not found.").AsEphemeral()
                );
                return Result.Failure("Player not found");
            }

            // Acknowledge interaction
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            // Get team and player data from the match
            var getTeam1 = await CoreService.Teams.GetByIdAsync(match.Team1Id, DatabaseComponent.Repository);
            var getTeam2 = await CoreService.Teams.GetByIdAsync(match.Team2Id, DatabaseComponent.Repository);

            if (!getTeam1.Success || getTeam1.Data is null || !getTeam2.Success || getTeam2.Data is null)
            {
                return Result.Failure("Failed to get team data");
            }

            var team1 = getTeam1.Data;
            var team2 = getTeam2.Data;

            if (match.Team1Players is null)
            {
                return Result.Failure("Team 1 players not found");
            }
            if (match.Team2Players is null)
            {
                return Result.Failure("Team 2 players not found");
            }

            // Get all players
            var team1Players = new List<Player>();
            foreach (var player in match.Team1Players)
            {
                team1Players.Add(player);
            }

            var team2Players = new List<Player>();
            foreach (var player in match.Team2Players)
            {
                team2Players.Add(player);
            }

            // The team that requested the rematch is the challenger
            bool acceptingPlayerIsTeam1 = match.Team1Players.Any(p => p.Id == getAcceptingPlayer.Id);
            bool acceptingPlayerIsTeam2 = match.Team2Players.Any(p => p.Id == getAcceptingPlayer.Id);

            if (!acceptingPlayerIsTeam1 && !acceptingPlayerIsTeam2)
            {
                return Result.Failure("Accepting player is not part of this match");
            }

            bool team1Requested = acceptingPlayerIsTeam2;
            bool team2Requested = acceptingPlayerIsTeam1;

            var challengerTeam = team1Requested ? team1 : team2;
            var opponentTeam = team1Requested ? team2 : team1;
            var challengerPlayers = team1Requested ? team1Players : team2Players;
            var opponentPlayers = team1Requested ? team2Players : team1Players;
            var issuedByPlayer = challengerPlayers.First();
            var acceptedByPlayer = getAcceptingPlayer;

            // Create a new challenge similar to ScrimmageCore.Factory.CreateChallenge
            // Note: CreateChallengeAsync expects teammate IDs (not including the issuer)
            var teammateIds = challengerPlayers.Where(p => p.Id != issuedByPlayer.Id).Select(p => p.Id).ToList();
            var challengeResult = await Core.Scrimmages.ScrimmageCore.CreateChallengeAsync(
                ChallengerTeamId: challengerTeam.Id,
                OpponentTeamId: opponentTeam.Id,
                IssuedByPlayerId: issuedByPlayer.Id,
                SelectedTeammateIds: teammateIds,
                teamSize: match.TeamSize,
                bestOf: match.BestOf
            );

            if (!challengeResult.Success || challengeResult.Data is null)
            {
                return Result.Failure($"Failed to create rematch challenge: {challengeResult.ErrorMessage}");
            }

            var challenge = challengeResult.Data;

            // Save the challenge
            var saveResult = await CoreService.ScrimmageChallenges.CreateAsync(challenge, DatabaseComponent.Repository);

            if (!saveResult.Success)
            {
                return Result.Failure($"Failed to save rematch challenge: {saveResult.ErrorMessage}");
            }

            // Create the scrimmage from the challenge (equivalent to accepting the challenge)
            challenge.ChallengeStatus = ScrimmageChallengeStatus.Accepted;
            await CoreService.ScrimmageChallenges.UpdateAsync(challenge, DatabaseComponent.Repository);

            // Create scrimmage using ProcessChallengeAcceptedAsync logic
            var scrimmageResult = await Core.Scrimmages.ScrimmageCore.CreateScrimmageAsync(
                challenge.Id,
                challengerTeam.Id,
                opponentTeam.Id,
                [.. challengerPlayers],
                [.. opponentPlayers],
                issuedByPlayer.Id,
                acceptedByPlayer.Id,
                match.TeamSize,
                match.BestOf,
                challengerTeam.ScrimmageTeamStats[match.TeamSize],
                opponentTeam.ScrimmageTeamStats[match.TeamSize]
            );

            if (!scrimmageResult.Success || scrimmageResult.Data is null)
            {
                return Result.Failure($"Failed to create scrimmage: {scrimmageResult.ErrorMessage}");
            }

            var scrimmage = scrimmageResult.Data;

            // Create the match for the scrimmage
            var newMatchResult = await Core.Common.Models.Common.MatchCore.CreateScrimmageMatchAsync(scrimmage.Id);

            if (!newMatchResult.Success || newMatchResult.Data is null)
            {
                return Result.Failure($"Failed to create match: {newMatchResult.ErrorMessage}");
            }

            var newMatch = newMatchResult.Data;

            // Create threads and start the match (similar to after challenge acceptance)
            var threadsResult = await ScrimmageApp.CreateScrimmageThreadsAsync(scrimmage.Id, newMatch.Id);

            if (!threadsResult.Success)
            {
                return Result.Failure($"Failed to create match threads: {threadsResult.ErrorMessage}");
            }

            // Update the original match complete containers to show rematch was accepted
            var updateResult = await Renderers.MatchRenderer.RenderMatchCompleteContainerWithRematchAcceptedAsync(
                matchId
            );

            return Result.CreateSuccess("Rematch accepted and new match started");
        }

        private static async Task<Result> ProcessDeclineRematchAsync(DiscordInteraction interaction, string customId)
        {
            // Parse match ID from custom ID: "decline_rematch_{matchId}"
            var matchIdStr = customId.Replace("decline_rematch_", "", StringComparison.Ordinal);
            if (!Guid.TryParse(matchIdStr, out var matchId))
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Invalid match ID.").AsEphemeral()
                );
                return Result.Failure("Invalid match ID");
            }

            // Acknowledge interaction
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            // Update match complete containers to show rematch was declined
            var renderResult = await Renderers.MatchRenderer.RenderMatchCompleteContainerWithRematchDeclinedAsync(
                matchId
            );

            if (!renderResult.Success)
            {
                return Result.Failure($"Failed to update rematch declined: {renderResult.ErrorMessage}");
            }

            return Result.CreateSuccess("Rematch declined");
        }
        #endregion

        #region Forfeit Match
        private static async Task<Result> ProcessForfeitMatchRequestAsync(
            DiscordInteraction interaction,
            string customId
        )
        {
            // Parse match ID and team ID from custom ID: "forfeit_match_{matchId}_{teamId}"
            var parts = customId.Replace("forfeit_match_", "", StringComparison.Ordinal).Split('_');
            if (
                parts.Length != 2
                || !Guid.TryParse(parts[0], out var matchId)
                || !Guid.TryParse(parts[1], out var teamId)
            )
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Invalid match or team ID.").AsEphemeral()
                );
                return Result.Failure("Invalid match or team ID");
            }

            // Get match to verify it exists and is in progress
            var getMatch = await CoreService.Matches.GetByIdAsync(matchId, DatabaseComponent.Repository);
            if (!getMatch.Success || getMatch.Data is null)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Match not found.").AsEphemeral()
                );
                return Result.Failure("Match not found");
            }

            var match = getMatch.Data;
            var currentStatus = MatchCore.Accessors.GetCurrentStatus(match);

            if (currentStatus != MatchStatus.InProgress)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("You can only forfeit matches that are in progress.")
                        .AsEphemeral()
                );
                return Result.Failure("Match is not in progress");
            }

            // Verify user is on the team
            var player = await CoreService.WithDbContext(async db =>
                await db.Players.FirstOrDefaultAsync(p => p.MashinaUser.DiscordUserId == interaction.User.Id)
            );

            if (player is null)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("Player not found. You must be registered to forfeit.")
                        .AsEphemeral()
                );
                return Result.Failure("Player not found");
            }

            if (match.Team1Players is null)
            {
                return Result.Failure("Team 1 players not found");
            }
            if (match.Team2Players is null)
            {
                return Result.Failure("Team 2 players not found");
            }

            bool isOnTeam =
                (teamId == match.Team1Id && match.Team1Players.Any(p => p.Id == player.Id))
                || (teamId == match.Team2Id && match.Team2Players.Any(p => p.Id == player.Id));

            if (!isOnTeam)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("You are not on this team and cannot forfeit for them.")
                        .AsEphemeral()
                );
                return Result.Failure("User not on team");
            }

            // Show confirmation dialog with confirm/cancel buttons
            var confirmButton = new DiscordButtonComponent(
                DiscordButtonStyle.Danger,
                $"confirm_forfeit_match_{matchId}_{teamId}",
                "Confirm Forfeit"
            );
            var cancelButton = new DiscordButtonComponent(
                DiscordButtonStyle.Secondary,
                $"cancel_forfeit_match_{matchId}_{teamId}",
                "Cancel"
            );

            var actionRow = new DiscordActionRowComponent([confirmButton, cancelButton]);
            var components = new List<DiscordComponent> { actionRow };
            var container = new DiscordContainerComponent(components);

            await interaction.CreateResponseAsync(
                DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent(
                        "⚠️ **Are you sure you want to forfeit this match?**\n\n"
                            + "This action cannot be undone and will result in a loss for your team."
                    )
                    .AsEphemeral()
            );

            // Send the buttons as a follow-up message with container
            await interaction.CreateFollowupMessageAsync(
                new DiscordFollowupMessageBuilder().AddContainerComponent(container).AsEphemeral()
            );

            return Result.CreateSuccess("Forfeit confirmation shown");
        }

        private static async Task<Result> ProcessConfirmForfeitMatchAsync(
            DiscordInteraction interaction,
            string customId
        )
        {
            // Parse match ID and team ID from custom ID: "confirm_forfeit_match_{matchId}_{teamId}"
            var parts = customId.Replace("confirm_forfeit_match_", "", StringComparison.Ordinal).Split('_');
            if (
                parts.Length != 2
                || !Guid.TryParse(parts[0], out var matchId)
                || !Guid.TryParse(parts[1], out var forfeitingTeamId)
            )
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Invalid match or team ID.").AsEphemeral()
                );
                return Result.Failure("Invalid match or team ID");
            }

            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            // Get match
            var getMatch = await CoreService.Matches.GetByIdAsync(matchId, DatabaseComponent.Repository);
            if (!getMatch.Success || getMatch.Data is null)
            {
                return Result.Failure("Match not found");
            }

            var match = getMatch.Data;

            // Determine winner (the team that didn't forfeit)
            var winnerTeamId = forfeitingTeamId == match.Team1Id ? match.Team2Id : match.Team1Id;

            // TODO: Call Core logic to forfeit the match
            // This should:
            // 1. Update match state to Forfeited
            // 2. Set winner
            // 3. Update any active games
            // 4. Publish MatchForfeited event
            // For now, we'll just show a placeholder message

            try
            {
                await interaction.EditOriginalResponseAsync(
                    new DiscordWebhookBuilder().WithContent(
                        "✅ **Match forfeited.**\n\n"
                            + "The match has been forfeited and the opposing team has been awarded the win."
                    )
                );
            }
            catch
            {
                // Ignore if we can't edit the response
            }

            return Result.CreateSuccess("Match forfeited");
        }

        private static async Task<Result> ProcessCancelForfeitMatchAsync(
            DiscordInteraction interaction,
            string customId
        )
        {
            // No need to parse IDs, just acknowledge the cancellation
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            try
            {
                await interaction.EditOriginalResponseAsync(
                    new DiscordWebhookBuilder().WithContent("Forfeit cancelled.")
                );
            }
            catch
            {
                // Ignore if we can't edit the response
            }

            return Result.CreateSuccess("Forfeit cancelled");
        }
        #endregion
    }
}
