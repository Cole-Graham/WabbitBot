using WabbitBot.Core.Common.Models;

namespace WabbitBot.DiscBot.DSharpPlus.Commands
{
    /// <summary>
    /// Shared helper methods for Discord commands
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Attempts to parse a string into a GameSize enum value
        /// </summary>
        public static bool TryParseGameSize(string size, out GameSize gameSize)
        {
            gameSize = size.ToLowerInvariant() switch
            {
                "1v1" => GameSize.OneVOne,
                "2v2" => GameSize.TwoVTwo,
                "3v3" => GameSize.ThreeVThree,
                "4v4" => GameSize.FourVFour,
                _ => GameSize.OneVOne
            };

            return size.ToLowerInvariant() is "1v1" or "2v2" or "3v3" or "4v4";
        }

        /// <summary>
        /// Converts a GameSize enum value to its display string
        /// </summary>
        public static string GetGameSizeDisplay(GameSize gameSize)
        {
            return gameSize switch
            {
                GameSize.OneVOne => "1v1",
                GameSize.TwoVTwo => "2v2",
                GameSize.ThreeVThree => "3v3",
                GameSize.FourVFour => "4v4",
                _ => gameSize.ToString()
            };
        }

        /// <summary>
        /// Attempts to parse a string into a TeamRole enum value
        /// </summary>
        public static bool TryParseTeamRole(string role, out TeamRole teamRole)
        {
            teamRole = role.ToLowerInvariant() switch
            {
                "core" => TeamRole.Core,
                "backup" => TeamRole.Substitute,
                _ => TeamRole.Core
            };

            return role.ToLowerInvariant() is "core" or "backup";
        }
    }
}
