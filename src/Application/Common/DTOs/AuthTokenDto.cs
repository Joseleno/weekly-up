namespace WeeklyUp.Application.Common.DTOs;

public sealed record AuthTokenDto(
    string Token,
    DateTimeOffset ExpiresAt);
