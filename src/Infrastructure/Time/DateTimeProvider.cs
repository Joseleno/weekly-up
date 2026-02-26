using WeeklyUp.Application.Common.Interfaces;

namespace WeeklyUp.Infrastructure.Time;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
