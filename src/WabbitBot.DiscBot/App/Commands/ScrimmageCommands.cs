using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Events.Core;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Models.Scrimmage;
using WabbitBot.Core.Common.Services;
using WabbitBot.DiscBot.App.Providers;
using WabbitBot.DiscBot.App.Services.DiscBot;
using WabbitBot.DiscBot.App.Utilities;

namespace WabbitBot.DiscBot.App.Commands
{
    /// <summary>
    /// Discord slash commands for scrimmage management.
    /// Translates Discord interactions into events via DiscBotService.PublishAsync.
    /// Uses DSharpPlus.Commands only (not CommandsNext or SlashCommands).
    /// </summary>
    [Command("scrimmage")]
    [Description("Scrimmage management commands")]
    public partial class ScrimmageCommands
    {
        #region User
        /// <summary>
        /// Challenge another team to a scrimmage.
        /// Opens an interactive configuration menu.
        /// </summary>
        [Command("challenge")]
        [Description("Challenge another team to a scrimmage")]
        public async Task ChallengeAsync(
            CommandContext ctx,
            [Description("Game Size")] TeamSize TeamSize,
            [Description("Your Team")]
            [SlashAutoCompleteProvider(typeof(UserTeamAutoComplete))]
                string ChallengerTeamName
        )
        {
            await ctx.DeferResponseAsync();

            var registrationResult = await UserRegistrationHelper.EnsureRegisteredAsync(ctx.User);
            if (!registrationResult.Success)
            {
                await ctx.EditResponseAsync(registrationResult.ErrorMessage ?? "Failed to verify registration.");
                return;
            }

            try
            {
                // Check if user already has an active challenge
                var existingChallenge = await CoreService.WithDbContext(async db =>
                {
                    // First, get the player ID from the Discord user ID
                    var player = await db
                        .Players.Include(p => p.MashinaUser)
                        .FirstOrDefaultAsync(p => p.MashinaUser.DiscordUserId == ctx.User.Id);

                    if (player is null)
                        return null;

                    // Check for pending challenges issued by this player
                    Console.WriteLine($"🔍 DEBUG: Checking for existing challenges for player ID: {player.Id}");
                    var challenges = await db
                        .ScrimmageChallenges.Where(c =>
                            c.IssuedByPlayerId == player.Id && c.ChallengeStatus == ScrimmageChallengeStatus.Pending
                        )
                        .ToListAsync();
                    Console.WriteLine($"🔍 DEBUG: Found {challenges.Count} pending challenges for player {player.Id}");
                    foreach (var challenge in challenges)
                    {
                        Console.WriteLine($"   Challenge ID: {challenge.Id}, Status: {challenge.ChallengeStatus}");
                    }
                    return challenges.FirstOrDefault();
                });

                if (existingChallenge is not null)
                {
                    await ctx.EditResponseAsync(
                        "You already have an active challenge. Please cancel it before creating a new one."
                    );
                    return;
                }

                // Validate challenger team exists and load with rosters
                var challengerTeam = await CoreService.WithDbContext(async db =>
                {
                    return await db
                        .Teams.Include(t => t.Rosters)
                        .ThenInclude(r => r.RosterMembers)
                        .ThenInclude(m => m.MashinaUser)
                        .FirstOrDefaultAsync(t => t.Name == ChallengerTeamName);
                });

                if (challengerTeam is null)
                {
                    await ctx.EditResponseAsync($"**{ChallengerTeamName}** is not a valid team.");
                    return;
                }

                // Verify user is on the team
                var userDiscordId = ctx.User.Id;
                var rosterGroup = TeamCore.TeamSizeRosterGrouping.GetRosterGroup(TeamSize);
                var roster = challengerTeam.Rosters.FirstOrDefault(r => r.RosterGroup == rosterGroup);

                if (roster is null)
                {
                    await ctx.EditResponseAsync(
                        $"**{ChallengerTeamName}** doesn't have a roster for {TeamSize.ToSizeString()}."
                    );
                    return;
                }

                if (!roster.RosterMembers.Any(m => m.MashinaUser?.DiscordUserId == userDiscordId))
                {
                    await ctx.EditResponseAsync($"You are not a member of **{ChallengerTeamName}**.");
                    return;
                }

                // Create the interactive challenge configuration container
                var containerResult = await Renderers.ScrimmageRenderer.RenderChallengeConfigurationAsync(
                    ctx.User.Id,
                    TeamSize,
                    challengerTeam.Id,
                    roster.RosterGroup
                );

                if (!containerResult.Success || containerResult.Data is null)
                {
                    await ctx.EditResponseAsync(
                        $"Failed to create challenge configuration: {containerResult.ErrorMessage}"
                    );
                    return;
                }

                // Delete the deferred response; we'll post the container in a private thread under MashinaChannel
                await ctx.DeleteResponseAsync();

                var threadName = $"Challenge Setup - {ctx.User.GlobalName}";
                var (thread, message) = await DiscBotService.ThreadContainers.CreateThreadAndSendAsync(
                    threadName,
                    containerResult.Data,
                    ctx.Guild,
                    ctx.User.Id,
                    DiscBotService.ScrimmageChannel,
                    enableInactivityCleanup: true
                );

                // Ephemeral followup with a link to the thread
                await ctx.FollowupAsync(
                    new DiscordFollowupMessageBuilder()
                        .WithContent($"Your challenge configuration is ready in {thread.Mention}")
                        .AsEphemeral()
                );
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to process scrimmage challenge command",
                    nameof(ChallengeAsync)
                );
                await ctx.EditResponseAsync("An error occurred while creating your challenge. Please try again.");
            }
        }

        /// <summary>
        /// Cancel a pending scrimmage challenge.
        /// Only the player who issued the challenge or the team captain can cancel it.
        /// Can only cancel challenges that have not been accepted yet.
        /// </summary>
        [Command("cancel-challenge")]
        [Description("Cancel a pending scrimmage challenge")]
        public async Task CancelAsync(
            CommandContext ctx,
            [Description("Challenge to cancel")]
            [SlashAutoCompleteProvider(typeof(CancellableChallengeAutoComplete))]
                string challengeId
        )
        {
            await ctx.DeferResponseAsync();

            try
            {
                // Parse challenge ID
                if (!Guid.TryParse(challengeId, out var id))
                {
                    await ctx.EditResponseAsync("Invalid challenge ID.");
                    return;
                }

                // Check if user is a Discord moderator
                var isModerator = false;
                if (ctx.User is DiscordMember member)
                {
                    isModerator = member.Permissions.HasFlag(DiscordPermission.ModerateMembers);
                }

                // Call the shared cancellation logic
                Console.WriteLine($"🔍 DEBUG: Attempting to cancel challenge ID: {id} for user: {ctx.User.Id}");
                var cancelResult = await ScrimmageApp.CancelChallengeAsync(id, ctx.User.Id, isModerator: isModerator);
                Console.WriteLine(
                    $"🔍 DEBUG: Cancel result - Success: {cancelResult.Success}, Error: {cancelResult.ErrorMessage}"
                );

                if (!cancelResult.Success || cancelResult.Data is null)
                {
                    await ctx.EditResponseAsync(cancelResult.ErrorMessage ?? "Failed to cancel challenge.");
                    return;
                }

                // Update the challenge container using the shared method
                var updateResult = await ScrimmageApp.UpdateChallengeContainerToCancelledAsync(
                    cancelResult.Data.ChallengerTeamName,
                    cancelResult.Data.OpponentTeamName,
                    ctx.User,
                    cancelResult.Data.ChallengeMessageId,
                    cancelResult.Data.ChallengeChannelId
                );

                if (!updateResult.Success)
                {
                    // Log but don't fail - the challenge was already deleted
                    await DiscBotService.ErrorHandler.CaptureAsync(
                        new Exception(updateResult.ErrorMessage),
                        "Failed to update challenge container after cancellation via command",
                        nameof(CancelAsync)
                    );
                }

                await ctx.EditResponseAsync(
                    $"Challenge **{cancelResult.Data.ChallengerTeamName}** vs. **{cancelResult.Data.OpponentTeamName}** "
                        + "has been cancelled successfully."
                );
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to process scrimmage cancel command",
                    nameof(CancelAsync)
                );
                await ctx.EditResponseAsync("An error occurred while processing your cancellation. Please try again.");
            }
        }

        /// <summary>
        /// Substitute a player in an active scrimmage.
        /// Replaces a player from your team with another roster member.
        /// If the substituted player has already submitted a deck code, it will be reset.
        /// </summary>
        [Command("substitute")]
        [Description("Substitute a player in an active scrimmage")]
        public async Task SubstituteAsync(
            CommandContext ctx,
            [Description("Active scrimmage")]
            [SlashAutoCompleteProvider(typeof(UserActiveScrimmageAutoComplete))]
                string scrimmageId,
            [Description("Player to substitute out")]
            [SlashAutoCompleteProvider(typeof(ScrimmagePlayerAutoComplete))]
                string playerToSubOut,
            [Description("Player to substitute in")]
            [SlashAutoCompleteProvider(typeof(SubstitutePlayerAutoComplete))]
                string playerToSubIn
        )
        {
            await ctx.DeferResponseAsync();

            try
            {
                // Parse IDs
                if (!Guid.TryParse(scrimmageId, out var scrimmageGuid))
                {
                    await ctx.EditResponseAsync("Invalid scrimmage ID.");
                    return;
                }

                if (!Guid.TryParse(playerToSubOut, out var playerOutGuid))
                {
                    await ctx.EditResponseAsync("Invalid player to substitute out.");
                    return;
                }

                if (!Guid.TryParse(playerToSubIn, out var playerInGuid))
                {
                    await ctx.EditResponseAsync("Invalid player to substitute in.");
                    return;
                }

                // Call the substitution logic
                var substituteResult = await ScrimmageApp.SubstitutePlayerAsync(
                    scrimmageGuid,
                    playerOutGuid,
                    playerInGuid,
                    ctx.User.Id
                );

                if (!substituteResult.Success)
                {
                    await ctx.EditResponseAsync(substituteResult.ErrorMessage ?? "Failed to substitute player.");
                    return;
                }

                await ctx.EditResponseAsync(substituteResult.Data ?? "Player substituted successfully.");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to process scrimmage substitute command",
                    nameof(SubstituteAsync)
                );
                await ctx.EditResponseAsync("An error occurred while processing your substitution. Please try again.");
            }
        }
        #endregion

        #region Admin
        #endregion
    }
}
