namespace WabbitBot.Common.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter)]
public class DescriptionAttribute : Attribute
{
    public string Text { get; }

    public DescriptionAttribute(string text)
    {
        Text = text;
    }
}

