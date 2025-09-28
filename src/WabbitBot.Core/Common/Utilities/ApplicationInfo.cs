using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace WabbitBot.Core.Common.Utilities
{
    public static class ApplicationInfo
    {
        public static Version CurrentVersion => new(
            FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion ?? "1.0.0.0");

        public static string VersionString => CurrentVersion.ToString(3);

        public static bool IsCompatibleWithSchema(string schemaVersion)
        {
            // Define compatibility ranges
            var ranges = new Dictionary<string, (string min, string max)>
            {
                ["1.0.x"] = ("001-1.0", "001-1.1"),
                ["1.1.x"] = ("001-1.0", "002-1.0"),
                ["1.2.x"] = ("002-1.0", "999-9.9")
            };

            foreach (var range in ranges)
            {
                if (VersionMatches(VersionString, range.Key))
                {
                    return VersionInRange(schemaVersion, range.Value.min, range.Value.max);
                }
            }

            return false;
        }

        private static bool VersionMatches(string version, string pattern)
        {
            if (pattern.EndsWith(".x"))
            {
                var baseVersion = pattern[..^2];
                return version.StartsWith(baseVersion);
            }
            return version == pattern;
        }

        private static bool VersionInRange(string version, string min, string max)
        {
            return string.Compare(version, min) >= 0 &&
                   string.Compare(version, max) <= 0;
        }
    }
}
