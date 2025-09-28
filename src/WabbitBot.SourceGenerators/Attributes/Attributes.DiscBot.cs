using System;


namespace WabbitBot.SourceGenerators.Attributes
{
    #region Embed
    /// <summary>
    /// Marks a class for embed factory generation. Classes marked with this attribute
    /// will have factory methods generated to create instances of the embed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateEmbedFactoryAttribute() : Attribute
    {
    }

    /// <summary>
    /// Marks a class for embed styling utilities generation. Classes marked with this attribute
    /// will have styling utilities generated to provide consistent styling across all embeds.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateEmbedStylingAttribute() : Attribute
    {
    }
    #endregion
}
