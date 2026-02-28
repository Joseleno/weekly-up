namespace WeeklyUp.WebApp.Models;

public sealed record IntegrationResponse(
    Guid Id,
    string Provider,
    string Status,
    string? AccountId,
    DateTimeOffset ConnectedAt);
