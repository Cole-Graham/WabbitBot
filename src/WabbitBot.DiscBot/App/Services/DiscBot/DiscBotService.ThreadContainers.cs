using System.Collections.Concurrent;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.DiscBot.App.Services.DiscBot
{
    public static partial class DiscBotService
    {
        /// <summary>
        /// Manages private threads that host interactive Discord container messages
        /// and cleans them up after inactivity.
        /// </summary>
        public static class ThreadContainers
        {
            private sealed class TrackedContainer
            {
                public required ulong MessageId { get; init; }
                public required ulong ThreadId { get; init; }
                public required ulong OwnerDiscordUserId { get; init; }
                public DateTime LastActivityUtc { get; set; }
                public required ulong ParentChannelId { get; init; }
                public bool InactivityCleanupEnabled { get; init; }
                public TimeSpan InactivityThreshold { get; init; }
            }

            private static readonly ConcurrentDictionary<ulong, TrackedContainer> _messageIdToContainer = new();
            private static readonly TimeSpan _defaultSweepInterval = TimeSpan.FromMinutes(1);
            private static volatile bool _cleanupStarted;

            private static TimeSpan GetInactivityThreshold()
            {
                var config = ConfigurationProvider.GetConfigurationService();
                var threadsConfig = config.GetSection<ThreadsOptions>(ThreadsOptions.SectionName);
                return TimeSpan.FromMinutes(threadsConfig.InactivityThresholdMinutes);
            }

            /// <summary>
            /// Closes any existing private container threads owned by the specified user.
            /// This is invoked before creating a new thread to ensure one-active-thread per user.
            /// </summary>
            public static async Task CloseExistingThreadsForUserAsync(ulong ownerDiscordUserId, ulong parentChannelId)
            {
                var candidates = _messageIdToContainer
                    .Where(kvp =>
                        kvp.Value.OwnerDiscordUserId == ownerDiscordUserId
                        && kvp.Value.ParentChannelId == parentChannelId
                    )
                    .Select(kvp => kvp.Value)
                    .ToList();

                foreach (var tracked in candidates)
                {
                    try
                    {
                        var thread = await Client.GetChannelAsync(tracked.ThreadId);
                        if (thread.Parent?.Id == parentChannelId)
                        {
                            await thread.DeleteAsync("Replacing with a new container thread for user");
                        }
                    }
                    catch (Exception ex)
                    {
                        await ErrorHandler.CaptureAsync(
                            ex,
                            $"Failed to remove existing container thread {tracked.ThreadId}",
                            nameof(ThreadContainers)
                        );
                    }
                    finally
                    {
                        // Remove all tracked entries for this thread (by message id)
                        var toRemove = _messageIdToContainer
                            .Where(p => p.Value.ThreadId == tracked.ThreadId)
                            .Select(p => p.Key)
                            .ToList();
                        foreach (var messageId in toRemove)
                        {
                            _messageIdToContainer.TryRemove(messageId, out _);
                        }
                    }
                }
            }

            /// <summary>
            /// Creates a private thread under MashinaChannel, invites the user, sends the container,
            /// and begins tracking it for inactivity cleanup.
            /// </summary>
            public static async Task<(DiscordChannel thread, DiscordMessage message)> CreateThreadAndSendAsync(
                string threadName,
                DiscordContainerComponent container,
                DiscordGuild? guild,
                ulong inviteDiscordUserId,
                DiscordChannel parentChannel,
                bool enableInactivityCleanup = true,
                TimeSpan? inactivityThreshold = null
            )
            {
                // Ensure we only keep a single active container thread per user per parent channel
                await CloseExistingThreadsForUserAsync(inviteDiscordUserId, parentChannel.Id);

                var thread = await parentChannel.CreateThreadAsync(
                    threadName,
                    DiscordAutoArchiveDuration.Hour,
                    DiscordChannelType.PrivateThread
                );

                if (guild is not null)
                {
                    try
                    {
                        var member = await guild.GetMemberAsync(inviteDiscordUserId);
                        await thread.AddThreadMemberAsync(member);
                    }
                    catch
                    {
                        // Non-fatal - continue without explicit invite
                    }
                }

                var message = await thread.SendMessageAsync(
                    new DiscordMessageBuilder().EnableV2Components().AddContainerComponent(container)
                );

                _messageIdToContainer[message.Id] = new TrackedContainer
                {
                    MessageId = message.Id,
                    ThreadId = thread.Id,
                    OwnerDiscordUserId = inviteDiscordUserId,
                    LastActivityUtc = DateTime.UtcNow,
                    ParentChannelId = parentChannel.Id,
                    InactivityCleanupEnabled = enableInactivityCleanup,
                    InactivityThreshold = inactivityThreshold ?? GetInactivityThreshold(),
                };

                // Persist tracking
                try
                {
                    var row = new DiscordThreadTracking
                    {
                        Id = Guid.NewGuid(),
                        GuildId = guild?.Id ?? 0,
                        ChannelId = parentChannel.Id,
                        ThreadId = thread.Id,
                        MessageId = message.Id,
                        CreatorDiscordUserId = inviteDiscordUserId,
                        Feature = "Container",
                        CreatedAt = DateTime.UtcNow,
                        LastActivityAt = DateTime.UtcNow,
                    };
                    await CoreService.DiscordThreads.CreateAsync(row, DatabaseComponent.Repository);
                }
                catch (Exception ex)
                {
                    await ErrorHandler.CaptureAsync(ex, "Failed to persist thread tracking", nameof(ThreadContainers));
                }

                return (thread, message);
            }

            /// <summary>
            /// Marks the container as active (resets inactivity timer).
            /// </summary>
            public static void MarkActivity(ulong messageId)
            {
                if (_messageIdToContainer.TryGetValue(messageId, out var tracked))
                {
                    tracked.LastActivityUtc = DateTime.UtcNow;
                }

                // Update persisted LastActivityAt
                _ = CoreService.WithDbContext(async db =>
                {
                    var rec = await db.Set<DiscordThreadTracking>().FirstOrDefaultAsync(x => x.MessageId == messageId);
                    if (rec is not null)
                    {
                        rec.LastActivityAt = DateTime.UtcNow;
                        await db.SaveChangesAsync();
                    }
                });
            }

            /// <summary>
            /// Starts a background cleanup loop to delete inactive threads.
            /// Safe to call multiple times; only starts once.
            /// </summary>
            public static Task StartCleanupLoop(TimeSpan? sweepInterval = null, TimeSpan? inactivity = null)
            {
                if (_cleanupStarted)
                {
                    return Task.CompletedTask;
                }

                _cleanupStarted = true;
                var interval = sweepInterval ?? _defaultSweepInterval;
                var threshold = inactivity ?? GetInactivityThreshold();

                return Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            await Task.Delay(interval);
                            var now = DateTime.UtcNow;

                            foreach (var kvp in _messageIdToContainer.ToArray())
                            {
                                var tracked = kvp.Value;
                                if (
                                    tracked.InactivityCleanupEnabled
                                    && now - tracked.LastActivityUtc >= tracked.InactivityThreshold
                                )
                                {
                                    try
                                    {
                                        var thread = await Client.GetChannelAsync(tracked.ThreadId);
                                        if (thread.Parent?.Id == tracked.ParentChannelId)
                                        {
                                            await thread.DeleteAsync("Inactive container thread cleanup");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        await ErrorHandler.CaptureAsync(
                                            ex,
                                            $"Failed to delete inactive thread {tracked.ThreadId}",
                                            nameof(ThreadContainers)
                                        );
                                    }
                                    finally
                                    {
                                        _messageIdToContainer.TryRemove(kvp.Key, out _);

                                        // Remove persisted record
                                        _ = CoreService.WithDbContext(async db =>
                                        {
                                            var rec = await db.Set<DiscordThreadTracking>()
                                                .FirstOrDefaultAsync(x => x.MessageId == kvp.Key);
                                            if (rec is not null)
                                            {
                                                db.Remove(rec);
                                                await db.SaveChangesAsync();
                                            }
                                        });
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            await ErrorHandler.CaptureAsync(
                                ex,
                                "ThreadContainers cleanup loop failed",
                                nameof(ThreadContainers)
                            );
                        }
                    }
                });
            }

            /// <summary>
            /// Loads persisted thread tracking rows into memory after bot restarts.
            /// Removes rows whose threads no longer exist.
            /// </summary>
            public static async Task LoadPersistedAsync()
            {
                try
                {
                    var rows = await CoreService.WithDbContext(async db =>
                        await db.Set<DiscordThreadTracking>().ToListAsync()
                    );
                    foreach (var r in rows)
                    {
                        try
                        {
                            var thread = await Client.GetChannelAsync(r.ThreadId);
                            // Thread exists; hydrate tracking
                            _messageIdToContainer[r.MessageId] = new TrackedContainer
                            {
                                MessageId = r.MessageId,
                                ThreadId = r.ThreadId,
                                OwnerDiscordUserId = r.CreatorDiscordUserId,
                                LastActivityUtc = r.LastActivityAt,
                                ParentChannelId = r.ChannelId,
                                InactivityCleanupEnabled = true,
                                InactivityThreshold = GetInactivityThreshold(),
                            };
                        }
                        catch
                        {
                            // Thread missing; delete row
                            await CoreService.WithDbContext(async db =>
                            {
                                var rec = await db.Set<DiscordThreadTracking>().FirstOrDefaultAsync(x => x.Id == r.Id);
                                if (rec is not null)
                                {
                                    db.Remove(rec);
                                    await db.SaveChangesAsync();
                                }
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    await ErrorHandler.CaptureAsync(ex, "Failed to load persisted threads", nameof(ThreadContainers));
                }
            }
        }
    }
}
