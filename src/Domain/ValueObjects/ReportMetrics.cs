using WeeklyUp.Domain.Common;

namespace WeeklyUp.Domain.ValueObjects;

public sealed class ReportMetrics : ValueObject
{
    public Money Revenue { get; private set; } = Money.Zero;
    public int SalesCount { get; private set; }
    public Money AverageTicket { get; private set; } = Money.Zero;
    public int NewCustomers { get; private set; }
    public int TotalVisits { get; private set; }
    public int UniqueVisitors { get; private set; }
    public int PageViews { get; private set; }
    public string? TopPage { get; private set; }
    public string? TopTrafficSource { get; private set; }
    public Money? PreviousRevenue { get; private set; }
    public int? PreviousVisits { get; private set; }

    private ReportMetrics() { }

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
