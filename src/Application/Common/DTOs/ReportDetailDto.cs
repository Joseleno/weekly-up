namespace WeeklyUp.Application.Common.DTOs;

public sealed record ReportDetailDto(
    Guid Id,
    string WeekLabel,
    string Status,
    MetricsDto? Metrics,
    InsightsDto? Insights,
    DemographicsDto? Demographics,
    DateTimeOffset CreatedAt);
