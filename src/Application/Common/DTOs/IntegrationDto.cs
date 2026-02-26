namespace WeeklyUp.Application.Common.DTOs;

public sealed record IntegrationDto(
    Guid Id,
    string Provider,
    string Status,
    string? AccountId,
    DateTimeOffset ConnectedAt);
