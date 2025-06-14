using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using WabbitBot.Common.Models;
using WabbitBot.Core.Tournaments;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Tournaments.Data
{
    public class TournamentListWrapper : BaseEntity
    {
        private readonly ConcurrentDictionary<string, Tournament> _tournaments = new();
        public DateTime LastUpdated { get; set; }
        public bool IncludeInactive { get; set; }
        public GameSize? FilterByGameSize { get; set; }

        public IReadOnlyDictionary<string, Tournament> Tournaments => _tournaments;

        public TournamentListWrapper()
        {
            LastUpdated = DateTime.UtcNow;
            IncludeInactive = false;
        }

        public void AddTournament(Tournament tournament)
        {
            _tournaments.TryAdd(tournament.Id.ToString(), tournament);
            LastUpdated = DateTime.UtcNow;
        }

        public bool TryGetTournament(string tournamentId, out Tournament? tournament)
        {
            return _tournaments.TryGetValue(tournamentId, out tournament);
        }

        public bool RemoveTournament(string tournamentId)
        {
            var result = _tournaments.TryRemove(tournamentId, out _);
            if (result)
            {
                LastUpdated = DateTime.UtcNow;
            }
            return result;
        }

        public IEnumerable<Tournament> GetTournamentsByStatus(TournamentStatus status)
        {
            return _tournaments.Values.Where(t => t.Status == status);
        }

        public IEnumerable<Tournament> GetTournamentsByGameSize(GameSize gameSize)
        {
            return _tournaments.Values.Where(t => t.GameSize == gameSize);
        }

        public IEnumerable<Tournament> GetUpcomingTournaments()
        {
            return _tournaments.Values
                .Where(t => t.Status == TournamentStatus.Registration)
                .OrderBy(t => t.StartDate);
        }

        public IEnumerable<Tournament> GetActiveTournaments()
        {
            return _tournaments.Values
                .Where(t => t.Status == TournamentStatus.InProgress)
                .OrderBy(t => t.StartDate);
        }

        public IEnumerable<Tournament> GetFilteredTournaments()
        {
            var query = _tournaments.Values.AsEnumerable();

            if (!IncludeInactive)
            {
                query = query.Where(t => t.Status != TournamentStatus.Cancelled);
            }

            if (FilterByGameSize.HasValue)
            {
                query = query.Where(t => t.GameSize == FilterByGameSize.Value);
            }

            return query.OrderBy(t => t.StartDate);
        }

        public IEnumerable<Tournament> GetTournamentsByDateRange(DateTime startDate, DateTime endDate)
        {
            return _tournaments.Values
                .Where(t => t.StartDate >= startDate && t.StartDate <= endDate)
                .OrderBy(t => t.StartDate);
        }

        public IEnumerable<Tournament> GetTournamentsByMaxParticipants(int maxParticipants)
        {
            return _tournaments.Values
                .Where(t => t.MaxParticipants <= maxParticipants)
                .OrderBy(t => t.StartDate);
        }

        public IEnumerable<Tournament> SearchTournaments(string searchTerm)
        {
            searchTerm = searchTerm.ToLower();
            return _tournaments.Values
                .Where(t => t.Name.ToLower().Contains(searchTerm) ||
                           t.Description.ToLower().Contains(searchTerm))
                .OrderBy(t => t.StartDate);
        }
    }
}