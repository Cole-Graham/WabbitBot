namespace WabbitBot.Generator.Shared;

/// <summary>
/// Constants for attribute names used in source generation.
/// </summary>
public static class AttributeNames
{
    public const string EntityMetadata = "WabbitBot.Common.Attributes.EntityMetadataAttribute";
    public const string EventBoundary = "WabbitBot.SourceGenerators.Attributes.EventBoundaryAttribute";
    public const string EventType = "WabbitBot.SourceGenerators.Attributes.EventTypeAttribute";
    public const string GenerateCrossBoundary = "WabbitBot.SourceGenerators.Attributes.GenerateCrossBoundaryAttribute";
    public const string WabbitCommand = "WabbitBot.SourceGenerators.Attributes.WabbitCommandAttribute";
    public const string GenerateEmbedFactory = "WabbitBot.SourceGenerators.Attributes.GenerateEmbedFactoryAttribute";
}
