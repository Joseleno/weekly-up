namespace WeeklyUp.Application.Common.DTOs;

public sealed record InsightsDto(
    string Highlight,
    string Alert,
    string Tip,
    DateTimeOffset GeneratedAt);
