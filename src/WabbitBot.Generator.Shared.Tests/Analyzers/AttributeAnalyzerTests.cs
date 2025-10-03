using WabbitBot.Generator.Shared.Analyzers;
using Xunit;

namespace WabbitBot.Generator.Shared.Tests.Analyzers;

public class AttributeAnalyzerTests
{
    [Fact]
    public void CommandInfo_ConstructsCorrectly()
    {
        var info = new CommandInfo("TestCommand", "test", "admin");

        Assert.Equal("TestCommand", info.ClassName);
        Assert.Equal("test", info.CommandName);
        Assert.Equal("admin", info.Group);
    }
}
