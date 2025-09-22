using System;
using DSharpPlus;

namespace WabbitBot.DiscBot.DiscBot.Services
{
    /// <summary>
    /// Service locator for providing access to the DiscordClient instance
    /// This allows event handlers and other services to access the client without dependency injection
    /// </summary>
    public static class DiscordClientProvider
    {
        private static DiscordClient? _client;
        private static readonly object _lock = new();
        private static bool _isInitialized = false;

        /// <summary>
        /// Sets the DiscordClient instance. Should be called once during bot initialization.
        /// </summary>
        /// <param name="client">The DiscordClient instance to provide</param>
        /// <exception cref="ArgumentNullException">Thrown when client is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when client is already set</exception>
        public static void SetClient(DiscordClient client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            lock (_lock)
            {
                if (_isInitialized)
                {
                    throw new InvalidOperationException("DiscordClient has already been set. This should only be called once during initialization.");
                }

                _client = client;
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Gets the DiscordClient instance.
        /// </summary>
        /// <returns>The DiscordClient instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when client has not been set</exception>
        public static DiscordClient GetClient()
        {
            lock (_lock)
            {
                if (!_isInitialized || _client == null)
                {
                    throw new InvalidOperationException("DiscordClient has not been initialized. Make sure DiscordBot.StartAsync() has been called.");
                }

                return _client;
            }
        }

        /// <summary>
        /// Checks if the DiscordClient has been initialized.
        /// </summary>
        /// <returns>True if the client is available, false otherwise</returns>
        public static bool IsInitialized
        {
            get
            {
                lock (_lock)
                {
                    return _isInitialized && _client != null;
                }
            }
        }

        /// <summary>
        /// Resets the provider (for testing purposes only).
        /// </summary>
        internal static void Reset()
        {
            lock (_lock)
            {
                _client = null;
                _isInitialized = false;
            }
        }
    }
}
