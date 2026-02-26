using WeeklyUp.Domain.Common;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Domain.Entities;

public sealed class ManualMetric : Entity
{
    public Guid UserId { get; private set; }
    public DateOnly WeekStart { get; private set; }
    public decimal? Revenue { get; private set; }
    public int? SalesCount { get; private set; }
    public int? NewCustomers { get; private set; }
    public int? Visits { get; private set; }

    private ManualMetric() { }

    public static Result<ManualMetric> Create(
        Guid userId,
        DateOnly weekStart,
        decimal? revenue,
        int? salesCount,
        int? newCustomers,
        int? visits)
    {
        if (weekStart.DayOfWeek != DayOfWeek.Monday)
        {
            return AppError.Validation(
                "ManualMetric.WeekStartNotMonday",
                "WeekStart deve ser uma segunda-feira.");
        }

        Result<bool> validation = ValidateValues(revenue, salesCount, newCustomers, visits);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        return new ManualMetric
        {
            UserId = userId,
            WeekStart = weekStart,
            Revenue = revenue,
            SalesCount = salesCount,
            NewCustomers = newCustomers,
            Visits = visits,
        };
    }

    public Result<bool> Update(
        decimal? revenue,
        int? salesCount,
        int? newCustomers,
        int? visits)
    {
        Result<bool> validation = ValidateValues(revenue, salesCount, newCustomers, visits);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        Revenue = revenue;
        SalesCount = salesCount;
        NewCustomers = newCustomers;
        Visits = visits;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    private static Result<bool> ValidateValues(
        decimal? revenue,
        int? salesCount,
        int? newCustomers,
        int? visits)
    {
        if (revenue.HasValue && revenue.Value < 0)
        {
            return AppError.Validation("ManualMetric.NegativeRevenue", "Revenue nao pode ser negativo.");
        }

        if (salesCount.HasValue && salesCount.Value < 0)
        {
            return AppError.Validation("ManualMetric.NegativeSalesCount", "SalesCount nao pode ser negativo.");
        }

        if (newCustomers.HasValue && newCustomers.Value < 0)
        {
            return AppError.Validation("ManualMetric.NegativeNewCustomers", "NewCustomers nao pode ser negativo.");
        }

        if (visits.HasValue && visits.Value < 0)
        {
            return AppError.Validation("ManualMetric.NegativeVisits", "Visits nao pode ser negativo.");
        }

        return true;
    }
}
