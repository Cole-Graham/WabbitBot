using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Leaderboards;

namespace WabbitBot.Core.Leaderboards.Data.Interface
{
    public interface ISeasonArchive : IArchive<Season>
    {
        Task<IEnumerable<Season>> GetByTeamIdAsync(string teamId);
    }
}
