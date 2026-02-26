namespace WeeklyUp.Application.Common.DTOs;

public sealed record MetricsDto(
    decimal Revenue,
    int SalesCount,
    decimal AverageTicket,
    int NewCustomers,
    int Visits,
    int PageViews,
    string? TopPage,
    string? TopSource);
