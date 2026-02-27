namespace WeeklyUp.WebApp.Models;

public sealed record AuthTokenResponse(
    string Token,
    DateTimeOffset ExpiresAt);
