using System.Collections.Generic;
using WabbitBot.Core.Common.Utilities;
using Xunit;

namespace WabbitBot.Core.Tests.Common.Utilities
{
    /// <summary>
    /// Tests for version compatibility logic between application and database schema.
    /// </summary>
    public class VersionCompatibilityTests
    {
        [Theory]
        [InlineData("1.0.0", "001-1.0", true)] // Exact match within range
        [InlineData("1.0.0", "001-1.1", true)] // Within compatible range
        [InlineData("1.0.0", "001-1.2", false)] // Incompatible - schema too new (outside range)
        [InlineData("1.1.0", "001-1.0", true)] // Backward compatible
        [InlineData("1.1.0", "002-1.0", true)] // Forward compatible
        [InlineData("1.2.0", "002-1.0", true)] // Modern features
        [InlineData("1.2.0", "001-1.0", false)] // Incompatible - schema too old (outside range)
        [InlineData("1.0.5", "001-1.0", true)] // Patch version compatible
        [InlineData("1.1.9", "001-1.0", true)] // Late patch compatible
        [InlineData("2.0.0", "002-1.0", false)] // Major version mismatch
        public void VersionCompatibility_WorksCorrectly(
            string appVersion,
            string schemaVersion,
            bool expectedCompatible
        )
        {
            // Arrange & Act
            bool actualCompatible = IsCompatible(appVersion, schemaVersion);

            // Assert
            Assert.Equal(expectedCompatible, actualCompatible);
        }

        [Theory]
        [InlineData("1.0.0", "1.0.x", true)]
        [InlineData("1.0.5", "1.0.x", true)]
        [InlineData("1.0.99", "1.0.x", true)]
        [InlineData("1.1.0", "1.0.x", false)]
        [InlineData("1.1.0", "1.1.x", true)]
        [InlineData("1.2.0", "1.2.x", true)]
        [InlineData("2.0.0", "1.2.x", false)]
        public void VersionMatches_WorksCorrectly(string version, string pattern, bool expectedMatch)
        {
            // Arrange & Act
            bool actualMatch = VersionMatches(version, pattern);

            // Assert
            Assert.Equal(expectedMatch, actualMatch);
        }

        [Theory]
        [InlineData("001-1.0", "001-1.0", "001-1.1", true)] // At min
        [InlineData("001-1.1", "001-1.0", "001-1.1", true)] // At max
        [InlineData("001-1.05", "001-1.0", "001-1.1", true)] // Within range
        [InlineData("001-0.9", "001-1.0", "001-1.1", false)] // Below min
        [InlineData("001-1.2", "001-1.0", "001-1.1", false)] // Above max
        [InlineData("002-1.0", "001-1.0", "002-1.0", true)] // At upper bound
        public void VersionInRange_WorksCorrectly(string version, string min, string max, bool expectedInRange)
        {
            // Arrange & Act
            bool actualInRange = VersionInRange(version, min, max);

            // Assert
            Assert.Equal(expectedInRange, actualInRange);
        }

        #region Helper Methods (Mirroring ApplicationInfo logic for testing)

        private bool IsCompatible(string appVersion, string schemaVersion)
        {
            // Define the same compatibility ranges as ApplicationInfo.cs
            var ranges = new Dictionary<string, (string min, string max)>
            {
                ["1.0.x"] = ("001-1.0", "001-1.1"),
                ["1.1.x"] = ("001-1.0", "002-1.0"),
                ["1.2.x"] = ("002-1.0", "999-9.9"),
            };

            foreach (var range in ranges)
            {
                if (VersionMatches(appVersion, range.Key))
                {
                    return VersionInRange(schemaVersion, range.Value.min, range.Value.max);
                }
            }

            return false;
        }

        private bool VersionMatches(string version, string pattern)
        {
            // Simple pattern matching: "1.1.x" matches "1.1.0", "1.1.5", etc.
            if (pattern.EndsWith(".x"))
            {
                var baseVersion = pattern.Substring(0, pattern.Length - 2);
                return version.StartsWith(baseVersion);
            }
            return version == pattern;
        }

        private bool VersionInRange(string version, string min, string max)
        {
            return string.Compare(version, min, System.StringComparison.Ordinal) >= 0
                && string.Compare(version, max, System.StringComparison.Ordinal) <= 0;
        }

        #endregion
    }
}
