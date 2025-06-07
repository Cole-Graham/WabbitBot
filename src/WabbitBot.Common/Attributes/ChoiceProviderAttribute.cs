namespace WabbitBot.Common.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public class ChoiceProviderAttribute : Attribute
{
    public Type ProviderType { get; }

    public ChoiceProviderAttribute(Type providerType)
    {
        ProviderType = providerType;
    }
}

public interface IChoiceProvider
{
    IEnumerable<CommandChoice> GetChoices();
}

public record CommandChoice(string Name, string Value);

