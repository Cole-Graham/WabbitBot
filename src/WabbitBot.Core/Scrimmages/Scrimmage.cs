using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Matches;
using WabbitBot.Core.Common.BotCore;

namespace WabbitBot.Core.Scrimmages
{
    public class Scrimmage : BaseEntity
    {
        private static readonly ICoreEventBus _eventBus;

        static Scrimmage()
        {
            _eventBus = CoreEventBus.Instance;
        }

        public string Team1Id { get; set; } = string.Empty;
        public string Team2Id { get; set; } = string.Empty;
        public List<string> Team1RosterIds { get; set; } = new();
        public List<string> Team2RosterIds { get; set; } = new();
        public GameSize GameSize { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? WinnerId { get; set; }
        public ScrimmageStatus Status { get; set; }
        public int Team1Rating { get; set; }
        public int Team2Rating { get; set; }
        public int RatingChange { get; set; }
        public double RatingMultiplier { get; set; } = 1.0;
        public double Team1Confidence { get; set; } = 0.0; // Confidence at time of match
        public double Team2Confidence { get; set; } = 0.0; // Confidence at time of match
        public DateTime? ChallengeExpiresAt { get; set; }
        public bool IsAccepted { get; set; }
        public Match? Match { get; private set; }
        public int BestOf { get; set; } = 1;

        public Scrimmage()
        {
            CreatedAt = DateTime.UtcNow;
            Status = ScrimmageStatus.Created;
            ChallengeExpiresAt = DateTime.UtcNow.AddHours(24); // 24 hour challenge window
        }

        public bool IsTeamMatch => GameSize != GameSize.OneVOne;

        private async Task PublishEventAsync(ICoreEvent @event)
        {
            await _eventBus.PublishAsync(@event);
        }

        public static Scrimmage Create(string team1Id, string team2Id, GameSize gameSize, int bestOf = 1)
        {
            var scrimmage = new Scrimmage
            {
                Team1Id = team1Id,
                Team2Id = team2Id,
                GameSize = gameSize,
                Status = ScrimmageStatus.Created,
                CreatedAt = DateTime.UtcNow,
                BestOf = bestOf
            };

            // Event will be published by source generator
            return scrimmage;
        }

        public void Accept()
        {
            if (Status != ScrimmageStatus.Created)
                throw new InvalidOperationException("Scrimmage can only be accepted when in Created state");

            if (DateTime.UtcNow > ChallengeExpiresAt)
                throw new InvalidOperationException("Challenge has expired");

            IsAccepted = true;
            Status = ScrimmageStatus.Accepted;

            // Create the match
            Match = Match.Create(Team1Id, Team2Id, GameSize, Id.ToString(), "Scrimmage", BestOf);

            // Event will be published by source generator
        }

        public void Decline()
        {
            if (Status != ScrimmageStatus.Created)
                throw new InvalidOperationException("Scrimmage can only be declined when in Created state");

            if (DateTime.UtcNow > ChallengeExpiresAt)
                throw new InvalidOperationException("Challenge has expired");

            Status = ScrimmageStatus.Declined;

            // Event will be published by source generator
        }

        public void Start()
        {
            if (Status != ScrimmageStatus.Accepted)
                throw new InvalidOperationException("Scrimmage can only be started when in Accepted state");

            if (Match == null)
                throw new InvalidOperationException("Match must be created before starting scrimmage");

            StartedAt = DateTime.UtcNow;
            Status = ScrimmageStatus.InProgress;
            Match.Start();

            // Event will be published by source generator
        }

        public async Task CompleteAsync(string winnerId, int team1Rating, int team2Rating, int ratingChange, double team1Confidence = 0.0, double team2Confidence = 0.0)
        {
            if (Status != ScrimmageStatus.InProgress)
                throw new InvalidOperationException("Scrimmage can only be completed when in Progress state");

            if (Match == null)
                throw new InvalidOperationException("Match must exist to complete scrimmage");

            if (winnerId != Team1Id && winnerId != Team2Id)
                throw new ArgumentException("Winner must be one of the participating teams");

            CompletedAt = DateTime.UtcNow;
            WinnerId = winnerId;
            Status = ScrimmageStatus.Completed;
            Team1Rating = team1Rating;
            Team2Rating = team2Rating;
            RatingChange = ratingChange;
            Team1Confidence = team1Confidence;
            Team2Confidence = team2Confidence;

            Match.Complete(winnerId);

            // Publish ScrimmageCompletedEvent with confidence values
            await PublishEventAsync(new ScrimmageCompletedEvent
            {
                ScrimmageId = Id.ToString(),
                MatchId = Match.Id,
                Team1Id = Team1Id,
                Team2Id = Team2Id,
                Team1Score = winnerId == Team1Id ? 1 : 0,
                Team2Score = winnerId == Team2Id ? 1 : 0,
                GameSize = GameSize,
                Team1Confidence = team1Confidence,
                Team2Confidence = team2Confidence
            });
        }

        public void Cancel(string reason, string cancelledBy)
        {
            if (Status == ScrimmageStatus.Completed)
                throw new InvalidOperationException("Cannot cancel a completed scrimmage");

            Status = ScrimmageStatus.Cancelled;

            if (Match != null)
            {
                Match.Cancel(reason, cancelledBy);
            }

            // Event will be published by source generator
        }

        public void Forfeit(string forfeitedTeamId, string reason)
        {
            if (Status != ScrimmageStatus.InProgress)
                throw new InvalidOperationException("Scrimmage can only be forfeited when in Progress state");

            if (Match == null)
                throw new InvalidOperationException("Match must exist to forfeit scrimmage");

            if (forfeitedTeamId != Team1Id && forfeitedTeamId != Team2Id)
                throw new ArgumentException("Forfeited team must be one of the participating teams");

            Status = ScrimmageStatus.Forfeited;
            WinnerId = forfeitedTeamId == Team1Id ? Team2Id : Team1Id;

            Match.Forfeit(forfeitedTeamId, reason);

            // Event will be published by source generator
        }

        public void AddRosterMember(string playerId, int teamNumber)
        {
            if (Status != ScrimmageStatus.Created)
                throw new InvalidOperationException("Roster members can only be added when scrimmage is in Created state");

            if (teamNumber != 1 && teamNumber != 2)
                throw new ArgumentException("Team number must be 1 or 2");

            var teamList = teamNumber == 1 ? Team1RosterIds : Team2RosterIds;
            if (!teamList.Contains(playerId))
            {
                teamList.Add(playerId);
                // Event will be published by source generator
            }
        }

        public void RemoveRosterMember(string playerId, int teamNumber)
        {
            if (Status != ScrimmageStatus.Created)
                throw new InvalidOperationException("Roster members can only be removed when scrimmage is in Created state");

            if (teamNumber != 1 && teamNumber != 2)
                throw new ArgumentException("Team number must be 1 or 2");

            var teamList = teamNumber == 1 ? Team1RosterIds : Team2RosterIds;
            if (teamList.Contains(playerId))
            {
                teamList.Remove(playerId);
                // Event will be published by source generator
            }
        }
    }

    public enum ScrimmageStatus
    {
        Created,
        Accepted,
        Declined,
        InProgress,
        Completed,
        Cancelled,
        Forfeited
    }
}
