using System;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Services
{
    /// <summary>
    /// Database service coordination for CoreService
    /// Provides unified DatabaseService instances for all entities
    /// </summary>
    public partial class CoreService
    {
        // DatabaseService instances for each entity type
        // Set to nullable because compiler can't guarantee InitalizeDatabaseServices()
        // will be called before these are used. Which also means we changed CoreService.Database.cs
        // to use null-forgiving operators (yikes!). We should probably find a better solution
        // for this.
        private DatabaseService<Player>? _playerDb;
        private DatabaseService<Team>? _teamDb;
        private DatabaseService<Game>? _gameDb;
        private DatabaseService<User>? _userDb;
        private DatabaseService<Map>? _mapDb;

        // Vertical slice database services (to be added later)
        // private readonly DatabaseService<Match> _matchDb;
        // private readonly DatabaseService<Scrimmage> _scrimmageDb;
        // private readonly DatabaseService<Tournament> _tournamentDb;
        // private readonly DatabaseService<Leaderboard> _leaderboardDb;
        // private readonly DatabaseService<Season> _seasonDb;

        /// <summary>
        /// Initializes database services for common entities
        /// </summary>
        private void InitializeDatabaseServices()
        {
            // Initialize DatabaseService instances for common entities
            _playerDb = new DatabaseService<Player>(
                tableName: "players",
                columns: new[] { "Id", "Name", "DisplayName", "Rating", "TeamIds", "CreatedAt", "UpdatedAt" },
                archiveTableName: "players_archive",
                archiveColumns: new[] { "Id", "Name", "DisplayName", "Rating", "TeamIds", "CreatedAt", "UpdatedAt", "ArchivedAt" }
            );

            _teamDb = new DatabaseService<Team>(
                tableName: "teams",
                columns: new[] { "Id", "Name", "TeamCaptainId", "TeamMember.PlayerId", "TeamMember.JoinedAt", "Stats", "CreatedAt", "UpdatedAt" },
                archiveTableName: "teams_archive",
                archiveColumns: new[] { "Id", "Name", "TeamCaptainId", "TeamMember.PlayerId", "TeamMember.JoinedAt", "Stats", "CreatedAt", "UpdatedAt", "ArchivedAt" }
            );

            _gameDb = new DatabaseService<Game>(
                tableName: "games",
                columns: new[] { "Id", "MatchId", "MapId", "Team1PlayerIds", "Team2PlayerIds", "WinnerId", "GameFormat", "GameStateSnapshot", "CancelledByUserId", "ForfeitedByUserId", "ForfeitedTeamId", "CreatedAt", "UpdatedAt" },
                archiveTableName: "games_archive",
                archiveColumns: new[] { "Id", "MatchId", "MapId", "Team1PlayerIds", "Team2PlayerIds", "WinnerId", "GameFormat", "GameStateSnapshot", "CancelledByUserId", "ForfeitedByUserId", "ForfeitedTeamId", "CreatedAt", "UpdatedAt", "ArchivedAt" }
            );

            _userDb = new DatabaseService<User>(
                tableName: "users",
                columns: new[] { "Id", "DiscordUserId", "PlayerId", "Username", "Discriminator", "AvatarUrl", "CreatedAt", "UpdatedAt" },
                archiveTableName: "users_archive",
                archiveColumns: new[] { "Id", "DiscordUserId", "PlayerId", "Username", "Discriminator", "AvatarUrl", "CreatedAt", "UpdatedAt", "ArchivedAt" }
            );

            _mapDb = new DatabaseService<Map>(
                tableName: "maps",
                columns: new[] { "Id", "Name", "Description", "IsActive", "Size", "IsInRandomPool", "IsInTournamentPool", "ThumbnailFilename", "CreatedAt", "UpdatedAt" },
                archiveTableName: "maps_archive",
                archiveColumns: new[] { "Id", "Name", "Description", "IsActive", "Size", "IsInRandomPool", "IsInTournamentPool", "ThumbnailFilename", "CreatedAt", "UpdatedAt", "ArchivedAt" }
            );
        }

        #region Database Service Properties

        /// <summary>
        /// Gets the player database service
        /// </summary>
        public DatabaseService<Player> Players => _playerDb!;

        /// <summary>
        /// Gets the team database service
        /// </summary>
        public DatabaseService<Team> Teams => _teamDb!;

        /// <summary>
        /// Gets the game database service
        /// </summary>
        public DatabaseService<Game> Games => _gameDb!;

        /// <summary>
        /// Gets the user database service
        /// </summary>
        public DatabaseService<User> Users => _userDb!;

        /// <summary>
        /// Gets the map database service
        /// </summary>
        public DatabaseService<Map> Maps => _mapDb!;

        #endregion
    }
}
