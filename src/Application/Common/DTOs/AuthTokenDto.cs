namespace WeeklyUp.Application.Common.DTOs;

public sealed record AuthTokenDto(
    string Token,
    string RefreshToken,
    DateTimeOffset ExpiresAt);
