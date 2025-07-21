using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace WabbitBot.DiscBot.DSharpPlus
{
    /// <summary>
    /// Simple permission service that doesn't use dependency injection
    /// </summary>
    public static class PermissionService
    {
        /// <summary>
        /// Checks if a user has the whitelisted role
        /// </summary>
        /// <param name="user">The Discord user</param>
        /// <param name="guild">The Discord guild</param>
        /// <returns>True if the user has the whitelisted role</returns>
        public static Task<bool> HasWhitelistedRoleAsync(DiscordUser user, DiscordGuild? guild)
        {
            if (user is not DiscordMember member || guild is null)
                return Task.FromResult(false);

            // TODO: Get whitelisted role ID from configuration
            // For now, return true for testing
            return Task.FromResult(true);
        }

        /// <summary>
        /// Checks if a user has admin privileges
        /// </summary>
        /// <param name="user">The Discord user</param>
        /// <param name="guild">The Discord guild</param>
        /// <returns>True if the user has admin privileges</returns>
        public static Task<bool> HasAdminPrivilegesAsync(DiscordUser user, DiscordGuild? guild)
        {
            if (user is not DiscordMember member || guild is null)
                return Task.FromResult(false);

            // Check if user has administrator permission
            return Task.FromResult(member.Permissions.HasPermission(DiscordPermission.Administrator));
        }
    }
}