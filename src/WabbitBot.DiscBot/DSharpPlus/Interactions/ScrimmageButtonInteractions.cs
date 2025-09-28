// using DSharpPlus;
// using DSharpPlus.Entities;
// using DSharpPlus.EventArgs;
// using System;
// using System.Threading.Tasks;
// using WabbitBot.Core.Scrimmages;
// using WabbitBot.DiscBot.DiscBot.Base;
// using WabbitBot.DiscBot.DiscBot.ErrorHandling;
// using WabbitBot.DiscBot.DSharpPlus.Embeds;
// using WabbitBot.Core.Common.BotCore;

// namespace WabbitBot.DiscBot.DSharpPlus.Interactions;

// /// <summary>
// /// Handles button interactions for scrimmage challenges
// /// </summary>
// public class ScrimmageButtonInteractions
// {
//     private static readonly ScrimmageService ScrimmageService = new();

//     /// <summary>
//     /// Handles button interaction events
//     /// </summary>
//     public static async Task HandleButtonInteractionAsync(DiscordClient client, ComponentInteractionCreatedEventArgs e)
//     {
//         try
//         {
//             var customId = e.Interaction.Data.CustomId;

//             if (customId.StartsWith("accept_challenge_"))
//             {
//                 await HandleAcceptChallengeAsync(e);
//             }
//             else if (customId.StartsWith("decline_challenge_"))
//             {
//                 await HandleDeclineChallengeAsync(e);
//             }
//             else if (customId.StartsWith("submit_map_bans_"))
//             {
//                 await HandleSubmitMapBansAsync(e);
//             }
//             else if (customId.StartsWith("submit_deck_"))
//             {
//                 await HandleSubmitDeckAsync(e);
//             }
//             else if (customId.StartsWith("report_result_"))
//             {
//                 await HandleReportResultAsync(e);
//             }
//             else if (customId.StartsWith("revise_deck_"))
//             {
//                 await HandleReviseDeckAsync(e);
//             }
//         }
//         catch (Exception ex)
//         {
//             await DiscordErrorHandler.Instance.HandleError(ex);

//             // Send ephemeral error message to user
//             await e.Interaction.CreateResponseAsync(
//                 DiscordInteractionResponseType.ChannelMessageWithSource,
//                 new DiscordInteractionResponseBuilder()
//                     .WithContent("An error occurred while processing your request.")
//                     .AsEphemeral());
//         }
//     }

//     /// <summary>
//     /// Handles accept challenge button clicks
//     /// </summary>
//     private static async Task HandleAcceptChallengeAsync(ComponentInteractionCreatedEventArgs e)
//     {
//         await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredChannelMessageWithSource);

//         try
//         {
//             // Extract scrimmage ID from button custom ID
//             var scrimmageId = e.Interaction.Data.CustomId.Replace("accept_challenge_", "");

//             // Get the user's team (this would need to be implemented based on your team system)
//             var userId = e.User.Id.ToString();
//             var userTeam = await GetUserTeamAsync(userId);

//             if (string.IsNullOrEmpty(userTeam))
//             {
//                 await e.Interaction.EditOriginalResponseAsync(
//                     new DiscordWebhookBuilder()
//                         .WithContent("❌ You must be a member of a team to accept challenges."));
//                 return;
//             }

//             // Call business logic to accept the challenge
//             if (!Guid.TryParse(scrimmageId, out var scrimmageGuid))
//             {
//                 await e.Interaction.EditOriginalResponseAsync(
//                     new DiscordWebhookBuilder()
//                         .WithContent("❌ Invalid scrimmage ID."));
//                 return;
//             }

//             var success = await ScrimmageService.AcceptChallengeAsync(scrimmageGuid, userId);

//             if (!success)
//             {
//                 await e.Interaction.EditOriginalResponseAsync(
//                     new DiscordWebhookBuilder()
//                         .WithContent("❌ Failed to accept challenge. It may have already been accepted or expired."));
//                 return;
//             }

//             // Update the original message to show accepted status
//             await UpdateChallengeMessageAsync(e, scrimmageId, "accepted");

//             await e.Interaction.EditOriginalResponseAsync(
//                 new DiscordWebhookBuilder()
//                     .WithContent("✅ Challenge accepted! Scrimmage is now in progress."));
//         }
//         catch (Exception ex)
//         {
//             await DiscordErrorHandler.Instance.HandleError(ex);
//             await e.Interaction.EditOriginalResponseAsync(
//                 new DiscordWebhookBuilder()
//                     .WithContent("❌ An error occurred while accepting the challenge."));
//         }
//     }

//     /// <summary>
//     /// Handles decline challenge button clicks
//     /// </summary>
//     private static async Task HandleDeclineChallengeAsync(ComponentInteractionCreatedEventArgs e)
//     {
//         await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredChannelMessageWithSource);

//         try
//         {
//             // Extract scrimmage ID from button custom ID
//             var scrimmageId = e.Interaction.Data.CustomId.Replace("decline_challenge_", "");

//             // Get the user's team
//             var userId = e.User.Id.ToString();
//             var userTeam = await GetUserTeamAsync(userId);

//             if (string.IsNullOrEmpty(userTeam))
//             {
//                 await e.Interaction.EditOriginalResponseAsync(
//                     new DiscordWebhookBuilder()
//                         .WithContent("❌ You must be a member of a team to decline challenges."));
//                 return;
//             }

//             // Call business logic to decline the challenge
//             if (!Guid.TryParse(scrimmageId, out var scrimmageGuid))
//             {
//                 await e.Interaction.EditOriginalResponseAsync(
//                     new DiscordWebhookBuilder()
//                         .WithContent("❌ Invalid scrimmage ID."));
//                 return;
//             }

//             var success = await ScrimmageService.DeclineChallengeAsync(scrimmageGuid, userId);

//             if (!success)
//             {
//                 await e.Interaction.EditOriginalResponseAsync(
//                     new DiscordWebhookBuilder()
//                         .WithContent("❌ Failed to decline challenge. It may have already been accepted or expired."));
//                 return;
//             }

//             // Update the original message to show declined status
//             await UpdateChallengeMessageAsync(e, scrimmageId, "declined");

//             await e.Interaction.EditOriginalResponseAsync(
//                 new DiscordWebhookBuilder()
//                     .WithContent("❌ Challenge declined."));
//         }
//         catch (Exception ex)
//         {
//             await DiscordErrorHandler.Instance.HandleError(ex);
//             await e.Interaction.EditOriginalResponseAsync(
//                 new DiscordWebhookBuilder()
//                     .WithContent("❌ An error occurred while declining the challenge."));
//         }
//     }

//     /// <summary>
//     /// Updates the challenge message with new status
//     /// </summary>
//     private static async Task UpdateChallengeMessageAsync(ComponentInteractionCreatedEventArgs e, string scrimmageId, string status)
//     {
//         try
//         {
//             // Create updated embed with basic status information
//             var embedBuilder = new DiscordEmbedBuilder()
//                 .WithTitle($"Scrimmage Challenge - {status.ToUpper()}")
//                 .WithTimestamp(DateTimeOffset.UtcNow);

//             switch (status.ToLower())
//             {
//                 case "accepted":
//                     embedBuilder.WithColor(DiscordColor.Green);
//                     embedBuilder.AddField("Status", "✅ Challenge Accepted", true);
//                     embedBuilder.AddField("Next Steps", "Map bans and deck submissions will begin shortly.", false);
//                     break;
//                 case "declined":
//                     embedBuilder.WithColor(DiscordColor.Red);
//                     embedBuilder.AddField("Status", "❌ Challenge Declined", true);
//                     embedBuilder.AddField("Result", "This scrimmage has been cancelled.", false);
//                     break;
//             }

//             // Create new message builder with updated embed and no buttons (since challenge is resolved)
//             var messageBuilder = new DiscordWebhookBuilder()
//                 .AddEmbed(embedBuilder);

//             await e.Interaction.EditOriginalResponseAsync(messageBuilder);
//         }
//         catch (Exception ex)
//         {
//             await DiscordErrorHandler.Instance.HandleError(ex);
//         }
//     }

//     /// <summary>
//     /// Handles submit map bans button clicks
//     /// </summary>
//     private static async Task HandleSubmitMapBansAsync(ComponentInteractionCreatedEventArgs e)
//     {
//         try
//         {
//             // Extract scrimmage ID and team ID from button custom ID
//             var customId = e.Interaction.Data.CustomId;
//             var parts = customId.Replace("submit_map_bans_", "").Split('_');
//             var scrimmageId = parts[0];
//             var teamId = parts[1];

//             // TODO: Get available maps from the scrimmage/match
//             var availableMaps = new List<string> { "Map 1", "Map 2", "Map 3", "Map 4", "Map 5", "Map 6", "Map 7", "Map 8", "Map 9" };

//             // Create and show the map bans dropdown
//             var dropdown = ScrimmageComponentBuilders.CreateMapBansDropdown(scrimmageId, teamId, availableMaps);

//             await e.Interaction.CreateResponseAsync(
//                 DiscordInteractionResponseType.ChannelMessageWithSource,
//                 new DiscordInteractionResponseBuilder()
//                     .WithContent("Select maps to ban:")
//                     .AddActionRowComponent(dropdown)
//                     .AsEphemeral());
//         }
//         catch (Exception ex)
//         {
//             await DiscordErrorHandler.Instance.HandleError(ex);
//             await e.Interaction.CreateResponseAsync(
//                 DiscordInteractionResponseType.ChannelMessageWithSource,
//                 new DiscordInteractionResponseBuilder()
//                     .WithContent("❌ An error occurred while opening the map bans modal.")
//                     .AsEphemeral());
//         }
//     }

//     /// <summary>
//     /// Handles submit deck button clicks
//     /// </summary>
//     private static async Task HandleSubmitDeckAsync(ComponentInteractionCreatedEventArgs e)
//     {
//         try
//         {
//             // Extract scrimmage ID and team ID from button custom ID
//             var customId = e.Interaction.Data.CustomId;
//             var parts = customId.Replace("submit_deck_", "").Split('_');
//             var scrimmageId = parts[0];
//             var teamId = parts[1];
//             var gameNumber = int.Parse(parts[2]);

//             // For deck submission, we'll use a slash command approach or text input
//             // This would typically be handled by a slash command like /scrimmage submit_deck
//             await e.Interaction.CreateResponseAsync(
//                 DiscordInteractionResponseType.ChannelMessageWithSource,
//                 new DiscordInteractionResponseBuilder()
//                     .WithContent($"Please use `/scrimmage submit_deck` to submit your deck for Game {gameNumber}")
//                     .AsEphemeral());
//         }
//         catch (Exception ex)
//         {
//             await DiscordErrorHandler.Instance.HandleError(ex);
//             await e.Interaction.CreateResponseAsync(
//                 DiscordInteractionResponseType.ChannelMessageWithSource,
//                 new DiscordInteractionResponseBuilder()
//                     .WithContent("❌ An error occurred while opening the deck submission modal.")
//                     .AsEphemeral());
//         }
//     }

//     /// <summary>
//     /// Handles report result button clicks
//     /// </summary>
//     private static async Task HandleReportResultAsync(ComponentInteractionCreatedEventArgs e)
//     {
//         try
//         {
//             // Extract scrimmage ID from button custom ID
//             var customId = e.Interaction.Data.CustomId;
//             var parts = customId.Replace("report_result_", "").Split('_');
//             var scrimmageId = parts[0];
//             var gameNumber = int.Parse(parts[1]);

//             // TODO: Get team IDs from the scrimmage/match
//             var team1Id = "Team1"; // Placeholder
//             var team2Id = "Team2"; // Placeholder

//             // Create and show the game result dropdown
//             var dropdown = ScrimmageComponentBuilders.CreateGameResultDropdown(scrimmageId, team1Id, team2Id, gameNumber);

//             await e.Interaction.CreateResponseAsync(
//                 DiscordInteractionResponseType.ChannelMessageWithSource,
//                 new DiscordInteractionResponseBuilder()
//                     .WithContent($"Select the winner for Game {gameNumber}:")
//                     .AddActionRowComponent(dropdown)
//                     .AsEphemeral());
//         }
//         catch (Exception ex)
//         {
//             await DiscordErrorHandler.Instance.HandleError(ex);
//             await e.Interaction.CreateResponseAsync(
//                 DiscordInteractionResponseType.ChannelMessageWithSource,
//                 new DiscordInteractionResponseBuilder()
//                     .WithContent("❌ An error occurred while opening the game result modal.")
//                     .AsEphemeral());
//         }
//     }

//     /// <summary>
//     /// Handles revise deck button clicks
//     /// </summary>
//     private static async Task HandleReviseDeckAsync(ComponentInteractionCreatedEventArgs e)
//     {
//         try
//         {
//             // Extract scrimmage ID and team ID from button custom ID
//             var customId = e.Interaction.Data.CustomId;
//             var parts = customId.Replace("revise_deck_", "").Split('_');
//             var scrimmageId = parts[0];
//             var teamId = parts[1];
//             var gameNumber = int.Parse(parts[2]);

//             // TODO: Get current deck code from the scrimmage/match
//             var currentDeckCode = "Current deck code"; // Placeholder

//             // For deck revision, we'll use a slash command approach
//             await e.Interaction.CreateResponseAsync(
//                 DiscordInteractionResponseType.ChannelMessageWithSource,
//                 new DiscordInteractionResponseBuilder()
//                     .WithContent($"Please use `/scrimmage submit_deck` to revise your deck for Game {gameNumber}")
//                     .AsEphemeral());
//         }
//         catch (Exception ex)
//         {
//             await DiscordErrorHandler.Instance.HandleError(ex);
//             await e.Interaction.CreateResponseAsync(
//                 DiscordInteractionResponseType.ChannelMessageWithSource,
//                 new DiscordInteractionResponseBuilder()
//                     .WithContent("❌ An error occurred while opening the deck revision modal.")
//                     .AsEphemeral());
//         }
//     }

//     /// <summary>
//     /// Gets the team name for a user (placeholder implementation)
//     /// </summary>
//     private static async Task<string> GetUserTeamAsync(string userId)
//     {
//         // TODO: Implement actual team lookup logic
//         // This would need to query your team system to find which team the user belongs to
//         // For now, return a placeholder
//         await Task.CompletedTask;
//         return "TeamName"; // Placeholder
//     }
// }
