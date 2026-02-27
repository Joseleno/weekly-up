namespace WeeklyUp.WebApp.Models;

public sealed record ReportSummaryResponse(
    Guid Id,
    string WeekLabel,
    string Status,
    decimal? Revenue,
    int? SalesCount,
    DateTimeOffset CreatedAt);
