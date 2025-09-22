using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Tournaments.Data;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Common.Attributes;

namespace WabbitBot.Core.Tournaments
{
    [GenerateEventSubscriptions(EnableMetrics = true, EnableErrorHandling = true, EnableLogging = true)]
    public partial class TournamentHandler : CoreHandler
    {
        private readonly TournamentStateMachine _stateMachine;
        private readonly TournamentService _tournamentService;

        public static TournamentHandler Instance { get; } = new();

        private TournamentHandler()
            : base(CoreEventBus.Instance)
        {
            _stateMachine = new TournamentStateMachine(CoreEventBus.Instance);
            _tournamentService = new TournamentService();
        }

        public override Task InitializeAsync()
        {
            // Register auto-generated event subscriptions
            RegisterEventSubscriptions();
            return Task.CompletedTask;
        }

        public async Task<Tournament> CreateTournamentAsync(
            string name,
            string description,
            EvenTeamFormat evenTeamFormat,
            DateTime startDate,
            int maxParticipants,
            int bestOf,
            string userId)
        {
            // Use TournamentService for business logic
            var result = await _tournamentService.CreateTournamentAsync(
                name, description, evenTeamFormat, startDate, maxParticipants, bestOf);

            if (!result.Success)
                throw new InvalidOperationException(result.ErrorMessage);

            var tournament = result.Data;
            if (tournament == null)
                throw new InvalidOperationException("Tournament data is null despite successful result");

            // Create initial state snapshot
            var initialState = new TournamentStateSnapshot
            {
                TournamentId = tournament.Id,
                UserId = userId,
                PlayerName = "System", // TODO: Get actual player name
                Name = name,
                Description = description,
                StartDate = startDate,
                MaxParticipants = maxParticipants
            };

            // Update tournament state
            await _tournamentService.UpdateTournamentStateAsync(tournament.Id, initialState);

            // Publish event
            var evt = new TournamentCreatedEvent
            {
                TournamentId = tournament.Id.ToString(),
                UserId = userId
            };

            await EventBus.PublishAsync(evt);
            return tournament;
        }

        public async Task UpdateTournamentAsync(
            Guid tournamentId,
            string? name = null,
            string? description = null,
            DateTime? startDate = null,
            int? maxParticipants = null,
            int? bestOf = null,
            string userId = "")
        {
            // Use TournamentService for business logic
            var result = await _tournamentService.UpdateTournamentAsync(
                tournamentId, name, description, startDate, maxParticipants, bestOf);

            if (!result.Success)
                throw new InvalidOperationException(result.ErrorMessage);

            var tournament = result.Data;

            // Publish event
            var evt = new TournamentUpdatedEvent
            {
                TournamentId = tournamentId.ToString(),
                ChangedProperties = new[] { "Updated" }, // TODO: Track actual changes
                UserId = userId
            };

            await EventBus.PublishAsync(evt);
        }

        public async Task ChangeTournamentStatusAsync(
            Guid tournamentId,
            TournamentStateSnapshot newStateSnapshot,
            string reason,
            string userId)
        {
            // Use TournamentService for state management
            var result = await _tournamentService.UpdateTournamentStateAsync(tournamentId, newStateSnapshot);

            if (!result.Success)
                throw new InvalidOperationException(result.ErrorMessage);

            if (result.Data == null)
                throw new InvalidOperationException("Tournament data is null despite successful result");

            // Publish event
            var evt = new TournamentStatusChangedEvent
            {
                TournamentId = tournamentId.ToString(),
                OldStatus = result.Data.StateHistory.Count > 1
                    ? result.Data.StateHistory[result.Data.StateHistory.Count - 2].GetType().Name
                    : "Unknown",
                NewStatus = newStateSnapshot.GetType().Name,
                Reason = reason,
                UserId = userId
            };

            await EventBus.PublishAsync(evt);
        }

        public async Task DeleteTournamentAsync(
            Guid tournamentId,
            string reason,
            string userId)
        {
            // Get current state for validation
            var currentState = _stateMachine.GetCurrentState(tournamentId);
            if (currentState == null)
                throw new InvalidOperationException("Tournament not found");

            // Publish event (actual deletion logic would be in TournamentService)
            var evt = new TournamentDeletedEvent
            {
                TournamentId = tournamentId.ToString(),
                Reason = reason,
                UserId = userId
            };

            await EventBus.PublishAsync(evt);
        }


        private string[] GetChangedProperties(Tournament before, Tournament after)
        {
            var changes = new List<string>();

            if (before.Name != after.Name) changes.Add(nameof(Tournament.Name));
            if (before.Description != after.Description) changes.Add(nameof(Tournament.Description));
            if (before.StartDate != after.StartDate) changes.Add(nameof(Tournament.StartDate));
            if (before.EndDate != after.EndDate) changes.Add(nameof(Tournament.EndDate));
            if (before.MaxParticipants != after.MaxParticipants) changes.Add(nameof(Tournament.MaxParticipants));

            return changes.ToArray();
        }
    }
}
