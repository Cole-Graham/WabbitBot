using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data.Interface
{
    public interface IGameRepository : IBaseRepository<Game>
    {
        Task<Game?> GetGameAsync(string gameId);
        Task<IEnumerable<Game>> GetGamesByMatchAsync(string matchId);
        Task SaveAsync(Game entity);
    }
}