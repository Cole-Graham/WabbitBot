using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data.Interface
{
    public interface IUserArchive : IArchive<User>
    {
        Task<IEnumerable<User>> GetArchivedUsersByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<User>> GetArchivedUsersByPlayerIdAsync(string playerId);
        Task<IEnumerable<User>> GetArchivedUsersByInactivityPeriodAsync(TimeSpan inactivityPeriod);
    }
}
