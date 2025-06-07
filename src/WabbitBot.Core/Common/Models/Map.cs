namespace WabbitBot.Core.Common.Models;

public class Map
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Size { get; init; }
    public bool IsInRandomPool { get; init; }
    public bool IsInTournamentPool { get; init; }
    public string? Thumbnail { get; init; }
}
