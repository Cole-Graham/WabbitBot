using System.Collections.Concurrent;
using DSharpPlus.Entities;

namespace WabbitBot.DiscBot.App.Utilities
{
    /// <summary>
    /// Generic state manager for interactive Discord messages with components.
    /// Tracks user selections and state across component interactions.
    /// </summary>
    /// <typeparam name="TState">The type of state to track for each message</typeparam>
    public class InteractiveMessageStateManager<TState>
        where TState : class, new()
    {
        private readonly ConcurrentDictionary<ulong, TState> _messageStates = new();

        /// <summary>
        /// Gets the state for a message, creating a new instance if it doesn't exist
        /// </summary>
        public TState GetOrCreateState(ulong messageId)
        {
            return _messageStates.GetOrAdd(messageId, _ => new TState());
        }

        /// <summary>
        /// Gets the state for a message, or null if it doesn't exist
        /// </summary>
        public TState? GetState(ulong messageId)
        {
            return _messageStates.TryGetValue(messageId, out var state) ? state : null;
        }

        /// <summary>
        /// Sets or updates the state for a message
        /// </summary>
        public void SetState(ulong messageId, TState state)
        {
            _messageStates[messageId] = state;
        }

        /// <summary>
        /// Updates the state for a message using a modifier function
        /// </summary>
        public TState UpdateState(ulong messageId, Func<TState, TState> modifier)
        {
            return _messageStates.AddOrUpdate(
                messageId,
                _ => modifier(new TState()),
                (_, existing) => modifier(existing)
            );
        }

        /// <summary>
        /// Removes and returns the state for a message (cleanup)
        /// </summary>
        public bool RemoveState(ulong messageId)
        {
            return _messageStates.TryRemove(messageId, out _);
        }

        /// <summary>
        /// Checks if a message has tracked state
        /// </summary>
        public bool HasState(ulong messageId)
        {
            return _messageStates.ContainsKey(messageId);
        }

        /// <summary>
        /// Gets the current count of tracked messages
        /// </summary>
        public int Count => _messageStates.Count;

        /// <summary>
        /// Clears all tracked states (use with caution)
        /// </summary>
        public void ClearAll()
        {
            _messageStates.Clear();
        }
    }

    /// <summary>
    /// Extension methods for working with message state and component updates
    /// </summary>
    public static class InteractiveMessageExtensions
    {
        /// <summary>
        /// Finds a component in a container by custom ID prefix
        /// </summary>
        public static T? FindComponent<T>(this DiscordContainerComponent container, string customIdPrefix)
            where T : DiscordComponent
        {
            foreach (var component in container.Components)
            {
                if (component is DiscordActionRowComponent actionRow)
                {
                    var found = actionRow
                        .Components.OfType<T>()
                        .FirstOrDefault(c =>
                        {
                            var customId = c switch
                            {
                                DiscordButtonComponent btn => btn.CustomId,
                                DiscordSelectComponent sel => sel.CustomId,
                                _ => null,
                            };
                            return customId?.StartsWith(customIdPrefix, StringComparison.Ordinal) ?? false;
                        });

                    if (found is not null)
                        return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the container component from a message
        /// </summary>
        public static DiscordContainerComponent? GetContainer(this DiscordMessage message)
        {
            return message.Components?.OfType<DiscordContainerComponent>().FirstOrDefault();
        }

        /// <summary>
        /// Updates a button's enabled/disabled state in a container
        /// </summary>
        public static bool UpdateButtonState(
            this DiscordContainerComponent container,
            string customIdPrefix,
            bool enabled
        )
        {
            var button = container.FindComponent<DiscordButtonComponent>(customIdPrefix);
            if (button is null)
                return false;

            if (enabled)
                button.Enable();
            else
                button.Disable();

            return true;
        }
    }
}
