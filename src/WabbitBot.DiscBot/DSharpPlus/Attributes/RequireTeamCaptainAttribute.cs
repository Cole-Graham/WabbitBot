// using System;
// using System.Threading.Tasks;
// using DSharpPlus.Commands;
// using WabbitBot.Core.Common.Models;
// using WabbitBot.Common.Models;

// namespace WabbitBot.DiscBot.DSharpPlus.Attributes
// {
//     /// <summary>
//     /// Requires that a user is a team captain to execute a command.
//     /// Can only be used on commands that include a teamId or teamName parameter.
//     /// </summary>
//     [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
//     public class RequireTeamCaptainAttribute : TeamPermissionBaseAttribute
//     {
//         /// <summary>
//         /// Creates a new instance of the RequireTeamCaptainAttribute
//         /// </summary>
//         /// <param name="teamParameterName">The name of the parameter that contains the team ID or name</param>
//         public RequireTeamCaptainAttribute(string teamParameterName = "teamName")
//             : base(teamParameterName)
//         {
//         }

//         /// <summary>
//         /// Check if the user is the team captain
//         /// </summary>
//         protected override Task<string?> PerformTeamPermissionCheckAsync(CommandContext context, Team team)
//         {
//             // Check if the user is the team captain
//             return team.IsCaptain(context.User.Id.ToString())
//                 ? Task.FromResult<string?>(null) // Success 
//                 : Task.FromResult<string?>("You must be the team captain to use this command.");
//         }


//     }
// }