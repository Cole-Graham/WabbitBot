using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Tournaments
{
    public class TournamentHandler
    {
        private readonly ICoreEventBus _eventBus;
        private readonly TournamentStateMachine _stateMachine;

        public TournamentHandler(ICoreEventBus eventBus, TournamentStateMachine stateMachine)
        {
            _eventBus = eventBus;
            _stateMachine = stateMachine;
        }

        public async Task<Tournament> CreateTournamentAsync(
            string name,
            string description,
            DateTime startDate,
            int maxParticipants,
            string userId)
        {
            var tournament = new Tournament
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                StartDate = startDate,
                MaxParticipants = maxParticipants,
                Status = TournamentStatus.Created
            };

            var evt = new TournamentCreatedEvent
            {
                TournamentId = tournament.Id,
                Tournament = tournament,
                Timestamp = DateTime.UtcNow,
                Version = 1,
                UserId = userId
            };

            await _eventBus.PublishAsync(evt);
            return tournament;
        }

        public async Task UpdateTournamentAsync(
            Guid tournamentId,
            Action<Tournament> updateAction,
            string[] changedProperties,
            string userId)
        {
            var currentState = _stateMachine.GetCurrentState(tournamentId);
            if (currentState == null)
            {
                throw new InvalidOperationException($"Tournament {tournamentId} not found");
            }

            var before = currentState.Clone();
            updateAction(currentState);

            var evt = new TournamentUpdatedEvent
            {
                TournamentId = tournamentId,
                Before = before,
                After = currentState,
                ChangedProperties = changedProperties,
                Timestamp = DateTime.UtcNow,
                Version = currentState.Version + 1,
                UserId = userId
            };

            await _eventBus.PublishAsync(evt);
        }

        public async Task ChangeTournamentStatusAsync(
            Guid tournamentId,
            TournamentStatus newStatus,
            string reason,
            string userId)
        {
            var currentState = _stateMachine.GetCurrentState(tournamentId);
            if (currentState == null)
                throw new InvalidOperationException("Tournament not found");

            if (!IsValidStatusTransition(currentState.Status, newStatus))
                throw new InvalidOperationException($"Invalid status transition from {currentState.Status} to {newStatus}");

            var evt = new TournamentStatusChangedEvent
            {
                TournamentId = tournamentId,
                OldStatus = currentState.Status,
                NewStatus = newStatus,
                Reason = reason,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                Version = currentState.Version + 1
            };

            await _eventBus.PublishAsync(evt);
        }

        public async Task DeleteTournamentAsync(
            Guid tournamentId,
            string reason,
            string userId)
        {
            var currentState = _stateMachine.GetCurrentState(tournamentId);
            if (currentState == null)
                throw new InvalidOperationException("Tournament not found");

            var evt = new TournamentDeletedEvent
            {
                TournamentId = tournamentId,
                Reason = reason,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                Version = currentState.Version + 1
            };

            await _eventBus.PublishAsync(evt);
        }

        public async Task<Tournament> GetTournamentAtVersionAsync(
            Guid tournamentId,
            int version)
        {
            return await _stateMachine.ReplayEventsAsync(tournamentId, version);
        }

        private bool IsValidStatusTransition(TournamentStatus from, TournamentStatus to)
        {
            return (from, to) switch
            {
                (TournamentStatus.Created, TournamentStatus.Registration) => true,
                (TournamentStatus.Registration, TournamentStatus.InProgress) => true,
                (TournamentStatus.InProgress, TournamentStatus.Completed) => true,
                (_, TournamentStatus.Cancelled) => true,
                _ => false
            };
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
