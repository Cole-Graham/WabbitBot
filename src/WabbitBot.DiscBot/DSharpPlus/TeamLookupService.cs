using System.Threading.Tasks;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Data.Interface;
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
                if (_teamRepository == null)
                {
                    // TODO: Get database connection from configuration
                    // For now, this is a placeholder that will need to be properly initialized
                    throw new InvalidOperationException("TeamLookupService not initialized. Call Initialize() first.");
                }
                return _teamRepository;
            }
        }

        /// <summary>
        /// Initialize the team lookup service with a repository
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
    }
}