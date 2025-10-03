using System;
using System.IO;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Services;
using WabbitBot.Core.Common.Interfaces;

namespace WabbitBot.Core.Common.Models.Common
{
    public class MapCore : IMapCore
    {
        /// <inheritdoc />
        public Task InitializeAsync() => Task.CompletedTask;

        /// <inheritdoc />
        public Task ValidateAsync() => Task.CompletedTask;

        public static class Validation
        {
            /// <summary>
            /// Validates if a map name is valid
            /// </summary>
            public static bool IsValidMapName(string name)
            {
                return !string.IsNullOrWhiteSpace(name) && name.Length <= 50;
            }

            /// <summary>
            /// Validates if a map size is valid
            /// </summary>
            public static bool IsValidMapSize(string size)
            {
                return !string.IsNullOrWhiteSpace(size) &&
                       size.ToLowerInvariant() is "1v1" or "2v2" or "3v3" or "4v4";
            }

            /// <summary>
            /// Validates if a thumbnail filename is valid
            /// </summary>
            public static bool IsValidThumbnailFilename(string filename)
            {
                if (string.IsNullOrWhiteSpace(filename))
                    return false;

                var extension = Path.GetExtension(filename).ToLowerInvariant();
                return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp";
            }

            /// <summary>
            /// Validates if a map configuration is complete
            /// </summary>
            public static bool IsCompleteMapConfiguration(Map map)
            {
                return IsValidMapName(map.Name) &&
                       IsValidMapSize(map.Size) &&
                       !string.IsNullOrEmpty(map.ThumbnailFilename);
            }
        }
    }
}
