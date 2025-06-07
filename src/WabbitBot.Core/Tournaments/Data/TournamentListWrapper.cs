using System.Collections.Generic;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Tournaments.Data
{
    public class TournamentListWrapper : BaseEntity
    {
        public List<Tournament> Tournaments { get; set; } = new List<Tournament>();
    }
}