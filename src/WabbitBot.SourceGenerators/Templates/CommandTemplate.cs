namespace WabbitBot.SourceGenerators.Templates;

/// <summary>
/// Template for generating command registration code
/// </summary>
public static class CommandTemplate
{
    /// <summary>
    /// Template for command registration class
    /// </summary>
    public const string CommandRegistrationClass =
        @"
using DSharpPlus.Commands;
using System.Threading.Tasks;

namespace WabbitBot.DiscBot.DSharpPlus.Generated
{{
    public static class CommandRegistration
    {{
        public static async Task RegisterAllCommandsAsync(CommandsExtension commands)
        {{
{0}
        }}
    }}
}}";

    /// <summary>
    /// Template for individual command registration
    /// </summary>
    public const string CommandRegistration =
        @"
            await commands.RegisterCommands<{0}>();";

    /// <summary>
    /// Template for command class with attributes
    /// </summary>
    public const string CommandClass =
        @"
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using System.ComponentModel;

namespace {0}
{{
    [Command(""{1}"")]
    [Description(""{2}"")]
    public partial class {3}
    {{
{4}
    }}
}}";

    /// <summary>
    /// Template for command method
    /// </summary>
    public const string CommandMethod =
        @"
        [Command(""{0}"")]
        [Description(""{1}"")]
        public async Task {2}Async(CommandContext ctx{3})
        {{
            await ctx.DeferResponseAsync();
            
            // TODO: Implement command logic
            
            await ctx.EditResponseAsync(""Command executed successfully"");
        }}";
}
