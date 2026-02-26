namespace WeeklyUp.Application.Common.DTOs;

public sealed record ReportSummaryDto(
    Guid Id,
    string WeekLabel,
    string Status,
    decimal? Revenue,
    int? SalesCount,
    DateTimeOffset CreatedAt);
