using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Tournaments;

namespace WabbitBot.Core.Tournaments.Data.Interface
{
    public interface ITournamentArchive : IArchive<Tournament>
    {
        // TournamentArchive currently has no additional methods beyond the base interface
    }
}
