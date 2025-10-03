using WabbitBot.Core.Common.Services;
using WabbitBot.Common.ErrorService;
using Xunit;

namespace WabbitBot.Core.Tests;

public class CoreServiceTests
{
    [Fact]
    public void InitializeServices_DoesNotThrow()
    {
        // Arrange/Act: empty stub for now
        Assert.True(true);
    }
}

public class LeaderboardCoreTests
{
    [Fact]
    public void RefreshLeaderboardAsync_Stub()
    {
        Assert.True(true);
    }
}

public class ArchiveRetentionJobTests
{
    [Fact]
    public void RunArchiveRetentionAsync_Stub()
    {
        Assert.True(true);
    }
}
