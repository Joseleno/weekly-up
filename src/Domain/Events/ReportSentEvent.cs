using WeeklyUp.Domain.Common;

namespace WeeklyUp.Domain.Events;

public sealed record ReportSentEvent(Guid ReportId, Guid UserId, string Channel) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
