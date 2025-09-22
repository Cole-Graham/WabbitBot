namespace WabbitBot.SourceGenerators.Attributes;

/// <summary>
/// Marks a class for cross-boundary duplication from Core to DiscBot.
/// Classes marked with this attribute will have duplicate definitions generated
/// in the DiscBot project for deserialization purposes.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
public class GenerateCoreToDiscBotAttribute : Attribute
{
}

/// <summary>
/// Marks a class for cross-boundary duplication from DiscBot to Core.
/// Classes marked with this attribute will have duplicate definitions generated
/// in the Core project for deserialization purposes.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
public class GenerateDiscBotToCoreAttribute : Attribute
{
}
