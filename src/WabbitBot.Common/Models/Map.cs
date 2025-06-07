namespace WabbitBot.Common.Models;

public class Map
{
    public required string Name { get; init; }
    public required string Id { get; init; }
    public string? Size { get; init; }
    public string? Thumbnail { get; init; }
    public bool IsInRandomPool { get; init; }
    public bool IsInTournamentPool { get; init; }
}