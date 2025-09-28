using Microsoft.CodeAnalysis;

namespace WabbitBot.SourceGenerators.Utils
{
    internal static class ProjectNames
    {
        internal const string Core = "WabbitBot.Core";
        internal const string DiscBot = "WabbitBot.DiscBot";
    }

    internal static class CompilationGating
    {
        public static IncrementalValueProvider<bool> IsAssembly(this IncrementalValueProvider<Compilation> provider, string assemblyName)
        {
            return provider.Select((comp, _) => string.Equals(comp.AssemblyName, assemblyName, System.StringComparison.OrdinalIgnoreCase));
        }

        public static IncrementalValueProvider<bool> IsCore(this IncrementalValueProvider<Compilation> provider)
            => provider.IsAssembly(ProjectNames.Core);

        public static IncrementalValueProvider<bool> IsDiscBot(this IncrementalValueProvider<Compilation> provider)
            => provider.IsAssembly(ProjectNames.DiscBot);
    }
}


