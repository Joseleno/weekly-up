using WeeklyUp.Domain.Common;
using WeeklyUp.Domain.Enums;

namespace WeeklyUp.Domain.Events;

public sealed record IntegrationDisconnectedEvent(Guid UserId, IntegrationProvider Provider) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
