using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data.Interface
{
    public interface ITeamArchive : IArchive<Team>
    {
        Task<IEnumerable<Team>> GetArchivedTeamsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Team>> GetArchivedTeamsByCaptainIdAsync(string captainId);
        Task<IEnumerable<Team>> GetArchivedTeamsByEvenTeamFormatAsync(EvenTeamFormat evenTeamFormat);
    }
}