using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Events.Core;
using WabbitBot.Core.Common.Services;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.DiscBot.App.Services.DiscBot;
using WabbitBot.DiscBot.App.Providers;
using WabbitBot.Common.Data.Interfaces;
using DSharpPlus.Commands.Processors.SlashCommands;
using WabbitBot.Common.Configuration;

namespace WabbitBot.DiscBot.App.Commands
{
    /// <summary>
    /// Enum for best-of options in scrimmages
    /// </summary>
    public enum BestOfOption
    {
        [Description("Best of 1")]
        BestOf1 = 1,
        [Description("Best of 3")]
        BestOf3 = 3,
    }
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
        /// Publishes ChallengeRequested to Global event bus.
        /// </summary>
        [Command("challenge")]
        [Description("Challenge another team to a scrimmage")]
        public async Task ChallengeAsync(
            CommandContext ctx,
            [Description("Game Size")] TeamSize TeamSize,
            [Description("Your Team")] [SlashAutoCompleteProvider(typeof(UserTeamAutoComplete))]
                string ChallengerTeamName,
            [Description("Opponent Team")] [SlashAutoCompleteProvider(typeof(OpponentTeamAutoComplete))]
                string OpponentTeamName,
            [Description("Select your teammates")] [SlashAutoCompleteProvider(typeof(RosterPlayerAutoComplete))]
                string[] SelectedPlayerNames,
            [Description("Best of games")] BestOfOption BestOf = BestOfOption.BestOf1)
        {
            await ctx.DeferResponseAsync();

            try
            {
                // Check if Best of 3 is allowed in configuration
                var config = ConfigurationProvider.GetConfigurationService();
                var scrimmageOptions = config.GetSection<ScrimmageOptions>("Bot:Scrimmage");

                if (BestOf == BestOfOption.BestOf3 && scrimmageOptions.BestOf < 3)
                {
                    await ctx.EditResponseAsync("Best of 3 scrimmages are not currently enabled in the bot configuration.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(ChallengerTeamName) || string.IsNullOrWhiteSpace(OpponentTeamName))
                {
                    await ctx.EditResponseAsync("Both team names are required.");
                    return;
                }

                var challengerTeamResult = await CoreService.Teams
                    .GetByNameAsync(ChallengerTeamName, DatabaseComponent.Repository);
                var opponentTeamResult = await CoreService.Teams
                    .GetByNameAsync(OpponentTeamName, DatabaseComponent.Repository);

                if (!challengerTeamResult.Success || challengerTeamResult.Data == null)
                {
                    await ctx.EditResponseAsync($"**{ChallengerTeamName}** is not a valid team.");
                    return;
                }

                if (!opponentTeamResult.Success || opponentTeamResult.Data == null)
                {
                    await ctx.EditResponseAsync($"**{OpponentTeamName}** is not a valid team.");
                    return;
                }

                var pubResult = await PublishChallengeRequestedAsync(
                    (int)TeamSize,
                    ChallengerTeamName,
                    OpponentTeamName,
                    SelectedPlayerNames,
                    ctx.User.Id,
                    (int)BestOf);
                if (!pubResult.Success)
                {
                    await ctx.EditResponseAsync($"Failed to publish challenge requested. {pubResult.ErrorMessage}");
                    return;
                }

                await ctx.EditResponseAsync($"Challenge request from **{ChallengerTeamName}** to " +
                    $"**{OpponentTeamName}** has been submitted. Core will validate and create the challenge.");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to process scrimmage challenge command",
                    nameof(ChallengeAsync));
                await ctx.EditResponseAsync("An error occurred while processing your challenge. Please try again.");
            }
        }
        #endregion

        #region Admin
        /// <summary>
        /// Delete a pending scrimmage challenge.
        /// </summary>
        [Command("delete-challenge")]
        [Description("Delete a scrimmage challenge")]
        [RequirePermissions(DiscordPermission.ModerateMembers)]
        public async Task DeleteAsync(
            CommandContext ctx,
            [Description("Challenge to delete")]
            [SlashAutoCompleteProvider(typeof(ActiveChallengeAutoComplete))]
            string challengeId)
        {
            await ctx.DeferResponseAsync();

            try
            {
                if (!Guid.TryParse(challengeId, out var id))
                {
                    await ctx.EditResponseAsync("Invalid challenge ID.");
                    return;
                }

                var deleteResult = await CoreService.ScrimmageChallenges.DeleteAsync(id, DatabaseComponent.Repository);
                if (!deleteResult.Success)
                {
                    await ctx.EditResponseAsync($"Failed to delete challenge. {deleteResult.ErrorMessage}");
                    return;
                }

                await ctx.EditResponseAsync($"Challenge has been deleted successfully.");
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to process scrimmage delete command",
                    nameof(DeleteAsync));
                await ctx.EditResponseAsync("An error occurred while processing your delete. Please try again.");
            }
        }
        #endregion
    }
}
