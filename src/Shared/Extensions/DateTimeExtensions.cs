namespace WeeklyUp.Shared.Extensions;

public static class DateTimeExtensions
{
    public static DateOnly ToDateOnly(this DateTime dateTime) =>
        DateOnly.FromDateTime(dateTime);

    public static DateTime ToStartOfDay(this DateOnly date) =>
        date.ToDateTime(TimeOnly.MinValue);

    public static DateTime ToEndOfDay(this DateOnly date) =>
        date.ToDateTime(TimeOnly.MaxValue);

    public static DateOnly GetPreviousMonday(this DateOnly date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek - 1 + 7) % 7;
        return date.AddDays(-daysSinceMonday - 7);
    }
}
