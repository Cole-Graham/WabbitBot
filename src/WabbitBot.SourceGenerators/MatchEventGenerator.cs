using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace WabbitBot.SourceGenerators
{
    [Generator]
    public class MatchEventGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // Register for syntax notifications
            context.RegisterForSyntaxNotifications(() => new MatchEventSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not MatchEventSyntaxReceiver receiver)
                return;

            // Get the Match class
            var matchClass = receiver.MatchClass;
            if (matchClass == null)
                return;

            // Generate the event publisher code
            var sourceBuilder = new StringBuilder(@"
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Matches
{
    public partial class Match
    {
        private static readonly ICoreEventBus _eventBus;

        static Match()
        {
            _eventBus = CoreEventBus.Instance;
        }

        private async Task PublishEventAsync(MatchEvent @event)
        {
            await _eventBus.PublishAsync(@event);
        }

        public static async Task<Match> CreateAsync(string team1Id, string team2Id, GameSize gameSize)
        {
            var match = new Match
            {
                Id = Guid.NewGuid().ToString(),
                Team1Id = team1Id,
                Team2Id = team2Id,
                GameSize = gameSize,
                Status = MatchStatus.Created
            };

            await match.PublishEventAsync(new MatchCreatedEvent
            {
                MatchId = match.Id,
                Team1Id = match.Team1Id,
                Team2Id = match.Team2Id,
                Team1PlayerIds = match.Team1PlayerIds,
                Team2PlayerIds = match.Team2PlayerIds,
                GameSize = match.GameSize
            });

            return match;
        }

        public async Task StartAsync()
        {
            if (Status != MatchStatus.Created)
                throw new InvalidOperationException(""Match can only be started when in Created state"");

            StartedAt = DateTime.UtcNow;
            Status = MatchStatus.InProgress;

            await PublishEventAsync(new MatchStartedEvent
            {
                MatchId = Id,
                StartedAt = StartedAt.Value
            });
        }

        public async Task CompleteAsync(string winnerId, int team1Rating, int team2Rating, int ratingChange)
        {
            if (Status != MatchStatus.InProgress)
                throw new InvalidOperationException(""Match can only be completed when in Progress state"");

            if (winnerId != Team1Id && winnerId != Team2Id)
                throw new ArgumentException(""Winner must be one of the participating teams"");

            CompletedAt = DateTime.UtcNow;
            WinnerId = winnerId;
            Status = MatchStatus.Completed;
            Team1Rating = team1Rating;
            Team2Rating = team2Rating;
            RatingChange = ratingChange;

            await PublishEventAsync(new MatchCompletedEvent
            {
                MatchId = Id,
                WinnerId = winnerId,
                CompletedAt = CompletedAt.Value,
                Team1Rating = team1Rating,
                Team2Rating = team2Rating,
                RatingChange = ratingChange
            });
        }

        public async Task CancelAsync(string reason, string cancelledBy)
        {
            if (Status == MatchStatus.Completed)
                throw new InvalidOperationException(""Cannot cancel a completed match"");

            Status = MatchStatus.Cancelled;

            await PublishEventAsync(new MatchCancelledEvent
            {
                MatchId = Id,
                Reason = reason,
                CancelledBy = cancelledBy
            });
        }

        public async Task ForfeitAsync(string forfeitedTeamId, string reason)
        {
            if (Status != MatchStatus.InProgress)
                throw new InvalidOperationException(""Match can only be forfeited when in Progress state"");

            if (forfeitedTeamId != Team1Id && forfeitedTeamId != Team2Id)
                throw new ArgumentException(""Forfeited team must be one of the participating teams"");

            Status = MatchStatus.Forfeited;
            WinnerId = forfeitedTeamId == Team1Id ? Team2Id : Team1Id;

            await PublishEventAsync(new MatchForfeitedEvent
            {
                MatchId = Id,
                ForfeitedTeamId = forfeitedTeamId,
                Reason = reason
            });
        }

        public async Task AddPlayerAsync(string playerId, int teamNumber)
        {
            if (Status != MatchStatus.Created)
                throw new InvalidOperationException(""Players can only be added when match is in Created state"");

            if (teamNumber != 1 && teamNumber != 2)
                throw new ArgumentException(""Team number must be 1 or 2"");

            var teamList = teamNumber == 1 ? Team1PlayerIds : Team2PlayerIds;
            if (!teamList.Contains(playerId))
            {
                teamList.Add(playerId);
                await PublishEventAsync(new MatchPlayerJoinedEvent
                {
                    MatchId = Id,
                    PlayerId = playerId,
                    TeamNumber = teamNumber
                });
            }
        }

        public async Task RemovePlayerAsync(string playerId, int teamNumber)
        {
            if (Status != MatchStatus.Created)
                throw new InvalidOperationException(""Players can only be removed when match is in Created state"");

            if (teamNumber != 1 && teamNumber != 2)
                throw new ArgumentException(""Team number must be 1 or 2"");

            var teamList = teamNumber == 1 ? Team1PlayerIds : Team2PlayerIds;
            if (teamList.Contains(playerId))
            {
                teamList.Remove(playerId);
                await PublishEventAsync(new MatchPlayerLeftEvent
                {
                    MatchId = Id,
                    PlayerId = playerId,
                    TeamNumber = teamNumber
                });
            }
        }
    }
}");

            context.AddSource("Match.g.cs", sourceBuilder.ToString());
        }
    }

    public class MatchEventSyntaxReceiver : ISyntaxReceiver
    {
        public ClassDeclarationSyntax? MatchClass { get; private set; }

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax classDeclaration)
            {
                if (classDeclaration.Identifier.Text == "Match")
                {
                    MatchClass = classDeclaration;
                }
            }
        }
    }
}