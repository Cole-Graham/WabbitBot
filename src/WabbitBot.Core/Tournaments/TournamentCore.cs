using System;
using System.Threading.Tasks;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Services;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.ErrorService;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Database;
using WabbitBot.Core.Common.Interfaces;

namespace WabbitBot.Core.Tournaments
{
    public partial class TournamentCore : ITournamentCore
    {
        /// <inheritdoc />
        public Task InitializeAsync() => Task.CompletedTask;

        /// <inheritdoc />
        public Task ValidateAsync() => Task.CompletedTask;

        public static class Validation { }
        public static class Accessors { }
        public static class State
        {
            private static readonly Dictionary<Guid, TournamentStateSnapshot> _currentState = new();

            static State()
            {
                CoreService.EventBus.Subscribe<TournamentStatusChangedEvent>(HandleStatusChanged);
            }

            public static bool HasState(Guid tournamentId) => _currentState.ContainsKey(tournamentId);

            public static TournamentStateSnapshot GetCurrentState(Guid tournamentId)
            {
                if (!_currentState.TryGetValue(tournamentId, out var snap))
                    throw new KeyNotFoundException($"No state found for tournament {tournamentId}");
                return snap; // or return a copied snapshot if needed
            }

            public static void AddOrUpdateTournament(Tournament tournament)
            {
                if (tournament is null) throw new ArgumentNullException(nameof(tournament));
                var snap = CreateSnapshotFromTournament(tournament);
                _currentState[tournament.Id] = snap;
            }

            public static void RemoveTournament(Guid tournamentId) => _currentState.Remove(tournamentId);

            private static Task HandleStatusChanged(TournamentStatusChangedEvent evt)
            {
                if (_currentState.TryGetValue(evt.TournamentId, out var snap))
                {
                    // Update the snapshot fields you treat as “current”
                    // Note: status lives on Tournament, so reflect it via a snapshot field of your choice
                    snap.UpdatedAt = DateTime.UtcNow;
                }
                return Task.CompletedTask;
            }

            private static TournamentStateSnapshot CreateSnapshotFromTournament(Tournament t)
            {
                return new TournamentStateSnapshot
                {
                    TournamentId = t.Id,
                    Timestamp = DateTime.UtcNow,
                    Name = t.Name,
                    Description = t.Description,
                    StartDate = t.StartDate,
                    MaxParticipants = t.MaxParticipants,
                    // Fill any other fields you consider part of “current”
                };
            }
        }
    }
}
