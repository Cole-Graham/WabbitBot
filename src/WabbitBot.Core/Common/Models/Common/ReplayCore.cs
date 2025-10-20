using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Models.Common
{
    public partial class ReplayCore
    {
        /// <summary>
        /// Interprets the victory code from a replay file.
        /// </summary>
        /// <param name="victoryCode">The victory code string from the replay ("0"-"6")</param>
        /// <param name="playerAlliance">The player's alliance ("0" or "1")</param>
        /// <returns>"Victory", "Defeat", or "Draw"</returns>
        public static string InterpretVictoryCode(string? victoryCode, string playerAlliance)
        {
            if (string.IsNullOrEmpty(victoryCode))
            {
                return "Unknown";
            }

            // Victory codes from Kraku's parser:
            // "4", "5", "6" = Victory
            // "0", "1", "2" = Defeat
            // Other = Draw
            //
            // However, this is from the perspective of Alliance 0
            // We need to flip the result if the player is Alliance 1
            var isAlliance0 = string.Equals(playerAlliance, "0", StringComparison.Ordinal);

            var result = victoryCode switch
            {
                "4" or "5" or "6" => isAlliance0 ? "Victory" : "Defeat",
                "0" or "1" or "2" => isAlliance0 ? "Defeat" : "Victory",
                _ => "Draw",
            };

            return result;
        }

        public static class Parser
        {
            /// <summary>
            /// Parses a .rpl3 replay file and creates Replay and ReplayPlayer entities.
            /// </summary>
            /// <param name="fileData">The raw byte array of the .rpl3 file</param>
            /// <param name="gameId">The game ID this replay is associated with</param>
            /// <param name="matchId">The match ID this replay is associated with</param>
            /// <param name="filePath">The secure file path where the replay is stored</param>
            /// <param name="originalFilename">The original filename of the replay</param>
            /// <returns>A Result containing the parsed Replay entity with ReplayPlayer entities</returns>
            public static Result<Replay> ParseReplayFile(
                byte[] fileData,
                Guid gameId,
                Guid matchId,
                string? filePath = null,
                string? originalFilename = null
            )
            {
                try
                {
                    // Decode entire file to search for both game metadata and result data
                    var fullText = Encoding.UTF8.GetString(fileData);

                    // Find the starting point of the game metadata JSON
                    var jsonStart = fullText.IndexOf("{\"game\":", StringComparison.Ordinal);
                    if (jsonStart == -1)
                    {
                        return Result<Replay>.Failure("No valid JSON data found in replay file");
                    }

                    // Extract the game metadata JSON (stop at "star" marker)
                    var jsonData = fullText.Substring(jsonStart);
                    var starIndex = jsonData.IndexOf("star", StringComparison.Ordinal);
                    if (starIndex != -1)
                    {
                        jsonData = jsonData.Substring(0, starIndex);
                    }

                    // Clean the JSON string to ensure it is valid
                    jsonData = CleanJsonString(jsonData);

                    // Parse the game metadata JSON
                    JsonDocument jsonDocument;
                    try
                    {
                        jsonDocument = JsonDocument.Parse(jsonData);
                    }
                    catch (JsonException ex)
                    {
                        return Result<Replay>.Failure($"Error parsing JSON from replay file: {ex.Message}");
                    }

                    // Look for result data (separate JSON object with Duration and Victory)
                    var resultMatch = Regex.Match(fullText, @"\{""Duration"":""(\d+)"",""Victory"":""(\d+)""\}");
                    string? victoryCode = null;
                    int? durationSeconds = null;

                    if (resultMatch.Success)
                    {
                        durationSeconds = int.Parse(resultMatch.Groups[1].Value);
                        victoryCode = resultMatch.Groups[2].Value;
                    }

                    // Extract game-level and player data
                    var parseResult = ExtractReplayData(
                        jsonDocument,
                        gameId,
                        matchId,
                        filePath,
                        originalFilename,
                        fileData.Length,
                        victoryCode,
                        durationSeconds
                    );

                    return parseResult;
                }
                catch (Exception ex)
                {
                    return Result<Replay>.Failure($"Error parsing replay file: {ex.Message}");
                }
            }

            /// <summary>
            /// Cleans the JSON string to ensure it is valid.
            /// </summary>
            private static string CleanJsonString(string jsonString)
            {
                // Remove excessive backslashes (normalize backslashes)
                jsonString = Regex.Replace(jsonString, @"\\+", @"\");

                // Replace escaped quotes with normal quotes
                jsonString = jsonString.Replace("\\\"", "\"", StringComparison.Ordinal);

                // Remove any leading or trailing whitespace
                jsonString = jsonString.Trim();

                return jsonString;
            }

            /// <summary>
            /// Extracts replay data from the parsed JSON document.
            /// </summary>
            private static Result<Replay> ExtractReplayData(
                JsonDocument jsonDocument,
                Guid gameId,
                Guid matchId,
                string? filePath,
                string? originalFilename,
                long fileSizeBytes,
                string? victoryCode,
                int? durationSeconds
            )
            {
                try
                {
                    var root = jsonDocument.RootElement;

                    if (!root.TryGetProperty("game", out var gameElement))
                    {
                        return Result<Replay>.Failure("Missing 'game' property in replay JSON");
                    }

                    // Create the Replay entity
                    var replay = new Replay
                    {
                        Id = Guid.NewGuid(),
                        GameId = gameId,
                        MatchId = matchId,
                        OriginalFilename = originalFilename,
                        FilePath = filePath,
                        FileSizeBytes = fileSizeBytes,
                        CreatedAt = DateTime.UtcNow,

                        // Game-level data
                        GameMode = GetJsonStringValue(gameElement, "GameMode") ?? string.Empty,
                        AllowObservers = GetJsonStringValue(gameElement, "AllowObservers"),
                        ObserverDelay = GetJsonStringValue(gameElement, "ObserverDelay"),
                        Seed = GetJsonStringValue(gameElement, "Seed"),
                        Private = GetJsonStringValue(gameElement, "Private"),
                        ServerName = GetJsonStringValue(gameElement, "ServerName"),
                        Version = GetJsonStringValue(gameElement, "Version"),
                        UniqueSessionId = GetJsonStringValue(gameElement, "UniqueSessionId"),
                        ModList = GetJsonStringValue(gameElement, "ModList"),
                        ModTagList = GetJsonStringValue(gameElement, "ModTagList"),
                        EnvironmentSettings = GetJsonStringValue(gameElement, "EnvironmentSettings"),
                        GameType = GetJsonStringValue(gameElement, "GameType"),
                        Map = GetJsonStringValue(gameElement, "Map") ?? string.Empty,
                        InitMoney = GetJsonStringValue(gameElement, "InitMoney"),
                        TimeLimit = GetJsonStringValue(gameElement, "TimeLimit"),
                        ScoreLimit = GetJsonStringValue(gameElement, "ScoreLimit"),
                        CombatRule = GetJsonStringValue(gameElement, "CombatRule"),
                        IncomeRate = GetJsonStringValue(gameElement, "IncomeRate"),
                        Upkeep = GetJsonStringValue(gameElement, "Upkeep"),

                        // Match result data
                        VictoryCode = victoryCode,
                        DurationSeconds = durationSeconds,
                    };

                    // Extract player data
                    var players = new List<ReplayPlayer>();
                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Name.StartsWith("player_", StringComparison.Ordinal))
                        {
                            var playerElement = property.Value;
                            var replayPlayer = new ReplayPlayer
                            {
                                Id = Guid.NewGuid(),
                                ReplayId = replay.Id,
                                CreatedAt = DateTime.UtcNow,

                                // Player data
                                PlayerUserId = GetJsonStringValue(playerElement, "PlayerUserId") ?? string.Empty,
                                PlayerName = GetJsonStringValue(playerElement, "PlayerName") ?? string.Empty,
                                PlayerElo = GetJsonStringValue(playerElement, "PlayerElo"),
                                PlayerLevel = GetJsonStringValue(playerElement, "PlayerLevel"),
                                PlayerAlliance = GetJsonStringValue(playerElement, "PlayerAlliance") ?? string.Empty,
                                PlayerScoreLimit = GetJsonStringValue(playerElement, "PlayerScoreLimit"),
                                PlayerIncomeRate = GetJsonStringValue(playerElement, "PlayerIncomeRate"),
                                PlayerAvatar = GetJsonStringValue(playerElement, "PlayerAvatar"),
                                PlayerReady = GetJsonStringValue(playerElement, "PlayerReady"),
                                PlayerDeckContent = GetJsonStringValue(playerElement, "PlayerDeckContent"),
                                PlayerDeckName = GetJsonStringValue(playerElement, "PlayerDeckName"),
                            };

                            players.Add(replayPlayer);
                        }
                    }

                    replay.Players = players;

                    return Result<Replay>.CreateSuccess(replay);
                }
                catch (Exception ex)
                {
                    return Result<Replay>.Failure($"Error extracting replay data: {ex.Message}");
                }
            }

            /// <summary>
            /// Helper method to safely get a string value from a JSON element.
            /// Returns null if the property doesn't exist or is not a string.
            /// </summary>
            private static string? GetJsonStringValue(JsonElement element, string propertyName)
            {
                if (element.TryGetProperty(propertyName, out var property))
                {
                    if (property.ValueKind == JsonValueKind.String)
                    {
                        return property.GetString();
                    }
                    // Handle other value kinds by converting to string
                    return property.ToString();
                }
                return null;
            }
        }
    }
}
