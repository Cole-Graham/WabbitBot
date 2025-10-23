using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using WabbitBot.Common.Models;

namespace WabbitBot.DiscBot.App.Utilities
{
    /// <summary>
    /// Fetches Discord user information from Discord API.
    /// Used primarily for development data seeding.
    /// </summary>
    public static class DiscordUserInfoFetcher
    {
        /// <summary>
        /// Fetches Discord user information for a single user ID.
        /// </summary>
        /// <param name="client">The Discord client instance</param>
        /// <param name="userId">The Discord user ID to fetch</param>
        /// <returns>Result containing DiscordUserInfo or error message</returns>
        public static async Task<Result<DiscordUserInfo>> FetchUserInfoAsync(DiscordClient client, ulong userId)
        {
            if (client is null)
            {
                return Result<DiscordUserInfo>.Failure("Discord client is null");
            }

            try
            {
                var user = await client.GetUserAsync(userId);
                if (user is null)
                {
                    return Result<DiscordUserInfo>.Failure($"Could not find Discord user with ID {userId}");
                }

                var userInfo = new DiscordUserInfo
                {
                    DiscordUserId = user.Id,
                    Username = user.Username,
                    GlobalName = user.GlobalName ?? user.Username,
                    Mention = user.Mention,
                    AvatarUrl = user.AvatarUrl,
                };

                return Result<DiscordUserInfo>.CreateSuccess(userInfo);
            }
            catch (Exception ex)
            {
                return Result<DiscordUserInfo>.Failure($"Failed to fetch Discord user {userId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Fetches Discord user information for multiple user IDs.
        /// </summary>
        /// <param name="client">The Discord client instance</param>
        /// <param name="userIds">Collection of Discord user IDs to fetch</param>
        /// <returns>Result containing list of DiscordUserInfo or error message</returns>
        public static async Task<Result<List<DiscordUserInfo>>> FetchUserInfosAsync(
            DiscordClient client,
            IEnumerable<ulong> userIds
        )
        {
            if (client is null)
            {
                return Result<List<DiscordUserInfo>>.Failure("Discord client is null");
            }

            var userInfos = new List<DiscordUserInfo>();

            foreach (var userId in userIds)
            {
                var result = await FetchUserInfoAsync(client, userId);
                if (!result.Success)
                {
                    return Result<List<DiscordUserInfo>>.Failure(result.ErrorMessage ?? "Unknown error");
                }

                if (result.Data is not null)
                {
                    userInfos.Add(result.Data);
                }
            }

            return Result<List<DiscordUserInfo>>.CreateSuccess(userInfos);
        }

        /// <summary>
        /// Fetches Discord user information for multiple user IDs with partial success support.
        /// Returns successfully fetched users even if some fail.
        /// </summary>
        /// <param name="client">The Discord client instance</param>
        /// <param name="userIds">Collection of Discord user IDs to fetch</param>
        /// <returns>Result containing list of successfully fetched DiscordUserInfo</returns>
        public static async Task<Result<List<DiscordUserInfo>>> FetchUserInfosPartialAsync(
            DiscordClient client,
            IEnumerable<ulong> userIds
        )
        {
            if (client is null)
            {
                return Result<List<DiscordUserInfo>>.Failure("Discord client is null");
            }

            var userInfos = new List<DiscordUserInfo>();
            var failedUserIds = new List<ulong>();

            foreach (var userId in userIds)
            {
                var result = await FetchUserInfoAsync(client, userId);
                if (result.Success && result.Data is not null)
                {
                    userInfos.Add(result.Data);
                }
                else
                {
                    failedUserIds.Add(userId);
                    Console.WriteLine($"⚠️  Failed to fetch user {userId}: {result.ErrorMessage}");
                }
            }

            if (userInfos.Count == 0)
            {
                return Result<List<DiscordUserInfo>>.Failure(
                    $"Failed to fetch any users. Failed IDs: {string.Join(", ", failedUserIds)}"
                );
            }

            var metadata = new Dictionary<string, object>();
            if (failedUserIds.Count > 0)
            {
                metadata["FailedUserIds"] = failedUserIds;
            }

            return Result<List<DiscordUserInfo>>.CreateSuccess(userInfos, metadata);
        }
    }
}
