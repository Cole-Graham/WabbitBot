using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using WabbitBot.Core.Scrimmages;
using WabbitBot.Core.Scrimmages.Data;
using WabbitBot.Core.Common.Services;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Data;
using WabbitBot.DiscBot.DiscBot.Base;
using WabbitBot.DiscBot.DiscBot.ErrorHandling;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.DiscBot.DiscBot.Events;
using WabbitBot.DiscBot.DSharpPlus.Services;
using WabbitBot.DiscBot.DiscBot.Services;

namespace WabbitBot.DiscBot.DiscBot.Events;

/// <summary>
/// Discord event handler for scrimmage-related events
/// </summary>
[GenerateEventHandler(EventBusType = EventBusType.DiscBot, EnableMetrics = true, EnableErrorHandling = true, EnableLogging = true)]
public partial class ScrimmageDiscordEventHandler : DiscordBaseHandler
{
    private IDiscordScrimmageOperations? _discordOperations;
    private IMatchDiscordOperations? _matchOperations;
    private readonly ScrimmageCache _scrimmageCache;
    private readonly ScrimmageRepository _scrimmageRepository;
    private readonly TeamService _teamService;
    private readonly UserService _userService;

    public ScrimmageDiscordEventHandler()
        : base(DiscordEventBus.Instance)
    {
        // Initialize Core services - DataServiceManager is already initialized in Program.cs (Core project)
        _scrimmageCache = new ScrimmageCache();
        _scrimmageRepository = new ScrimmageRepository(DataServiceManager.DatabaseConnection);
        _teamService = new TeamService();
        _userService = new UserService();
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Subscribe to scrimmage events
        EventBus.Subscribe<ScrimmageAcceptedEvent>(HandleScrimmageAcceptedAsync);
        EventBus.Subscribe<ScrimmageDeclinedEvent>(HandleScrimmageDeclinedAsync);
        EventBus.Subscribe<ScrimmageCompletedEvent>(HandleScrimmageCompletedAsync);
    }

    /// <summary>
    /// Initializes the operations with the DiscordClient from the provider
    /// </summary>
    private void EnsureOperationsInitialized()
    {
        if (_discordOperations == null || _matchOperations == null)
        {
            var client = DiscordClientProvider.GetClient();
            _discordOperations = new DSharpPlusScrimmageEventHandler(client);
            _matchOperations = new DSharpPlusMatchEventHandler(null!); // TODO: Get MatchService from provider
        }
    }


    /// <summary>
    /// Handles scrimmage accepted events from the core system
    /// </summary>
    [EventHandler(EventType = "ScrimmageAcceptedEvent")]
    public async Task HandleScrimmageAcceptedAsync(ScrimmageAcceptedEvent @event)
    {
        try
        {
            EnsureOperationsInitialized();

            // Get scrimmage data to access additional information
            var scrimmage = await GetScrimmageAsync(@event.ScrimmageId);
            if (scrimmage == null)
            {
                Console.WriteLine($"[Discord] Scrimmage not found: {@event.ScrimmageId}");
                return;
            }

            Console.WriteLine($"[Discord] Scrimmage Accepted: {@event.ScrimmageId}");

            // Find and update the challenge message (we don't have the specific user who accepted, so use generic message)
            await _discordOperations!.UpdateScrimmageMessageAsync(@event.ScrimmageId, "accepted", "Team Member");

            // Create match threads for the accepted scrimmage
            await CreateMatchThreadsForScrimmageAsync(@event);

            // Send notification to team members
            await _discordOperations.NotifyTeamMembersAsync(@event.ScrimmageId, "accepted");

            // Update thread status
            await _discordOperations.UpdateThreadStatusAsync(@event.ScrimmageId, "accepted");
        }
        catch (Exception ex)
        {
            await DiscordErrorHandler.Instance.HandleError(ex);
        }
    }

    /// <summary>
    /// Handles scrimmage declined events from the core system
    /// </summary>
    [EventHandler(EventType = "ScrimmageDeclinedEvent")]
    public async Task HandleScrimmageDeclinedAsync(ScrimmageDeclinedEvent @event)
    {
        try
        {
            EnsureOperationsInitialized();

            // Use declined by information from event, fallback to database if not provided
            var declinedBy = @event.DeclinedBy ?? "Team Member"; // Fallback for database lookup if needed

            Console.WriteLine($"[Discord] Scrimmage Declined: {@event.ScrimmageId} by {declinedBy}");

            // Find and update the challenge message
            await _discordOperations!.UpdateScrimmageMessageAsync(@event.ScrimmageId, "declined", declinedBy);

            // Send notification to team members
            await _discordOperations.NotifyTeamMembersAsync(@event.ScrimmageId, "declined");

            // Archive the thread since challenge was declined
            await _discordOperations.ArchiveThreadAsync(@event.ScrimmageId, "Challenge declined");
        }
        catch (Exception ex)
        {
            await DiscordErrorHandler.Instance.HandleError(ex);
        }
    }

    /// <summary>
    /// Handles scrimmage completed events from the core system
    /// </summary>
    [EventHandler(EventType = "ScrimmageCompletedEvent")]
    public async Task HandleScrimmageCompletedAsync(ScrimmageCompletedEvent @event)
    {
        try
        {
            EnsureOperationsInitialized();

            // Fetch scrimmage data from repository since event is now ID-only
            var scrimmage = await _scrimmageCache.GetAsync(@event.ScrimmageId) ??
                           await _scrimmageRepository.GetByIdAsync(@event.ScrimmageId);

            if (scrimmage == null)
            {
                Console.WriteLine($"[Discord] Scrimmage not found: {@event.ScrimmageId}");
                return;
            }

            var winner = scrimmage.Team1Score > scrimmage.Team2Score ? scrimmage.Team1Id : scrimmage.Team2Id;
            Console.WriteLine($"[Discord] Scrimmage Completed: {@event.ScrimmageId} - Winner: {winner}");

            // Create final results embed - recreate event from scrimmage data
            var completedEvent = new ScrimmageCompletedEvent
            {
                ScrimmageId = scrimmage.Id.ToString(),
                MatchId = scrimmage.Match?.Id.ToString() ?? string.Empty
            };
            await _discordOperations!.CreateFinalResultsEmbedAsync(completedEvent);

            // Send completion notifications
            await _discordOperations.NotifyTeamMembersAsync(@event.ScrimmageId, "completed");

            // Archive the thread after a delay to allow players to see results
            await _discordOperations.ArchiveThreadAsync(@event.ScrimmageId, "Scrimmage completed", delayMinutes: 5);
        }
        catch (Exception ex)
        {
            await DiscordErrorHandler.Instance.HandleError(ex);
        }
    }

    /// <summary>
    /// Creates match threads for an accepted scrimmage
    /// </summary>
    private async Task CreateMatchThreadsForScrimmageAsync(ScrimmageAcceptedEvent @event)
    {
        try
        {
            // Look up scrimmage data from cache first, then repository
            var scrimmage = await GetScrimmageAsync(@event.ScrimmageId);
            if (scrimmage == null)
            {
                Console.WriteLine($"[Discord] Scrimmage not found: {@event.ScrimmageId}");
                return;
            }

            // Get team names from TeamService
            var team1 = await _teamService.GetByIdAsync(scrimmage.Team1Id);
            var team2 = await _teamService.GetByIdAsync(scrimmage.Team2Id);
            var team1Name = team1?.Name ?? $"Team {scrimmage.Team1Id}";
            var team2Name = team2?.Name ?? $"Team {scrimmage.Team2Id}";
            var evenTeamFormat = scrimmage.EvenTeamFormat.ToString();

            // Get Discord user IDs for team members
            var team1MemberIds = await GetDiscordUserIdsAsync(scrimmage.Team1RosterIds);
            var team2MemberIds = await GetDiscordUserIdsAsync(scrimmage.Team2RosterIds);

            // Get the match ID from the scrimmage
            var matchId = scrimmage.Match?.Id.ToString() ?? scrimmage.Id.ToString();

            // Create the match threads
            var (channelId, team1ThreadId, team2ThreadId) = await _discordOperations!.CreateMatchThreadsAsync(
                matchId,
                team1Name,
                team2Name,
                evenTeamFormat,
                team1MemberIds,
                team2MemberIds
            );

            // Update the Match entity with the channel and thread IDs
            await _matchOperations!.UpdateMatchDiscordInfoAsync(matchId, channelId, team1ThreadId, team2ThreadId);

            Console.WriteLine($"[Discord] Created match threads - Channel: {channelId}, Team1 Thread: {team1ThreadId}, Team2 Thread: {team2ThreadId}");
        }
        catch (Exception ex)
        {
            await DiscordErrorHandler.Instance.HandleError(ex);
        }
    }

    /// <summary>
    /// Gets scrimmage data using cache-first strategy
    /// </summary>
    private async Task<Scrimmage?> GetScrimmageAsync(string scrimmageId)
    {
        if (!Guid.TryParse(scrimmageId, out var scrimmageGuid))
            return null;

        // Try cache first
        var cached = await _scrimmageCache!.GetScrimmageAsync(scrimmageGuid);
        if (cached != null)
            return cached;

        // Fallback to repository
        return await _scrimmageRepository!.GetScrimmageAsync(scrimmageId);
    }

    /// <summary>
    /// Converts player IDs to Discord user IDs
    /// </summary>
    private async Task<List<ulong>> GetDiscordUserIdsAsync(List<string> playerIds)
    {
        var discordIds = new List<ulong>();

        foreach (var playerId in playerIds)
        {
            try
            {
                // Get all users and find the one with matching PlayerId
                var users = await _userService.GetAllAsync();
                var user = users.FirstOrDefault(u => u.PlayerId == playerId);

                if (user != null && ulong.TryParse(user.DiscordId, out var discordId))
                {
                    discordIds.Add(discordId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Discord] Failed to get Discord ID for player {playerId}: {ex.Message}");
            }
        }

        return discordIds;
    }

}
