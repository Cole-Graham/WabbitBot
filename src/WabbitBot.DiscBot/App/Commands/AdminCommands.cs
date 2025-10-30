using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using WabbitBot.DiscBot.App.Renderers;
using WabbitBot.DiscBot.App.Services.DiscBot;

namespace WabbitBot.DiscBot.App.Commands
{
    [Command("admin")]
    [Description("Administrative commands")]
    [RequireGuild]
    [RequirePermissions(DiscordPermission.ModerateMembers)]
    public sealed class AdminCommands
    {
        [Command("manage-teams")]
        [Description("Open the Manage Teams editor in a private thread")]
        public async Task ManageTeamsAsync(
            CommandContext ctx,
            [Description("Team to manage")]
            [SlashAutoCompleteProvider(typeof(Providers.AllTeamsAutoComplete))]
                string team
        )
        {
            await ctx.DeferResponseAsync();

            if (!Guid.TryParse(team, out var teamId))
            {
                await ctx.EditResponseAsync("Invalid team selection.");
                return;
            }

            // Render admin-mode team role editor (preselected team, no team select)
            var container = await TeamRenderer.RenderTeamEditorAsync(ctx.User.Id, teamId, adminMode: true);

            // Delete the deferred response; we'll post the container in a private thread under MashinaChannel
            await ctx.DeleteResponseAsync();

            var threadName = $"Manage Teams - {ctx.User.GlobalName}";
            var (thread, message) = await DiscBotService.ThreadContainers.CreateThreadAndSendAsync(
                threadName,
                container,
                ctx.Guild,
                ctx.User.Id,
                DiscBotService.MashinaChannel,
                enableInactivityCleanup: true
            );
        }
    }
}
