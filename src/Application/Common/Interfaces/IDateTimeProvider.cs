namespace WeeklyUp.Application.Common.Interfaces;

public interface IDateTimeProvider
{
    public DateTime UtcNow { get; }
    public DateOnly Today { get; }
}
