namespace WeeklyUp.WebApp.Models;

public sealed record InsightsResponse(
    string Highlight,
    string Alert,
    string Tip,
    DateTimeOffset GeneratedAt);
