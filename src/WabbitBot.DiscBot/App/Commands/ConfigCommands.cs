using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using WabbitBot.Common.Configuration;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.DiscBot.App.Commands
{
    [Command("config")]
    [Description("Moderator-only configuration commands")]
    [RequireGuild]
    [RequirePermissions(DiscordPermission.ModerateMembers)]
    public sealed class ConfigCommands
    {
        private static List<ulong> GetRoleIds(DiscordMember member)
        {
            var roles = ConfigurationProvider.GetSection<RolesOptions>(RolesOptions.SectionName);
            return member
                .Roles.Where(r =>
                    roles.Moderator.HasValue && r.Id == roles.Moderator.Value
                    || roles.Admin.HasValue && r.Id == roles.Admin.Value
                    || roles.Whitelisted.HasValue && r.Id == roles.Whitelisted.Value
                )
                .Select(r => r.Id)
                .ToList();
        }

        [Command("teamrules-view")]
        [Description("View current TeamRules configuration")]
        public async Task ViewTeamRulesAsync(CommandContext ctx)
        {
            await ctx.DeferResponseAsync();

            var scrim = ConfigurationProvider.GetSection<ScrimmageOptions>(ScrimmageOptions.SectionName);
            var tr = scrim.TeamRules;

            string content =
                $"Base Captains: {tr.BaseRules.MatchCaptainsRequiredCount}\n"
                + $"Solo: cap={tr.Solo.CaptainRosterSlots}, core={tr.Solo.CoreRosterSlots}, max={tr.Solo.MaxRosterSlots}\n"
                + $"  1v1: cap?={tr.Solo.OneVOne?.MatchCaptainsRequiredCount?.ToString() ?? "(base)"}, coreReq={tr.Solo.OneVOne?.MatchCorePlayersRequiredCount}, coreEq={tr.Solo.OneVOne?.MatchCorePlayersEqualToCaptainCount}\n"
                + $"Duo: cap={tr.Duo.CaptainRosterSlots}, core={tr.Duo.CoreRosterSlots}, max={tr.Duo.MaxRosterSlots}\n"
                + $"  2v2: cap?={tr.Duo.TwoVTwo?.MatchCaptainsRequiredCount?.ToString() ?? "(base)"}, coreReq={tr.Duo.TwoVTwo?.MatchCorePlayersRequiredCount}, coreEq={tr.Duo.TwoVTwo?.MatchCorePlayersEqualToCaptainCount}\n"
                + $"Squad: cap={tr.Squad.CaptainRosterSlots}, core={tr.Squad.CoreRosterSlots}, max={tr.Squad.MaxRosterSlots}\n"
                + $"  3v3: cap?={tr.Squad.ThreeVThree?.MatchCaptainsRequiredCount?.ToString() ?? "(base)"}, coreReq={tr.Squad.ThreeVThree?.MatchCorePlayersRequiredCount}, coreEq={tr.Squad.ThreeVThree?.MatchCorePlayersEqualToCaptainCount}\n"
                + $"  4v4: cap?={tr.Squad.FourVFour?.MatchCaptainsRequiredCount?.ToString() ?? "(base)"}, coreReq={tr.Squad.FourVFour?.MatchCorePlayersRequiredCount}, coreEq={tr.Squad.FourVFour?.MatchCorePlayersEqualToCaptainCount}";

            await ctx.EditResponseAsync($"```${content}```");
        }

        [Command("teamrules-set-base-captains")]
        [Description("Set BaseRules.MatchCaptainsRequiredCount")]
        public async Task SetBaseCaptainsAsync(CommandContext ctx, [Description("Captains required")] int count)
        {
            await ctx.DeferResponseAsync();

            var config = await ConfigurationService.Persistence.LoadConfigurationAsync();
            if (config is null)
            {
                await ctx.EditResponseAsync("Failed to load configuration.");
                return;
            }

            config.Scrimmage.TeamRules.BaseRules.MatchCaptainsRequiredCount = count;
            var ok = await ConfigurationService.Persistence.SaveConfigurationAsync(config);
            if (!ok)
            {
                await ctx.EditResponseAsync("Failed to save configuration.");
                return;
            }

            await ctx.EditResponseAsync($"Base captains set to {count}.");
        }

        [Command("teamrules-set-roster")]
        [Description("Set roster caps for a group (Solo, Duo, Squad)")]
        public async Task SetRosterCapsAsync(
            CommandContext ctx,
            [Description("Group")] string group,
            int captainSlots,
            int coreSlots,
            int maxSlots
        )
        {
            await ctx.DeferResponseAsync();

            var config = await ConfigurationService.Persistence.LoadConfigurationAsync();
            if (config is null)
            {
                await ctx.EditResponseAsync("Failed to load configuration.");
                return;
            }

            var tr = config.Scrimmage.TeamRules;
            switch (group.ToLowerInvariant())
            {
                case "solo":
                    tr.Solo.CaptainRosterSlots = captainSlots;
                    tr.Solo.CoreRosterSlots = coreSlots;
                    tr.Solo.MaxRosterSlots = maxSlots;
                    break;
                case "duo":
                    tr.Duo.CaptainRosterSlots = captainSlots;
                    tr.Duo.CoreRosterSlots = coreSlots;
                    tr.Duo.MaxRosterSlots = maxSlots;
                    break;
                case "squad":
                    tr.Squad.CaptainRosterSlots = captainSlots;
                    tr.Squad.CoreRosterSlots = coreSlots;
                    tr.Squad.MaxRosterSlots = maxSlots;
                    break;
                default:
                    await ctx.EditResponseAsync("Invalid group. Use Solo, Duo, or Squad.");
                    return;
            }

            var ok = await ConfigurationService.Persistence.SaveConfigurationAsync(config);
            await ctx.EditResponseAsync(ok ? "Roster caps updated." : "Failed to save configuration.");
        }

        [Command("teamrules-set-match")]
        [Description("Set per-size match rules (captains override, core requirements)")]
        public async Task SetMatchRulesAsync(
            CommandContext ctx,
            [Description("Size (1v1,2v2,3v3,4v4)")] string size,
            int? captainsOverride,
            int coreRequired,
            int coreEqualToCaptain
        )
        {
            await ctx.DeferResponseAsync();

            var config = await ConfigurationService.Persistence.LoadConfigurationAsync();
            if (config is null)
            {
                await ctx.EditResponseAsync("Failed to load configuration.");
                return;
            }

            var tr = config.Scrimmage.TeamRules;
            TeamMatchRules? target = size switch
            {
                "1v1" => tr.Solo.OneVOne ??= new TeamMatchRules(),
                "2v2" => tr.Duo.TwoVTwo ??= new TeamMatchRules(),
                "3v3" => tr.Squad.ThreeVThree ??= new TeamMatchRules(),
                "4v4" => tr.Squad.FourVFour ??= new TeamMatchRules(),
                _ => null,
            };

            if (target is null)
            {
                await ctx.EditResponseAsync("Invalid size. Use 1v1, 2v2, 3v3, or 4v4.");
                return;
            }

            target.MatchCaptainsRequiredCount = captainsOverride;
            target.MatchCorePlayersRequiredCount = coreRequired;
            target.MatchCorePlayersEqualToCaptainCount = coreEqualToCaptain;

            var ok = await ConfigurationService.Persistence.SaveConfigurationAsync(config);
            await ctx.EditResponseAsync(ok ? $"Match rules updated for {size}." : "Failed to save configuration.");
        }

        [Command("teamconfig-view")]
        [Description("View TeamConfig settings")]
        public async Task ViewTeamConfigAsync(CommandContext ctx)
        {
            await ctx.DeferResponseAsync();

            var scrim = ConfigurationProvider.GetSection<ScrimmageOptions>(ScrimmageOptions.SectionName);
            var tc = scrim.TeamConfig;
            var content =
                $"Base: rejoin={tc.BaseConfig.RejoinTeamCooldownDays}, captainCD={tc.BaseConfig.ChangeCaptainCooldownDays}, coreCD={tc.BaseConfig.ChangeCoreCooldownDays}\n"
                + $"Solo: limit={tc.Solo.UserTeamMembershipLimitCount}, coreCD={tc.Solo.ChangeCoreCooldownDays?.ToString() ?? "(base)"}\n"
                + $"Duo: limit={tc.Duo.UserTeamMembershipLimitCount}, coreCD={tc.Duo.ChangeCoreCooldownDays?.ToString() ?? "(base)"}\n"
                + $"Squad: limit={tc.Squad.UserTeamMembershipLimitCount}, coreCD={tc.Squad.ChangeCoreCooldownDays?.ToString() ?? "(base)"}";
            await ctx.EditResponseAsync($"```${content}```");
        }

        [Command("teamconfig-set-base")]
        [Description("Set base TeamConfig cooldowns")]
        public async Task SetTeamConfigBaseAsync(
            CommandContext ctx,
            int rejoinDays,
            int captainCooldownDays,
            int coreCooldownDays
        )
        {
            await ctx.DeferResponseAsync();

            var config = await ConfigurationService.Persistence.LoadConfigurationAsync();
            if (config is null)
            {
                await ctx.EditResponseAsync("Failed to load configuration.");
                return;
            }

            config.Scrimmage.TeamConfig.BaseConfig.RejoinTeamCooldownDays = rejoinDays;
            config.Scrimmage.TeamConfig.BaseConfig.ChangeCaptainCooldownDays = captainCooldownDays;
            config.Scrimmage.TeamConfig.BaseConfig.ChangeCoreCooldownDays = coreCooldownDays;
            var ok = await ConfigurationService.Persistence.SaveConfigurationAsync(config);
            await ctx.EditResponseAsync(ok ? "TeamConfig base updated." : "Failed to save configuration.");
        }

        [Command("teamconfig-set-group")]
        [Description("Set per-group TeamConfig (membership limit and optional core cooldown override)")]
        public async Task SetTeamConfigGroupAsync(
            CommandContext ctx,
            string group,
            int userMembershipLimit,
            int? coreCooldownOverride = null
        )
        {
            await ctx.DeferResponseAsync();

            var config = await ConfigurationService.Persistence.LoadConfigurationAsync();
            if (config is null)
            {
                await ctx.EditResponseAsync("Failed to load configuration.");
                return;
            }

            var tc = config.Scrimmage.TeamConfig;
            switch (group.ToLowerInvariant())
            {
                case "solo":
                    tc.Solo.UserTeamMembershipLimitCount = userMembershipLimit;
                    tc.Solo.ChangeCoreCooldownDays = coreCooldownOverride;
                    break;
                case "duo":
                    tc.Duo.UserTeamMembershipLimitCount = userMembershipLimit;
                    tc.Duo.ChangeCoreCooldownDays = coreCooldownOverride;
                    break;
                case "squad":
                    tc.Squad.UserTeamMembershipLimitCount = userMembershipLimit;
                    tc.Squad.ChangeCoreCooldownDays = coreCooldownOverride;
                    break;
                default:
                    await ctx.EditResponseAsync("Invalid group. Use Solo, Duo, or Squad.");
                    return;
            }

            var ok = await ConfigurationService.Persistence.SaveConfigurationAsync(config);
            await ctx.EditResponseAsync(ok ? "TeamConfig group updated." : "Failed to save configuration.");
        }

        [Command("emojis-view")]
        [Description("View current emoji configuration")]
        public async Task ViewEmojisAsync(CommandContext ctx)
        {
            await ctx.DeferResponseAsync();

            var emojis = ConfigurationProvider.GetSection<EmojisOptions>(EmojisOptions.SectionName);
            var content =
                $"TeamRole1EmojiId: {emojis.TeamRole1EmojiId}\n" + $"TeamRole2EmojiId: {emojis.TeamRole2EmojiId}";

            await ctx.EditResponseAsync($"```${content}```");
        }

        [Command("emojis-set-teamrole1")]
        [Description("Set TeamRole1EmojiId")]
        public async Task SetTeamRole1EmojiAsync(CommandContext ctx, [Description("Emoji ID")] ulong emojiId)
        {
            await ctx.DeferResponseAsync();

            var config = await ConfigurationService.Persistence.LoadConfigurationAsync();
            if (config is null)
            {
                await ctx.EditResponseAsync("Failed to load configuration.");
                return;
            }

            config.Emojis.TeamRole1EmojiId = emojiId;
            var ok = await ConfigurationService.Persistence.SaveConfigurationAsync(config);
            if (!ok)
            {
                await ctx.EditResponseAsync("Failed to save configuration.");
                return;
            }

            await ctx.EditResponseAsync($"TeamRole1EmojiId set to {emojiId}.");
        }

        [Command("emojis-set-teamrole2")]
        [Description("Set TeamRole2EmojiId")]
        public async Task SetTeamRole2EmojiAsync(CommandContext ctx, [Description("Emoji ID")] ulong emojiId)
        {
            await ctx.DeferResponseAsync();

            var config = await ConfigurationService.Persistence.LoadConfigurationAsync();
            if (config is null)
            {
                await ctx.EditResponseAsync("Failed to load configuration.");
                return;
            }

            config.Emojis.TeamRole2EmojiId = emojiId;
            var ok = await ConfigurationService.Persistence.SaveConfigurationAsync(config);
            if (!ok)
            {
                await ctx.EditResponseAsync("Failed to save configuration.");
                return;
            }

            await ctx.EditResponseAsync($"TeamRole2EmojiId set to {emojiId}.");
        }

        [Command("threads-set-inactivity-threshold")]
        [Description("Set thread inactivity threshold in minutes (1-1440)")]
        public async Task SetThreadInactivityThresholdAsync(CommandContext ctx, [Description("Minutes")] int minutes)
        {
            await ctx.DeferResponseAsync();

            if (minutes < 1 || minutes > 1440)
            {
                await ctx.EditResponseAsync(
                    "Thread inactivity threshold must be between 1 and 1440 minutes (24 hours)."
                );
                return;
            }

            var configCommands = new WabbitBot.Core.Common.Commands.ConfigurationCommands();
            var result = await configCommands.SetThreadInactivityThresholdAsync(minutes);

            if (!result.Success)
            {
                await ctx.EditResponseAsync($"Failed to set thread inactivity threshold: {result.ErrorMessage}");
                return;
            }

            await ctx.EditResponseAsync($"Thread inactivity threshold set to {minutes} minutes.");
        }
    }
}
