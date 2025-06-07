namespace WabbitBot.Common.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public class AutoCompleteProviderAttribute : Attribute
{
    public Type ProviderType { get; }

    public AutoCompleteProviderAttribute(Type providerType)
    {
        ProviderType = providerType;
    }
}

public interface IAutoCompleteProvider
{
    IEnumerable<CommandChoice> GetSuggestions(string userInput);
}