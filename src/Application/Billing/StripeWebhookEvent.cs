namespace WeeklyUp.Application.Billing;

public sealed record StripeWebhookEvent(
    string Type,
    string Id,
    string? UserId,
    string? PriceId);
