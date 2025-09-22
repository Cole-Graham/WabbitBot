using System;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common;

/// <summary>
/// User-specific validation rules and operations
/// </summary>
public static partial class CoreValidation
{
    /// <summary>
    /// Validates that a user is not linked to a player
    /// </summary>
    public static Func<Models.User, bool> IsNotLinkedToPlayer() =>
        user => string.IsNullOrEmpty(user.PlayerId);

    /// <summary>
    /// Validates that a user has been inactive for the threshold period
    /// </summary>
    public static Func<Models.User, bool> HasBeenInactive() =>
        user => DateTime.UtcNow - user.LastActive >= TimeSpan.FromDays(30);

    /// <summary>
    /// Validates that a user can be archived
    /// </summary>
    public static Result<Models.User> ValidateForArchiving(Models.User user)
    {
        if (user == null)
            return Result<Models.User>.Failure("User cannot be null");

        // Check if user is already inactive
        if (!user.IsActive)
            return Result<Models.User>.Failure("User is already inactive");

        // Check if user is linked to a player
        if (!string.IsNullOrEmpty(user.PlayerId))
            return Result<Models.User>.Failure("User is linked to a player");

        // Check inactivity threshold
        if (!HasBeenInactive()(user))
            return Result<Models.User>.Failure("User has been active within the last 30 days");

        return Result<Models.User>.CreateSuccess(user);
    }

    /// <summary>
    /// Validates that a user can be unarchived
    /// </summary>
    public static Result<Models.User> ValidateForUnarchiving(Models.User user)
    {
        if (user == null)
            return Result<Models.User>.Failure("User cannot be null");

        if (user.IsActive)
            return Result<Models.User>.Failure("User is still active");

        return Result<Models.User>.CreateSuccess(user);
    }
}