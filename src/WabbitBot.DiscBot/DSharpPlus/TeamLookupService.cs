using System.Threading.Tasks;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Data.Interface;
using WabbitBot.Core.Common.Data;
using WabbitBot.Common.Data;

namespace WabbitBot.DiscBot.DSharpPlus
{
    /// <summary>
    /// Simple team lookup service that doesn't use dependency injection
    /// </summary>
    public static class TeamLookupService
    {
        private static ITeamRepository? _teamRepository;

        private static ITeamRepository TeamRepository
        {
            get
            {
                if (_teamRepository is null)
                {
                    // Get database connection from provider and create repository
                    var connection = DatabaseConnectionProvider.GetConnectionAsync().Result;
                    _teamRepository = new TeamRepository(connection);
                }
                return _teamRepository;
            }
        }

        /// <summary>
        /// Initialize the team lookup service with a repository (optional - for testing or custom setup)
        /// </summary>
        public static void Initialize(ITeamRepository teamRepository)
        {
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
        }

        /// <summary>
        /// Get a team by name
        /// </summary>
        public static async Task<Team?> GetByNameAsync(string name)
        {
            return await TeamRepository.GetByNameAsync(name);
        }

        /// <summary>
        /// Get a team by tag
        /// </summary>
        public static async Task<Team?> GetByTagAsync(string tag)
        {
            return await TeamRepository.GetByTagAsync(tag);
        }

        /// <summary>
        /// Search teams by name or tag
        /// </summary>
        public static async Task<IEnumerable<Team>> SearchTeamsAsync(string searchTerm, int limit = 25)
        {
            return await TeamRepository.SearchTeamsAsync(searchTerm, limit);
        }

        /// <summary>
        /// Search teams by name or tag, filtered by game size
        /// </summary>
        public static async Task<IEnumerable<Team>> SearchTeamsByGameSizeAsync(string searchTerm, GameSize gameSize, int limit = 25)
        {
            return await TeamRepository.SearchTeamsByGameSizeAsync(searchTerm, gameSize, limit);
        }

        /// <summary>
        /// Get all teams that a user is a member of
        /// </summary>
        public static async Task<IEnumerable<Team>> GetUserTeamsAsync(string userId)
        {
            return await TeamRepository.GetTeamsByMemberAsync(userId);
        }
    }
}