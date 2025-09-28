// using DSharpPlus.Entities;
// using WabbitBot.Core.Matches;
// using WabbitBot.Core.Scrimmages;
// using WabbitBot.Core.Common.Models;
// using WabbitBot.DiscBot.DSharpPlus.Generated;

// namespace WabbitBot.DiscBot.DSharpPlus.Embeds;

// /// <summary>
// /// Single dynamic embed for scrimmage matches that updates based on current state
// /// Game 1 embed handles: challenge → map bans → deck submission → game 1 results
// /// Game 2+ embeds handle: deck submission → game results
// /// </summary>
// public class ScrimmageEmbed : MatchEmbed
// {
//     protected Scrimmage Scrimmage { get; private set; } = null!;
//     public int GameNumber { get; private set; } = 1;
//     public string? ChallengerTeamName { get; private set; }
//     public string? OpponentTeamName { get; private set; }

//     public void SetScrimmage(Scrimmage scrimmage, int gameNumber = 1, string? challengerTeamName = null, string? opponentTeamName = null)
//     {
//         Scrimmage = scrimmage;
//         GameNumber = gameNumber;
//         ChallengerTeamName = challengerTeamName;
//         OpponentTeamName = opponentTeamName;
//         UpdateEmbed();
//     }

//     protected override void UpdateEmbed()
//     {
//         // Determine embed type and content based on scrimmage state and game number
//         if (Scrimmage.Status == ScrimmageStatus.Created)
//         {
//             UpdateChallengeEmbed();
//         }
//         else if (Scrimmage.Status == ScrimmageStatus.Accepted)
//         {
//             UpdateAcceptedEmbed();
//         }
//         else if (Scrimmage.Status == ScrimmageStatus.InProgress)
//         {
//             if (GameNumber == 1)
//             {
//                 UpdateGame1Embed();
//             }
//             else
//             {
//                 UpdateGameNEmbed();
//             }
//         }
//         else if (Scrimmage.Status == ScrimmageStatus.Completed)
//         {
//             UpdateCompletedEmbed();
//         }
//     }

//     private void UpdateChallengeEmbed()
//     {
//         SetTitle($"{EmbedStyling.GetScrimmageStatusEmoji(Scrimmage.Status)} Scrimmage Challenge");
//         SetDescription($"{EmbedStyling.FormatTeamName(ChallengerTeamName ?? "Challenger Team")} has challenged {EmbedStyling.FormatTeamName(OpponentTeamName ?? "Opponent Team")} to a scrimmage!");
//         SetColor(EmbedStyling.GetScrimmageStatusColor(Scrimmage.Status));

//         AddField("Game Size", EmbedStyling.FormatTeamSize(Scrimmage.TeamSize), true);
//         AddField("Challenge ID", Scrimmage.Id.ToString(), true);
//         AddField("Expires", Scrimmage.ChallengeExpiresAt?.ToString("g") ?? "Unknown", true);

//         SetFooter("The challenged team can accept or decline this challenge");
//     }

//     private void UpdateAcceptedEmbed()
//     {
//         SetTitle($"Scrimmage: {EmbedStyling.FormatTeamName(ChallengerTeamName ?? "Team 1")} vs {EmbedStyling.FormatTeamName(OpponentTeamName ?? "Team 2")}");
//         SetDescription("Challenge accepted! Preparing for match...");
//         SetColor(EmbedStyling.GetScrimmageStatusColor(Scrimmage.Status));

//         AddField("Game Size", EmbedStyling.FormatTeamSize(Scrimmage.TeamSize), true);
//         AddField("Best Of", Scrimmage.BestOf.ToString(), true);
//         AddField("Status", "Ready to start", true);

//         SetFooter("Match will begin shortly");
//     }

//     private void UpdateGame1Embed()
//     {
//         SetTitle($"Scrimmage Game 1: {EmbedStyling.FormatTeamName(ChallengerTeamName ?? "Team 1")} vs {EmbedStyling.FormatTeamName(OpponentTeamName ?? "Team 2")}");
//         SetDescription(EmbedStyling.FormatMatchProgress(Match?.CurrentState ?? MatchState.Created));
//         SetColor(EmbedStyling.GetMatchStateColor(Match?.CurrentState ?? MatchState.Created));

//         // Add team information
//         AddField("Team 1", GetTeamPlayers(1), true);
//         AddField("Team 2", GetTeamPlayers(2), true);

//         // Add stage-specific content based on match state
//         AddField("📝 Instructions", EmbedStyling.FormatStageInstructions(Match?.GetCurrentActionNeeded() ?? "Match created - waiting to start"), false);

//         // Add game-specific content based on current action
//         var currentAction = Match?.GetCurrentActionNeeded() ?? "Match created - waiting to start";
//         if (currentAction.Contains("Map banning"))
//         {
//             AddField("Map Pool", "Available maps for selection", false);
//         }
//         else if (currentAction.Contains("Deck submission"))
//         {
//             AddField("Deck Submissions", "Submit your deck for Game 1", false);
//         }
//         else if (currentAction.Contains("Game results"))
//         {
//             AddField("Game 1 Results", "Report the result of Game 1", false);
//         }
//     }

//     private void UpdateGameNEmbed()
//     {
//         SetTitle($"Scrimmage Game {GameNumber}: {EmbedStyling.FormatTeamName(ChallengerTeamName ?? "Team 1")} vs {EmbedStyling.FormatTeamName(OpponentTeamName ?? "Team 2")}");
//         SetDescription(EmbedStyling.FormatMatchProgress(GameNumber, Scrimmage.BestOf));
//         SetColor(EmbedStyling.GetMatchStateColor(Match?.CurrentState ?? MatchState.Created));

//         // Add current score
//         AddField("Current Score", GetCurrentScore(), true);
//         AddField("Game", $"Game {GameNumber} of {Scrimmage.BestOf}", true);

//         // Add stage-specific content
//         AddField("📝 Instructions", EmbedStyling.FormatStageInstructions(Match?.GetCurrentActionNeeded() ?? "Match created - waiting to start"), false);

//         var currentAction = Match?.GetCurrentActionNeeded() ?? "Match created - waiting to start";
//         if (currentAction.Contains("Deck submission"))
//         {
//             AddField($"Deck Submissions - Game {GameNumber}", "Submit your deck for this game", false);
//         }
//         else if (currentAction.Contains("Game results"))
//         {
//             AddField($"Game {GameNumber} Results", $"Report the result of Game {GameNumber}", false);
//         }
//     }

//     private void UpdateCompletedEmbed()
//     {
//         SetTitle($"Scrimmage Completed: {EmbedStyling.FormatTeamName(ChallengerTeamName ?? "Team 1")} vs {EmbedStyling.FormatTeamName(OpponentTeamName ?? "Team 2")}");
//         SetDescription($"Winner: {EmbedStyling.FormatTeamName(GetTeamName(Match?.WinnerId ?? ""))}");
//         SetColor(EmbedStyling.GetScrimmageStatusColor(Scrimmage.Status));

//         // Add final score
//         AddField("Final Score", GetFinalScore(), true);
//         AddField("Games Played", $"{GameNumber} of {Scrimmage.BestOf}", true);

//         // Add rating changes if available
//         if (Scrimmage.Status == ScrimmageStatus.Completed)
//         {
//             AddField("Team 1 Rating Change", EmbedStyling.FormatRatingChange(Scrimmage.Team1RatingChange), true);
//             AddField("Team 2 Rating Change", EmbedStyling.FormatRatingChange(Scrimmage.Team2RatingChange), true);
//         }
//     }

//     private string GetCurrentScore()
//     {
//         // This would be calculated from completed games
//         return EmbedStyling.FormatScore(0, 0); // Placeholder
//     }

//     private string GetFinalScore()
//     {
//         // This would be calculated from all games
//         return EmbedStyling.FormatScore(1, 0); // Placeholder
//     }

//     /// <summary>
//     /// Gets the current game based on the GameNumber property
//     /// </summary>
//     private Game? GetCurrentGame()
//     {
//         if (Match?.Games == null || Match.Games.Count == 0)
//         {
//             return null;
//         }

//         // Find the game that matches the current GameNumber (1-based)
//         return Match.Games.FirstOrDefault(g => g.GameNumber == GameNumber);
//     }

//     /// <summary>
//     /// Gets the appropriate buttons for the current match stage
//     /// </summary>
//     public List<DiscordComponent> GetStageButtons(string teamId)
//     {
//         var buttons = new List<DiscordComponent>();

//         if (Match == null) return buttons;

//         var currentAction = Match.GetCurrentActionNeeded();

//         if (currentAction.Contains("Map banning"))
//         {
//             // Add submit map bans button
//             buttons.Add(new DiscordButtonComponent(
//                 DiscordButtonStyle.Primary,
//                 $"submit_map_bans_{Scrimmage.Id}_{teamId}",
//                 "Submit Map Bans"));
//         }
//         else if (currentAction.Contains("Deck submission"))
//         {
//             // Add submit deck button
//             var currentGame = GetCurrentGame();
//             if (currentGame != null)
//             {
//                 buttons.Add(new DiscordButtonComponent(
//                     DiscordButtonStyle.Primary,
//                     $"submit_deck_{Scrimmage.Id}_{teamId}_{currentGame.GameNumber}",
//                     $"Submit Deck for Game {currentGame.GameNumber}"));
//             }
//         }
//         else if (currentAction.Contains("Deck revision"))
//         {
//             // Add confirm and revise deck buttons
//             var game = GetCurrentGame();
//             if (game != null)
//             {
//                 buttons.Add(new DiscordButtonComponent(
//                     DiscordButtonStyle.Success,
//                     $"confirm_deck_{Scrimmage.Id}_{teamId}_{game.GameNumber}",
//                     "Confirm Deck"));
//                 buttons.Add(new DiscordButtonComponent(
//                     DiscordButtonStyle.Secondary,
//                     $"revise_deck_{Scrimmage.Id}_{teamId}_{game.GameNumber}",
//                     "Revise Deck"));
//             }
//         }
//         else if (currentAction.Contains("Game results"))
//         {
//             // Add report result button
//             var currentGameForResult = GetCurrentGame();
//             if (currentGameForResult != null)
//             {
//                 buttons.Add(new DiscordButtonComponent(
//                     DiscordButtonStyle.Primary,
//                     $"report_result_{Scrimmage.Id}_{currentGameForResult.GameNumber}",
//                     $"Report Game {currentGameForResult.GameNumber} Result"));
//             }
//         }

//         return buttons;
//     }
// }