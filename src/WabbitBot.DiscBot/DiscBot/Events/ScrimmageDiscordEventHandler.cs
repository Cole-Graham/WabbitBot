// using System;
// using System.Threading.Tasks;
// using System.Collections.Generic;
// using System.Linq;
// using WabbitBot.Core.Scrimmages;
// using WabbitBot.Core.Common.Services;
// using WabbitBot.Core.Common.Models;
// using WabbitBot.DiscBot.DiscBot.Base;
// using WabbitBot.DiscBot.DiscBot.ErrorHandling;
// using WabbitBot.Common.Attributes;
// using WabbitBot.Common.Events;
// using WabbitBot.Common.Events.EventInterfaces;
// using WabbitBot.DiscBot.DSharpPlus.Services;
// using WabbitBot.DiscBot.DiscBot.Services;
// using WabbitBot.Common.Data.Service;
// using WabbitBot.Common.Models;
// using WabbitBot.Common.ErrorService;
// using WabbitBot.Common.Data.Interfaces;

// namespace WabbitBot.DiscBot.DiscBot.Events;

// /// <summary>
// /// Discord event handler for scrimmage-related events
// /// </summary>
// public partial class ScrimmageDiscordEventHandler : DiscordBaseHandler
// {
//     private IDiscordScrimmageOperations? _discordOperations;
//     private IMatchDiscordOperations? _matchOperations;
//     private readonly DatabaseService<Scrimmage> _scrimmageData;
//     private readonly DatabaseService<Team> _teamData;
//     private readonly DatabaseService<User> _userData;
//     private readonly IErrorService _errorService;

//     public ScrimmageDiscordEventHandler(IErrorService errorService)
//         : base(DiscordEventBus.Instance)
//     {
//         _errorService = errorService;
//         _scrimmageData = new DatabaseService<Scrimmage>();
//         _teamData = new DatabaseService<Team>();
//         _userData = new DatabaseService<User>();
//     }

//     public override async Task InitializeAsync()
//     {
//         await base.InitializeAsync();

//         // Subscribe to scrimmage events
//         EventBus.Subscribe<ScrimmageAcceptedEvent>(HandleScrimmageAcceptedAsync);
//         EventBus.Subscribe<ScrimmageDeclinedEvent>(HandleScrimmageDeclinedAsync);
//         EventBus.Subscribe<ScrimmageCompletedEvent>(HandleScrimmageCompletedAsync);
//     }

//     /// <summary>
//     /// Initializes the operations with the DiscordClient from the provider
//     /// </summary>
//     private void EnsureOperationsInitialized()
//     {
//         if (_discordOperations == null || _matchOperations == null)
//         {
//             var client = DiscordClientProvider.GetClient();
//             _discordOperations = new DSharpPlusScrimmageEventHandler(client);
//             _matchOperations = new DSharpPlusMatchEventHandler(null!); // TODO: Get MatchService from provider
//         }
//     }


//     /// <summary>
//     /// Handles scrimmage accepted events from the core system
//     /// </summary>
//     [EventHandler(EventType = "ScrimmageAcceptedEvent")]
//     public async Task HandleScrimmageAcceptedAsync(ScrimmageAcceptedEvent @event)
//     {
//         try
//         {
//             EnsureOperationsInitialized();

//             // TODO: Event payload should be enriched with all necessary data. This is a temporary lookup.
//             var scrimmage = await _scrimmageData.GetByIdAsync(@event.ScrimmageId, DatabaseComponent.Repository);
//             if (scrimmage == null)
//             {
//                 Console.WriteLine($"[Discord] Scrimmage not found: {@event.ScrimmageId}");
//                 return;
//             }

//             Console.WriteLine($"[Discord] Scrimmage Accepted: {@event.ScrimmageId}");

//             // Find and update the challenge message (we don't have the specific user who accepted, so use generic message)
//             await _discordOperations!.UpdateScrimmageMessageAsync(@event.ScrimmageId.ToString(), "accepted", "Team Member");

//             // Create match threads for the accepted scrimmage
//             await CreateMatchThreadsForScrimmageAsync(@event);

//             // Send notification to team members
//             await _discordOperations.NotifyTeamMembersAsync(@event.ScrimmageId.ToString(), "accepted");

//             // Update thread status
//             await _discordOperations.UpdateThreadStatusAsync(@event.ScrimmageId.ToString(), "accepted");
//         }
//         catch (Exception ex)
//         {
//             await _errorService.CaptureAsync(ex, "Failed to handle scrimmage accepted event", nameof(HandleScrimmageAcceptedAsync));
//         }
//     }

//     /// <summary>
//     /// Handles scrimmage declined events from the core system
//     /// </summary>
//     [EventHandler(EventType = "ScrimmageDeclinedEvent")]
//     public async Task HandleScrimmageDeclinedAsync(ScrimmageDeclinedEvent @event)
//     {
//         try
//         {
//             EnsureOperationsInitialized();

//             // Use declined by information from event, fallback to database if not provided
//             var declinedBy = @event.DeclinedBy ?? "Team Member"; // Fallback for database lookup if needed

//             Console.WriteLine($"[Discord] Scrimmage Declined: {@event.ScrimmageId} by {declinedBy}");

//             // Find and update the challenge message
//             await _discordOperations!.UpdateScrimmageMessageAsync(@event.ScrimmageId.ToString(), "declined", declinedBy);

//             // Send notification to team members
//             await _discordOperations.NotifyTeamMembersAsync(@event.ScrimmageId.ToString(), "declined");

//             // Archive the thread since challenge was declined
//             await _discordOperations.ArchiveThreadAsync(@event.ScrimmageId.ToString(), "Challenge declined");
//         }
//         catch (Exception ex)
//         {
//             await _errorService.CaptureAsync(ex, "Failed to handle scrimmage declined event", nameof(HandleScrimmageDeclinedAsync));
//         }
//     }

//     /// <summary>
//     /// Handles scrimmage completed events from the core system
//     /// </summary>
//     [EventHandler(EventType = "ScrimmageCompletedEvent")]
//     public async Task HandleScrimmageCompletedAsync(ScrimmageCompletedEvent @event)
//     {
//         try
//         {
//             EnsureOperationsInitialized();

//             // TODO: Event payload should be enriched with all necessary data. This is a temporary lookup.
//             var scrimmage = await _scrimmageData.GetByIdAsync(@event.ScrimmageId, DatabaseComponent.Repository);

//             if (scrimmage == null)
//             {
//                 Console.WriteLine($"[Discord] Scrimmage not found: {@event.ScrimmageId}");
//                 return;
//             }

//             var winner = scrimmage.Team1Score > scrimmage.Team2Score ? scrimmage.Team1Id : scrimmage.Team2Id;
//             Console.WriteLine($"[Discord] Scrimmage Completed: {@event.ScrimmageId} - Winner: {winner}");

//             // Create final results embed - recreate event from scrimmage data
//             var completedEvent = new ScrimmageCompletedEvent
//             {
//                 ScrimmageId = scrimmage.Id,
//                 MatchId = scrimmage.Match?.Id ?? Guid.Empty
//             };
//             await _discordOperations!.CreateFinalResultsEmbedAsync(completedEvent);

//             // Send completion notifications
//             await _discordOperations.NotifyTeamMembersAsync(@event.ScrimmageId.ToString(), "completed");

//             // Archive the thread after a delay to allow players to see results
//             await _discordOperations.ArchiveThreadAsync(@event.ScrimmageId.ToString(), "Scrimmage completed", delayMinutes: 5);
//         }
//         catch (Exception ex)
//         {
//             await _errorService.CaptureAsync(ex, "Failed to handle scrimmage completed event", nameof(HandleScrimmageCompletedAsync));
//         }
//     }

//     /// <summary>
//     /// Creates match threads for an accepted scrimmage
//     /// </summary>
//     private async Task CreateMatchThreadsForScrimmageAsync(ScrimmageAcceptedEvent @event)
//     {
//         try
//         {
//             // TODO: Event payload should be enriched with all necessary data. This is a temporary lookup.
//             var scrimmage = await _scrimmageData.GetByIdAsync(@event.ScrimmageId, DatabaseComponent.Repository);
//             if (scrimmage == null)
//             {
//                 Console.WriteLine($"[Discord] Scrimmage not found: {@event.ScrimmageId}");
//                 return;
//             }

//             // Get team names
//             var team1 = await _teamData.GetByIdAsync(scrimmage.Team1Id, DatabaseComponent.Repository);
//             var team2 = await _teamData.GetByIdAsync(scrimmage.Team2Id, DatabaseComponent.Repository);
//             var team1Name = team1?.Name ?? $"Team {scrimmage.Team1Id}";
//             var team2Name = team2?.Name ?? $"Team {scrimmage.Team2Id}";
//             var TeamSize = scrimmage.TeamSize.ToString();

//             // Get Discord user IDs for team members
//             var team1MemberIds = await GetDiscordUserIdsAsync(scrimmage.Team1RosterIds);
//             var team2MemberIds = await GetDiscordUserIdsAsync(scrimmage.Team2RosterIds);

//             // Get the match ID from the scrimmage
//             var matchId = scrimmage.Match?.Id ?? scrimmage.Id;

//             // Create the match threads
//             var (channelId, team1ThreadId, team2ThreadId) = await _discordOperations!.CreateMatchThreadsAsync(
//                 matchId.ToString(),
//                 team1Name,
//                 team2Name,
//                 TeamSize,
//                 team1MemberIds,
//                 team2MemberIds
//             );

//             // Update the Match entity with the channel and thread IDs
//             await _matchOperations!.UpdateMatchDiscordInfoAsync(matchId.ToString(), channelId, team1ThreadId, team2ThreadId);

//             Console.WriteLine($"[Discord] Created match threads - Channel: {channelId}, Team1 Thread: {team1ThreadId}, Team2 Thread: {team2ThreadId}");
//         }
//         catch (Exception ex)
//         {
//             await _errorService.CaptureAsync(ex, "Failed to create match threads", nameof(CreateMatchThreadsForScrimmageAsync));
//         }
//     }

//     /// <summary>
//     /// Converts player IDs to Discord user IDs
//     /// </summary>
//     private async Task<List<ulong>> GetDiscordUserIdsAsync(List<Guid> playerIds)
//     {
//         var discordIds = new List<ulong>();
//         if (!playerIds.Any())
//             return discordIds;

//         // TODO: This is inefficient. This lookup should be optimized or the data provided in the event.
//         var users = await _userData.GetAllAsync(DatabaseComponent.Repository);
//         if (users == null) return discordIds;

//         var userMap = users.Where(u => u.PlayerId != Guid.Empty).ToDictionary(u => u.PlayerId);

//         foreach (var playerId in playerIds)
//         {
//             try
//             {
//                 if (userMap.TryGetValue(playerId, out var user) && ulong.TryParse(user.DiscordId, out var discordId))
//                 {
//                     discordIds.Add(discordId);
//                 }
//             }
//             catch (Exception ex)
//             {
//                 await _errorService.CaptureAsync(ex, $"Failed to get Discord ID for player {playerId}", nameof(GetDiscordUserIdsAsync));
//             }
//         }

//         return discordIds;
//     }

// }
