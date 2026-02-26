using WeeklyUp.Domain.Common;
using WeeklyUp.Domain.ValueObjects;

namespace WeeklyUp.Domain.Events;

public sealed record ReportGeneratedEvent(Guid ReportId, Guid UserId, DateRange WeekRange) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
