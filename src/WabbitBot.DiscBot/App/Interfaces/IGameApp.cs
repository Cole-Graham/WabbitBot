namespace WabbitBot.DiscBot.App.Interfaces
{
    /// <summary>
    /// Interface for game flow operations.
    /// Handles per-game containers, deck submission, map selection, and game lifecycle.
    /// </summary>
    public interface IGameApp : IDiscBotApp
    {
        /// <summary>
        /// Starts the deck submission DM flow for players before a game.
        /// </summary>
        Task StartDeckSubmissionDMsAsync(
            Guid matchId,
            int gameNumber,
            ulong player1DiscordUserId,
            ulong player2DiscordUserId
        );

        /// <summary>
        /// Handles player deck code submission (provisional).
        /// </summary>
        Task OnDeckSubmittedAsync(Guid matchId, int gameNumber, ulong playerId, string deckCode);

        /// <summary>
        /// Handles player deck code confirmation (final).
        /// </summary>
        Task OnDeckConfirmedAsync(Guid matchId, int gameNumber, ulong playerId, string deckCode);

        /// <summary>
        /// Starts the next game in a match series.
        /// Chooses a map and requests per-game container creation.
        /// </summary>
        Task StartNextGameAsync(Guid matchId, int gameNumber, string[] remainingMaps);

        /// <summary>
        /// Handles game replay submission and winner determination.
        /// </summary>
        Task OnReplaySubmittedAsync(Guid matchId, int gameNumber, Guid[] replayFileIds);

        /// <summary>
        /// Continues to next game or finishes the match based on series state.
        /// </summary>
        Task ContinueOrFinishAsync(Guid matchId, bool hasWinner, Guid? winnerTeamId);
    }
}
