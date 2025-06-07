using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Tournaments
{
    public class TournamentStateMachine
    {
        private readonly ICoreEventBus _eventBus;
        private readonly Dictionary<Guid, Tournament> _currentState;
        private readonly Dictionary<Guid, List<TournamentEvent>> _eventStreams;

        public TournamentStateMachine(ICoreEventBus eventBus)
        {
            _eventBus = eventBus;
            _currentState = new Dictionary<Guid, Tournament>();
            _eventStreams = new Dictionary<Guid, List<TournamentEvent>>();

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

        public IEnumerable<TournamentEvent> GetEventStream(Guid tournamentId)
        {
            return _eventStreams.TryGetValue(tournamentId, out var events) ? events : Array.Empty<TournamentEvent>();
        }

        public async Task<Tournament> ReplayEventsAsync(Guid tournamentId, int targetVersion = -1)
        {
            if (!_eventStreams.ContainsKey(tournamentId))
                return null;

            var state = new Tournament { Id = tournamentId };
            var events = _eventStreams[tournamentId];

            foreach (var evt in events)
            {
                if (targetVersion != -1 && evt.Version > targetVersion)
                    break;

                state = ApplyEvent(state, evt);
            }

            return state;
        }

        private void StoreEvent(TournamentEvent evt)
        {
            if (!_eventStreams.ContainsKey(evt.TournamentId))
            {
                _eventStreams[evt.TournamentId] = new List<TournamentEvent>();
            }

            evt.Version = _eventStreams[evt.TournamentId].Count + 1;
            evt.Timestamp = DateTime.UtcNow;
            _eventStreams[evt.TournamentId].Add(evt);
        }

        private Tournament ApplyEvent(Tournament state, TournamentEvent evt)
        {
            switch (evt)
            {
                case TournamentCreatedEvent created:
                    return created.Tournament;

                case TournamentUpdatedEvent updated:
                    return updated.After;

                case TournamentStatusChangedEvent statusChanged:
                    var newState = state.Clone();
                    newState.Status = statusChanged.NewStatus;
                    newState.UpdatedAt = evt.Timestamp;
                    newState.Version = evt.Version;
                    return newState;

                case TournamentDeletedEvent:
                    return null;

                default:
                    return state;
            }
        }

        private Task HandleCreated(TournamentCreatedEvent evt)
        {
            StoreEvent(evt);
            _currentState[evt.TournamentId] = evt.Tournament;
            return Task.CompletedTask;
        }

        private Task HandleUpdated(TournamentUpdatedEvent evt)
        {
            StoreEvent(evt);
            _currentState[evt.TournamentId] = evt.After;
            return Task.CompletedTask;
        }

        private Task HandleStatusChanged(TournamentStatusChangedEvent evt)
        {
            StoreEvent(evt);
            if (_currentState.TryGetValue(evt.TournamentId, out var state))
            {
                var newState = state.Clone();
                newState.Status = evt.NewStatus;
                newState.UpdatedAt = evt.Timestamp;
                newState.Version = evt.Version;
                _currentState[evt.TournamentId] = newState;
            }
            return Task.CompletedTask;
        }

        private Task HandleDeleted(TournamentDeletedEvent evt)
        {
            StoreEvent(evt);
            _currentState.Remove(evt.TournamentId);
            return Task.CompletedTask;
        }
    }
}
