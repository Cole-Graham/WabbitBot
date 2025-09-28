// using System;
// using System.Linq;
// using System.Threading.Tasks;
// using DSharpPlus.Commands;
// using WabbitBot.Core.Common.Models;

// namespace WabbitBot.DiscBot.DSharpPlus.Attributes
// {
//     /// <summary>
//     /// Requires that a user is a team manager (captain or core player) to execute a command.
//     /// Can only be used on commands that include a teamId or teamName parameter.
//     /// </summary>
//     [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
//     public class RequireTeamManagerAttribute : TeamPermissionBaseAttribute
//     {
//         /// <summary>
//         /// Creates a new instance of the RequireTeamManagerAttribute
//         /// </summary>
//         /// <param name="teamParameterName">The name of the parameter that contains the team ID or name</param>
//         public RequireTeamManagerAttribute(string teamParameterName = "teamName")
//             : base(teamParameterName)
//         {
//         }

//         /// <summary>
//         /// Check if the user is a team manager (captain or core player)
//         /// </summary>
//         protected override Task<string?> PerformTeamPermissionCheckAsync(CommandContext context, Team team)
//         {
//             if (team.IsTeamManager(context.User.Id.ToString()))
//             {
//                 return Task.FromResult<string?>(null); // Success
//             }

//             return Task.FromResult<string?>("You must be a team captain or core player to use this command.");
//         }
//     }
// }