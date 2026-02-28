namespace WeeklyUp.WebApp.Models;

public sealed record MetricsResponse(
    decimal Revenue,
    int SalesCount,
    decimal AverageTicket,
    int NewCustomers,
    int Visits,
    int PageViews,
    string? TopPage,
    string? TopSource);
