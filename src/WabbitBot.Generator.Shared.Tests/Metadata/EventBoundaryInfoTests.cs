using WabbitBot.Generator.Shared.Metadata;
using Xunit;

namespace WabbitBot.Generator.Shared.Tests.Metadata;

public class EventBoundaryInfoTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var info = new EventBoundaryInfo(
            ClassName: "TestClass",
            GenerateRequestResponse: true,
            BusType: WabbitBot.Generator.Shared.EventBusType.Core,
            TargetProjects: "Core");

        Assert.Equal("TestClass", info.ClassName);
        Assert.True(info.GenerateRequestResponse);
        Assert.Equal(WabbitBot.Generator.Shared.EventBusType.Core, info.BusType);
        Assert.Equal("Core", info.TargetProjects);
    }
}
