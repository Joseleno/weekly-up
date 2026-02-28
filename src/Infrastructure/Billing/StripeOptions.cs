namespace WeeklyUp.Infrastructure.Billing;

public sealed class StripeOptions
{
    public string SecretKey { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;
}
