using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Events.Core;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.Core.Common.Models.Common
{
    public partial class MatchHandler
    {
        public static async Task<Result> HandleScrimmageCreatedAsync(ScrimmageCreated evt)
        {
            var NewMatch = await MatchCore.CreateScrimmageMatchAsync(evt.ScrimmageId);
            if (!NewMatch.Success)
            {
                return Result.Failure("Failed to create scrimmage match");
            }
            if (NewMatch.Data == null)
            {
                return Result.Failure("Failed to create scrimmage match");
            }

            // Commented out pending generator publishers
            var pubResult = await PublishScrimmageMatchCreatedAsync(evt.ScrimmageId, NewMatch.Data.Id);
            if (!pubResult.Success)
            {
                return Result.Failure("Failed to publish ScrimmageMatchCreated event");
            }

            return Result.CreateSuccess();
        }
    }
}
