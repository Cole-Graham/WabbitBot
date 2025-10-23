using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Interfaces;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.Core.Common.Models.Common
{
    public partial class MashinaUserCore : IMashinaUserCore
    {
        /// <inheritdoc />
        public Task InitializeAsync() => Task.CompletedTask;

        /// <inheritdoc />
        public Task ValidateAsync() => Task.CompletedTask;

        /// <summary>
        /// Checks if a user is registered in the system. Does NOT auto-create if missing.
        /// Handles Discord ID changes by checking both current and previous IDs.
        /// If user exists, updates their information and returns their Player.
        /// If user doesn't exist, returns failure with "REGISTRATION_REQUIRED" message.
        /// </summary>
        /// <param name="discordUserId">Discord user ID</param>
        /// <param name="discordUsername">Discord username (e.g., "user123")</param>
        /// <param name="discordGlobalname">Discord display name</param>
        /// <param name="discordMention">Discord mention string (e.g., "<@123456>")</param>
        /// <param name="discordAvatarUrl">Optional avatar URL</param>
        /// <returns>Result containing the Player entity if registered, or failure if not</returns>
        public static async Task<Result<Player>> EnsureUserRegisteredAsync(
            ulong discordUserId,
            string? discordUsername = null,
            string? discordGlobalname = null,
            string? discordMention = null,
            string? discordAvatarUrl = null
        )
        {
            try
            {
                return await CoreService.WithDbContext(async db =>
                {
                    // Check if MashinaUser exists by current ID
                    var mashinaUser = await db
                        .MashinaUsers.Include(mu => mu.Player)
                        .FirstOrDefaultAsync(mu => mu.DiscordUserId == discordUserId);

                    // If not found by current ID, check previous IDs
                    if (mashinaUser is null)
                    {
                        mashinaUser = await db
                            .MashinaUsers.Include(mu => mu.Player)
                            .FirstOrDefaultAsync(mu => mu.PreviousDiscordUserIds.Contains(discordUserId));
                    }

                    // If user doesn't exist, return registration required error
                    if (mashinaUser is null)
                    {
                        return Result<Player>.Failure(
                            "Registration required. Please use the /user register command"
                                + "to register your account. Get your steam id by selecting \"Account details:\" from"
                                + "the account menu in the top right corner of the steam client."
                        );
                    }
                    else
                    {
                        // User exists - check if Discord info has changed
                        var hasChanges = false;

                        // Handle Discord User ID change (found in previous IDs)
                        if (mashinaUser.DiscordUserId != discordUserId)
                        {
                            // Move current ID to previous IDs if not already there
                            if (!mashinaUser.PreviousDiscordUserIds.Contains(mashinaUser.DiscordUserId))
                            {
                                mashinaUser.PreviousDiscordUserIds.Add(mashinaUser.DiscordUserId);
                            }

                            // Remove the new ID from previous IDs (it's now current)
                            mashinaUser.PreviousDiscordUserIds.Remove(discordUserId);

                            // Set the new ID as current
                            mashinaUser.DiscordUserId = discordUserId;
                            hasChanges = true;
                        }

                        // Handle Discord Username change
                        if (
                            !string.IsNullOrEmpty(discordUsername)
                            && !string.Equals(mashinaUser.DiscordUsername, discordUsername, StringComparison.Ordinal)
                        )
                        {
                            // Move current username to previous if not null and not already there
                            if (
                                !string.IsNullOrEmpty(mashinaUser.DiscordUsername)
                                && !mashinaUser.PreviousDiscordUsernames.Contains(mashinaUser.DiscordUsername)
                            )
                            {
                                mashinaUser.PreviousDiscordUsernames.Add(mashinaUser.DiscordUsername);
                            }

                            // Remove new username from previous if it exists there
                            mashinaUser.PreviousDiscordUsernames.Remove(discordUsername);

                            // Set new username as current
                            mashinaUser.DiscordUsername = discordUsername;
                            hasChanges = true;
                        }

                        // Handle Discord Globalname change
                        if (
                            !string.IsNullOrEmpty(discordGlobalname)
                            && !string.Equals(
                                mashinaUser.DiscordGlobalname,
                                discordGlobalname,
                                StringComparison.Ordinal
                            )
                        )
                        {
                            // Move current globalname to previous if not null and not already there
                            if (
                                !string.IsNullOrEmpty(mashinaUser.DiscordGlobalname)
                                && !mashinaUser.PreviousDiscordGlobalnames.Contains(mashinaUser.DiscordGlobalname)
                            )
                            {
                                mashinaUser.PreviousDiscordGlobalnames.Add(mashinaUser.DiscordGlobalname);
                            }

                            // Remove new globalname from previous if it exists there
                            mashinaUser.PreviousDiscordGlobalnames.Remove(discordGlobalname);

                            // Set new globalname as current
                            mashinaUser.DiscordGlobalname = discordGlobalname;
                            hasChanges = true;
                        }

                        // Handle Discord Mention change
                        if (
                            !string.IsNullOrEmpty(discordMention)
                            && !string.Equals(mashinaUser.DiscordMention, discordMention, StringComparison.Ordinal)
                        )
                        {
                            // Move current mention to previous if not null and not already there
                            if (
                                !string.IsNullOrEmpty(mashinaUser.DiscordMention)
                                && !mashinaUser.PreviousDiscordMentions.Contains(mashinaUser.DiscordMention)
                            )
                            {
                                mashinaUser.PreviousDiscordMentions.Add(mashinaUser.DiscordMention);
                            }

                            // Remove new mention from previous if it exists there
                            mashinaUser.PreviousDiscordMentions.Remove(discordMention);

                            // Set new mention as current
                            mashinaUser.DiscordMention = discordMention;
                            hasChanges = true;
                        }

                        // Handle Avatar URL change (no history tracking for this)
                        if (
                            !string.IsNullOrEmpty(discordAvatarUrl)
                            && !string.Equals(mashinaUser.DiscordAvatarUrl, discordAvatarUrl, StringComparison.Ordinal)
                        )
                        {
                            mashinaUser.DiscordAvatarUrl = discordAvatarUrl;
                            hasChanges = true;
                        }

                        // Always update last active timestamp
                        mashinaUser.LastActive = DateTime.UtcNow;
                        hasChanges = true;

                        if (hasChanges)
                        {
                            db.MashinaUsers.Update(mashinaUser);
                            await db.SaveChangesAsync();

                            // Update cache
                            var cacheResult = await CoreService.MashinaUsers.UpdateAsync(
                                mashinaUser,
                                DatabaseComponent.Cache
                            );
                            if (!cacheResult.Success)
                            {
                                Console.WriteLine(
                                    $"Warning: Failed to update MashinaUser cache: {cacheResult.ErrorMessage}"
                                );
                            }
                        }
                    }

                    // Check if Player exists for this MashinaUser
                    var player = mashinaUser.Player;
                    if (player is null)
                    {
                        player = await db.Players.FirstOrDefaultAsync(p => p.MashinaUserId == mashinaUser.Id);
                    }

                    // If Player doesn't exist, registration is required
                    if (player is null)
                    {
                        return Result<Player>.Failure("REGISTRATION_REQUIRED");
                    }

                    // Update last active timestamp
                    player.LastActive = DateTime.UtcNow;
                    db.Players.Update(player);
                    await db.SaveChangesAsync();

                    return Result<Player>.CreateSuccess(player);
                });
            }
            catch (Exception ex)
            {
                return Result<Player>.Failure(
                    $"Failed to ensure user registration for Discord ID {discordUserId}: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Validates if a string is a valid Steam ID format.
        /// Supports SteamID64 (17 digits starting with 7656119) and SteamID3 format ([U:1:XXXXXXXX]).
        /// </summary>
        /// <param name="steamId">The Steam ID to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidSteamId(string steamId)
        {
            if (string.IsNullOrWhiteSpace(steamId))
            {
                return false;
            }

            steamId = steamId.Trim();

            // Check SteamID64 format (most common): 17 digits starting with 7656119
            if (steamId.Length == 17 && steamId.StartsWith("7656119", StringComparison.Ordinal))
            {
                return steamId.All(char.IsDigit);
            }

            // Check SteamID3 format: [U:1:XXXXXXXX]
            if (
                steamId.StartsWith("[U:1:", StringComparison.Ordinal) && steamId.EndsWith("]", StringComparison.Ordinal)
            )
            {
                var accountId = steamId.Substring(5, steamId.Length - 6);
                return accountId.All(char.IsDigit) && accountId.Length > 0;
            }

            return false;
        }

        /// <summary>
        /// Registers a new user with their Steam ID. Creates both MashinaUser and Player entities.
        /// This should be called after user provides their Steam ID through the registration flow.
        /// </summary>
        /// <param name="discordUserId">Discord user ID</param>
        /// <param name="steamId">User's Steam ID</param>
        /// <param name="discordUsername">Discord username (e.g., "user123")</param>
        /// <param name="discordGlobalname">Discord display name</param>
        /// <param name="discordMention">Discord mention string (e.g., "<@123456>")</param>
        /// <param name="discordAvatarUrl">Optional avatar URL</param>
        /// <returns>Result containing the newly created Player entity if successful</returns>
        public static async Task<Result<Player>> RegisterUserWithSteamIdAsync(
            ulong discordUserId,
            string steamId,
            string? discordUsername = null,
            string? discordGlobalname = null,
            string? discordMention = null,
            string? discordAvatarUrl = null
        )
        {
            if (string.IsNullOrWhiteSpace(steamId))
            {
                return Result<Player>.Failure("Steam ID is required");
            }

            if (!IsValidSteamId(steamId))
            {
                return Result<Player>.Failure(
                    "Invalid Steam ID format. Please provide a valid SteamID64 (17 digits starting with 7656119) "
                        + "or SteamID3 format ([U:1:XXXXXXXX])"
                );
            }

            try
            {
                return await CoreService.WithDbContext(async db =>
                {
                    // Check if user already exists
                    var existingUser = await db
                        .MashinaUsers.Include(mu => mu.Player)
                        .FirstOrDefaultAsync(mu => mu.DiscordUserId == discordUserId);

                    if (existingUser is not null && existingUser.Player is not null)
                    {
                        return Result<Player>.Failure(
                            "User is already registered. Please use the scrimmage commands to participate."
                        );
                    }

                    // Check if this Steam ID is already associated with an existing Player
                    // Check both current and previous platform IDs
                    // First get all players and then filter in memory to avoid EF translation issues
                    var allPlayers = await db.Players.Include(p => p.MashinaUser).ToListAsync();

                    var existingPlayerWithSteamId = allPlayers
                        .Where(p =>
                            (p.CurrentPlatformIds.ContainsKey("Steam") && p.CurrentPlatformIds["Steam"] == steamId)
                            || (
                                p.PreviousPlatformIds.ContainsKey("Steam")
                                && p.PreviousPlatformIds["Steam"].Contains(steamId)
                            )
                        )
                        .FirstOrDefault();

                    // If Steam ID already exists, link this Discord user to the existing Player's MashinaUser
                    if (existingPlayerWithSteamId is not null)
                    {
                        var existingMashinaUser = existingPlayerWithSteamId.MashinaUser;

                        // Update the existing MashinaUser with new Discord info
                        if (existingMashinaUser.DiscordUserId != discordUserId)
                        {
                            // Move old Discord ID to previous IDs if not already there
                            if (!existingMashinaUser.PreviousDiscordUserIds.Contains(existingMashinaUser.DiscordUserId))
                            {
                                existingMashinaUser.PreviousDiscordUserIds.Add(existingMashinaUser.DiscordUserId);
                            }

                            // Update to new Discord ID
                            existingMashinaUser.DiscordUserId = discordUserId;
                        }

                        // Update other Discord properties
                        if (!string.IsNullOrEmpty(discordUsername))
                        {
                            existingMashinaUser.DiscordUsername = discordUsername;
                        }

                        if (!string.IsNullOrEmpty(discordGlobalname))
                        {
                            existingMashinaUser.DiscordGlobalname = discordGlobalname;
                        }

                        if (!string.IsNullOrEmpty(discordMention))
                        {
                            existingMashinaUser.DiscordMention = discordMention;
                        }

                        if (!string.IsNullOrEmpty(discordAvatarUrl))
                        {
                            existingMashinaUser.DiscordAvatarUrl = discordAvatarUrl;
                        }

                        existingMashinaUser.LastActive = DateTime.UtcNow;
                        existingMashinaUser.IsActive = true;

                        db.MashinaUsers.Update(existingMashinaUser);
                        await db.SaveChangesAsync();

                        // Update cache
                        var cacheResult = await CoreService.MashinaUsers.UpdateAsync(
                            existingMashinaUser,
                            DatabaseComponent.Cache
                        );
                        if (!cacheResult.Success)
                        {
                            Console.WriteLine(
                                $"Warning: Failed to update MashinaUser cache: {cacheResult.ErrorMessage}"
                            );
                        }

                        // Ensure Steam ID is in CurrentPlatformIds (might have been in PreviousPlatformIds)
                        var currentSteamId = existingPlayerWithSteamId.CurrentPlatformIds.GetValueOrDefault("Steam");
                        if (currentSteamId != steamId)
                        {
                            // Move old current Steam ID to previous if it exists
                            if (!string.IsNullOrEmpty(currentSteamId))
                            {
                                if (!existingPlayerWithSteamId.PreviousPlatformIds.ContainsKey("Steam"))
                                {
                                    existingPlayerWithSteamId.PreviousPlatformIds["Steam"] = [];
                                }

                                if (!existingPlayerWithSteamId.PreviousPlatformIds["Steam"].Contains(currentSteamId))
                                {
                                    existingPlayerWithSteamId.PreviousPlatformIds["Steam"].Add(currentSteamId);
                                }
                            }

                            // Set new Steam ID as current
                            existingPlayerWithSteamId.CurrentPlatformIds["Steam"] = steamId;

                            // Remove from previous if it was there
                            if (
                                existingPlayerWithSteamId.PreviousPlatformIds.ContainsKey("Steam")
                                && existingPlayerWithSteamId.PreviousPlatformIds["Steam"].Contains(steamId)
                            )
                            {
                                existingPlayerWithSteamId.PreviousPlatformIds["Steam"].Remove(steamId);
                            }
                        }

                        // Update Player's last active
                        existingPlayerWithSteamId.LastActive = DateTime.UtcNow;
                        db.Players.Update(existingPlayerWithSteamId);
                        await db.SaveChangesAsync();

                        return Result<Player>.CreateSuccess(existingPlayerWithSteamId);
                    }

                    // No existing Player with this Steam ID - create new account
                    // If MashinaUser exists but Player doesn't, use existing MashinaUser
                    MashinaUser mashinaUser;
                    if (existingUser is not null)
                    {
                        mashinaUser = existingUser;
                    }
                    else
                    {
                        // Create new MashinaUser
                        mashinaUser = new MashinaUser
                        {
                            DiscordUserId = discordUserId,
                            DiscordUsername = discordUsername ?? $"User{discordUserId}",
                            DiscordGlobalname = discordGlobalname ?? discordUsername ?? $"User {discordUserId}",
                            DiscordMention = discordMention ?? $"<@{discordUserId}>",
                            DiscordAvatarUrl = discordAvatarUrl,
                            JoinedAt = DateTime.UtcNow,
                            LastActive = DateTime.UtcNow,
                            IsActive = true,
                        };

                        db.MashinaUsers.Add(mashinaUser);
                        await db.SaveChangesAsync();

                        // Cache the new user
                        var cacheResult = await CoreService.MashinaUsers.CreateAsync(
                            mashinaUser,
                            DatabaseComponent.Cache
                        );
                        if (!cacheResult.Success)
                        {
                            Console.WriteLine($"Warning: Failed to cache new MashinaUser: {cacheResult.ErrorMessage}");
                        }
                    }

                    // Create Player with Steam ID
                    var player = PlayerCore.InitializeDefaults(
                        new Player
                        {
                            MashinaUserId = mashinaUser.Id,
                            Name = discordGlobalname ?? discordUsername ?? $"Player{discordUserId}",
                            LastActive = DateTime.UtcNow,
                            CurrentPlatformIds = new Dictionary<string, string> { ["Steam"] = steamId },
                        }
                    );

                    db.Players.Add(player);
                    await db.SaveChangesAsync();

                    // Link the player to the MashinaUser
                    mashinaUser.PlayerId = player.Id;
                    db.MashinaUsers.Update(mashinaUser);
                    await db.SaveChangesAsync();

                    // Cache the new player
                    var playerCacheResult = await CoreService.Players.CreateAsync(player, DatabaseComponent.Cache);
                    if (!playerCacheResult.Success)
                    {
                        Console.WriteLine($"Warning: Failed to cache new Player: {playerCacheResult.ErrorMessage}");
                    }

                    return Result<Player>.CreateSuccess(player);
                });
            }
            catch (Exception ex)
            {
                return Result<Player>.Failure($"Failed to register user: {ex.Message}");
            }
        }

        public async Task<Result> UpdateLastActiveAsync(Guid mashinaUserId)
        {
            var userResult = await CoreService.MashinaUsers.GetByIdAsync(
                mashinaUserId,
                WabbitBot.Common.Data.Interfaces.DatabaseComponent.Repository
            );
            if (!userResult.Success || userResult.Data is null)
            {
                return Result.Failure($"User not found: {userResult.ErrorMessage}");
            }
            var user = userResult.Data;
            user.LastActive = DateTime.UtcNow;
            var updateRepo = await CoreService.MashinaUsers.UpdateAsync(
                user,
                WabbitBot.Common.Data.Interfaces.DatabaseComponent.Repository
            );
            var updateCache = await CoreService.MashinaUsers.UpdateAsync(
                user,
                WabbitBot.Common.Data.Interfaces.DatabaseComponent.Cache
            );
            return updateRepo.Success && updateCache.Success
                ? Result.CreateSuccess()
                : Result.Failure(
                    $"Failed to update user last active: {updateRepo.ErrorMessage} / {updateCache.ErrorMessage}"
                );
        }

        public async Task<Result> SetActiveAsync(Guid mashinaUserId, bool isActive)
        {
            var userResult = await CoreService.MashinaUsers.GetByIdAsync(
                mashinaUserId,
                WabbitBot.Common.Data.Interfaces.DatabaseComponent.Repository
            );
            if (!userResult.Success || userResult.Data is null)
            {
                return Result.Failure($"User not found: {userResult.ErrorMessage}");
            }
            var user = userResult.Data;
            user.IsActive = isActive;
            user.LastActive = DateTime.UtcNow;
            var update = await CoreService.MashinaUsers.UpdateAsync(
                user,
                WabbitBot.Common.Data.Interfaces.DatabaseComponent.Repository
            );
            return update.Success
                ? Result.CreateSuccess()
                : Result.Failure(update.ErrorMessage ?? "Failed to update user active state");
        }

        public async Task<Result> LinkPlayerAsync(Guid mashinaUserId, Guid playerId)
        {
            if (playerId == Guid.Empty)
            {
                return Result.Failure("Player ID cannot be empty");
            }
            var userResult = await CoreService.MashinaUsers.GetByIdAsync(
                mashinaUserId,
                WabbitBot.Common.Data.Interfaces.DatabaseComponent.Repository
            );
            if (!userResult.Success || userResult.Data is null)
            {
                return Result.Failure($"User not found: {userResult.ErrorMessage}");
            }
            var user = userResult.Data;
            user.PlayerId = playerId;
            var update = await CoreService.MashinaUsers.UpdateAsync(
                user,
                WabbitBot.Common.Data.Interfaces.DatabaseComponent.Repository
            );
            return update.Success
                ? Result.CreateSuccess()
                : Result.Failure(update.ErrorMessage ?? "Failed to link player");
        }

        public async Task<Result> UnlinkPlayerAsync(Guid mashinaUserId)
        {
            var userResult = await CoreService.MashinaUsers.GetByIdAsync(
                mashinaUserId,
                WabbitBot.Common.Data.Interfaces.DatabaseComponent.Repository
            );
            if (!userResult.Success || userResult.Data is null)
            {
                return Result.Failure($"User not found: {userResult.ErrorMessage}");
            }
            var user = userResult.Data;
            user.PlayerId = null;
            var update = await CoreService.MashinaUsers.UpdateAsync(
                user,
                WabbitBot.Common.Data.Interfaces.DatabaseComponent.Repository
            );
            return update.Success
                ? Result.CreateSuccess()
                : Result.Failure(update.ErrorMessage ?? "Failed to unlink player");
        }
    }
}
