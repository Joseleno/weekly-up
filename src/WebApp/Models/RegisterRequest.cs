namespace WeeklyUp.WebApp.Models;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string Name,
    string BusinessName,
    string BusinessType);
