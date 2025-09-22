namespace WabbitBot.DiscBot.DSharpPlus.Embeds;

/// <summary>
/// Interface for embed fields
/// </summary>
public interface IEmbedField
{
    string Name { get; }
    string Value { get; }
    bool IsInline { get; }
}

/// <summary>
/// Interface for embed authors
/// </summary>
public interface IEmbedAuthor
{
    string Name { get; }
    string? IconUrl { get; }
    string? Url { get; }
}
