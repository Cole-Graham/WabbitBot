namespace WabbitBot.Common.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class WabbitCommandAttribute : Attribute
{
    public string Name { get; }
    public string? Group { get; init; }

    public WabbitCommandAttribute(string name)
    {
        Name = name;
    }
}