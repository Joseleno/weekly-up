namespace WeeklyUp.WebApp.Models;

public sealed record LoginRequest(
    string Email,
    string Password);
