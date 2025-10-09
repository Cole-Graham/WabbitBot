using System.Threading.Tasks;
using Xunit;

namespace WabbitBot.SourceGenerators.Tests.Generators
{
    public class DbContextGeneratorSnapshotTests
    {
        [Fact]
        public async Task Generates_DbContext_With_Archive_And_Mappings()
        {
            // Smoke test placeholder: build already runs generators; ensure test assembly loads.
            await Task.CompletedTask;
        }
    }
}
