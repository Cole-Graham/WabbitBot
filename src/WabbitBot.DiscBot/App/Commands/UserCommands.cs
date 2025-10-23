using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;
using WabbitBot.DiscBot.App.Utilities;

namespace WabbitBot.DiscBot.App.Commands
{
    /// <summary>
    /// User registration and profile commands.
    /// </summary>
    [Command("user")]
    [Description("User registration and profile management")]
    [RequireGuild]
    public sealed partial class UserCommands
    {
        /// <summary>
        /// Register for scrimmages by providing your Steam ID.
        /// </summary>
        [Command("register")]
        [Description("Register for scrimmages")]
        public async Task RegisterAsync(
            CommandContext ctx,
            [Description("Your Steam ID (e.g., 76561198012345678)")] string steam_id
        )
        {
            await ctx.DeferResponseAsync();

            try
            {
                // Check if user is already registered
                var registrationCheck = await UserRegistrationHelper.EnsureRegisteredAsync(ctx.User);

                if (registrationCheck.Success)
                {
                    await ctx.EditResponseAsync("✅ You are already registered!");
                    return;
                }

                // Register the user with their Steam ID
                var registrationResult = await UserRegistrationHelper.RegisterWithSteamIdAsync(ctx.User, steam_id);

                if (!registrationResult.Success)
                {
                    await ctx.EditResponseAsync(
                        $"❌ Registration failed: {registrationResult.ErrorMessage}\n\n"
                            + "Please check your Steam ID and try again, or contact an administrator if the"
                            + "problem persists."
                    );
                    return;
                }

                await ctx.EditResponseAsync($"✅ **Registration successful!**\n\n");
            }
            catch (System.Exception ex)
            {
                await Services.DiscBot.DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to process register command",
                    nameof(RegisterAsync)
                );

                await ctx.EditResponseAsync(
                    "❌ An error occurred while processing your registration request. Please try again."
                );
            }
        }
    }
}
