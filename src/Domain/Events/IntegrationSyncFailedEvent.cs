using WeeklyUp.Domain.Common;
using WeeklyUp.Domain.Enums;

namespace WeeklyUp.Domain.Events;

public sealed record IntegrationSyncFailedEvent(Guid IntegrationId, IntegrationProvider Provider, string Error) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
