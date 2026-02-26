namespace WeeklyUp.Infrastructure.WhatsApp;

public sealed class EvolutionApiOptions
{
    public string BaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string Instance { get; init; } = string.Empty;
}
