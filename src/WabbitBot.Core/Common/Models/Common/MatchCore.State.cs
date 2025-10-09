using System;
using System.Collections.Generic;
using System.Linq;
using WabbitBot.Core.Common.Events;

namespace WabbitBot.Core.Common.Models.Common
{
    public partial class MatchCore
    {
        public partial class State
        {
            public class MatchState
            {
                private readonly Dictionary<Guid, Match> _activeMatches = new();

                // Removed _matchStates dictionary - using state snapshots instead
                private readonly Dictionary<Guid, List<MatchStateSnapshot>> _stateHistory = new();
                private readonly Dictionary<Guid, MatchStateSnapshot> _currentStateSnapshots = new();

                // ------------------------ State Machine Definition ---------------------------
                private static readonly Dictionary<MatchStatus, List<MatchStatus>> _validTransitions = new()
                {
                    [MatchStatus.Created] = new() { MatchStatus.InProgress, MatchStatus.Cancelled },
                    [MatchStatus.InProgress] = new()
                    {
                        MatchStatus.Completed,
                        MatchStatus.Cancelled,
                        MatchStatus.Forfeited,
                    },
                    [MatchStatus.Completed] = new(), // Terminal state
                    [MatchStatus.Cancelled] = new(), // Terminal state
                    [MatchStatus.Forfeited] = new(), // Terminal state
                };

                /// <summary>
                /// Captures a state snapshot for recovery purposes
                /// </summary>
                public void CaptureStateSnapshot(MatchStateSnapshot snapshot)
                {
                    snapshot.CreatedAt = DateTime.UtcNow;

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
                    return _stateHistory.TryGetValue(matchId, out var history)
                        ? history
                        : Enumerable.Empty<MatchStateSnapshot>();
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

                #region Create
                /// <summary>
                /// Creates a state snapshot from a match based on its current status
                /// </summary>
                public MatchStateSnapshot CreateSnapshotFromMatch(Match match, Guid userId, string userName)
                {
                    var currentSnapshot = Accessors.GetCurrentSnapshot(match);
                    var snapshot = new MatchStateSnapshot
                    {
                        MatchId = match.Id,
                        TriggeredByUserId = userId,
                        TriggeredByUserName = userName,
                        StartedAt = match.StartedAt,
                        CompletedAt = match.CompletedAt,
                        WinnerId = match.WinnerId,
                        CurrentGameNumber = currentSnapshot.CurrentGameNumber,
                        CurrentMapId = match
                            .Games?.FirstOrDefault(g => g.GameNumber == currentSnapshot.CurrentGameNumber)
                            ?.MapId,

                        // Map ban properties
                        AvailableMaps = match.AvailableMaps,
                        Team1MapBans = match.Team1MapBans,
                        Team2MapBans = match.Team2MapBans,
                        Team1BansSubmitted = match.Team1MapBansConfirmedAt.HasValue,
                        Team2BansSubmitted = match.Team2MapBansConfirmedAt.HasValue,
                        Team1BansConfirmed = currentSnapshot.Team1BansConfirmed,
                        Team2BansConfirmed = currentSnapshot.Team2BansConfirmed,
                        FinalMapPool = currentSnapshot.FinalMapPool,

                        // Copy any additional state from existing snapshot
                        CancelledAt = currentSnapshot.CancelledAt,
                        ForfeitedAt = currentSnapshot.ForfeitedAt,
                        CancelledByUserId = currentSnapshot.CancelledByUserId,
                        ForfeitedByUserId = currentSnapshot.ForfeitedByUserId,
                        ForfeitedTeamId = currentSnapshot.ForfeitedTeamId,
                        CancellationReason = currentSnapshot.CancellationReason,
                        ForfeitReason = currentSnapshot.ForfeitReason,
                    };

                    return snapshot;
                }
                #endregion

                // ------------------------ State Machine Logic --------------------------------
                public bool CanTransition(Match match, MatchStatus to)
                {
                    var from = MatchCore.Accessors.GetCurrentStatus(match);
                    return _validTransitions.ContainsKey(from) && _validTransitions[from].Contains(to);
                }

                public List<MatchStatus> GetValidTransitions(Match match)
                {
                    var from = MatchCore.Accessors.GetCurrentStatus(match);
                    return _validTransitions.ContainsKey(from)
                        ? new List<MatchStatus>(_validTransitions[from])
                        : new List<MatchStatus>();
                }

                public bool TryTransition(
                    Match match,
                    MatchStatus toState,
                    Guid userId,
                    string userName,
                    string? reason = null
                )
                {
                    if (!CanTransition(match, toState))
                        return false;

                    var currentSnapshot = Accessors.GetCurrentSnapshot(match);
                    var newSnapshot = MatchCore.Factory.CreateMatchStateSnapshotFromOther(currentSnapshot);

                    switch (toState)
                    {
                        case MatchStatus.InProgress:
                            newSnapshot.StartedAt = DateTime.UtcNow;
                            break;
                        case MatchStatus.Completed:
                            newSnapshot.CompletedAt = DateTime.UtcNow;
                            break;
                        case MatchStatus.Cancelled:
                            newSnapshot.CancelledAt = DateTime.UtcNow;
                            newSnapshot.CancelledByUserId = userId;
                            newSnapshot.CancellationReason = reason;
                            break;
                        case MatchStatus.Forfeited:
                            newSnapshot.ForfeitedAt = DateTime.UtcNow;
                            newSnapshot.ForfeitedByUserId = userId;
                            newSnapshot.ForfeitReason = reason;
                            break;
                    }

                    newSnapshot.TriggeredByUserId = userId;
                    newSnapshot.TriggeredByUserName = userName;
                    match.StateHistory.Add(newSnapshot);

                    return true;
                }
            }
        }
    }
}
