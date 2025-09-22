using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Scrimmages;

namespace WabbitBot.Core.Scrimmages.Data.Interface
{
    public interface IScrimmageArchive : IArchive<Scrimmage>
    {
        Task<IEnumerable<Scrimmage>> GetScrimmagesByTeamAsync(string teamId);
        Task<IEnumerable<Scrimmage>> GetScrimmagesSinceAsync(DateTime since);
    }
}
