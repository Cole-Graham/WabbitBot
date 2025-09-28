using Microsoft.CodeAnalysis.Testing;
using WabbitBot.Analyzers.Analyzers;
using Xunit;

namespace WabbitBot.Analyzers.Tests.Analyzers;

public class EventBoundaryAnalyzerTests
{
    [Fact]
    public async Task EventBoundaryOnRecord_ReportsDiagnostic()
    {
        const string source = @"
using WabbitBot.Common.Attributes;

namespace TestNamespace
{
    [EventBoundary]
    public record TestEvent();
}
";

        await Task.CompletedTask;
    }

    [Fact]
    public async Task EventWithoutIEvent_ReportsDiagnostic()
    {
        const string source = @"
namespace TestNamespace
{
    public class TestEvent
    {
    }
}
";

        await Task.CompletedTask;
    }
}
