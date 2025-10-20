using System.Net.NetworkInformation;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Events.Core;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Database;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Models.Scrimmage;
using WabbitBot.Core.Common.Services;
using WabbitBot.Core.Scrimmages;

namespace WabbitBot.Core.Scrimmages
{
    public static partial class ScrimmageHandler
    {
        public static async Task<Result> HandleChallengeRequestedAsync(ChallengeRequested evt)
        {
            try
            {
                Console.WriteLine($"🎯 DEBUG: HandleChallengeRequestedAsync called!");
                Console.WriteLine($"   Event TeamSize: {evt.TeamSize}");
                Console.WriteLine($"   Event ChallengerTeamId: {evt.ChallengerTeamId}");
                Console.WriteLine($"   Event OpponentTeamId: {evt.OpponentTeamId}");
                Console.WriteLine($"   Event SelectedPlayerIds: [{string.Join(", ", evt.SelectedPlayerIds)}]");
                Console.WriteLine($"   Event IssuedByPlayerId: {evt.IssuedByPlayerId}");
                Console.WriteLine($"   Event BestOf: {evt.BestOf}");

                // Get entities by ID (using WithDbContext to avoid tracking conflicts)
                var challengerTeam = await CoreService.WithDbContext(async db =>
                    await db.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == evt.ChallengerTeamId)
                );
                var opponentTeam = await CoreService.WithDbContext(async db =>
                    await db.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == evt.OpponentTeamId)
                );

                if (challengerTeam == null)
                {
                    Console.WriteLine($"   ❌ Challenger team not found: {evt.ChallengerTeamId}");
                    return Result.Failure("Challenger team not found");
                }
                if (opponentTeam == null)
                {
                    Console.WriteLine($"   ❌ Opponent team not found: {evt.OpponentTeamId}");
                    return Result.Failure("Opponent team not found");
                }

                var ChallengerTeam = challengerTeam;
                var OpponentTeam = opponentTeam;
                Console.WriteLine($"   ✅ Teams resolved: {ChallengerTeam.Name} vs {OpponentTeam.Name}");

                // Get issuer player by ID
                var IssuedByPlayer = await CoreService.WithDbContext(async db =>
                    await db.Players.AsNoTracking().FirstOrDefaultAsync(p => p.Id == evt.IssuedByPlayerId)
                );
                if (IssuedByPlayer == null)
                {
                    Console.WriteLine($"   ❌ Issuer player not found: {evt.IssuedByPlayerId}");
                    return Result.Failure("Issuer player not found");
                }
                Console.WriteLine($"   ✅ Issuer player resolved: {IssuedByPlayer.Id}");

                // Get selected players by IDs
                var SelectedPlayers = await CoreService.WithDbContext(async db =>
                {
                    var players = await db
                        .Players.AsNoTracking()
                        .Where(p => evt.SelectedPlayerIds.Contains(p.Id))
                        .ToArrayAsync();

                    if (players.Length != evt.SelectedPlayerIds.Length)
                    {
                        Console.WriteLine(
                            $"   ❌ Some players not found. Expected: {evt.SelectedPlayerIds.Length}, Found: {players.Length}"
                        );
                        return null;
                    }

                    Console.WriteLine($"🔍 DEBUG: Resolving {evt.SelectedPlayerIds.Length} selected players...");
                    foreach (var player in players)
                    {
                        Console.WriteLine($"   ✅ Player resolved: {player.Id}");
                    }

                    return players;
                });

                if (SelectedPlayers == null)
                {
                    return Result.Failure("Some selected players not found");
                }

                var challengeResult = await ScrimmageCore.Factory.CreateChallenge(
                    ChallengerTeam.Id,
                    OpponentTeam.Id,
                    IssuedByPlayer.Id,
                    evt.SelectedPlayerIds,
                    (TeamSize)evt.TeamSize,
                    evt.BestOf
                );
                if (!challengeResult.Success)
                {
                    return Result.Failure("Failed to create challenge");
                }
                var challenge = challengeResult.Data;
                if (challenge == null)
                {
                    return Result.Failure("Failed to create challenge");
                }

                // Save the challenge to the database
                await CoreService.WithDbContext(async db =>
                {
                    db.ScrimmageChallenges.Add(challenge);
                    await db.SaveChangesAsync();
                });
                Console.WriteLine($"   ✅ Challenge saved to database with ID: {challenge.Id}");

                Console.WriteLine(
                    $"📤 DEBUG: About to publish ChallengeCreated event for challenge ID: {challenge.Id}"
                );
                var pubResult = await ScrimmageCore.PublishChallengeCreatedAsync(challenge.Id);
                Console.WriteLine($"📤 DEBUG: PublishChallengeCreatedAsync result: Success={pubResult.Success}");
                if (!pubResult.Success)
                {
                    Console.WriteLine($"   Error: {pubResult.ErrorMessage}");
                    return Result.Failure("Failed to publish challenge created");
                }
                Console.WriteLine($"   ChallengeCreated event published successfully");

                return Result.CreateSuccess(challenge.Id.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 EXCEPTION in HandleChallengeRequestedAsync: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                return Result.Failure($"Handler exception: {ex.Message}");
            }
        }

        public static async Task<Result> HandleScrimmageMatchCompletedAsync(ScrimmageMatchCompleted evt)
        {
            try
            {
                // Fetch the match to determine if it's a scrimmage match
                var matchResult = await CoreService.Matches.GetByIdAsync(evt.MatchId, DatabaseComponent.Repository);
                if (!matchResult.Success || matchResult.Data is null)
                {
                    return Result.Failure("Failed to fetch match");
                }

                var match = matchResult.Data;

                // Only process scrimmage matches
                if (match.ParentType != MatchParentType.Scrimmage || !match.ParentId.HasValue)
                {
                    // Not a scrimmage match, ignore
                    return Result.Failure("Not a scrimmage match");
                }

                // Fetch the scrimmage
                var scrimmageResult = await CoreService.Scrimmages.GetByIdAsync(
                    match.ParentId.Value,
                    DatabaseComponent.Repository
                );
                if (!scrimmageResult.Success || scrimmageResult.Data is null)
                {
                    return Result.Failure("Failed to fetch scrimmage");
                }

                var scrimmage = scrimmageResult.Data;

                // Determine winner and loser team IDs
                Guid winnerTeamId = evt.WinnerTeamId;
                Guid loserTeamId = match.Team1Id == winnerTeamId ? match.Team2Id : match.Team1Id;

                // Calculate and apply rating changes
                var ratingResult = await ScrimmageCore.RatingCalculator.CalculateAndApplyRatingChangesAsync(
                    match.Id,
                    scrimmage.Id,
                    winnerTeamId,
                    loserTeamId,
                    match.TeamSize
                );

                if (!ratingResult.Success || ratingResult.Data is null)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("Failed to calculate rating changes"),
                        $"Failed to calculate rating changes for match {match.Id}: {ratingResult.ErrorMessage}",
                        nameof(HandleScrimmageMatchCompletedAsync)
                    );
                    return Result.Failure($"Failed to calculate rating changes: {ratingResult.ErrorMessage}");
                }

                var ratingChanges = ratingResult.Data;

                // Update scrimmage entity with calculation metadata
                scrimmage.CompletedAt = DateTime.UtcNow;
                scrimmage.WinnerId = winnerTeamId;

                // Store rating data
                bool challengerIsWinner = scrimmage.ChallengerTeamId == winnerTeamId;
                scrimmage.ChallengerTeamRatingChange = challengerIsWinner
                    ? ratingChanges.WinnerChange
                    : ratingChanges.LoserChange;
                scrimmage.OpponentTeamRatingChange = challengerIsWinner
                    ? ratingChanges.LoserChange
                    : ratingChanges.WinnerChange;

                // Store variety bonuses
                scrimmage.ChallengerTeamVarietyBonusUsed = challengerIsWinner
                    ? ratingChanges.WinnerVarietyBonus
                    : ratingChanges.LoserVarietyBonus;
                scrimmage.OpponentTeamVarietyBonusUsed = challengerIsWinner
                    ? ratingChanges.LoserVarietyBonus
                    : ratingChanges.WinnerVarietyBonus;

                // Store multipliers
                scrimmage.ChallengerTeamMultiplierUsed = challengerIsWinner
                    ? ratingChanges.WinnerMultiplier
                    : ratingChanges.LoserMultiplier;
                scrimmage.OpponentTeamMultiplierUsed = challengerIsWinner
                    ? ratingChanges.LoserMultiplier
                    : ratingChanges.WinnerMultiplier;

                // Store catch-up bonuses
                scrimmage.ChallengerTeamCatchUpBonusUsed = challengerIsWinner
                    ? ratingChanges.WinnerCatchUpBonus
                    : ratingChanges.LoserCatchUpBonus;
                scrimmage.OpponentTeamCatchUpBonusUsed = challengerIsWinner
                    ? ratingChanges.LoserCatchUpBonus
                    : ratingChanges.WinnerCatchUpBonus;

                // Store gap scaling
                scrimmage.HigherRatedTeamId = ratingChanges.HigherRatedTeamId;
                scrimmage.ChallengerTeamGapScalingAppliedValue =
                    scrimmage.ChallengerTeamId == ratingChanges.HigherRatedTeamId
                        ? ratingChanges.GapScalingApplied
                        : 1.0;
                scrimmage.OpponentTeamGapScalingAppliedValue =
                    scrimmage.OpponentTeamId == ratingChanges.HigherRatedTeamId ? ratingChanges.GapScalingApplied : 1.0;

                // Get final ratings for storage
                var challengerTeamResult = await CoreService.Teams.GetByIdAsync(
                    scrimmage.ChallengerTeamId,
                    DatabaseComponent.Repository
                );
                var opponentTeamResult = await CoreService.Teams.GetByIdAsync(
                    scrimmage.OpponentTeamId,
                    DatabaseComponent.Repository
                );

                ScrimmageTeamStats? challengerStats = null;
                ScrimmageTeamStats? opponentStats = null;

                if (
                    challengerTeamResult.Success
                    && challengerTeamResult.Data is not null
                    && challengerTeamResult.Data.ScrimmageTeamStats.TryGetValue(
                        match.TeamSize,
                        out var tempChallengerStats
                    )
                )
                {
                    challengerStats = tempChallengerStats;
                    scrimmage.ChallengerTeamRating = challengerStats.CurrentRating;
                    scrimmage.ChallengerTeamConfidence = challengerStats.Confidence;
                }

                if (
                    opponentTeamResult.Success
                    && opponentTeamResult.Data is not null
                    && opponentTeamResult.Data.ScrimmageTeamStats.TryGetValue(match.TeamSize, out var tempOpponentStats)
                )
                {
                    opponentStats = tempOpponentStats;
                    scrimmage.OpponentTeamRating = opponentStats.CurrentRating;
                    scrimmage.OpponentTeamConfidence = opponentStats.Confidence;
                }

                // Update scrimmage in database
                var updateResult = await CoreService.Scrimmages.UpdateAsync(scrimmage, DatabaseComponent.Repository);
                if (!updateResult.Success)
                {
                    return Result.Failure($"Failed to update scrimmage: {updateResult.ErrorMessage}");
                }

                // Recalculate variety stats for both teams with opponent availability normalization
                if (challengerStats is not null)
                {
                    int challengerGamesPlayed = challengerStats.Wins + challengerStats.Losses;
                    await TeamCore.ScrimmageStats.CalculateAndUpdateVarietyStatsAsync(
                        scrimmage.ChallengerTeamId,
                        match.TeamSize,
                        scrimmage.ChallengerTeamRating,
                        challengerGamesPlayed
                    );
                }

                if (opponentStats is not null)
                {
                    int opponentGamesPlayed = opponentStats.Wins + opponentStats.Losses;
                    await TeamCore.ScrimmageStats.CalculateAndUpdateVarietyStatsAsync(
                        scrimmage.OpponentTeamId,
                        match.TeamSize,
                        scrimmage.OpponentTeamRating,
                        opponentGamesPlayed
                    );
                }

                // Check if this match is eligible for proven potential tracking
                if (challengerStats is not null && opponentStats is not null)
                {
                    int challengerGamesPlayed = challengerStats.Wins + challengerStats.Losses;
                    int opponentGamesPlayed = opponentStats.Wins + opponentStats.Losses;

                    bool isEligible = ScrimmageCore.ProvenPotential.IsEligibleForTracking(
                        scrimmage.ChallengerTeamConfidence,
                        scrimmage.OpponentTeamConfidence,
                        challengerGamesPlayed,
                        opponentGamesPlayed
                    );

                    if (isEligible)
                    {
                        // Create proven potential record
                        var ppResult = await ScrimmageCore.ProvenPotential.CreateProvenPotentialRecordAsync(
                            match.Id,
                            scrimmage.ChallengerTeamId,
                            scrimmage.OpponentTeamId,
                            scrimmage.ChallengerTeamRating,
                            scrimmage.OpponentTeamRating,
                            scrimmage.ChallengerTeamConfidence,
                            scrimmage.OpponentTeamConfidence,
                            scrimmage.ChallengerTeamRatingChange,
                            scrimmage.OpponentTeamRatingChange,
                            match.TeamSize
                        );

                        if (!ppResult.Success)
                        {
                            // Log but don't fail the whole operation
                            await CoreService.ErrorHandler.CaptureAsync(
                                new InvalidOperationException("Failed to create PP record"),
                                $"Failed to create proven potential record for match {match.Id}: {ppResult.ErrorMessage}",
                                nameof(HandleScrimmageMatchCompletedAsync)
                            );
                        }
                    }

                    // Check and apply proven potential adjustments for both teams
                    await ScrimmageCore.ProvenPotential.CheckAndApplyProvenPotentialAsync(
                        scrimmage.ChallengerTeamId,
                        match.TeamSize,
                        scrimmage.ChallengerTeamRating,
                        scrimmage.ChallengerTeamConfidence,
                        match.Id
                    );

                    await ScrimmageCore.ProvenPotential.CheckAndApplyProvenPotentialAsync(
                        scrimmage.OpponentTeamId,
                        match.TeamSize,
                        scrimmage.OpponentTeamRating,
                        scrimmage.OpponentTeamConfidence,
                        match.Id
                    );
                }

                return Result.CreateSuccess();
            }
            catch (Exception ex)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to handle MatchCompleted event",
                    nameof(HandleScrimmageMatchCompletedAsync)
                );
                return Result.Failure($"Error handling match completion: {ex.Message}");
            }
        }
    }
}
