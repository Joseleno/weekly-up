namespace WeeklyUp.WebApp.Models;

public sealed record UpdateProfileRequest(
    string BusinessName,
    string BusinessType);
