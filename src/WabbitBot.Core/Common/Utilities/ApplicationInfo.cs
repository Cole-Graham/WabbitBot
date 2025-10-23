using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace WabbitBot.Core.Common.Utilities
{
    public static class ApplicationInfo
    {
        public static Version CurrentVersion
        {
            get
            {
                var entryAssembly = Assembly.GetEntryAssembly();
                string? versionString = entryAssembly
                    ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion;

                if (string.IsNullOrWhiteSpace(versionString))
                {
                    var filePath = entryAssembly?.Location ?? Assembly.GetExecutingAssembly().Location;
                    versionString = FileVersionInfo.GetVersionInfo(filePath).FileVersion ?? "0.1.0.0";
                }

                var clean = versionString.Split('+')[0];
                return Version.TryParse(clean, out var parsed) ? parsed : new Version(1, 0, 0, 0);
            }
        }

        public static string VersionString => CurrentVersion.ToString(3);

        public static bool IsCompatibleWithSchema(string schemaVersion)
        {
            // Define compatibility ranges
            var ranges = new Dictionary<string, (string min, string max)> { ["0.1.x"] = ("001-1.0", "999-9.9") };

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
            if (string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(min) || string.IsNullOrWhiteSpace(max))
            {
                return false;
            }

            if (!SchemaVersion.TryParse(version, out var v))
            {
                return false;
            }
            if (!SchemaVersion.TryParse(min, out var lo))
            {
                return false;
            }
            if (!SchemaVersion.TryParse(max, out var hi))
            {
                return false;
            }

            return v.CompareTo(lo) >= 0 && v.CompareTo(hi) <= 0;
        }

        private readonly record struct SchemaVersion(int Plan, int Major, int Minor) : IComparable<SchemaVersion>
        {
            public static bool TryParse(string s, out SchemaVersion version)
            {
                version = default;
                var parts = s.Split('-', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    return false;
                }
                if (!int.TryParse(parts[0], out var plan))
                {
                    return false;
                }

                var verParts = parts[1]
                    .Split('.', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (verParts.Length == 0 || !int.TryParse(verParts[0], out var major))
                {
                    return false;
                }
                var minor = 0;
                if (verParts.Length > 1 && !int.TryParse(verParts[1], out minor))
                {
                    return false;
                }

                version = new SchemaVersion(plan, major, minor);
                return true;
            }

            public int CompareTo(SchemaVersion other)
            {
                if (Plan != other.Plan)
                    return Plan.CompareTo(other.Plan);
                if (Major != other.Major)
                    return Major.CompareTo(other.Major);
                return Minor.CompareTo(other.Minor);
            }
        }
    }
}
