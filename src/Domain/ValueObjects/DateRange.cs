using WeeklyUp.Domain.Common;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Domain.ValueObjects;

public sealed class DateRange : ValueObject
{
    private const int MaxDaysDifference = 6;

    public DateOnly Start { get; }
    public DateOnly End { get; }

    private DateRange(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    public static Result<DateRange> Create(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            return AppError.Validation("DateRange.EndBeforeStart", "Data de fim deve ser maior ou igual a data de inicio.");
        }

        if (end.DayNumber - start.DayNumber > MaxDaysDifference)
        {
            return AppError.Validation("DateRange.TooLong", $"Intervalo nao pode ser maior que {MaxDaysDifference} dias.");
        }

        return new DateRange(start, end);
    }

    public static DateRange PreviousWeek()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        int daysSinceMonday = ((int)today.DayOfWeek - 1 + 7) % 7;
        DateOnly thisMonday = today.AddDays(-daysSinceMonday);
        DateOnly prevMonday = thisMonday.AddDays(-7);
        DateOnly prevSunday = prevMonday.AddDays(6);
        return new DateRange(prevMonday, prevSunday);
    }

    public static DateRange CurrentWeek()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        int daysSinceMonday = ((int)today.DayOfWeek - 1 + 7) % 7;
        DateOnly monday = today.AddDays(-daysSinceMonday);
        DateOnly sunday = monday.AddDays(6);
        return new DateRange(monday, sunday);
    }

    public bool Contains(DateOnly date) => date >= Start && date <= End;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }

    public override string ToString() => $"{Start:yyyy-MM-dd} a {End:yyyy-MM-dd}";
}
