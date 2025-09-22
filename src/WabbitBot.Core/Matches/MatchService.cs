using System;
using System.Threading.Tasks;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.ErrorHandling;
using WabbitBot.Core.Matches.Data;
using WabbitBot.Core.Matches.Data.Interface;
using static WabbitBot.Core.Matches.MatchStateMachine;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Services;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Events;
using WabbitBot.Core.Common.Data.Interface;
using WabbitBot.Core.Common;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Attributes;

namespace WabbitBot.Core.Matches
{
    /// <summary>
    /// Service for handling match-related business logic.
    /// </summary>
    [GenerateEventPublisher(EventBusType = EventBusType.Core, EnableValidation = true, EnableTimestamps = true)]
    public partial class MatchService : CoreService, ICoreDataService<Match>
    {
        private readonly MatchStateMachine _stateMachine;

        public MatchService()
            : base(CoreEventBus.Instance, CoreErrorHandler.Instance)
        {
            _stateMachine = new MatchStateMachine();
        }

        private IMatchRepository MatchRepository =>
            WabbitBot.Core.Common.Data.DataServiceManager.MatchRepository;

        private IMatchCache MatchCache =>
            WabbitBot.Core.Common.Data.DataServiceManager.MatchCache;

        private IGameRepository GameRepository =>
            WabbitBot.Core.Common.Data.DataServiceManager.GameRepository;

        private IGameCache GameCache =>
            WabbitBot.Core.Common.Data.DataServiceManager.GameCache;

        private IMatchArchive MatchArchive =>
            WabbitBot.Core.Common.Data.DataServiceManager.MatchArchive;


        /// <summary>
        /// Adds a new match to the state machine
        /// </summary>
        public void AddMatch(Match match)
        {
            // Capture the initial state snapshot
            if (match.CurrentStateSnapshot != null)
            {
                _stateMachine.CaptureStateSnapshot(match.CurrentStateSnapshot);
            }
        }

        /// <summary>
        /// Gets a match by ID
        /// </summary>
        public Match? GetMatch(Guid matchId)
        {
            return _stateMachine.GetCurrentMatch(matchId);
        }

        /// <summary>
        /// Updates a match in the state machine
        /// </summary>
        public void UpdateMatch(Match match)
        {
            _stateMachine.UpdateMatch(match);
        }

        /// <summary>
        /// Checks if a team has any active matches
        /// </summary>
        public async Task<bool> HasActiveMatchesAsync(string teamId)
        {
            try
            {
                // Get all matches for the team from repository
                var teamMatches = await MatchRepository.GetMatchesByTeamIdAsync(teamId);

                // Check if any are in progress
                return teamMatches.Any(match => match.CurrentState == MatchState.InProgress);
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex, $"Failed to check active matches for team {teamId}");
                // Return false to be safe - assume no active matches if we can't check
                return false;
            }
        }

        /// <summary>
        /// Gets the default map pool for matches from MapService
        /// </summary>
        private List<string> GetDefaultMapPool()
        {
            var maps = new MapService().GetMaps();
            return maps.Select(m => m.Name).ToList();
        }

        /// <summary>
        /// Starts a match with full business logic orchestration
        /// </summary>
        public async Task<bool> StartMatchAsync(Match match, string userId, string playerName)
        {
            try
            {
                // Validate match can be started
                if (match.CurrentStateSnapshot != null)
                    throw new InvalidOperationException("Match can only be started when in Created state");

                // Set start time
                match.StartedAt = DateTime.UtcNow;

                // Initialize available maps from MapService
                match.AvailableMaps = GetDefaultMapPool();

                // Create the first game
                var game = new Game
                {
                    MatchId = match.Id.ToString(),
                    EvenTeamFormat = match.EvenTeamFormat,
                    Team1PlayerIds = match.Team1PlayerIds,
                    Team2PlayerIds = match.Team2PlayerIds,
                    GameNumber = 1
                };
                match.Games.Add(game);

                // Create initial state snapshot
                var snapshot = new MatchStateSnapshot
                {
                    MatchId = match.Id,
                    StartedAt = match.StartedAt.Value,
                    CurrentGameNumber = 1,
                    AvailableMaps = match.AvailableMaps,
                    Team1MapBans = match.Team1MapBans,
                    Team2MapBans = match.Team2MapBans,
                    Team1BansSubmitted = match.Team1MapBansSubmittedAt.HasValue,
                    Team2BansSubmitted = match.Team2MapBansSubmittedAt.HasValue,
                    FinalMapPool = match.AvailableMaps.Except(match.Team1MapBans).Except(match.Team2MapBans).ToList()
                };

                // Capture state snapshot
                _stateMachine.CaptureStateSnapshot(snapshot);
                match.CurrentStateSnapshot = snapshot;
                match.StateHistory.Add(snapshot);

                // Publish event for Discord integration
                await EventBus.PublishAsync(new MatchStartedEvent
                {
                    MatchId = match.Id.ToString(),
                    StartedAt = match.StartedAt.Value,
                    GameId = game.Id.ToString()
                });

                return true;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return false;
            }
        }



        /// <summary>
        /// Cancels a match
        /// </summary>
        public async Task<bool> CancelMatchAsync(Guid matchId, string userId, string playerName)
        {
            try
            {
                // Get the current state snapshot
                var currentSnapshot = _stateMachine.GetCurrentStateSnapshot(matchId);
                if (currentSnapshot == null)
                    return false;

                // Publish event for Discord integration
                await EventBus.PublishAsync(new MatchCancelledEvent
                {
                    MatchId = matchId.ToString(),
                    Reason = "Cancelled by user",
                    CancelledBy = userId
                });

                return true;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return false;
            }
        }

        /// <summary>
        /// Forfeits a match
        /// </summary>
        public async Task<bool> ForfeitMatchAsync(Guid matchId, string userId, string playerName)
        {
            try
            {
                // Get the current state snapshot
                var currentSnapshot = _stateMachine.GetCurrentStateSnapshot(matchId);
                if (currentSnapshot == null)
                    return false;

                // Publish event for Discord integration
                await EventBus.PublishAsync(new MatchForfeitedEvent
                {
                    MatchId = matchId.ToString(),
                    ForfeitedTeamId = string.Empty, // Will be determined by business logic
                    Reason = "Forfeited by user",
                    GameId = string.Empty
                });

                return true;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return false;
            }
        }

        /// <summary>
        /// Adds a player to a match
        /// </summary>
        public async Task<bool> AddPlayerAsync(Guid matchId, string playerId, string userId, string playerName)
        {
            try
            {
                // Get the current state snapshot
                var currentSnapshot = _stateMachine.GetCurrentStateSnapshot(matchId);
                if (currentSnapshot == null)
                    return false;

                // Publish event for Discord integration
                await EventBus.PublishAsync(new MatchPlayerJoinedEvent
                {
                    MatchId = matchId.ToString(),
                    PlayerId = playerId
                });

                return true;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return false;
            }
        }

        /// <summary>
        /// Removes a player from a match
        /// </summary>
        public async Task<bool> RemovePlayerAsync(Guid matchId, string playerId, string userId, string playerName)
        {
            try
            {
                // Get the current state snapshot
                var currentSnapshot = _stateMachine.GetCurrentStateSnapshot(matchId);
                if (currentSnapshot == null)
                    return false;

                // Publish event for Discord integration
                await EventBus.PublishAsync(new MatchPlayerLeftEvent
                {
                    MatchId = matchId.ToString(),
                    PlayerId = playerId
                });

                return true;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return false;
            }
        }

        /// <summary>
        /// Starts the next game in a match by requesting game creation from GameService
        /// </summary>
        public async Task<bool> StartNextGameAsync(Match match, string userId, string playerName)
        {
            try
            {
                // Validate match can start next game
                if (match.CurrentStateSnapshot?.GetCurrentState() != MatchState.InProgress)
                    throw new InvalidOperationException("Next game can only be started when match is in Progress");

                var currentGame = match.Games.Last();
                if (currentGame.Status != GameStatus.Completed)
                    throw new InvalidOperationException("Current game must be completed before starting next game");

                // Determine the next game number and map
                var nextGameNumber = match.Games.Count + 1;
                var nextMapId = match.CurrentStateSnapshot?.FinalMapPool?.FirstOrDefault() ?? "default_map";

                // Publish event to request game creation from GameService
                await EventBus.PublishAsync(new GameCreationRequestedEvent(
                    match.Id.ToString(),
                    nextMapId,
                    match.EvenTeamFormat,
                    match.Team1PlayerIds,
                    match.Team2PlayerIds,
                    nextGameNumber,
                    userId,
                    playerName
                ));

                return true;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return false;
            }
        }

        /// <summary>
        /// Submits map bans for a match with full business logic orchestration
        /// </summary>
        public async Task<bool> SubmitMapBansAsync(Match match, string teamId, List<string> mapBans, string userId, string playerName)
        {
            try
            {
                // Validate match state
                if (match.CurrentStateSnapshot?.GetCurrentState() != MatchState.InProgress)
                    throw new InvalidOperationException("Map bans can only be submitted when match is in Progress");

                if (match.CurrentStateSnapshot?.Team1BansSubmitted == true && match.CurrentStateSnapshot?.Team2BansSubmitted == true)
                    throw new InvalidOperationException("Map bans have already been submitted by both teams");

                if (teamId != match.Team1Id && teamId != match.Team2Id)
                    throw new ArgumentException("Team must be one of the participating teams");

                // Use Common validation for collection count
                var mapBansCountValidation = CoreValidation.ValidateCount(mapBans, "map bans", 3);
                if (!mapBansCountValidation.Success)
                    throw new ArgumentException(mapBansCountValidation.ErrorMessage ?? "Invalid number of map bans");

                // Validate that all banned maps are in the available maps list
                var invalidMaps = mapBans.Where(map => !match.AvailableMaps.Contains(map)).ToList();
                if (invalidMaps.Any())
                {
                    var invalidMapsValidation = CoreValidation.ValidateNotEmpty(invalidMaps, "invalid maps");
                    if (invalidMapsValidation.Success) // If there are invalid maps, throw error
                        throw new ArgumentException($"Invalid maps: {string.Join(", ", invalidMaps)}");
                }

                // Update team bans
                if (teamId == match.Team1Id)
                {
                    match.Team1MapBans = mapBans;
                    match.Team1MapBansSubmittedAt = DateTime.UtcNow;
                }
                else
                {
                    match.Team2MapBans = mapBans;
                    match.Team2MapBansSubmittedAt = DateTime.UtcNow;
                }

                // Check if both teams have submitted their bans
                if (match.Team1MapBansSubmittedAt.HasValue && match.Team2MapBansSubmittedAt.HasValue)
                {
                    // Create updated state snapshot with deck submission stage
                    var updatedSnapshot = new MatchStateSnapshot
                    {
                        MatchId = match.Id,
                        StartedAt = match.CurrentStateSnapshot?.StartedAt,
                        CurrentGameNumber = match.CurrentStateSnapshot?.CurrentGameNumber ?? 1,
                        Games = match.CurrentStateSnapshot?.Games ?? new List<Game>(),
                        CurrentMapId = match.CurrentStateSnapshot?.CurrentMapId,
                        AvailableMaps = match.AvailableMaps,
                        Team1MapBans = match.Team1MapBans,
                        Team2MapBans = match.Team2MapBans,
                        Team1BansSubmitted = match.Team1MapBansSubmittedAt.HasValue,
                        Team2BansSubmitted = match.Team2MapBansSubmittedAt.HasValue,
                        FinalMapPool = match.AvailableMaps.Except(match.Team1MapBans).Except(match.Team2MapBans).ToList()
                    };

                    // Capture state snapshot
                    _stateMachine.CaptureStateSnapshot(updatedSnapshot);
                    match.CurrentStateSnapshot = updatedSnapshot;
                    match.StateHistory.Add(updatedSnapshot);

                    // Publish event for Discord integration
                    await EventBus.PublishAsync(new MatchCreatedEvent
                    {
                        MatchId = match.Id.ToString(),
                        Team1Id = match.Team1Id,
                        Team2Id = match.Team2Id,
                        Team1PlayerIds = match.Team1PlayerIds,
                        Team2PlayerIds = match.Team2PlayerIds,
                        EvenTeamFormat = match.EvenTeamFormat,
                        BestOf = match.BestOf
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return false;
            }
        }



        /// <summary>
        /// Gets the current state snapshot for a match
        /// </summary>
        public MatchStateSnapshot? GetCurrentStateSnapshot(Guid matchId)
        {
            return _stateMachine.GetCurrentStateSnapshot(matchId);
        }

        /// <summary>
        /// Gets the state history for a match
        /// </summary>
        public IEnumerable<MatchStateSnapshot> GetStateHistory(Guid matchId)
        {
            return _stateMachine.GetStateHistory(matchId);
        }

        /// <summary>
        /// Checks if match victory conditions are met after a game completion
        /// </summary>
        public async Task CheckMatchVictoryConditionsAsync(string matchId, string userId, string playerName)
        {
            try
            {
                // Get the match from cache/repository
                var match = GetMatch(Guid.Parse(matchId));
                if (match == null)
                {
                    Console.WriteLine($"Match not found: {matchId}");
                    return;
                }

                // Check if match victory conditions are met
                var team1Wins = match.Games.Count(g => g.WinnerId == match.Team1Id);
                var team2Wins = match.Games.Count(g => g.WinnerId == match.Team2Id);
                var gamesToWin = (match.BestOf + 1) / 2; // Ceiling division by 2

                if (team1Wins >= gamesToWin || team2Wins >= gamesToWin)
                {
                    // Match is complete - complete the match
                    var matchWinnerId = team1Wins >= gamesToWin ? match.Team1Id : match.Team2Id;
                    await CompleteMatchAsync(match, matchWinnerId, userId, playerName);
                }
                else if (match.Games.Count >= match.BestOf)
                {
                    // All games have been played but no winner yet (shouldn't happen with proper logic)
                    Console.WriteLine($"All games played but no clear winner for match {match.Id}");
                }
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
            }
        }

        /// <summary>
        /// Completes a match when victory conditions are met
        /// </summary>
        public async Task<bool> CompleteMatchAsync(Match match, string winnerId, string userId, string playerName)
        {
            try
            {
                // Validate match state
                if (match.CurrentStateSnapshot?.GetCurrentState() != MatchState.InProgress)
                    throw new InvalidOperationException("Match can only be completed when in Progress state");

                // Validate winner
                if (winnerId != match.Team1Id && winnerId != match.Team2Id)
                    throw new ArgumentException("Winner must be one of the participating teams");

                // Set match completion
                match.WinnerId = winnerId;
                match.CompletedAt = DateTime.UtcNow;

                // Create completion state snapshot
                var snapshot = new MatchStateSnapshot
                {
                    MatchId = match.Id,
                    CompletedAt = match.CompletedAt.Value,
                    WinnerId = winnerId,
                    FinalScore = $"{match.Games.Count(g => g.WinnerId == match.Team1Id)}-{match.Games.Count(g => g.WinnerId == match.Team2Id)}",
                    FinalGames = match.Games.ToList()
                };

                // Capture state snapshot
                _stateMachine.CaptureStateSnapshot(snapshot);
                match.CurrentStateSnapshot = snapshot;
                match.StateHistory.Add(snapshot);

                // Save to repository
                await MatchRepository.UpdateAsync(match);

                // Publish event for Discord integration
                await EventBus.PublishAsync(new MatchCompletedEvent
                {
                    MatchId = match.Id.ToString(),
                    WinnerId = winnerId,
                    CompletedAt = match.CompletedAt.Value,
                    GameId = string.Empty // Will be set by the event system
                });

                return true;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return false;
            }
        }

        /// <summary>
        /// Updates a match when a new game is created
        /// </summary>
        public async Task UpdateMatchWithGameAsync(Game game)
        {
            // Find the match that this game belongs to
            var match = await MatchRepository.GetByIdAsync(game.MatchId);
            if (match is null)
            {
                await ErrorHandler.HandleErrorAsync(new InvalidOperationException($"Match not found for game: {game.Id}"));
                return;
            }

            // Add the game to the match
            match.Games.Add(game);

            // Update state snapshot
            var currentSnapshot = match.CurrentStateSnapshot;
            var updatedSnapshot = new MatchStateSnapshot
            {
                MatchId = match.Id,
                StartedAt = currentSnapshot?.StartedAt ?? DateTime.UtcNow,
                CurrentGameNumber = game.GameNumber,
                Games = match.Games,
                CurrentMapId = game.MapId,
                AvailableMaps = currentSnapshot?.AvailableMaps ?? new List<string>(),
                Team1MapBans = currentSnapshot?.Team1MapBans ?? new List<string>(),
                Team2MapBans = currentSnapshot?.Team2MapBans ?? new List<string>(),
                Team1BansSubmitted = currentSnapshot?.Team1BansSubmitted ?? false,
                Team2BansSubmitted = currentSnapshot?.Team2BansSubmitted ?? false,
                FinalMapPool = currentSnapshot?.FinalMapPool ?? new List<string>()
            };

            // Capture state snapshot
            _stateMachine.CaptureStateSnapshot(updatedSnapshot);
            match.CurrentStateSnapshot = updatedSnapshot;
            match.StateHistory.Add(updatedSnapshot);

            // Save the updated match
            await MatchRepository.UpdateAsync(match);

            // Publish GameStartedEvent for Discord integration
            await EventBus.PublishAsync(new GameStartedEvent(
                game.Id.ToString()
            ));
        }

        #region ICoreDataService<Match> Implementation

        /// <summary>
        /// Gets a match by ID
        /// </summary>
        public async Task<Match?> GetByIdAsync(object id)
        {
            try
            {
                if (id is string matchId && Guid.TryParse(matchId, out var matchGuid))
                {
                    // Try cache first
                    var cached = await MatchCache.GetMatchAsync(matchGuid);
                    if (cached != null)
                        return cached;

                    // Try repository
                    return await MatchRepository.GetByIdAsync(id);
                }
                return null;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return null;
            }
        }

        /// <summary>
        /// Gets all matches
        /// </summary>
        public async Task<IEnumerable<Match>> GetAllAsync()
        {
            try
            {
                // Get from repository (cache doesn't have GetAll method)
                var all = await MatchRepository.GetAllAsync();
                return all ?? Array.Empty<Match>();
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Array.Empty<Match>();
            }
        }

        /// <summary>
        /// Searches for matches
        /// </summary>
        public async Task<IEnumerable<Match>> SearchAsync(string searchTerm, int limit = 25)
        {
            try
            {
                // For now, return all matches (can be enhanced with proper search logic)
                var all = await GetAllAsync();
                return all.Take(limit);
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Array.Empty<Match>();
            }
        }

        /// <summary>
        /// Adds a new match
        /// </summary>
        public async Task<int> AddAsync(Match entity)
        {
            try
            {
                var result = await MatchRepository.AddAsync(entity);
                if (result > 0)
                {
                    await MatchCache.SetMatchAsync(entity);
                }
                return result;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return 0;
            }
        }

        /// <summary>
        /// Updates an existing match
        /// </summary>
        public async Task<bool> UpdateAsync(Match entity)
        {
            try
            {
                var result = await MatchRepository.UpdateAsync(entity);
                if (result)
                {
                    await MatchCache.SetMatchAsync(entity);
                }
                return result;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return false;
            }
        }

        /// <summary>
        /// Deletes a match
        /// </summary>
        public async Task<bool> DeleteAsync(object id)
        {
            try
            {
                var result = await MatchRepository.DeleteAsync(id);
                if (result && id is string matchId)
                {
                    await MatchCache.RemoveMatchAsync(matchId);
                }
                return result;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return false;
            }
        }

        /// <summary>
        /// Archives a match
        /// </summary>
        public async Task<bool> ArchiveAsync(Match entity)
        {
            try
            {
                var result = await MatchArchive.ArchiveAsync(entity);
                if (result > 0)
                {
                    await MatchCache.RemoveMatchAsync(entity.Id.ToString());
                }
                return result > 0;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return false;
            }
        }

        /// <summary>
        /// Checks if a match exists
        /// </summary>
        public async Task<bool> ExistsAsync(object id)
        {
            try
            {
                if (id is string matchId)
                {
                    // Try cache first (cache uses string IDs)
                    if (await MatchCache.MatchExistsAsync(matchId))
                        return true;

                    // Try repository
                    return await MatchRepository.ExistsAsync(id);
                }
                return false;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return false;
            }
        }

        /// <summary>
        /// Creates a new match with business logic validation
        /// </summary>
        public async Task<Result<Match>> CreateEntityAsync(Match entity)
        {
            try
            {
                // Business logic validation for match creation
                if (entity == null)
                    return Result<Match>.Failure("Match cannot be null");

                // Use Common validation for string validation
                var matchIdValidation = CoreValidation.ValidateString(entity.Id.ToString(), "Match ID", required: true);
                if (!matchIdValidation.Success)
                    return Result<Match>.Failure(matchIdValidation.ErrorMessage ?? "Invalid match ID");

                // Validate that teams exist by fetching from repository
                var team1 = await WabbitBot.Core.Common.Data.DataServiceManager.TeamRepository.GetByIdAsync(entity.Team1Id);
                var team2 = await WabbitBot.Core.Common.Data.DataServiceManager.TeamRepository.GetByIdAsync(entity.Team2Id);

                if (team1 == null)
                    return Result<Match>.Failure("Team 1 not found");

                if (team2 == null)
                    return Result<Match>.Failure("Team 2 not found");

                var result = await AddAsync(entity);
                if (result > 0)
                {
                    await PublishMatchCreatedEventAsync(entity);
                    return Result<Match>.CreateSuccess(entity);
                }

                return Result<Match>.Failure("Failed to create match");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Result<Match>.Failure($"Failed to create match: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates a match with business logic validation
        /// </summary>
        public async Task<Result<Match>> UpdateEntityAsync(Match entity)
        {
            try
            {
                // Business logic validation for match updates
                if (entity == null)
                    return Result<Match>.Failure("Match cannot be null");

                var result = await UpdateAsync(entity);
                if (result)
                {
                    await PublishMatchUpdatedEventAsync(entity);
                    return Result<Match>.CreateSuccess(entity);
                }

                return Result<Match>.Failure("Failed to update match");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Result<Match>.Failure($"Failed to update match: {ex.Message}");
            }
        }

        /// <summary>
        /// Archives a match with business logic validation
        /// </summary>
        public async Task<Result<Match>> ArchiveEntityAsync(Match entity)
        {
            try
            {
                // Business logic validation for match archiving
                if (entity == null)
                    return Result<Match>.Failure("Match cannot be null");

                // Cannot archive active matches
                if (entity.CurrentState != MatchState.Completed &&
                    entity.CurrentState != MatchState.Cancelled)
                {
                    return Result<Match>.Failure("Cannot archive active match");
                }

                var result = await ArchiveAsync(entity);
                if (result)
                {
                    await PublishMatchArchivedEventAsync(entity);
                    return Result<Match>.CreateSuccess(entity);
                }

                return Result<Match>.Failure("Failed to archive match");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Result<Match>.Failure($"Failed to archive match: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a match with business logic validation
        /// </summary>
        public async Task<Result<Match>> DeleteEntityAsync(object id)
        {
            try
            {
                // Get entity before deletion for event publishing
                var entity = await GetByIdAsync(id);
                if (entity == null)
                {
                    return Result<Match>.Failure("Match not found");
                }

                // Business logic validation for match deletion
                // Only allow deletion of completed/cancelled matches
                if (entity.CurrentState != MatchState.Completed &&
                    entity.CurrentState != MatchState.Cancelled)
                {
                    return Result<Match>.Failure("Cannot delete active match");
                }

                var result = await DeleteAsync(id);
                if (result)
                {
                    await PublishMatchDeletedEventAsync(entity);
                    return Result<Match>.CreateSuccess(entity);
                }

                return Result<Match>.Failure("Failed to delete match");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(ex);
                return Result<Match>.Failure($"Failed to delete match: {ex.Message}");
            }
        }

        #region Event Publishing Methods

        /// <summary>
        /// Publishes match created event
        /// </summary>
        protected virtual async Task PublishMatchCreatedEventAsync(Match entity)
        {
            // Publish to event bus for cross-system notifications
            await Task.CompletedTask; // Placeholder for generated event publisher
        }

        /// <summary>
        /// Publishes match updated event
        /// </summary>
        protected virtual async Task PublishMatchUpdatedEventAsync(Match entity)
        {
            // Default implementation - can be enhanced with specific update events
            await Task.CompletedTask;
        }

        /// <summary>
        /// Publishes match archived event
        /// </summary>
        protected virtual async Task PublishMatchArchivedEventAsync(Match entity)
        {
            // Default implementation - can be enhanced with archive events
            await Task.CompletedTask;
        }

        /// <summary>
        /// Publishes match deleted event
        /// </summary>
        protected virtual async Task PublishMatchDeletedEventAsync(Match entity)
        {
            // Default implementation - can be enhanced with delete events
            await Task.CompletedTask;
        }

        #endregion

        #endregion
    }
}
