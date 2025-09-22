using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data.Interface
{
    public interface IPlayerArchive : IArchive<Player>
    {
        Task<IEnumerable<Player>> GetArchivedPlayersByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Player>> GetArchivedPlayersByTeamIdAsync(string teamId);
        Task<IEnumerable<Player>> GetArchivedPlayersByInactivityPeriodAsync(TimeSpan inactivityPeriod);
    }
}