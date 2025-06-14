using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using WabbitBot.Common.Models;
using WabbitBot.Core.Scrimmages;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Scrimmages.Data
{
    public class ScrimmageListWrapper : BaseEntity
    {
        private readonly ConcurrentDictionary<string, Scrimmage> _scrimmages = new();
        public DateTime LastUpdated { get; set; }

        public IReadOnlyDictionary<string, Scrimmage> Scrimmages => _scrimmages;

        public ScrimmageListWrapper()
        {
            LastUpdated = DateTime.UtcNow;
        }

        public void AddScrimmage(Scrimmage scrimmage)
        {
            _scrimmages.TryAdd(scrimmage.Id.ToString(), scrimmage);
            LastUpdated = DateTime.UtcNow;
        }

        public bool TryGetScrimmage(string scrimmageId, out Scrimmage? scrimmage)
        {
            return _scrimmages.TryGetValue(scrimmageId, out scrimmage);
        }

        public bool RemoveScrimmage(string scrimmageId)
        {
            var result = _scrimmages.TryRemove(scrimmageId, out _);
            if (result)
            {
                LastUpdated = DateTime.UtcNow;
            }
            return result;
        }

        public IEnumerable<Scrimmage> GetScrimmagesByStatus(ScrimmageStatus status)
        {
            return _scrimmages.Values.Where(s => s.Status == status);
        }

        public IEnumerable<Scrimmage> GetScrimmagesByTeam(string teamId)
        {
            return _scrimmages.Values.Where(s =>
                s.Team1Id == teamId || s.Team2Id == teamId);
        }

        public IEnumerable<Scrimmage> GetScrimmagesByGameSize(GameSize gameSize)
        {
            return _scrimmages.Values.Where(s => s.GameSize == gameSize);
        }
    }
}
