using System.Threading.Tasks;
using DSharpPlus.Entities;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;

namespace WabbitBot.DiscBot.App.Utilities
{
    /// <summary>
    /// Helper utilities for ensuring users are registered in the scrimmage system.
    /// </summary>
    public static class UserRegistrationHelper
    {
        /// <summary>
        /// Checks if a Discord user is registered in the system.
        /// If not registered, returns a failure with "REGISTRATION_REQUIRED" message.
        /// Call this at the beginning of any scrimmage interaction.
        /// </summary>
        /// <param name="discordUser">The Discord user to check</param>
        /// <returns>Result containing the Player entity if successful, or failure if not registered</returns>
        public static async Task<Result<Player>> EnsureRegisteredAsync(DiscordUser discordUser)
        {
            if (discordUser is null)
            {
                return Result<Player>.Failure("Discord user cannot be null");
            }

            return await MashinaUserCore.EnsureUserRegisteredAsync(
                discordUserId: discordUser.Id,
                discordUsername: discordUser.Username,
                discordGlobalname: discordUser.GlobalName ?? discordUser.Username,
                discordMention: discordUser.Mention,
                discordAvatarUrl: discordUser.AvatarUrl
            );
        }

        /// <summary>
        /// Registers a Discord user with their Steam ID.
        /// </summary>
        /// <param name="discordUser">The Discord user to register</param>
        /// <param name="steamId">The user's Steam ID</param>
        /// <returns>Result containing the newly created Player entity if successful</returns>
        public static async Task<Result<Player>> RegisterWithSteamIdAsync(DiscordUser discordUser, string steamId)
        {
            if (discordUser is null)
            {
                return Result<Player>.Failure("Discord user cannot be null");
            }

            return await MashinaUserCore.RegisterUserWithSteamIdAsync(
                discordUserId: discordUser.Id,
                steamId: steamId,
                discordUsername: discordUser.Username,
                discordGlobalname: discordUser.GlobalName ?? discordUser.Username,
                discordMention: discordUser.Mention,
                discordAvatarUrl: discordUser.AvatarUrl
            );
        }

        /// <summary>
        /// Ensures multiple Discord users are registered in the system.
        /// Useful for bulk registration operations.
        /// </summary>
        /// <param name="discordUsers">Collection of Discord users to register</param>
        /// <returns>Result indicating success or failure</returns>
        public static async Task<Result> EnsureRegisteredAsync(params DiscordUser[] discordUsers)
        {
            foreach (var user in discordUsers)
            {
                var result = await EnsureRegisteredAsync(user);
                if (!result.Success)
                {
                    return Result.Failure(
                        $"Failed to register user {user.Username} ({user.Id}): {result.ErrorMessage}"
                    );
                }
            }

            return Result.CreateSuccess();
        }
    }
}
