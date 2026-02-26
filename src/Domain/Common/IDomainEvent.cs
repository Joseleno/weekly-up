namespace WeeklyUp.Domain.Common;

public interface IDomainEvent
{
    public Guid EventId { get; }
    public DateTime OccurredAt { get; }
}
