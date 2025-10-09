
# 1. /Challenge issued
  - Create new challenge
  - Set: ChallengerTeam, OpponentTeam, ChalengerTeamId, OpponentTeamId, IssuedByPlayer,
         IssuedByPlayerId, AcceptedByPlayer, AcceptedByPlayerId, ChallengeStatus = Pending,
         TeamSize, BestOf, ChallengeExpiresAt
  - `PublishChallengedCreatedAsync`

# 2. HandleChallengeCreatedAsync (DiscBot)
  - `PublishChallengeDeclinedAsync`, `PublishChallengeCancelledAsync`, or `PublishChallengeAcceptedAsync`

# 3. HandleChallengeAcceptedAsync (Core)
  - Call `ScrimmageCore.ValidateChallengeAsync`
  - Call `ScrimmageCore.CreateScrimmageAsync`