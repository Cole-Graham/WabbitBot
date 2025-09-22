using System.Runtime.CompilerServices;

namespace WabbitBot.SourceGenerators.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class WabbitCommandAttribute : Attribute
{
    public string Name { get; }
    public string? Group { get; set; }

    public WabbitCommandAttribute(string name)
    {
        Name = name;
    }
}
