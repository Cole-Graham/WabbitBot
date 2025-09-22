using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.Models.Interface;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Tournaments.Data
{

    public class TournamentStateMachine
    {
        private readonly ICoreEventBus _eventBus;
        private readonly Dictionary<Guid, Tournament> _currentState;

        public TournamentStateMachine(ICoreEventBus eventBus)
        {
            _eventBus = eventBus;
            _currentState = new Dictionary<Guid, Tournament>();

            // Subscribe to tournament events
            _eventBus.Subscribe<TournamentCreatedEvent>(HandleCreated);
            _eventBus.Subscribe<TournamentUpdatedEvent>(HandleUpdated);
            _eventBus.Subscribe<TournamentStatusChangedEvent>(HandleStatusChanged);
            _eventBus.Subscribe<TournamentDeletedEvent>(HandleDeleted);
        }

        public bool HasState(Guid tournamentId)
        {
            return _currentState.ContainsKey(tournamentId);
        }

        public Tournament GetCurrentState(Guid tournamentId)
        {
            if (!_currentState.ContainsKey(tournamentId))
            {
                throw new KeyNotFoundException($"No state found for tournament {tournamentId}");
            }

            return _currentState[tournamentId].Clone();
        }

        private async Task HandleCreated(TournamentCreatedEvent evt)
        {
            // Fetch tournament data from repository since events are now ID-based
            var tournament = await WabbitBot.Core.Common.Data.DataServiceManager.TournamentRepository.GetByIdAsync(evt.TournamentId);
            if (tournament != null)
            {
                _currentState[Guid.Parse(evt.TournamentId)] = tournament;
            }
        }

        private async Task HandleUpdated(TournamentUpdatedEvent evt)
        {
            // Fetch updated tournament data from repository since events are now ID-based
            var tournament = await WabbitBot.Core.Common.Data.DataServiceManager.TournamentRepository.GetByIdAsync(evt.TournamentId);
            if (tournament != null)
            {
                _currentState[Guid.Parse(evt.TournamentId)] = tournament;
            }
        }

        private Task HandleStatusChanged(TournamentStatusChangedEvent evt)
        {
            var tournamentId = Guid.Parse(evt.TournamentId);
            if (_currentState.TryGetValue(tournamentId, out var state))
            {
                var newState = state.Clone();
                newState.UpdatedAt = evt.Timestamp;
                // State snapshot management is handled by TournamentService
                // This just updates the cached state
                _currentState[tournamentId] = newState;
            }
            return Task.CompletedTask;
        }

        private Task HandleDeleted(TournamentDeletedEvent evt)
        {
            _currentState.Remove(Guid.Parse(evt.TournamentId));
            return Task.CompletedTask;
        }
    }
}
