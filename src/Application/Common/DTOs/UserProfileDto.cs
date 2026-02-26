namespace WeeklyUp.Application.Common.DTOs;

public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string Name,
    string BusinessName,
    string BusinessType,
    string Plan,
    bool IsEmailVerified,
    bool IsActive,
    DateTimeOffset CreatedAt);
