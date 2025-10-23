namespace WabbitBot.Common.Models
{
    /// <summary>
    /// Data transfer object containing Discord user information.
    /// Used to pass Discord data across project boundaries without exposing DSharpPlus types.
    /// </summary>
    public sealed class DiscordUserInfo
    {
        public required ulong DiscordUserId { get; init; }
        public required string Username { get; init; }
        public required string GlobalName { get; init; }
        public required string Mention { get; init; }
        public string? AvatarUrl { get; init; }

        /// <summary>
        /// Creates a DiscordUserInfo instance.
        /// </summary>
        public DiscordUserInfo() { }

        /// <summary>
        /// Creates a DiscordUserInfo instance with all required fields.
        /// </summary>
        public DiscordUserInfo(
            ulong discordUserId,
            string username,
            string globalName,
            string mention,
            string? avatarUrl = null
        )
        {
            DiscordUserId = discordUserId;
            Username = username;
            GlobalName = globalName;
            Mention = mention;
            AvatarUrl = avatarUrl;
        }
    }
}
