namespace WabbitBot.Common.Attributes;

/// <summary>
/// Marks a class for embed styling utilities generation. Classes marked with this attribute
/// will have styling utilities generated to provide consistent styling across all embeds.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class GenerateEmbedStylingAttribute : Attribute
{
}