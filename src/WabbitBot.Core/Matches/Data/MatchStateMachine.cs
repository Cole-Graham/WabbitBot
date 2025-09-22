using System;
using System.Collections.Generic;
using System.Linq;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Matches.Data
{
    /// <summary>
    /// State machine for managing match lifecycle and transitions
    /// </summary>
    public class MatchStateMachine
    {
        private readonly Dictionary<Guid, Match> _activeMatches = new();
        // Removed _matchStates dictionary - using state snapshots instead
        private readonly Dictionary<Guid, List<MatchStateSnapshot>> _stateHistory = new();
        private readonly Dictionary<Guid, MatchStateSnapshot> _currentStateSnapshots = new();

        /// <summary>
        /// Captures a state snapshot for recovery purposes
        /// </summary>
        public void CaptureStateSnapshot(MatchStateSnapshot snapshot)
        {
            snapshot.Timestamp = DateTime.UtcNow;

            // Add to history
            if (!_stateHistory.ContainsKey(snapshot.MatchId))
            {
                _stateHistory[snapshot.MatchId] = new List<MatchStateSnapshot>();
            }
            _stateHistory[snapshot.MatchId].Add(snapshot);

            // Update current snapshot
            _currentStateSnapshots[snapshot.MatchId] = snapshot;
        }

        /// <summary>
        /// Gets the current state snapshot for a match
        /// </summary>
        public MatchStateSnapshot? GetCurrentStateSnapshot(Guid matchId)
        {
            return _currentStateSnapshots.TryGetValue(matchId, out var snapshot) ? snapshot : null;
        }

        /// <summary>
        /// Gets the complete state history for a match
        /// </summary>
        public IEnumerable<MatchStateSnapshot> GetStateHistory(Guid matchId)
        {
            return _stateHistory.TryGetValue(matchId, out var history) ? history : Enumerable.Empty<MatchStateSnapshot>();
        }

        /// <summary>
        /// Recovers a match from its current state snapshot
        /// </summary>
        public Match? RecoverMatch(Guid matchId)
        {
            if (!_currentStateSnapshots.TryGetValue(matchId, out var snapshot))
            {
                return null;
            }

            // Try to get from active matches first
            if (_activeMatches.TryGetValue(matchId, out var activeMatch))
            {
                return activeMatch;
            }

            // If not in active matches, we'd need to reconstruct from snapshot
            // This would require additional logic to rebuild the match from the snapshot
            // For now, return null - this would be implemented based on specific recovery needs
            return null;
        }

        /// <summary>
        /// Gets a match by ID
        /// </summary>
        public Match? GetCurrentMatch(Guid matchId)
        {
            return _activeMatches.TryGetValue(matchId, out var match) ? match : null;
        }

        /// <summary>
        /// Updates a match in the active matches dictionary
        /// </summary>
        public void UpdateMatch(Match match)
        {
            _activeMatches[match.Id] = match;
        }

        /// <summary>
        /// Creates a state snapshot from a match based on its current status
        /// </summary>
        public MatchStateSnapshot CreateSnapshotFromMatch(Match match, string userId, string playerName)
        {
            var snapshot = new MatchStateSnapshot
            {
                MatchId = match.Id,
                UserId = userId,
                PlayerName = playerName,
                StartedAt = match.StartedAt,
                CompletedAt = match.CompletedAt,
                WinnerId = match.WinnerId,
                CurrentGameNumber = match.CurrentStateSnapshot?.CurrentGameNumber ?? 1,
                Games = match.Games ?? new List<Game>(),
                CurrentMapId = match.Games?.FirstOrDefault(g => g.GameNumber == (match.CurrentStateSnapshot?.CurrentGameNumber ?? 1))?.MapId,

                // Map ban properties
                AvailableMaps = match.AvailableMaps,
                Team1MapBans = match.Team1MapBans,
                Team2MapBans = match.Team2MapBans,
                Team1BansSubmitted = match.Team1MapBansSubmittedAt.HasValue,
                Team2BansSubmitted = match.Team2MapBansSubmittedAt.HasValue,
                Team1BansConfirmed = match.CurrentStateSnapshot?.Team1BansConfirmed ?? false,
                Team2BansConfirmed = match.CurrentStateSnapshot?.Team2BansConfirmed ?? false,
                FinalMapPool = match.CurrentStateSnapshot?.FinalMapPool ?? new List<string>()
            };

            // Copy any additional state from existing snapshot
            if (match.CurrentStateSnapshot != null)
            {
                snapshot.CancelledAt = match.CurrentStateSnapshot.CancelledAt;
                snapshot.ForfeitedAt = match.CurrentStateSnapshot.ForfeitedAt;
                snapshot.CancelledByUserId = match.CurrentStateSnapshot.CancelledByUserId;
                snapshot.ForfeitedByUserId = match.CurrentStateSnapshot.ForfeitedByUserId;
                snapshot.ForfeitedTeamId = match.CurrentStateSnapshot.ForfeitedTeamId;
                snapshot.CancellationReason = match.CurrentStateSnapshot.CancellationReason;
                snapshot.ForfeitReason = match.CurrentStateSnapshot.ForfeitReason;
            }

            return snapshot;
        }
    }
}