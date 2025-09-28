using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Trees;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Models;
using WabbitBot.DiscBot.DSharpPlus;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Data.Interfaces;

namespace WabbitBot.DiscBot.DSharpPlus.Attributes
{
    /// <summary>
    /// Base class for team-related permission attributes
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public abstract class TeamPermissionBaseAttribute : Attribute, IContextCheck
    {
        private readonly DatabaseService<Team> _teamData = new();
        /// <summary>
        /// The name of the parameter that contains the team ID or name
        /// </summary>
        protected readonly string TeamParameterName;

        /// <summary>
        /// Creates a new instance of a team permission attribute
        /// </summary>
        /// <param name="teamParameterName">The name of the parameter that contains the team ID or name</param>
        public TeamPermissionBaseAttribute(string teamParameterName = "teamName")
        {
            TeamParameterName = teamParameterName;
        }

        /// <summary>
        /// Executes the permission check
        /// </summary>
        public async ValueTask<string?> ExecuteCheckAsync(CommandContext context)
        {
            try
            {
                // First make sure the user has the whitelisted role
                // if (!await PermissionService.HasWhitelistedRoleAsync(context.User, context.Guild))
                //     return "You need the Whitelisted role to use this command.";

                // If it's an admin, they can bypass team permission checks
                if (await PermissionService.HasAdminPrivilegesAsync(context.User, context.Guild))
                    return null; // Success - Admin bypass

                // Find the team parameter from arguments
                var parameter = context.Arguments.Keys.FirstOrDefault(p => p.Name == TeamParameterName);
                if (parameter == null)
                    return $"Parameter '{TeamParameterName}' not found.";

                if (!context.Arguments.TryGetValue(parameter, out var teamParameterObj) || teamParameterObj == null)
                    return $"Parameter '{TeamParameterName}' has no value.";

                string? teamParameter = teamParameterObj.ToString();
                if (string.IsNullOrEmpty(teamParameter))
                    return $"Parameter '{TeamParameterName}' cannot be empty.";

                // Get the team (could be by ID or name)
                // TODO: Make sure teamParameter is a valid ID or name
                var team = teamParameter.StartsWith("team-")
                    ? await _teamData.GetByNameAsync(teamParameter, DatabaseComponent.Repository)
                    : await _teamData.GetByIdAsync(teamParameter, DatabaseComponent.Repository);

                // If team not found, fail
                if (team == null)
                    return $"Team '{teamParameter}' not found.";

                // Perform the specific permission check for this attribute
                return await PerformTeamPermissionCheckAsync(context, team.Data!);
            }
            catch (Exception ex)
            {
                return $"Error checking team permissions: {ex.Message}";
            }
        }

        /// <summary>
        /// Perform the specific permission check for this attribute
        /// </summary>
        /// <param name="context">The command context</param>
        /// <param name="team">The team being accessed</param>
        /// <returns>Null if check passes, an error message otherwise</returns>
        protected abstract Task<string?> PerformTeamPermissionCheckAsync(CommandContext context, Team team);
    }
}