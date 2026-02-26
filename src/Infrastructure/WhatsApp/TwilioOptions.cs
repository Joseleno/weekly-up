namespace WeeklyUp.Infrastructure.WhatsApp;

public sealed class TwilioOptions
{
    public string AccountSid { get; init; } = string.Empty;
    public string AuthToken { get; init; } = string.Empty;
    public string FromNumber { get; init; } = string.Empty;
}
