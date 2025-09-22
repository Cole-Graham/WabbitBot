using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Leaderboards;

namespace WabbitBot.Core.Leaderboards.Data.Interface
{
    public interface ILeaderboardArchive : IArchive<Leaderboard>
    {
        Task<IEnumerable<Leaderboard>> GetByEvenTeamFormatAsync(EvenTeamFormat evenTeamFormat);
    }
}
