using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing.Verifiers;
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

        var expected1 = new DiagnosticResult("WB001", DiagnosticSeverity.Warning)
            .WithSpan(7, 19, 7, 28);
        var expected2 = new DiagnosticResult("WB004", DiagnosticSeverity.Error)
            .WithSpan(7, 19, 7, 28);
        var test = new CSharpAnalyzerTest<EventBoundaryAnalyzer, DefaultVerifier>
        {
            TestState = { Sources = { source } }
        };
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(WabbitBot.Common.Attributes.EventBoundaryAttribute).Assembly.Location));
        test.ExpectedDiagnostics.AddRange(new[] { expected1, expected2 });
        await test.RunAsync();
    }

    [Fact]
    public async Task EventWithoutIEvent_ReportsDiagnostic()
    {
        const string source = @"
using WabbitBot.Common.Attributes;
namespace TestNamespace
{
    [EventBoundary]
    public class TestEvent { }
}
";

        var expectedA = new DiagnosticResult("WB002", DiagnosticSeverity.Warning)
            .WithSpan(6, 18, 6, 27);
        var expectedB = new DiagnosticResult("WB004", DiagnosticSeverity.Error)
            .WithSpan(6, 18, 6, 27);
        var test = new CSharpAnalyzerTest<EventBoundaryAnalyzer, DefaultVerifier>
        {
            TestState = { Sources = { source } }
        };
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(WabbitBot.Common.Attributes.EventBoundaryAttribute).Assembly.Location));
        test.ExpectedDiagnostics.AddRange(new[] { expectedA, expectedB });
        await test.RunAsync();
    }
}
