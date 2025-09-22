using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Scrimmages.Data.Interface;
using WabbitBot.Core.Scrimmages;

namespace WabbitBot.Core.Scrimmages.Data
{
    public class ScrimmageRepository : Repository<Scrimmage>, IScrimmageRepository
    {
        private static readonly string[] ColumnNames = new[]
        {
            "Id", "Team1Id", "Team2Id", "Team1RosterIds", "Team2RosterIds", "EvenTeamFormat",
            "StartedAt", "CompletedAt", "WinnerId", "Status", "Team1Rating", "Team2Rating",
            "Team1RatingChange", "Team2RatingChange", "Team1Confidence", "Team2Confidence",
            "ChallengeExpiresAt", "IsAccepted", "BestOf", "CreatedAt", "UpdatedAt"
        };

        private readonly ScrimmageStateMachine _stateMachine;

        public ScrimmageRepository(IDatabaseConnection connection, ScrimmageStateMachine? stateMachine = null)
            : base(connection, "Scrimmages", ColumnNames, "Id")
        {
            _stateMachine = stateMachine ?? new ScrimmageStateMachine();
        }

        public async Task<Scrimmage?> GetScrimmageAsync(string scrimmageId)
        {
            return await GetByIdAsync(scrimmageId);
        }

        public async Task<IEnumerable<Scrimmage>> GetScrimmagesByTeamAsync(string teamId)
        {
            var sql = $"SELECT * FROM {_tableName} WHERE Team1Id = @TeamId OR Team2Id = @TeamId ORDER BY CreatedAt DESC";
            return await QueryAsync(sql, new { TeamId = teamId });
        }

        public async Task<IEnumerable<Scrimmage>> GetScrimmagesByStatusAsync(ScrimmageStatus status)
        {
            var sql = $"SELECT * FROM {_tableName} WHERE Status = @Status ORDER BY CreatedAt DESC";
            return await QueryAsync(sql, new { Status = (int)status });
        }

        public async Task<IEnumerable<Scrimmage>> GetRecentScrimmagesAsync(int count)
        {
            var sql = $"SELECT * FROM {_tableName} ORDER BY CreatedAt DESC LIMIT @Count";
            return await QueryAsync(sql, new { Count = count });
        }

        public async Task<IEnumerable<Scrimmage>> GetScrimmagesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var sql = $"SELECT * FROM {_tableName} WHERE CreatedAt BETWEEN @StartDate AND @EndDate ORDER BY CreatedAt DESC";
            return await QueryAsync(sql, new { StartDate = startDate, EndDate = endDate });
        }

        public async Task<IEnumerable<Scrimmage>> GetActiveScrimmagesAsync()
        {
            var sql = $"SELECT * FROM {_tableName} WHERE Status = @Status ORDER BY CreatedAt DESC";
            return await QueryAsync(sql, new { Status = (int)ScrimmageStatus.InProgress });
        }

        public async Task<IEnumerable<Scrimmage>> GetCompletedScrimmagesAsync()
        {
            var sql = $"SELECT * FROM {_tableName} WHERE Status = @Status ORDER BY CreatedAt DESC";
            return await QueryAsync(sql, new { Status = (int)ScrimmageStatus.Completed });
        }

        public async Task<IEnumerable<Scrimmage>> GetScrimmagesByEvenTeamFormatAsync(EvenTeamFormat evenTeamFormat)
        {
            var sql = $"SELECT * FROM {_tableName} WHERE EvenTeamFormat = @EvenTeamFormat ORDER BY CreatedAt DESC";
            return await QueryAsync(sql, new { EvenTeamFormat = (int)evenTeamFormat });
        }

        public async Task<IEnumerable<Scrimmage>> GetScrimmagesByTeamIdAsync(string teamId)
        {
            var sql = $"SELECT * FROM {_tableName} WHERE Team1Id = @TeamId OR Team2Id = @TeamId ORDER BY CreatedAt DESC";
            return await QueryAsync(sql, new { TeamId = teamId });
        }

        protected override Scrimmage MapEntity(IDataReader reader)
        {
            return new Scrimmage
            {
                Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
                Team1Id = reader.GetString(reader.GetOrdinal("Team1Id")),
                Team2Id = reader.GetString(reader.GetOrdinal("Team2Id")),
                Team1RosterIds = JsonUtil.Deserialize<List<string>>(reader.GetString(reader.GetOrdinal("Team1RosterIds"))) ?? new List<string>(),
                Team2RosterIds = JsonUtil.Deserialize<List<string>>(reader.GetString(reader.GetOrdinal("Team2RosterIds"))) ?? new List<string>(),
                EvenTeamFormat = (EvenTeamFormat)reader.GetInt32(reader.GetOrdinal("EvenTeamFormat")),
                StartedAt = reader.IsDBNull(reader.GetOrdinal("StartedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("StartedAt")),
                CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                WinnerId = reader.IsDBNull(reader.GetOrdinal("WinnerId")) ? null : reader.GetString(reader.GetOrdinal("WinnerId")),
                Status = (ScrimmageStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                Team1Rating = reader.GetDouble(reader.GetOrdinal("Team1Rating")),
                Team2Rating = reader.GetDouble(reader.GetOrdinal("Team2Rating")),
                Team1RatingChange = reader.GetDouble(reader.GetOrdinal("Team1RatingChange")),
                Team2RatingChange = reader.GetDouble(reader.GetOrdinal("Team2RatingChange")),
                Team1Confidence = reader.GetDouble(reader.GetOrdinal("Team1Confidence")),
                Team2Confidence = reader.GetDouble(reader.GetOrdinal("Team2Confidence")),
                ChallengeExpiresAt = reader.IsDBNull(reader.GetOrdinal("ChallengeExpiresAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ChallengeExpiresAt")),
                IsAccepted = reader.GetInt32(reader.GetOrdinal("IsAccepted")) != 0,
                BestOf = reader.GetInt32(reader.GetOrdinal("BestOf")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }

        protected override object BuildParameters(Scrimmage entity)
        {
            return new
            {
                Id = entity.Id.ToString(),
                entity.Team1Id,
                entity.Team2Id,
                Team1RosterIds = JsonUtil.Serialize(entity.Team1RosterIds),
                Team2RosterIds = JsonUtil.Serialize(entity.Team2RosterIds),
                EvenTeamFormat = (int)entity.EvenTeamFormat,
                StartedAt = entity.StartedAt,
                CompletedAt = entity.CompletedAt,
                entity.WinnerId,
                Status = (int)entity.Status,
                entity.Team1Rating,
                entity.Team2Rating,
                entity.Team1RatingChange,
                entity.Team2RatingChange,
                entity.Team1Confidence,
                entity.Team2Confidence,
                ChallengeExpiresAt = entity.ChallengeExpiresAt,
                IsAccepted = entity.IsAccepted ? 1 : 0,
                entity.BestOf,
                entity.CreatedAt,
                entity.UpdatedAt
            };
        }

        #region Hybrid Data Access Methods

        /// <summary>
        /// Gets a scrimmage using hybrid approach: state machine first, then database
        /// </summary>
        public async Task<Scrimmage?> GetScrimmageHybridAsync(string scrimmageId)
        {
            // 1. Try state machine first (fast path for active scrimmages)
            if (Guid.TryParse(scrimmageId, out var guid))
            {
                var scrimmage = _stateMachine.GetCurrentScrimmage(guid);
                if (scrimmage != null) return scrimmage;
            }

            // 2. Fallback to database (persistent data)
            var repositoryScrimmage = await GetScrimmageAsync(scrimmageId);
            if (repositoryScrimmage != null)
            {
                // Add back to state machine if it's active
                if (repositoryScrimmage.Status == ScrimmageStatus.InProgress)
                {
                    _stateMachine.AddScrimmage(repositoryScrimmage);
                }
            }

            return repositoryScrimmage;
        }

        /// <summary>
        /// Creates a scrimmage and adds it to the state machine
        /// </summary>
        public async Task<Scrimmage> CreateScrimmageHybridAsync(Scrimmage scrimmage)
        {
            // Persist to database
            await AddAsync(scrimmage);

            // Add to state machine for fast access
            _stateMachine.AddScrimmage(scrimmage);

            return scrimmage;
        }

        /// <summary>
        /// Updates a scrimmage and synchronizes with state machine
        /// </summary>
        public async Task<bool> UpdateScrimmageHybridAsync(Scrimmage scrimmage)
        {
            // Persist to database
            var result = await UpdateAsync(scrimmage);

            if (result)
            {
                // Update state machine based on status
                if (scrimmage.Status == ScrimmageStatus.InProgress)
                {
                    _stateMachine.AddScrimmage(scrimmage);
                }
                else
                {
                    // Remove from state machine if no longer active
                    _stateMachine.RemoveScrimmage(scrimmage.Id);
                }
            }

            return result;
        }

        /// <summary>
        /// Loads all active scrimmages from database into state machine
        /// </summary>
        public async Task LoadActiveScrimmagesAsync()
        {
            var activeScrimmages = await GetScrimmagesByStatusAsync(ScrimmageStatus.InProgress);

            foreach (var scrimmage in activeScrimmages)
            {
                _stateMachine.AddScrimmage(scrimmage);
            }
        }

        #endregion
    }
}
