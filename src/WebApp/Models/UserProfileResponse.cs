namespace WeeklyUp.WebApp.Models;

public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string Name,
    string BusinessName,
    string BusinessType,
    string Plan,
    bool IsEmailVerified,
    bool IsActive,
    DateTimeOffset CreatedAt);
