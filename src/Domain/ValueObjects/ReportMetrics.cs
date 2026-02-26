using WeeklyUp.Domain.Common;

namespace WeeklyUp.Domain.ValueObjects;

public sealed class ReportMetrics : ValueObject
{
    public Money Revenue { get; }
    public int SalesCount { get; }
    public Money AverageTicket { get; }
    public int NewCustomers { get; }
    public int TotalVisits { get; }
    public int UniqueVisitors { get; }
    public int PageViews { get; }
    public string? TopPage { get; }
    public string? TopTrafficSource { get; }
    public Money? PreviousRevenue { get; }
    public int? PreviousVisits { get; }

    public ReportMetrics(
        Money revenue,
        int salesCount,
        Money averageTicket,
        int newCustomers,
        int totalVisits,
        int uniqueVisitors,
        int pageViews,
        string? topPage,
        string? topTrafficSource,
        Money? previousRevenue,
        int? previousVisits)
    {
        ArgumentNullException.ThrowIfNull(revenue);
        ArgumentNullException.ThrowIfNull(averageTicket);
        ArgumentOutOfRangeException.ThrowIfNegative(salesCount);
        ArgumentOutOfRangeException.ThrowIfNegative(newCustomers);
        ArgumentOutOfRangeException.ThrowIfNegative(totalVisits);
        ArgumentOutOfRangeException.ThrowIfNegative(uniqueVisitors);
        ArgumentOutOfRangeException.ThrowIfNegative(pageViews);

        Revenue = revenue;
        SalesCount = salesCount;
        AverageTicket = averageTicket;
        NewCustomers = newCustomers;
        TotalVisits = totalVisits;
        UniqueVisitors = uniqueVisitors;
        PageViews = pageViews;
        TopPage = topPage;
        TopTrafficSource = topTrafficSource;
        PreviousRevenue = previousRevenue;
        PreviousVisits = previousVisits;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Revenue;
        yield return SalesCount;
        yield return AverageTicket;
        yield return NewCustomers;
        yield return TotalVisits;
        yield return UniqueVisitors;
        yield return PageViews;
        yield return TopPage;
        yield return TopTrafficSource;
        yield return PreviousRevenue;
        yield return PreviousVisits;
    }
}
