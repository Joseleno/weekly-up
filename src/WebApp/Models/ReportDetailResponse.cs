namespace WeeklyUp.WebApp.Models;

public sealed record ReportDetailResponse(
    Guid Id,
    string WeekLabel,
    string Status,
    MetricsResponse? Metrics,
    InsightsResponse? Insights,
    DemographicsResponse? Demographics,
    DateTimeOffset CreatedAt);
