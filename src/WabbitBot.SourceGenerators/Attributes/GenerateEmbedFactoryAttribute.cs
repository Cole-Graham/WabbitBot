namespace WabbitBot.SourceGenerators.Attributes;

/// <summary>
/// Marks a class for embed factory generation. Classes marked with this attribute
/// will have factory methods generated to create instances of the embed.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class GenerateEmbedFactoryAttribute : Attribute
{
}
